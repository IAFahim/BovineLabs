# Grid Algorithms TODO

## Immediate (Blockers)

- [ ] **Anya `Search_WithWall`** — Only failing test (1/34). `(0,0)→(9,0)` with blocked `(5,0)`. Root cause: interval scanning hits blocked cell and `continue`s past it, but doesn't properly split into two sub-intervals `[0,5)` and `[6,10)`. The node from row 1 expanding back up to row 0 with infinite-width projection scans all x=0..9, hits blocked x=5, skips it, but the interval `oL..oR` at x=6..9 never gets created because `cellY = min(1,0) = 0` means it checks row 0 cells which includes x=5. The fix: when scanning `startX..endX`, accumulate consecutive free cells into contiguous intervals instead of per-cell intervals. See AGENTS.md §9 "Anya Interval Expansion (KNOWN BUG)".

## Steppable Refactoring (for Visualization)

These algorithms have monolithic `while` loops that need `TryInit` + `TryStep` decomposition so a visualization system can drive them frame-by-frame.

- [ ] **AnyaApi** → `TryInitSearch(start, goal)` + `TryStepSearch(out bool foundGoal)`. Extract the `while (!heap.IsEmpty)` body into single-pop step.
- [ ] **WfcApi** → `TryObserveStep()` + `TryPropagateStep()`. Split `TryRun`'s `while(!heap.IsEmpty)` into observe-one + propagate-one.
- [ ] **CbsApi** → Separate high-level constraint tree stepper from low-level A* stepper. Nested loop → two state machines.
- [ ] **JpsApi** → `TryInitSearch` + `TryStepSearch` (jump point expansion per step).

## Recast-Level Optimization (Performance Pass)

Current code uses `NativeArray<T>` + `GetUnsafePtr()` which is functional but not Recast-standard. For maximum throughput:

- [ ] **State structs → raw pointers** — Replace `NativeArray<T>` fields with `T*` + `AllocatorManager.AllocatorHandle`. Eliminates safety handle overhead entirely. Pattern: `AllocatorManager.Allocate(handle, size, align)` in `TryCreate`, `AllocatorManager.Free` in `Dispose`.
- [ ] **BeliefApi** — `Messages`/`MessagesNext` pointer swap instead of copy. Already partially done. Add `UnsafeUtility.MemClear` for zeroing.
- [ ] **EHLIndexer** — `NativeHashMap` allocation inside double loop → hoist out, `.Clear()` per cell. `NativeArray<NativeList<T>>` → flat `T*` + offset/count arrays.
- [ ] **MeshAStar** — `UnsafeUtility.Malloc` per query → pre-allocate in `MeshAStarState`, `MemClear` at query start.
- [ ] **MultiResThetaStar / Wavestar** — `NativeMinPQ`, `NativeHashSet`, `NativeHashMap` allocated per `Execute()` → store in query context, `.Clear()` at start.
- [ ] **MinHeap comparison** — Add `[MethodImpl(AggressiveInlining)]` to `Less`, `Swap`, `SiftUp`, `SiftDown`.
- [ ] **HashLife** — `NativeParallelHashMap<ulong,int>` → custom open-addressing flat hash for Intern/ResultCache.
- [ ] **Anya helpers** — `PushNode`, `IsEdgePassable`, `NodeEquals` need `[MethodImpl(AggressiveInlining)]`.

## CBS Edge Constraints

- [ ] **Upgrade `CbsConstraint`** — Add `CellFrom` field. `CellFrom == -1` → vertex constraint (agent can't occupy `Cell` at `Time`). `CellFrom >= 0` → edge constraint (agent can't traverse `CellFrom → Cell` at `Time`).
- [ ] **Update `FindConflict`** — Detect swap conflicts (agents swap positions between T and T+1). Generate edge constraints instead of approximating with vertex constraints.
- [ ] **Update `TryAStar`** — Validate both vertex and edge constraints during neighbor expansion.

## Test Coverage Gaps

- [ ] **Create_Dimensions** — Every algorithm should verify memory sizes and Grid2D correctness after `TryCreate`.
- [ ] **Dispose_Double** — Every algorithm should verify calling `Dispose(ref s)` twice does not throw.
- [ ] **Blocked/NoPath** — Fully blocked goal, out-of-bounds start/goal, empty grid.
- [ ] **Fuzz** — Pathfinder equivalence tests: A* vs JPS vs Anya on random grids (same start/goal, verify path cost matches within epsilon).

## Done ✓

- [x] **CBS** — Edge swap conflict detection, goal-wait clamping, multi-agent bottleneck tests
- [x] **Domino** — Manual bipartite flow network, 4-directional edges, negative diff handling, mutilated chessboard test
- [x] **GraphCut** — Undirected pairwise, public `AddEdgeInternal`, bottleneck + partition tests
- [x] **Belief** — `MessagesNext.Fill(0f)` per iteration, consensus + ghost belief tests
- [x] **Anya** — Double precision, bidirectional expansion, LineOfSight shortcut, euclidean cost + corner tests
- [x] **Test asmdef** — All 5 test assemblies use correct template
- [x] **AGENTS.md** — Full workflow, gotchas, known bugs, cross-package references
- [x] **Skill file** — `grid-tests` skill with compactor loop
