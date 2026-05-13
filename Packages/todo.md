## Goal

Build `BovineLabs.Quill.Grid` as a **plug-and-play algorithm visualizer framework** for Unity ECS/DOTS, where each algorithm can expose its “essence” through reusable visual blocks: grid cells, heatmaps, frontier queues, intervals, paths, constraints, agents, labels, messages, arrows, text, timelines, and overlays.

The key idea: **algorithms should not directly draw**. Algorithms should emit compact debug/visual state, and Quill systems should render that state using reusable blocks.

---

## Why Quill fits this

Your uploaded Quill docs show the right foundation: Quill is already a debug drawing library for Entities, with a `Drawer` that can be requested from `DrawSystem.Singleton`, passed into jobs, and used for lines, points, cuboids, spheres, triangles, solids, and text. It also supports `CreateDrawer<T>(category)`, so each visualizer can be registered under toolbar categories/systems for filtering. 

The existing debug toolbar already has the concept of:

* global enabled state
* category filters
* system filters
* saved category/system selection
* App UI dropdowns for enabling/disabling groups

That matches your “lego block” goal very well. 

---

# Proposed architecture

## 1. Split the system into 4 layers

### Layer A — Algorithm Layer

This is your real algorithm code.

Examples from your uploaded grid algorithms:

* `AnyaApi` with `AnyaState`, `Interval`, `AnyaNode`, heap, node pool, root, parent, and path extraction. 
* `BeliefApi` with unary costs, messages, next messages, belief values, scratch memory, and decoded labels. 
* `CbsApi` with agents, constraints, CBS nodes, flat paths, path lengths, conflicts, and time-expanded A*. 
* `FastMarchingApi` with `T`, cell state, heap, and propagation steps. 
* `EdtApi` with distance transform buffers like `Temp`, `V`, and `Z`. 

This layer should stay Burst-friendly and allocation-controlled.

### Layer B — Capture Layer

This layer records visual events or snapshots from the algorithm.

Examples:

```csharp
AnyaDebugRecorder.RecordExpandedInterval(...)
AnyaDebugRecorder.RecordSuccessor(...)
CbsDebugRecorder.RecordConflict(...)
BeliefDebugRecorder.RecordMessage(...)
FastMarchingDebugRecorder.RecordAcceptedCell(...)
```

The algorithm never calls Quill directly. It only writes to `NativeList`, `NativeStream`, `UnsafeList`, or entity buffers.

### Layer C — Visual Model Layer

This converts algorithm-specific data into generic visual blocks.

Example conversion:

```text
AnyaNode        -> IntervalBlock + RootPointBlock + ParentLineBlock
Belief messages -> DirectionalArrowBlock + HeatmapBlock
CBS conflict    -> AgentPathBlock + ConstraintCellBlock + TimeMarkerBlock
Fast marching   -> HeatmapBlock + FrontierBlock + AcceptedCellBlock
EDT             -> DistanceHeatmapBlock + NearestObstacleVectorBlock
```

This layer is where your “lego” system lives.

### Layer D — Quill Render Layer

This is the only layer that uses `Drawer`.

Each render system does:

```csharp
var drawer = SystemAPI
    .GetSingleton<DrawSystem.Singleton>()
    .CreateDrawer<MyVisualizerSystem>("Grid Algorithms");

if (!drawer.IsEnabled)
{
    return;
}
```

Then it schedules jobs that render blocks using Quill primitives: lines, points, cuboids, triangles, solids, text, arrows, circles, and durations. Quill already supports the primitive vocabulary you need. 

---

# Core package design

## Package name

```text
BovineLabs.Quill.Grid
```

## Suggested folder structure

