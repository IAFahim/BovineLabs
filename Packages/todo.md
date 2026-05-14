# TODO — ReSharper Warning Fixes

Each item found by `jb inspectcode`. Fix one, verify with `jb inspectcode <csproj> -o=/tmp/out.xml`.
Skip `UnusedMember.Global`, `UnusedType.Global` — shared lib types used by other packages.
Skip `MemberCanBePrivate.Global` unless confirmed not used by tests.
Skip `InconsistentNaming`, `MergeIntoPattern`, `UseIndexFromEndExpression`, `SwapViaDeconstruction`, `ForLoopCanBeConvertedToForeach` — style only, not bugs.

Run first to get fresh data:
```bash
cd /home/i/Documents/BovineLabs
for csproj in com.bovinelabs.grid.*.csproj; do
  name=$(basename "$csproj" .csproj)
  [[ "$name" == *.Player || "$name" == *.tests* ]] && continue
  jb inspectcode "$csproj" -o="/tmp/inspect-${name}.xml" --format=Xml 2>/dev/null
done
```

# TODO.md — Validation-Ready Fix Plan for `com.bovinelabs.grid.*`

Purpose: make this code safe enough for high-value validation.  
Rule for the implementer: **do not skip acceptance checks**. A task is not done because the code compiles. A task is done only when the listed tests pass and the failure case is proven impossible.

---

## Priority key

- **P0 / STOP-SHIP**: must fix before any validation run.
- **P1 / CORRECTNESS**: can silently return wrong results.
- **P2 / HARDENING**: prevents future regressions and unsafe edge cases.
- **P3 / CLEANUP**: quality improvements after correctness is proven.

---

## Global rules for every task

1. Build with Burst enabled and disabled.
2. Run tests with Unity safety checks enabled.
3. Run tests with leak detection enabled.
4. Add at least one negative test for every new validation guard.
5. Never ignore a `Try*` return value.
6. Never call `GetUnsafePtr()` or `GetUnsafeReadOnlyPtr()` before validating `IsCreated` and `Length`.
7. Never return success after partially building an invalid graph, path, heap, state, or output buffer.
8. After every `Dispose(ref state)`, set the state to `default` unless there is a specific reason not to.
9. For every public API that writes to an output list/array, clear or initialize the output before use.

---

# Phase 0 — Make the test suite trustworthy

## TASK P0-001 — Fix compile blocker in Domino

**Files**

- `com.bovinelabs.grid.domino/Runtime/DominoApi.cs`
- `com.bovinelabs.grid.domino/Tests/DominoTests.cs`

**Problem**

`TryFlipAt` compares and assigns the pointer `s.MatchingDir` directly instead of indexing into it. Examples to search for:

```csharp
s.MatchingDir == 1
s.MatchingDir = 2
s.MatchingDir = 0
```

This is either a compile failure or a severe pointer misuse.

**Steps**

1. Open `DominoApi.cs`.
2. Search for every use of `s.MatchingDir` inside `TryFlipAt`.
3. Replace every direct comparison/assignment with indexed access:
    - `s.MatchingDir[n0]`
    - `s.MatchingDir[n1]`
    - or another correct cell index based on the logic.
4. Do not guess direction values. Define named constants:

```csharp
private const byte DirNone = 0;
private const byte DirRight = 1;
private const byte DirDown = 2;
private const byte DirLeft = 3;
private const byte DirUp = 4;
```

5. Update all matching-direction writes to use those constants.
6. Add a test that flips one domino and verifies both cells have opposite directions.

**Acceptance checks**

- Project compiles.
- Domino tests pass.
- New test verifies both cells in a domino reference each other correctly.
- Double-dispose still passes.

---

## TASK P0-002 — Fix Belief test memory corruption

**Files**

- `com.bovinelabs.grid.belief/Tests/BeliefTests.cs`

**Problem**

`ClearMessages` creates a 3×3 grid with 2 labels. Valid message count is:

```text
3 * 3 * 4 * 2 = 72 floats
```

The test writes 400 floats:

```csharp
for (var i = 0; i < 400; i++) s.Messages[i] = 5f;
```

This is out-of-bounds memory corruption.

**Steps**

1. Replace hard-coded `400` with:

```csharp
var messageCount = s.Grid.Length * 4 * s.LabelCount;
```

2. Use `messageCount` in both loops.
3. Also fill and verify `MessagesNext`.
4. Add a regression test named:

```csharp
ClearMessages_DoesNotWritePastAllocatedBuffer
```

5. Run tests with Unity safety checks and leak detection.

**Acceptance checks**

- No test writes beyond `s.Grid.Length * 4 * s.LabelCount`.
- Safety checks do not report memory errors.
- `Messages` and `MessagesNext` are both zero after `ClearMessages`.

---

## TASK P0-003 — Fix Anya blocked-goal test name

**Files**

- `com.bovinelabs.grid.anya/Tests/AnyaTests.cs`

**Problem**

`Search_BlockedGoal` blocks index `0`, which is the start `(0,0)`, not the goal `(5,5)`.

**Steps**

1. Change:

```csharp
blocked[0] = 1;
```

to:

```csharp
blocked[s.Grid.ToIndex(5, 5)] = 1;
```

2. Add a separate test named:

```csharp
Search_BlockedStart_NoPath
```

3. In that new test, block `s.Grid.ToIndex(0, 0)`.

**Acceptance checks**

- One test proves blocked goal fails.
- One test proves blocked start fails.
- Test names match what they actually test.

---

## TASK P0-004 — Fix DStarLite tests that contradict blocked start/goal behavior

**Files**

- `com.bovinelabs.grid.dstarlite/Runtime/DStarLiteApi.cs`
- `com.bovinelabs.grid.dstarlite/Tests/DStarLiteTests.cs`

**Problem**

Some tests expect initialization or repair to succeed when start or goal is blocked. A pathfinder should not silently accept an invalid start/goal unless the API explicitly supports that contract.

**Steps**

1. Decide the contract:
    - Recommended contract: blocked start or blocked goal returns `false`.
2. Update `TryInitialize` to enforce that contract.
3. Update tests:
    - `Initialize_BlockedStart_ReturnsFalse`
    - `Initialize_BlockedGoal_ReturnsFalse`
    - `Repair_WhenGoalBecomesBlocked_ReturnsFalse`
4. If the design requires allowing blocked goal during repair, document that explicitly and add a separate status enum. Do not overload `true/false`.

