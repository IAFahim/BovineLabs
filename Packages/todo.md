# Grid Algorithms TODO

## Recast-Level Optimization (Performance Pass)

Current code uses `NativeArray<T>` + `GetUnsafePtr()` which is functional but not Recast-standard. For maximum throughput:

- [x] **State structs → raw pointers** — Replace `NativeArray<T>` fields with `T*` + `AllocatorManager.AllocatorHandle`. Eliminates safety handle overhead entirely. Pattern: `AllocatorManager.Allocate(handle, size, align)` in `TryCreate`, `AllocatorManager.Free` in `Dispose`.
- [x] **EHLIndexer** — `NativeHashMap` allocation inside double loop → hoist out, `.Clear()` per cell. `NativeArray<NativeList<T>>` → flat `T*` + offset/count arrays.
- [x] **HashLife** — `NativeParallelHashMap<ulong,int>` → custom open-addressing flat hash for Intern/ResultCache.

## Done ✓

- [x] **Anya `Search_WithWall`** — Fixed forward-direction guard blocking ExpandCorners in backward direction. Moved ExpandCorners before forwardDir check. Added goal-on-same-row detection when node is popped.
- [x] **AnyaApi steppable** — `TryInitSearch` + `TryStepSearch` + `TryExtractPath` decomposition.
- [x] **WfcApi steppable** — `TryInitWfc` + `TryObserveStep` + `TryExtractOutput` decomposition.
- [x] **CbsApi steppable** — `TryInitSolve` + `TryStepSolve` + `TryExtractSolution` decomposition.
- [x] **JpsApi steppable** — `TryInitSearch` + `TryStepSearch` decomposition.
- [x] **BeliefApi pointer swap** — `Messages`/`MessagesNext` swap via tuple deconstruction, `UnsafeUtility.MemSet` for zeroing.
- [x] **MinHeap inlining** — `[MethodImpl(AggressiveInlining)]` on `Less`, `Swap`, `SiftUp`, `SiftDown`.
- [x] **Anya helper inlining** — `[MethodImpl(AggressiveInlining)]` on `PushNode`, `IsEdgePassable`, `NodeEquals`.
- [x] **CBS edge constraints** — `CellFrom` field, swap conflict detection in `FindConflict`, edge constraint validation in `TryAStar`.
- [x] **EHLIndexer static wrapper** — `EHLIndexer.TryBuild` + `EHLIndexer.TryAssembleIndex` wrapping `EHLIndexerJob`. Deep-copy NativeArrays in `TryAssembleIndex` to prevent use-after-dispose.
- [x] **Create_Dimensions tests** — Added to EDT, JPS, HashLife.
- [x] **Dispose_Double tests** — Added to FieldDStar, HashLife.
- [x] **Blocked/NoPath tests** — Added `Search_FullyBlocked_NoPath`, `Search_OutOfBounds_NoPath` to Anya. Added `FullyBlocked_NoPath` to JPS.
- [x] **Fuzz tests** — `PathfinderFuzzTests` in shattered-unit-tests package. JPS vs Anya cost equivalence on random grids.
- [x] **JPS asmdef fix** — Fixed test assembly asmdef (includePlatforms, overrideReferences, precompiledReferences).
- [x] **CBS** — Edge swap conflict detection, goal-wait clamping, multi-agent bottleneck tests
- [x] **Domino** — Manual bipartite flow network, 4-directional edges, negative diff handling, mutilated chessboard test
- [x] **GraphCut** — Undirected pairwise, public `AddEdgeInternal`, bottleneck + partition tests
- [x] **Belief** — `MessagesNext.Fill(0f)` per iteration, consensus + ghost belief tests
- [x] **Anya** — Double precision, bidirectional expansion, LineOfSight shortcut, euclidean cost + corner tests
- [x] **Test asmdef** — All test assemblies use correct template
- [x] **AGENTS.md** — Full workflow, gotchas, known bugs, cross-package references
- [x] **Skill file** — `grid-tests` skill with compactor loop