```text
BovineLabs.Quill.Grid/
  Runtime/
    Core/
      GridVisualizerSettings.cs
      GridVisualizerSingleton.cs
      GridVisualizationWorld.cs
      GridVisualFrame.cs
      GridVisualTimeline.cs

    Blocks/
      IGridVisualBlock.cs
      GridBlock.cs
      CellLayerBlock.cs
      HeatmapBlock.cs
      PathBlock.cs
      AgentPathBlock.cs
      FrontierBlock.cs
      IntervalBlock.cs
      ConstraintBlock.cs
      VectorFieldBlock.cs
      TextLabelBlock.cs
      TimelineBlock.cs

    Rendering/
      GridBlockRenderSystem.cs
      HeatmapRenderSystem.cs
      PathRenderSystem.cs
      IntervalRenderSystem.cs
      AgentRenderSystem.cs
      TextRenderSystem.cs

    Algorithms/
      Anya/
        AnyaVisualCapture.cs
        AnyaVisualAdapter.cs
        AnyaVisualSystem.cs

      Belief/
        BeliefVisualCapture.cs
        BeliefVisualAdapter.cs
        BeliefVisualSystem.cs

      CBS/
        CbsVisualCapture.cs
        CbsVisualAdapter.cs
        CbsVisualSystem.cs

      FastMarching/
        FastMarchingVisualCapture.cs
        FastMarchingVisualAdapter.cs
        FastMarchingVisualSystem.cs

      EDT/
        EdtVisualCapture.cs
        EdtVisualAdapter.cs
        EdtVisualSystem.cs

  Editor/
    GridVisualizerWindow.cs
    GridVisualizerAuthoring.cs
```

---

# The “lego block” model

Every algorithm visualizer should be built from reusable visual blocks.

## Basic blocks

| Block              | Purpose                                                          |
| ------------------ | ---------------------------------------------------------------- |
| `GridBlock`        | Draws grid bounds, cell outlines, axes, coordinates              |
| `CellLayerBlock`   | Draws occupied/free/unknown/cost cells                           |
| `HeatmapBlock`     | Draws scalar values such as distance, cost, belief, arrival time |
| `PathBlock`        | Draws one path as line/points                                    |
| `AgentPathBlock`   | Draws multi-agent paths over time                                |
| `FrontierBlock`    | Draws heap/open-set/frontier cells                               |
| `ClosedSetBlock`   | Draws processed/settled/visited cells                            |
| `IntervalBlock`    | Draws Anya intervals as horizontal spans                         |
| `RootBlock`        | Draws Anya root points and root-to-interval visibility cones     |
| `ConstraintBlock`  | Draws CBS constraints by agent/cell/time                         |
| `ConflictBlock`    | Draws CBS conflicts between agents                               |
| `MessageBlock`     | Draws belief propagation messages as directional arrows          |
| `VectorFieldBlock` | Draws gradient, flow, steering, or direction fields              |
| `LabelBlock`       | Draws labels, states, ids, costs, scores                         |
| `TimelineBlock`    | Selects algorithm step/frame/time                                |

The visualizer for each algorithm is just a composition of these.

Example:

```text
Anya Visualizer =
  GridBlock
  ObstacleLayerBlock
  IntervalBlock
  RootBlock
  VisibilityConeBlock
  FrontierBlock
  PathBlock
  LabelBlock
```

```text
CBS Visualizer =
  GridBlock
  ObstacleLayerBlock
  AgentPathBlock
  ConstraintBlock
  ConflictBlock
  TimelineBlock
  LabelBlock
```

```text
Belief Propagation Visualizer =
  GridBlock
  HeatmapBlock
  MessageBlock
  LabelBlock
  IterationTimelineBlock
```

---

# Visualizer contract

Create a small contract that every algorithm visualizer follows.

```csharp
public interface IGridAlgorithmVisualizer
{
    FixedString64Bytes Name { get; }
    FixedString32Bytes Category { get; }

    void CaptureFrame(ref SystemState state);
    void BuildBlocks(ref GridVisualFrame frame);
    void Draw(ref SystemState state, Drawer drawer);
}
```

In practice, because Unity Burst/ECS does not like managed polymorphism in hot paths, use this concept structurally rather than literally:

```text
Algorithm State
   ↓
Capture System
   ↓
Native debug buffers
   ↓
Adapter System
   ↓
Generic visual block buffers
   ↓
Quill Render Systems
```

---

# ECS data model

## Global singleton

```csharp
public struct GridVisualizerSingleton : IComponentData
{
    public bool Enabled;
    public int CurrentFrame;
    public int MaxFrames;
    public float CellSize;
    public float3 Origin;
    public GridVisualizerMode Mode;
}
```

## Per-algorithm config

```csharp
public struct GridAlgorithmVisualConfig : IComponentData
{
    public FixedString64Bytes AlgorithmName;
    public FixedString32Bytes Category;

    public bool DrawGrid;
    public bool DrawObstacles;
    public bool DrawFrontier;
    public bool DrawClosed;
    public bool DrawPath;
    public bool DrawLabels;
    public bool DrawHeatmap;
    public bool DrawTimeline;
}
```