**Acceptance checks**

- Tests match runtime behavior.
- No test expects success when no valid path can exist.
- Blocked start and blocked goal behavior is documented in XML comments.

---

## TASK P0-005 — Add one shared validation helper file

**Files**

- Add: `com.bovinelabs.grid/Runtime/GridValidation.cs`
- Use from every module.

**Problem**

Every module repeats unsafe pointer access and forgets different checks.

**Steps**

Create this helper:

```csharp
using Unity.Collections;

namespace BovineLabs.Grid
{
    public static class GridValidation
    {
        public static bool HasLength<T>(in NativeArray<T> array, int required)
            where T : struct
        {
            return array.IsCreated && array.Length >= required;
        }

        public static bool HasExactLength<T>(in NativeArray<T> array, int required)
            where T : struct
        {
            return array.IsCreated && array.Length == required;
        }

        public static bool InBounds(Grid2D grid, int index)
        {
            return index >= 0 && index < grid.Length;
        }

        public static bool IsUsable(Grid2D grid)
        {
            return grid.Width > 0 && grid.Height > 0 && grid.Length == grid.Width * grid.Height;
        }
    }
}
```

**Acceptance checks**

- All public APIs use this or equivalent checks.
- No public API gets an unsafe pointer from an unvalidated `NativeArray`.

---

# Phase 1 — Public API safety guards

## TASK P0-006 — Add input validation to every public pathfinding API

**Files**

- `AnyaApi.cs`
- `CbsApi.cs`
- `DStarLiteApi.cs`
- `FieldDStarApi.cs`
- `JpsApi.cs`
- `MeshAApi.cs`
- `SippApi.cs`
- `SubgoalApi.cs`
- `WavestarApi.cs`

**Problem**

Most public APIs use blocked/cost arrays without validating length.

**Steps**

For each public method:

1. Validate state is created.
2. Validate input arrays are created.
3. Validate input array length.
4. Validate start index/coordinate.
5. Validate goal index/coordinate.
6. Validate output list is created.
7. Clear output list before search.
8. Reject blocked start and blocked goal unless documented otherwise.

Use this pattern:

```csharp
if (!blocked.IsCreated || blocked.Length < s.Grid.Length) return false;
if (!path.IsCreated) return false;
if (!s.Grid.InBounds(start) || !s.Grid.InBounds(goal)) return false;
```

For index-based APIs:

```csharp
if (start < 0 || start >= s.Grid.Length) return false;
if (goal < 0 || goal >= s.Grid.Length) return false;
```

**Acceptance checks**

- Wrong-length blocked array returns `false`.
- Uncreated blocked array returns `false`.
- Out-of-bounds start returns `false`.
- Out-of-bounds goal returns `false`.
- Blocked start returns `false`.
- Blocked goal returns `false`.
- No crash in any case.

---

## TASK P0-007 — Add input validation to every grid-field API

**Files**

- `BeliefApi.cs`
- `ContinuumApi.cs`
- `EdtApi.cs`
- `FastMarchingApi.cs`
- `FastSweepingApi.cs`
- `ThinningApi.cs`
- `WatershedApi.cs`

**Problem**

These APIs read/write arrays such as unary, pairwise, speed, source, output, distance, labels, or flow without sufficient length checks.

**Steps**

For every public method that accepts `NativeArray<T>`:

1. Compute required length.
2. Check `IsCreated`.
3. Check `Length >= required`.
4. Return `false` for invalid input.
5. If method currently returns `void`, change to `bool` if invalid input can occur.

Examples:

Belief:

```csharp
if (!unary.IsCreated || unary.Length < s.Grid.Length * s.LabelCount) return false;
if (!pairwise.IsCreated || pairwise.Length < s.LabelCount * s.LabelCount) return false;
if (!labels.IsCreated || labels.Length < s.Grid.Length) return false;
```

Fast marching/sweeping:

```csharp
if (!speed.IsCreated || speed.Length < s.Grid.Length) return false;
if (!sources.IsCreated || sources.Length == 0) return false;
for each source: if source out of bounds return false;
```

EDT:

```csharp
if (!blocked.IsCreated || blocked.Length < expectedLength) return false;
if (!dist2.IsCreated || dist2.Length < expectedLength) return false;
```

**Acceptance checks**

- Every invalid array case returns `false`.
- No unsafe pointer is acquired before checks.
- Tests cover short arrays and uncreated arrays.

---

## TASK P0-008 — Add input validation to every graph/cut/matching API

**Files**

- `GraphCutApi.cs`
- `DynamicCutApi.cs`
- `KasteleynApi.cs`
- `DominoApi.cs`
- `WilsonApi.cs`
- `CftpApi.cs`
- `SandpileApi.cs`

**Problem**

Graph/matching modules index cells, regions, capacities, random updates, and labels without full bounds checks.

**Steps**

For each public method:

1. Validate state.
2. Validate arrays.
3. Validate every passed cell index.
4. Validate every passed coordinate.
5. Validate capacity before writing to `UnsafeList`.
6. Return `false` on invalid input.

**Acceptance checks**

- Invalid cell index never crashes.
- Region arrays shorter than grid length return `false`.
- Capacity exhaustion returns `false`.
- No partially constructed graph is accepted as valid.

---

# Phase 2 — Allocator and lifetime correctness

## TASK P0-009 — Fix Wilson allocator ownership

**Files**

- `com.bovinelabs.grid.wilson/Runtime/WilsonApi.cs`

**Problem**

`TryCreate` allocates with allocator `a`, but `WilsonState.Allocator` is not assigned. `Dispose` frees with `s.Allocator`, which is default.

**Steps**

1. In `TryCreate`, add:

```csharp
Allocator = a,
```

inside the returned state initializer.

2. Ensure every allocated pointer is freed using the same allocator.
3. At end of `Dispose(ref s)`, add:

```csharp
s = default;
```

4. Add a test:

```csharp
Dispose_UsesOriginalAllocator_DoesNotLeak
```

**Acceptance checks**

- Leak detection reports no leak.
- Double-dispose passes.
- `s.Allocator` is non-default after successful create.
- `s.Allocator` is default after dispose.

---

## TASK P0-010 — Audit allocator assignment in every `TryCreate`

**Files**

All `*State` and `*Api.TryCreate` files.

**Problem**

Any state that owns raw pointers must store the allocator used for allocation.

