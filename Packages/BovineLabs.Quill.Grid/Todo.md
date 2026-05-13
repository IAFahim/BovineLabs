This is a fantastic foundation. You have two extremely strong pillars:

1.  The Grid Algorithms: A highly optimized, Burst-compiled,
    unsafe-memory-backed collection of advanced pathfinding and grid algorithms.
2.  BovineLabs.Quill / Quill.Grid: A robust, zero-allocation, shader-driven DOTS
    rendering backend capable of instancing thousands of lines, text, and
    cuboids instantly.

How Far Are We?

You are about 80% of the way to a world-class visualizer. You have the engine
(Algorithms) and the monitor (Quill). What is currently missing is the
Transmission / Middleware layer.

Right now, your algorithms are designed to run to completion (TrySearch, TryRun,
TryBuild), while your visualizer expects frame-by-frame data in DynamicBuffers
(like GridCellVisual or GridPathVisual).

To perfectly visualize algorithms like Anya or CBS, you need to be able to pause
the algorithm mid-execution, extract its Open/Closed sets (Frontier/History),
pass that to Quill, and resume on the next frame.

The "Best Possible" Architecture

The ideal architecture for this is the Steppable ECS State Machine Pattern. We
must strictly separate the Mathematical State from the Visual State, connecting
them via an ECS System.

Here is the 4-Layer Architecture you should build toward:

Layer 1: The Core Algorithm (Unmanaged Steppers)

Algorithms should not just have a TrySearch(start, goal) method. They need an
initialization method and a step method. (You actually already did this
perfectly in FastMarchingApi.TryPropagateStep!)

- Init(): Sets up start/goal, clears heaps.
- Step(): Pops one node off the heap, expands neighbors, returns
  Status.Running or Status.Complete.

Layer 2: ECS Solver Components (The Data Holder)

We wrap the unsafe algorithm state (AnyaState, CbsState) inside an unmanaged
IComponentData. This ties the lifecycle of the C# unmanaged memory directly to
an Entity.

Layer 3: The Bridge Systems (The Missing Link)

These are ISystems that run every frame. They check the GridVisualizerMode.

- If Live: They run a while(Step() == Running) {} loop to finish the path
  instantly, then record the final path to Quill.
- If Step: They call Step() exactly once, then loop through the algorithm's
  internal arrays (Closed, Heap, Intervals) and copy them into Quill's
  DynamicBuffers using GridVisualRecorder.

Layer 4: Quill Rendering (Already Done!)

Systems like GridCellRenderSystem and GridIntervalRenderSystem read the buffers
and dispatch to DrawSystem.Singleton. You don't need to change this layer at
all.

Implementation Plan & Concrete Example

Here is exactly how you implement this "Best Possible" architecture. Let's use
Fast Marching as the golden standard, as it already supports stepping.

Step 1: Create the Component Data wrapper

using Unity.Entities;
using BovineLabs.Grid.FastMarching;

public struct FastMarchingSolver : IComponentData
{
public FastMarchingState State;
public bool IsInitialized;
public bool IsComplete;
}

Step 2: Create the Bridge System

This system handles the stepping logic and bridges the raw data into Quill's
visual buffers.

using Unity.Burst;
using Unity.Entities;
using BovineLabs.Quill.Grid.Data;
using BovineLabs.Grid.FastMarching;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct FastMarchingVisualizerSystem : ISystem
{
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
foreach (var (solver, visualizer, global, cells) in SystemAPI.Query<
RefRW<FastMarchingSolver>,
RefRO<GridVisualizerData>,
RefRO<GridVisualizerGlobal>,
DynamicBuffer<GridCellVisual>>())
{
if (!global.ValueRO.Enabled) continue;

            ref var fm = ref solver.ValueRW;
            
            // 1. Advance the Algorithm State
            if (fm.IsInitialized && !fm.IsComplete)
            {
                if (global.ValueRO.Mode == GridVisualizerMode.Live)
                {
                    // Run to completion instantly
                    fm.IsComplete = FastMarchingApi.TryPropagateAll(ref fm.State, /* speed array */);
                }
                else if (global.ValueRO.Mode == GridVisualizerMode.Step)
                {
                    // Run exactly one expansion per frame
                    fm.IsComplete = !FastMarchingApi.TryPropagateStep(ref fm.State, /* speed array */);
                }
            }

            // 2. Record the Visuals into Quill
            cells.Clear();
            var gridLen = visualizer.ValueRO.GridWidth * visualizer.ValueRO.GridHeight;
            
            for (int i = 0; i < gridLen; i++)
            {
                byte cellState = fm.State.State[i];
                float tValue = fm.State.T[i];

                if (cellState == 1) // Frontier (In Heap)
                {
                    cells.Add(new GridCellVisual(i, tValue, GridCellVisual.LayerFrontier));
                }
                else if (cellState == 2) // Closed / Settled
                {
                    // Draw heatmap for settled nodes based on arrival time T
                    cells.Add(new GridCellVisual(i, tValue, GridCellVisual.LayerHeatmap));
                }
            }
        }
    }
}

Architectural Next Steps for Specific Algorithms

To fully migrate the rest of your algorithms to this architecture, here is your
refactoring checklist:

1. Anya Refactoring

- Current: TrySearch is a monolithic while loop.
- Goal: Break it into TryInitSearch(start, goal) and TryStepSearch(out bool
  foundGoal).
- Quill Hookup: In the Step system, iterate through AnyaState.Pool and read
  the intervals. Push them into Quill's DynamicBuffer<GridIntervalVisual>.
  Quill's GridIntervalRenderSystem is perfectly designed to render this
  instantly.

2. CBS (Conflict-Based Search) Refactoring

- Current: A nested A* loop inside a Constraint Tree loop.
- Goal: Extract the High-Level (Constraint Tree) and Low-Level (A*) into
  steppers.
- Quill Hookup: Add a CbsVisualizerSystem. Every time the high-level tree
  finds a conflict, populate Quill's DynamicBuffer<GridConflictVisual>. Every
  time an agent updates its path, populate DynamicBuffer<GridAgentPathVisual>.

3. WFC (Wave Function Collapse) Refactoring

- Current: TryRun is a massive while(!heap.IsEmpty) block containing
  TryPropagate.
- Goal: Extract into TryObserveStep() and TryPropagateStep().
- Quill Hookup: Write a visualizer that reads WfcState.Entropy and maps it to
  GridCellVisual.LayerHeatmap (Low entropy = Cold Color, High entropy = Hot
  Color) so you can literally watch the wave collapse spreading across the
  grid in real-time.

Summary

Your Quill rendering architecture is excellent. The separation of GridCellVisual
buffer -> GridCellRenderSystem -> Drawer is exactly how you build
high-performance DOTS debug tooling.

To reach "the best possible", you just need to flatten your algorithm while
loops into state machines (Init() / Step()) and write standard ECS Systems that
pump the algorithm state into the Quill dynamic buffers.