## Generic visual block buffers

```csharp
public struct GridCellVisual : IBufferElementData
{
    public int Cell;
    public float Value;
    public byte Layer;
}

public struct GridLineVisual : IBufferElementData
{
    public float3 From;
    public float3 To;
    public float4 Color;
    public byte Style;
}

public struct GridTextVisual : IBufferElementData
{
    public float3 Position;
    public FixedString64Bytes Text;
    public float4 Color;
}

public struct GridIntervalVisual : IBufferElementData
{
    public int Y;
    public float XL;
    public float XR;
    public float2 Root;
    public float Cost;
}
```

This allows Quill render systems to be algorithm-agnostic.

---

# Algorithm-specific visual plans

## Anya pathfinding visualizer

Your Anya implementation has the perfect internal concepts for visualization: `Interval`, `Root`, `G`, `Parent`, heap, pool, successors, line of sight, and final path. 

Show:

* blocked cells
* start and goal
* current root
* interval span
* projected interval on next row
* successor intervals
* heap/open intervals
* parent chain
* final path
* failed line-of-sight checks

Best visual blocks:

```text
GridBlock
ObstacleLayerBlock
IntervalBlock
RootBlock
ProjectionBlock
VisibilityConeBlock
FrontierBlock
PathBlock
TextLabelBlock
```

Essence:

```text
“Anya searches through visible row intervals from root points, not individual cells.”
```

---

## Belief propagation visualizer

Your Belief state exposes unary cost, directional messages, next messages, belief values, scratch, and decoded labels. 

Show:

* unary cost heatmap per label
* message arrows between neighboring cells
* belief heatmap
* decoded label map
* per-iteration changes
* convergence/delta heatmap

Best visual blocks:

```text
HeatmapBlock
MessageArrowBlock
LabelBlock
DirectionBlock
IterationTimelineBlock
```

Essence:

```text
“Each cell repeatedly receives neighbor messages, updates its belief, and chooses the lowest-cost label.”
```

---

## CBS multi-agent pathfinding visualizer

CBS has agents, constraints, nodes, conflict detection, flat paths, path lengths, child expansion, and A* under constraints. 

Show:

* all agent paths
* time scrubber
* conflict cell at time `t`
* constraint added to each child branch
* CBS tree node cost
* selected node’s paths
* blocked cells
* re-planned agent path

Best visual blocks:

```text
AgentPathBlock
ConstraintBlock
ConflictBlock
TimelineBlock
SearchTreeBlock
PathBlock
TextLabelBlock
```

Essence:

```text
“CBS resolves multi-agent conflicts by branching on constraints and re-planning only the affected agent.”
```

---

## Fast marching visualizer

Your Fast Marching state has `T`, `State`, and a heap-driven propagation step. 

Show:

* source cells
* accepted/frontier/far states
* arrival time heatmap
* expanding wavefront
* heap frontier
* speed field
* gradient arrows

Best visual blocks:

```text
HeatmapBlock
FrontierBlock
WavefrontBlock
StateLayerBlock
VectorFieldBlock
```

Essence:

```text
“Fast marching grows an arrival-time wavefront from source cells through a speed field.”
```

---

## EDT visualizer

Your EDT state exposes distance buffers and the 1D transform helper arrays `V` and `Z`. 

Show:

* obstacle cells
* squared distance heatmap
* nearest obstacle vectors
* row/column pass overlay
* parabola envelope explanation view
* final distance field

Best visual blocks:

```text
DistanceHeatmapBlock
ObstacleLayerBlock
VectorFieldBlock
PassTimelineBlock
TextLabelBlock
```

Essence:

```text
“EDT converts obstacle occupancy into a distance field using separable 1D transforms.”
```

---

# UI plan

Use Quill’s existing toolbar pattern first.

The uploaded Quill toolbar already supports enabled state, category list, and system list, which means `BovineLabs.Quill.Grid` should register each visualizer as a category/system rather than building everything from scratch. 

Suggested categories:

```text
Grid/Anya
Grid/CBS
Grid/Belief
Grid/FastMarching
Grid/EDT
Grid/FlowFields
Grid/Steering
Grid/InfluenceMaps
Grid/SpatialQueries
```

Add a dedicated Grid Visualizer panel later with:

```text
Algorithm
Scenario
Frame
Playback speed
Draw grid
Draw obstacles
Draw heatmap
Draw frontier
Draw closed set
Draw path
Draw labels
Draw costs
Draw parent links
Draw constraints
Draw messages
```