**Steps**

For every state with raw pointers:

1. Confirm it has an allocator field.
2. Confirm `TryCreate` assigns that field.
3. Confirm `Dispose` frees every raw pointer with that field.
4. Confirm `Dispose` sets every pointer to `null`.
5. Confirm `Dispose` sets the state to `default`.

Checklist:

- [ ] `AnyaState`
- [ ] `BeliefState`
- [ ] `CftpState`
- [ ] `ContinuumState`
- [ ] `CpdState`
- [ ] `DStarLiteState`
- [ ] `DynamicCutState`
- [ ] `EdtState`
- [ ] `FastMarchingState`
- [ ] `FastSweepingState`
- [ ] `GraphCutState`
- [ ] `HashlifeState`
- [ ] `JpsState`
- [ ] `KasteleynState`
- [ ] `MeshAState`
- [ ] `MorseState`
- [ ] `SandpileState`
- [ ] `SippState`
- [ ] `SubgoalState`
- [ ] `ThinningState`
- [ ] `WatershedState`
- [ ] `WavestarState`
- [ ] `WfcState`
- [ ] `WilsonState`

**Acceptance checks**

- Every raw pointer owner stores allocator.
- Every disposer is idempotent.
- Every disposer resets state to `default`.
- Leak detection passes after all tests.

---

## TASK P0-011 — Make `TryCreate` atomic

**Files**

All `*Api.TryCreate` methods.

**Problem**

If allocation number 3 fails after allocation number 1 and 2 succeeded, the method can leak memory.

**Steps**

For each `TryCreate`:

1. Initialize `result = default`.
2. Allocate into local variables.
3. After each allocation, check for failure if the allocator API can fail.
4. If any allocation/list/heap creation fails, free everything already allocated.
5. Only assign `result` after all allocations succeed.

Pattern:

```csharp
result = default;
var ptrA = default(byte*);
var ptrB = default(float*);
try
{
    ptrA = Allocate...
    ptrB = Allocate...
    result = new State { A = ptrA, B = ptrB, Allocator = a };
    return true;
}
catch
{
    if (ptrA != null) Free...
    if (ptrB != null) Free...
    result = default;
    return false;
}
```

If exceptions are not allowed in Burst path, use manual cleanup labels instead of `try/catch`.

**Acceptance checks**

- Simulated capacity/allocation failures do not leak.
- Failed create returns `false`.
- Failed create leaves `result` as `default`.

---

# Phase 3 — Stop silent partial results

## TASK P0-012 — Stop ignoring heap insert failures

**Files**

Search all runtime files for:

```csharp
TryInsertOrDecrease(
```

**Problem**

Some code calls `TryInsertOrDecrease` and ignores the return value. If the heap is full, the algorithm can silently drop work and still return success.

**Steps**

1. Search for every call to `TryInsertOrDecrease`.
2. If the result is ignored, change the caller to return `bool`.
3. Propagate failure up to the public API.
4. Add a capacity-exhaustion test with deliberately tiny heap capacity.
5. Expected result must be `false`, not a wrong path/result.

**Acceptance checks**

- No ignored `TryInsertOrDecrease` remains.
- Capacity exhaustion test returns `false`.
- No algorithm returns success after dropping a heap item.

---

## TASK P0-013 — Stop ignoring `UnsafeList.TryAdd` / capacity failures

**Files**

Search all runtime files for:

```csharp
TryAdd(
Add(
SetCapacity(
```

**Problem**

Many algorithms assume list capacity is enough. Some call `Add` without checking capacity in performance-critical graph/path code.

**Steps**

1. For fixed-capacity states, check capacity before `Add`.
2. For growable states, verify `SetCapacity` succeeded or can throw safely.
3. Replace unchecked writes with checked helper methods where possible.
4. If capacity is exceeded, return `false`.

Pattern:

```csharp
if (list.Length >= list.Capacity) return false;
list.Add(value);
```

**Acceptance checks**

- Small-capacity tests return `false`.
- No buffer overflow.
- No partially valid state is returned.

---

## TASK P0-014 — Make graph builders return `bool`

**Files**

- `GraphCutApi.cs`
- `DynamicCutApi.cs`
- Any other `Build*` method that can fail.

**Problem**

A graph builder that returns `void` can silently stop after capacity failure and leave a partial graph.

**Steps**

1. Change:

```csharp
public static void BuildBinaryEnergy(...)
```

to:

```csharp
public static bool TryBuildBinaryEnergy(...)
```

2. Return `false` on:
    - bad input length;
    - bad grid state;
    - capacity failure;
    - invalid capacity value;
    - partial add failure.
3. Update all callers.
4. Update tests.

**Acceptance checks**

- Partial graph construction cannot be used.
- DynamicCut refuses to repair if binary-energy rebuild fails.
- Tests prove tiny capacity fails cleanly.

---

# Phase 4 — Module-specific correctness fixes

## TASK P1-001 — Anya: clear output path in all paths

**Files**

- `AnyaApi.cs`
- `AnyaTests.cs`

**Problem**

`TryExtractPath` clears the path, but normal `TrySearch` directly calls `ExtractPath` without clearing first.

**Steps**

1. At the top of `TrySearch`, after validating `path.IsCreated`, call:

```csharp
path.Clear();
```

2. Do not call `ExtractPath` on a non-empty path.
3. Add a test:
    - First call finds one path.
    - Second call reuses same `NativeList<int2>`.
    - Verify second result does not contain old points.

**Acceptance checks**

- Reused path list contains only the latest path.

---

## TASK P1-002 — Anya: replace hash-only node identity

**Files**

- `AnyaApi.cs`
- `AnyaNode.cs`

**Problem**

`NodeLookup` maps `ulong hash -> int`. Hash collisions merge unrelated nodes.

**Steps**

1. Create a real key struct:

```csharp
public struct AnyaNodeKey : IEquatable<AnyaNodeKey>
{
    public long Lq;
    public long Rq;
    public int Y;
    public long RootXq;
    public long RootYq;
    public int Dy;
}
```

2. Quantize values with one shared method:

```csharp
private static long Quantize(double v)
{
    return (long)math.round(v * 1000000.0);
}
```

3. Use `NativeHashMap<AnyaNodeKey, int>` instead of `NativeHashMap<ulong, int>`.
4. Include `dy` if it changes expansion semantics.
5. Add collision regression tests:
    - Construct two different intervals that previously hash to the same `ulong`, or use a fake collision test hook.
    - Verify they remain separate nodes.

**Acceptance checks**

- No hash-only identity remains.
- Equal keys mean equal quantized interval/root/direction.
- Collision cannot merge unrelated nodes.

---

## TASK P1-003 — Anya: check every pool/heap/lookup write

**Files**

- `AnyaApi.cs`

**Problem**

`Pool.Add`, `NodeLookup.TryAdd`, and `Heap.TryInsertOrDecrease` can fail or overflow.

**Steps**

1. Change `PushNode` to return `bool`.
2. Before adding to `Pool`, check capacity.
3. Check `NodeLookup.TryAdd`.
4. Check `Heap.TryInsertOrDecrease`.
5. Propagate `false` to `TrySearch` / `TryStepSearch`.
6. Add a tiny `maxNodes` test.

**Acceptance checks**

- `maxNodes = 1` cannot corrupt memory.
- Failure returns `false`.

---

## TASK P1-004 — Anya: validate extracted path geometry

**Files**

- `AnyaApi.cs`
- `AnyaTests.cs`

**Problem**

Any-angle roots are rounded to `int2`. Rounding can create a path segment that crosses blocked cells.

**Steps**

1. Add a helper test method:

```csharp
AssertPathHasLineOfSightForEverySegment(grid, blocked, path)
```

2. For every adjacent pair in output path, call `AnyaApi.LineOfSight`.
3. Add randomized obstacle tests.
4. If failures occur, change path representation to preserve continuous roots or emit valid integer waypoints.

**Acceptance checks**

- Every Anya test verifies all path segments are traversable.
- 1,000 random small grids pass against a reference visibility check.

---

## TASK P1-005 — Belief: validate unary/pairwise lengths

**Files**

- `BeliefApi.cs`
- `BeliefTests.cs`

**Problem**

`SetUnary` and `TryIterate` do not fully validate input lengths.

**Steps**

1. Change `SetUnary` from `void` to `bool`.
2. Add:

```csharp
if (!unary.IsCreated || unary.Length < s.Grid.Length * s.LabelCount) return false;
```

3. In `TryIterate`, add:

```csharp
if (!pairwise.IsCreated || pairwise.Length < s.LabelCount * s.LabelCount) return false;
```

4. Add tests for short unary and short pairwise arrays.

**Acceptance checks**

- Short arrays return `false`.
- No unsafe copy occurs with invalid input.

---

## TASK P1-006 — Belief: verify message direction math

**Files**

- `BeliefApi.cs`
- `BeliefTests.cs`

**Problem**

Message update excludes `oppDir`, but the stored messages appear to be indexed by incoming direction. The update likely excludes the wrong message and can double-count feedback.

**Steps**

1. Write down the convention in comments:
    - `Messages[cell, dir, label]` means message from which neighbor to this cell?
    - Or message from this cell to which neighbor?
2. For update from cell `i` to neighbor `j`, exclude the incoming message from `j` to `i`.
3. Use explicit named directions:

```csharp
const int Right = 0;
const int Down = 1;
const int Left = 2;
const int Up = 3;
```

4. Create a brute-force MAP solver for tiny chains/grids.
5. Compare Belief decode to brute force on:
    - 2-cell chain;
    - 3-cell chain;
    - 2×2 grid;
    - asymmetric pairwise matrix.

**Acceptance checks**

- Direction convention is documented.
- Tiny-grid tests match brute force.
- Asymmetric pairwise test passes.

---

## TASK P1-007 — CBS: validate low-level A* inputs

**Files**

- `CbsApi.cs`
- `CbsTests.cs`

**Problem**

`TryAStar` does not validate start, goal, blocked length, or blocked start/goal before indexing.

**Steps**

1. At the start of `TryAStar`, add:
    - grid state validation;
    - blocked pointer cannot be null;
    - start/goal index in range;
    - start/goal not blocked.
2. Because pointer has no length, public wrappers must validate length before taking pointer.
3. Prefer changing private pointer methods to receive length too.

**Acceptance checks**

- Invalid start returns `false`.
- Invalid goal returns `false`.
- Blocked start returns `false`.
- Blocked goal returns `false`.

---

## TASK P1-008 — CBS: handle future goal constraints

**Files**

- `CbsApi.cs`
- `CbsTests.cs`

**Problem**

Low-level A* returns immediately when `u == goal`. CBS later treats agents as staying at their goal forever. If a future constraint forbids that goal cell, the planner can return an invalid path.

**Steps**

1. Add a function:

```csharp
private static bool GoalIsSafeForeverOrUntilHorizon(...)
```

2. When A* reaches goal, do not return unless all future constraints for this agent are satisfied.
3. If a future vertex constraint forbids the goal at time `T`, the agent must wait elsewhere or arrive later.
4. Add test:
    - Agent wants goal cell `G`.
    - Constraint forbids `G` at time `10`.
    - A* must return a path that is not at `G` at time `10`, or return `false`.

**Acceptance checks**

- CBS solution has no post-goal vertex conflict.
- Goal-wait conflict test checks all timesteps, not just path endpoints.

---

## TASK P1-009 — CBS: remove fixed unsafe time horizon

**Files**

- `CbsApi.cs`

**Problem**

A fixed horizon of `max(64, (width + height) * 2)` can reject valid solutions that require longer waiting.

**Steps**

1. Add `maxTime` as a solver parameter.
2. Default should be based on:
    - grid size;
    - number of agents;
    - constraints;
    - or user-provided validation limit.
3. Return a specific failure if horizon exhausted.
4. Add test where solution requires waiting longer than old horizon.

**Acceptance checks**

- Solver can solve a valid long-wait instance.
- Horizon exhaustion returns `false` and does not pretend no solution exists.

---

## TASK P1-010 — CBS: verify returned paths are conflict-free

**Files**

- `CbsTests.cs`

**Problem**

Current tests mainly check that paths exist. They do not prove no collisions.

**Steps**

1. Add helper:

```csharp
AssertNoVertexOrEdgeConflicts(flatPaths, lengths, agentCount)
```

2. For every pair of agents and every time from 0 to max path length:
    - check same-cell conflict;
    - check edge-swap conflict;
    - clamp after path end to goal cell.
3. Use this helper in every CBS success test.

**Acceptance checks**

- All CBS success tests call the conflict checker.
- A deliberately conflicting path fails the helper.