---

# Recording modes

Use three modes.

## 1. Live mode

Draw current state only.

Best for normal gameplay debugging.

```text
Low memory
Always current
No timeline
```

## 2. Step mode

Record one frame per algorithm step.

Best for understanding algorithms.

```text
Higher memory
Frame-by-frame playback
Great for debugging pathfinding/search
```

## 3. Snapshot mode

Record only important milestones.

Best for AAA-scale worlds.

```text
Compact
Good for production debugging
Records: start, major expansions, conflicts, final path, failure reason
```

---

# Rendering rules for AAA ECS

Use these constraints from day one:

1. **No managed allocations in algorithm hot paths.**
2. **No direct Quill calls inside algorithm APIs.**
3. **Capture only when visualizer is enabled.**
4. **Use compile flags:** `UNITY_EDITOR || BL_DEBUG`.
5. **Use `NativeList`, `NativeStream`, `UnsafeList`, or dynamic buffers.**
6. **Limit max recorded frames.**
7. **Cull by camera/frustum for large grids.**
8. **Use level-of-detail for big maps.**
9. **Do not draw text for thousands of cells by default.**
10. **Separate simulation data from visualization data.**

Your existing Quill physics debug system already follows the right pattern: it checks whether debug drawing is enabled, gets a drawer, culls where appropriate, and schedules drawing jobs. 

---

# Build order

## Phase 1 — Foundation

Create:

```text
BovineLabs.Quill.Grid
GridVisualizerSingleton
GridVisualFrame
GridVisualBlock buffers
Grid coordinate conversion helpers
Basic Quill render systems
```

Deliverable:

```text
Draw grid, blocked cells, paths, labels.
```

## Phase 2 — First real visualizer: Anya

Build Anya first because it has clear visual concepts: root, interval, projection, heap, parent, path.

Deliverable:

```text
Visualize Anya search step-by-step.
```

## Phase 3 — Generic block library

Add reusable blocks:

```text
Heatmap
Frontier
Closed set
Parent links
Vector field
Timeline
Labels
Cell states
```

Deliverable:

```text
Any grid algorithm can reuse the same visual vocabulary.
```

## Phase 4 — CBS visualizer

Add:

```text
Multi-agent paths
Time scrubber
Conflicts
Constraints
CBS search tree summary
```

Deliverable:

```text
Debug agent conflicts visually.
```

## Phase 5 — Belief / Fast Marching / EDT

Add scalar-field and message-field visualizers:

```text
Belief heatmaps
Message arrows
Fast marching wavefronts
Distance fields
Gradient fields
```

Deliverable:

```text
All scalar/grid-field algorithms share the same heatmap/vector system.
```

## Phase 6 — Tooling polish

Add:

```text
Scenario picker
Frame recording
Playback
Pause/step
Export visual frame
Preset styles
Per-algorithm toggles
Failure reason labels
```

Deliverable:

```text
A real AAA debugging tool, not just temporary debug lines.
```

---

# Recommended mental model

Treat each algorithm visualizer like this:

```text
Algorithm emits facts.
Adapter translates facts into blocks.
Blocks render through Quill.
Toolbar controls visibility.
Timeline controls time.
```

Example:

```text
Anya:
  Fact: expanded interval
  Block: IntervalBlock
  Quill: Line + translucent quad + text label

CBS:
  Fact: conflict at agent A/B cell/time
  Block: ConflictBlock
  Quill: highlighted cell + agent lines + label

Belief:
  Fact: message from cell A to cell B
  Block: MessageArrowBlock
  Quill: arrow + magnitude heat/color/width

Fast Marching:
  Fact: accepted cell with arrival time
  Block: HeatmapBlock
  Quill: cell quad + frontier outline
```

---

# The design principle

`BovineLabs.Quill.Grid` should not be “one visualizer per algorithm.”

It should be:

```text
A visual language for grid algorithms.
```

Then every algorithm becomes a composition of blocks:

```text
Pathfinding = grid + obstacles + frontier + parents + path
Multi-agent = grid + agents + paths + conflicts + constraints + time
Inference = grid + heatmaps + messages + labels + iterations
Fields = grid + scalar field + vectors + contours + wavefront
```

That gives you the lego system you want: reusable, composable, Quill-powered, ECS-safe, Burst-friendly, and suitable for a large AAA ECS project.