---

## TASK P1-011 — CPD: replace invalid range compression

**Files**

- `CpdApi.cs`
- `CpdTests.cs`

**Problem**

`TryBuild` groups targets by first move and stores numeric target ranges. Sorting by first move does not mean target IDs are contiguous. `TryGetFirstMove` can return the wrong move for unrelated targets inside the numeric range.

**Steps**

1. Remove numeric `TargetMin`/`TargetMax` compression unless target IDs are proven contiguous.
2. Use one of these safe designs:
    - store one entry per `(source, target)`;
    - or store sorted target IDs per first move;
    - or build intervals only after sorting by target ID and verifying same first move for every ID in the interval.
3. Add a brute-force BFS reference.
4. Test all source-target pairs on random grids.

**Acceptance checks**

- Every `TryGetFirstMove(source, target)` matches BFS first step.
- Random 5×5, 10×10, and obstacle grids pass.
- No false first move for non-contiguous target IDs.

---

## TASK P1-012 — SIPP: fix unsafe waiting

**Files**

- `SippApi.cs`
- `SippTests.cs`

**Problem**

When a neighbor interval starts later than arrival, the algorithm waits implicitly, but it may wait in the current cell after the current safe interval has ended.

**Steps**

1. Track current interval end.
2. For a move from current cell to neighbor:
    - `departTime = max(currentTime, neighborInterval.Start - travelTime)`
    - verify `departTime <= currentInterval.End`
    - verify `departTime + travelTime <= neighborInterval.End`
3. If waiting is required but current interval ends too soon, reject that successor.
4. Add a test:
    - Current cell safe interval ends at `5`.
    - Neighbor opens at `10`.
    - Algorithm must not wait in current cell until `9`.

**Acceptance checks**

- No path contains occupancy outside safe intervals.
- Add helper to validate every returned time-cell pair against intervals.

---

## TASK P1-013 — SIPP: validate start/goal and obstacle intervals

**Files**

- `SippApi.cs`
- `SippTests.cs`

**Steps**

1. Validate start and goal indices.
2. Validate blocked/static obstacle length if used.
3. Validate obstacle intervals:
    - cell in bounds;
    - start <= end;
    - non-negative times.
4. Reject invalid intervals.

**Acceptance checks**

- Invalid intervals return `false`.
- Start/goal out of bounds return `false`.

---

## TASK P1-014 — DynamicCut: fix dirty-node loop

**Files**

- `DynamicCutApi.cs`
- `DynamicCutTests.cs`

**Problem**

`EditUnary` or repair logic loops over grid length while indexing `DirtyNodes[i]`. `DirtyNodes.Length` can be smaller.

**Steps**

1. Find loop that looks like:

```csharp
for (var i = 0; i < s.Cut.Grid.Length; i++)
{
    var dirty = s.DirtyNodes[i];
}
```

2. Replace with:

```csharp
for (var i = 0; i < s.DirtyNodes.Length; i++)
{
    var dirty = s.DirtyNodes[i];
}
```

3. Add tests:
    - zero dirty nodes;
    - one dirty node;
    - many dirty nodes.

**Acceptance checks**

- Empty dirty list does not crash.
- Single dirty node repair works.
- Safety checks report no out-of-bounds access.

---

## TASK P1-015 — DynamicCut: preserve edited pairwise terms

**Files**

- `DynamicCutApi.cs`

**Problem**

`TryRepair` rebuilds pairwise terms using a hard-coded array filled with `1`. That discards previous pairwise edits.

**Steps**

1. Store pairwise capacities in `DynamicCutState`.
2. `EditPairwise` must update that stored pairwise data.
3. `TryRepair` must rebuild from stored pairwise data, not hard-coded `1`.
4. Add a test:
    - Set pairwise capacity to a non-1 value.
    - Call repair.
    - Verify the cut result changes as expected.

**Acceptance checks**

- Pairwise edits survive repair.
- No hard-coded pairwise reset remains.

---

## TASK P1-016 — DynamicCut: initialize unary arrays

**Files**

- `DynamicCutApi.cs`

**Problem**

Unary arrays may contain uninitialized memory after creation.

**Steps**

1. After allocating `UnarySource` and `UnarySink`, set both to zero:

```csharp
UnsafeUtility.MemClear(ptr, lengthInBytes);
```

2. Add test:
    - Create state.
    - Run repair without edits.
    - Output must be deterministic.

**Acceptance checks**

- No uninitialized unary data.
- Repeat test gives same result every run.

---

## TASK P1-017 — GraphCut: fail on partial edge construction

**Files**

- `GraphCutApi.cs`
- `GraphCutTests.cs`

**Problem**

If graph capacity runs out, graph construction can silently stop.

**Steps**

1. Convert every graph construction function to return `bool`.
2. Before adding every edge, check capacity.
3. If capacity is insufficient:
    - clear graph or mark invalid;
    - return `false`.
4. Add tiny-capacity test.

**Acceptance checks**

- Tiny capacity returns `false`.
- `TryMinCut` refuses to run on invalid graph.

---

## TASK P1-018 — Hashlife: replace hash-only interning with structural equality

**Files**

- `HashlifeApi.cs`
- `HashlifeTests.cs`

**Problem**

Hash collisions can merge different nodes.

**Steps**

1. Store node structure fields in the key:
    - level;
    - child IDs;
    - population/live bits if relevant.
2. Use a key type with equality, not only a hash value.
3. Add a collision test hook:
    - Force two different nodes to have the same hash.
    - Verify they produce different node IDs.

**Acceptance checks**

- Collision cannot merge different nodes.
- Random evolution tests match brute-force cellular automaton for small grids.

---

## TASK P1-019 — JPS: prevent diagonal corner cutting

**Files**

- `JpsApi.cs`
- `JpsTests.cs`

**Problem**

Diagonal moves can be invalid if they pass between two blocked orthogonal cells.

**Steps**

1. For move `(dx, dy)` where both are nonzero:
    - require `(x + dx, y)` passable;
    - require `(x, y + dy)` passable.
2. Add test:
    - 2×2 grid.
    - Start `(0,0)`, goal `(1,1)`.
    - Block `(1,0)` and `(0,1)`.
    - Search must return `false`.

**Acceptance checks**

- No diagonal corner cutting.
- Existing open-grid diagonal test still passes.

---

## TASK P1-020 — Kasteleyn: validate orientation mathematically

**Files**

- `KasteleynApi.cs`
- `KasteleynTests.cs`

**Problem**

Orientation appears ad hoc. It may not be a valid Kasteleyn orientation for arbitrary planar regions, holes, or boundary cases.

**Steps**

1. Document supported region type:
    - full rectangle only?
    - simply connected region?
    - holes allowed?
2. Implement a checker:
    - For every face, count clockwise-oriented edges.
    - Kasteleyn condition must hold.
3. Add tests:
    - 2×2 rectangle;
    - 3×3 rectangle;
    - region with a hole;
    - disconnected region.
4. If holes/disconnected regions are unsupported, return `false` with tests.

**Acceptance checks**

- Orientation checker passes on supported regions.
- Unsupported regions fail cleanly.

---

## TASK P1-021 — MeshA: actually respect `startTheta`

**Files**

- `MeshAApi.cs`
- `MeshATests.cs`

**Problem**

The search seeds all headings at the start, so `startTheta` is ignored.

**Steps**

1. Initialize only the heading corresponding to `startTheta`.
2. If a tolerance is intended, document and implement it.
3. Add test:
    - Same start/goal.
    - Different start headings.
    - At least one scenario should produce different first primitive or cost.

**Acceptance checks**

- `startTheta` affects the search.
- Test fails on old implementation and passes after fix.

---

## TASK P1-022 — MeshA: validate swept cells

**Files**

- `MeshAApi.cs`

**Problem**

Motion primitives can pass through blocked cells if only endpoint checks are used.

**Steps**

1. For every primitive, sample or rasterize the swept footprint.
2. Reject primitive if any covered cell is blocked.
3. Add narrow-wall test.

**Acceptance checks**

- Primitive cannot jump through obstacle.
- Path validator checks all primitive sweeps.

---

## TASK P1-023 — EDT: handle all-infinity input in `Transform1D`

**Files**

- `EdtApi.cs`
- `EdtTests.cs`

**Problem**

If all values are `PositiveInfinity`, intersection math can create `NaN`.

**Steps**

1. Add explicit handling:
    - If no finite source exists, output all `PositiveInfinity`.
2. Add test:
    - input all infinity;
    - output all infinity;
    - no NaN.

**Acceptance checks**

- `float.IsNaN(output[i])` is false for all cells.

---

## TASK P1-024 — FastMarching/FastSweeping/FieldDStar: reject invalid speeds/costs

**Files**

- `FastMarchingApi.cs`
- `FastSweepingApi.cs`
- `FieldDStarApi.cs`

**Problem**

Zero, negative, NaN, or infinity traversal costs can break update equations.

**Steps**

1. Validate every speed/cost before use:
    - speed must be finite and `> 0`;
    - cost must be finite and `> 0`.
2. If invalid, return `false`.
3. Add tests for:
    - zero;
    - negative;
    - NaN;
    - infinity.

**Acceptance checks**

- Invalid speed/cost returns `false`.
- No NaN appears in output fields.

---

## TASK P1-025 — Watershed: ensure heap mutation persists

**Files**

- `WatershedApi.cs`

**Problem**

A helper appears to accept state by value and then mutates `s.Heap`. If heap header is copied, mutations may not persist.

**Steps**

1. Find helpers that accept `WatershedState s` instead of `ref WatershedState s`.
2. Change them to `ref WatershedState s`.
3. Add test:
    - multiple basins;
    - flood must propagate beyond first popped item.

**Acceptance checks**

- Heap count changes are visible to caller.
- Watershed result labels all reachable cells.

---

## TASK P1-026 — WFC: preserve observations/constraints

**Files**

- `WfcApi.cs`
- `WfcTests.cs`

**Problem**

`TryRun` resets all possibilities, so prior observations or constraints can be lost.

**Steps**

1. Separate methods:
    - `ResetAllPossibilities`
    - `ObserveCell`
    - `Propagate`
    - `RunFromCurrentState`
2. `TryRun` should not erase observations unless explicitly requested.
3. Add test:
    - constrain one cell to tile A;
    - run WFC;
    - verify that cell remains tile A.

**Acceptance checks**

- Observed cells are preserved.
- Contradictions return `false`.

---

## TASK P1-027 — WFC: validate adjacency output

**Files**

- `WfcApi.cs`
- `WfcTests.cs`

**Problem**

Local adjacency rules must be verified after generation.

**Steps**

1. Add test helper:

```csharp
AssertEveryNeighborPairAllowed(output, rules)
```

2. Check right, left, up, and down neighbors.
3. Run this helper after every successful WFC test.

**Acceptance checks**

- Every generated grid satisfies rules.
- Deliberately invalid output fails helper.

---

## TASK P1-028 — Wilson: validate root/start and maze output

**Files**

- `WilsonApi.cs`
- `WilsonTests.cs`

**Problem**

Root/start bounds are unchecked, and extracted maze walls may leave the root marked as wall.

**Steps**

1. Validate root index before use.
2. Validate every random/current cell index.
3. Mark root as passage in output.
4. Ensure maze output encodes edges or cells consistently.
5. Add test:
    - generated maze has exactly one connected component over passages;
    - every non-root cell has one parent;
    - root is passable.

**Acceptance checks**

- Root cannot be out of bounds.
- Maze output is connected.
- Root is not left as wall.

---

## TASK P1-029 — Sandpile: add bounds and overflow checks

**Files**

- `SandpileApi.cs`
- `SandpileTests.cs`

**Problem**

`AddGrains` can write invalid cell indices or overflow integer grain counts.

**Steps**

1. Validate cell index.
2. Reject negative grain count if unsupported.
3. Check for overflow before addition:

```csharp
if (grains > int.MaxValue - s.Cells[cell]) return false;
```

4. Add tests:
    - invalid index;
    - overflow;
    - negative grains.

**Acceptance checks**

- Invalid operations return `false`.
- No integer overflow.

---

## TASK P1-030 — Sandpile: fix step return semantics

**Files**

- `SandpileApi.cs`

**Problem**

A step can topple the final queued cell and then return `false`, making it ambiguous whether work happened.

**Steps**

1. Define return meaning:
    - Option A: `true` means “a toppling happened”.
    - Option B: `true` means “more work remains”.
2. Recommended: return an enum or two booleans:

```csharp
public enum SandpileStepResult
{
    NoWork,
    ToppledMoreRemain,
    ToppledNowStable
}
```

3. Update tests.

**Acceptance checks**

- Caller can distinguish no-op from final toppling.

---

## TASK P1-031 — RSR: return successor costs

**Files**

- `RsrApi.cs`
- `RsrTests.cs`

**Problem**

Returning only perimeter successors is insufficient if caller assumes unit cost. Jump successors have distance cost.

**Steps**

1. Change successor structure to include cost:

```csharp
public struct RsrSuccessor
{
    public int Cell;
    public int Cost;
}
```

2. Cost should be Manhattan distance between source and successor unless diagonal movement is supported.
3. Update callers/tests.

**Acceptance checks**

- Path cost through RSR equals reference grid A* cost.
- No successor silently implies unit cost unless distance is 1.

---

## TASK P1-032 — Thinning: normalize binary input

**Files**

- `ThinningApi.cs`

**Problem**

Algorithm assumes values are exactly `0` or `1`. Any nonzero byte greater than `1` corrupts neighbor counts.

**Steps**

1. Before processing, normalize:
    - `0` remains `0`;
    - any nonzero becomes `1`.
2. Or reject non-binary input.
3. Add test with values `255`.

**Acceptance checks**

- `255` behaves as foreground or returns `false`, depending on chosen contract.
- Contract is documented.

---

## TASK P1-033 — Subgoal: replace sampling line-of-sight with exact grid traversal

**Files**

- `SubgoalApi.cs`
- `SubgoalTests.cs`

**Problem**

Fixed-step sampling can miss blocked cells or corner cases.

**Steps**

1. Replace sample-based LOS with exact grid traversal:
    - Bresenham for grid cells;
    - or Amanatides-Woo traversal for continuous lines.
2. Add tests:
    - thin obstacle crossed diagonally;
    - corner-touching obstacle;
    - long shallow line.

**Acceptance checks**

- LOS agrees with reference traversal.
- No sampled line skips obstacle.

---

## TASK P1-034 — Wavestar: fix 3D storage sizing

**Files**

- `WavestarApi.cs`

**Problem**

Some closed/f-score storage appears sized by `sizeX * sizeY` instead of `sizeX * sizeY * sizeZ`.

**Steps**

1. Search all allocations in Wavestar.
2. Every per-voxel buffer must use:

```csharp
volume = sizeX * sizeY * sizeZ;
```

3. Check for integer overflow before multiplying.
4. Add 3D test with `sizeZ > 1`.

**Acceptance checks**

- No out-of-bounds when `sizeZ > 1`.
- Path can use different Z layers.

---

## TASK P1-035 — Wavestar: replace Morton key if coordinates can collide

**Files**

- `WavestarApi.cs`

**Problem**

Morton key packing can collide if coordinates exceed assumed bit width.

**Steps**

1. Define max coordinate range.
2. Validate dimensions fit range.
3. If dimensions exceed range, return `false` or use a larger key.
4. Add boundary tests at max supported dimension.

**Acceptance checks**

- No key collision inside documented range.
- Oversized dimensions fail cleanly.

---

## TASK P1-036 — CFTP: make coupling-from-the-past contract honest

**Files**

- `CftpApi.cs`
- `CftpTests.cs`

**Problem**

The current finite retry/update process is not enough to prove exact CFTP sampling unless historical updates are extended backward correctly and convergence is proven.

**Steps**

1. Document whether this is:
    - exact CFTP sampler;
    - approximate monotone sampler;
    - or demo/prototype.
2. If exact CFTP is required:
    - preserve old random updates;
    - prepend older updates when doubling history;
    - replay full history from extremes;
    - stop only when low and high coalesce.
3. If approximate only:
    - rename API to avoid claiming exact CFTP;
    - expose max history and convergence failure.

**Acceptance checks**

- Exact mode uses preserved history when extending backward.
- Failure to coalesce returns `false`.
- Tests prove deterministic replay from fixed seed.

---

## TASK P1-037 — Kasteleyn: guard dense matrix size

**Files**

- `KasteleynApi.cs`

**Problem**

Dense `VertexCount * VertexCount` matrix can overflow or allocate enormous memory.

**Steps**

1. Before allocation/build, check:

```csharp
if (vertexCount > MaxSupportedVertices) return false;
if ((long)vertexCount * vertexCount > MaxMatrixEntries) return false;
```

2. Document max supported vertices.
3. Add test with too-large grid.

**Acceptance checks**

- Oversized instance returns `false`.
- No integer overflow.

---

## TASK P1-038 — Morse: document limited scope or implement true pairing

**Files**

- `MorseApi.cs`
- `MorseTests.cs`

**Problem**

Current minima/maxima and pairing logic may not be a complete discrete Morse or persistence algorithm.

**Steps**

1. Decide supported feature set.
2. If only local extrema:
    - rename API/comments to avoid claiming full Morse complex.
3. If persistence is required:
    - implement proper lower-star filtration or union-find pairing;
    - validate against known small scalar fields.
4. Add tests with known persistence pairs.

**Acceptance checks**

- Documentation matches implementation.
- Known scalar-field tests pass.

---

# Phase 5 — Universal validation tests

## TASK P2-001 — Add path validator helpers

**Files**

- Add: `com.bovinelabs.grid/Tests/GridPathAssert.cs`

**Steps**

Create helpers:

```csharp
public static void AssertIndexPathValid(Grid2D grid, NativeArray<byte> blocked, NativeList<int> path, int start, int goal)
```

Checks:

1. Path length > 0.
2. First cell is start.
3. Last cell is goal.
4. Every cell in bounds.
5. No cell is blocked.
6. Every consecutive pair is adjacent or allowed by that algorithm.
7. No diagonal corner cut if diagonals are allowed.

For any-angle paths:

```csharp
public static void AssertWaypointPathVisible(...)
```

Checks line of sight for every segment.

**Acceptance checks**

- Every pathfinding test uses a validator.
- Deliberately invalid path fails validator.

---

## TASK P2-002 — Add CBS conflict validator helper

**Files**

- Add: `com.bovinelabs.grid.cbs/Tests/CbsAssert.cs`

**Steps**

Create:

```csharp
public static void AssertNoConflicts(NativeList<int> flatPaths, NativeList<int> lengths)
```

Checks:

1. For every agent pair.
2. For every time up to max path length.
3. Vertex conflict:
    - same cell at same time.
4. Edge conflict:
    - agents swap cells between time `t` and `t + 1`.
5. After path end, agent remains at final cell.

**Acceptance checks**

- Every CBS success test uses this helper.

---

## TASK P2-003 — Add brute-force references for small cases

**Files**

- Add test-only reference implementations.

**Reference solvers needed**

- BFS / Dijkstra for grid shortest path.
- Brute-force binary graph cut for tiny grids.
- Brute-force MAP for tiny belief grids/chains.
- Brute-force WFC adjacency checker.
- Brute-force cellular automaton for Hashlife small grids.

**Acceptance checks**

- Random small tests compare optimized result to brute force.
- At least 1,000 random seeds per critical module in edit-mode tests.

---

## TASK P2-004 — Add wrong-length array tests to every module

**Files**

All test files.

**Steps**

For every public API accepting `NativeArray<T>`:

1. Test uncreated array.
2. Test length `0`.
3. Test length `required - 1`.
4. Test exact required length.
5. Test length greater than required.

**Acceptance checks**

- Invalid lengths return `false`.
- Valid lengths do not crash.

---

## TASK P2-005 — Add capacity exhaustion tests

**Files**

All heap/list-heavy modules.

**Modules**

- Anya
- CBS
- CPD
- GraphCut
- Hashlife
- JPS
- Kasteleyn
- MeshA
- SIPP
- Subgoal
- WFC
- Wavestar

**Steps**

1. Create state with tiny capacity.
2. Run a scenario that requires more capacity.
3. Verify method returns `false`.
4. Verify no partial success output is returned.

**Acceptance checks**

- No algorithm succeeds after dropping data.
- No memory overwrite.

---

## TASK P2-006 — Add dispose-after-failure tests

**Files**

All test files.

**Steps**

For every `TryCreate`:

1. Attempt create with invalid dimensions.
2. Dispose returned state anyway.
3. Attempt create with tiny capacity that fails during use.
4. Dispose state.
5. Dispose again.

**Acceptance checks**

- No leaks.
- No double-free.
- State is `default` after dispose.

---

## TASK P2-007 — Add randomized fuzz tests

**Files**

Add one fuzz test class per major module.

**Pathfinding fuzz**

For 1,000 seeds:

1. Generate small grid.
2. Random blocked cells.
3. Random start/goal.
4. Compare:
    - path existence to BFS where applicable;
    - path validity;
    - cost optimality if algorithm claims optimality.

**Graph/cut fuzz**

For 1,000 seeds:

1. Generate tiny unary/pairwise energies.
2. Compare optimized graph cut to brute force.

**Belief fuzz**

For 1,000 seeds:

1. Tiny 1D/2D grid.
2. Random unary/pairwise.
3. Compare decoded MAP to brute force where exact after enough iterations or on tree graphs.

**Acceptance checks**

- Fuzz tests are deterministic by seed.
- Failing seed is printed.

---

# Phase 6 — API contracts and documentation

## TASK P2-008 — Document every public method contract

**Files**

All public API files.

**For every public method, document**

1. Required input lengths.
2. Whether start/goal may be blocked.
3. Whether output is cleared.
4. Whether result is optimal, approximate, or heuristic.
5. Whether method is Burst-safe.
6. Whether state may be reused.
7. What `false` means.

**Acceptance checks**

- Every public method has XML comments.
- Tests match documented behavior.

---

## TASK P2-009 — Add result enums for ambiguous APIs

**Files**

Where applicable.

**Problem**

A boolean cannot distinguish:
- no path exists;
- invalid input;
- capacity exhausted;
- max iteration reached;
- graph build failed.

**Steps**

For validation-sensitive modules, add result enum:

```csharp
public enum GridSolveStatus
{
    Success,
    InvalidInput,
    NoSolution,
    CapacityExceeded,
    IterationLimitReached,
    InvalidState
}
```

Keep `Try*` wrapper if needed, but expose detailed status for validation.

**Acceptance checks**

- Critical solvers can report why they failed.
- Tests assert the correct failure reason.

---

# Phase 7 — Final validation checklist

Before claiming validation-ready, every item below must be checked.

## Build

- [ ] Compiles in Editor.
- [ ] Compiles in player build.
- [ ] Burst enabled tests pass.
- [ ] Burst disabled tests pass.
- [ ] No compiler warnings from changed code.

## Memory

- [ ] Unity safety checks pass.
- [ ] Leak detection passes.
- [ ] Double-dispose tests pass for every state.
- [ ] Failed-create dispose tests pass.
- [ ] Wrong-length array tests pass.
- [ ] Capacity exhaustion tests pass.

## Correctness

- [ ] All path outputs are validated.
- [ ] All CBS outputs are conflict-free.
- [ ] CPD matches BFS on random small grids.
- [ ] SIPP respects safe intervals.
- [ ] GraphCut matches brute force on tiny cases.
- [ ] Belief matches brute force on tree/tiny cases.
- [ ] WFC output satisfies all adjacency rules.
- [ ] Hashlife matches brute-force CA on small grids.
- [ ] Wilson maze is connected and valid.
- [ ] Kasteleyn orientation is checked or unsupported regions fail.

## Documentation

- [ ] Public API contracts written.
- [ ] Unsupported inputs documented.
- [ ] Approximate algorithms are not described as exact.
- [ ] Validation limitations are documented.

---

# Suggested execution order

1. `P0-001` Domino compile blocker.
2. `P0-002` Belief memory-corrupting test.
3. `P0-003` Anya wrong blocked-goal test.
4. `P0-004` DStarLite contract/test mismatch.
5. `P0-009` Wilson allocator bug.
6. `P1-014` DynamicCut dirty-node out-of-bounds.
7. `P0-005` shared validation helpers.
8. `P0-006` to `P0-008` public input validation.
9. `P0-010` and `P0-011` allocator/create/dispose audit.
10. `P0-012` to `P0-014` no silent partial results.
11. All P1 module correctness tasks.
12. All P2 validation tests.
13. Final validation checklist.

---

# Definition of done for the whole repository

The repository is validation-ready only when:

```text
All P0 tasks are complete.
All P1 tasks are complete or explicitly documented as unsupported.
All P2 validation tests exist and pass.
No public API can crash from bad user input.
No solver can return success after capacity failure.
Every generated path/cut/belief/maze/WFC output has an independent validator.
Leak detection passes.
Burst enabled and disabled runs produce the same pass/fail results.
```

