# Pathfinding & Grid Algorithms - TODO

## In Progress
(none currently)

## Done
- [x] **CBS** — Edge swap conflicts, goal-wait conflicts, multi-agent bottleneck tests all passing
- [x] **Domino** — Bipartite matching via manual flow network (bypass BuildBinaryEnergy), 4-directional edges, negative diff handling
- [x] **GraphCut** — Undirected pairwise edges, bottleneck test, grid partition test
- [x] **Belief** — Message buffer clearing per iteration, consensus chain test, ghost belief test
- [x] **Test asmdef** — All 5 test assemblies use correct template (Editor platform, overrideReferences, nunit.framework.dll)
- [x] **Anya** — LineOfSight shortcut, bidirectional expansion, Euclidean cost test, corner test
- [x] **Anya Search_WithWall** — Fixed: `(int)math.ceil(double.PositiveInfinity)` overflow guard, zero-length interval filter, duplicate node detection
- [x] **Anya precision** — `DoubleMinHeap` + `DoubleHeapNode` with `double` keys; AnyaApi now uses full double-precision f-values eliminating float-truncation suboptimality
- [x] **Anya NoBlockedTraversal** — Replaced Bresenham LoS with Amanatides & Woo fast voxel traversal (epsilon-robust)
- [x] **CBS edge constraints** — `CbsConstraint` upgraded to `(Agent, Cell, CellFrom, Time)` format. Vertex constraints: `CellFrom == -1`. Edge constraints: `CellFrom >= 0`. `FindConflict` returns `conflictType` (0=vertex, 1=swap). `TryAStar` validates both. Test: `AStar_EdgeConstraint`
- [x] **Fuzz** — `PathfinderFuzzTests` in `shattered-unit-tests` package. 7 test configurations × 10-20 trials each. Random xorshift grids at varying densities. Validates path start/goal correctness, cross-validates JPS/Anya reachability, monitors Anya optimality gaps. 145 total tests all pass.
- [x] **Anya completeness** — Interval splitting at blocked cells: clear-run walker creates sub-intervals `[max(pL,x), min(pR,runEnd+1)]` around blocked cells instead of `continue`-ing
- [x] **Anya optimality** — `ExpandCorners` scans all integer x positions in `[L,R]` for left/right wall corners; `TryAddCornerNode` creates bi-directional expansion from detected corners
- [x] **MeshA hash map elimination** — Replaced 3× `NativeHashMap` with flat `float*`/`int*` arrays + `uint*` bit-packed closed set (1 bit per state). Uses `MinHeap` with `TryInsertOrDecrease` for decrease-key. All via `UnsafeUtility.Malloc(Allocator.Temp)`.
- [x] **EHL SIMD Jaccard** — `ComputeOverlap` uses `ulong` bitmask + `math.countbits()` for Jaccard coefficient. Fast path for ≤64 hubs, multi-word fallback for larger.
- [x] **Belief propagation optimization** — `TryIterate` rewritten with precomputed bounds, running `cellIdx++` increment, inlined direction mapping, unrolled 4-direction message sum, `UnsafeUtility.MemSet` for buffer clear, pointer-only access (zero NativeArray indexing). `TryDecodeMap` uses unrolled 4-direction message sum.
- [x] **Anya steppable** — `TryInitSearch` + `TryStepSearch` + `TryExtractPath`. Monolithic `TrySearch` preserved as backward-compatible wrapper. Steppable state: `Start`, `Goal`, `BestNode`, `BestCost`, `SearchComplete`.
- [x] **WFC steppable** — `TryInitWfc` + `TryObserveStep` + `TryExtractOutput`. Monolithic `TryRun` preserved as backward-compatible wrapper. Steppable state: `ObserveHeap`, `WfcComplete` (0=running, 1=done, 2=contradiction).
- [x] **CBS steppable** — `TryInitSolve` + `TryStepSolve` + `TryExtractSolution`. Monolithic `TrySolve` preserved as backward-compatible wrapper. Steppable state: `SolveComplete`, `AgentCount`, `SolutionNode`.
- [x] **EDT ScheduleParallel** — `TryBuildScheduled` returns `JobHandle` pipeline: `EdtRowJob.ScheduleParallel` → `EdtColJob.ScheduleParallel` with proper dependency chaining. `TryBuildParallel` convenience wrapper completes inline. `TryBuild` (sequential) unchanged.
- [x] **Continuum crowd optimization** — `TrySolvePotential` Gauss-Seidel sweeps flattened to 1D running `idx++`/`idx--` pointer increments. `RelaxCell` accepts precomputed index. `TryBuildFlow` flattened to 1D with running `idx++`. All neighbor reads via `pot[idx±1]`/`pot[idx±w]` — zero division/index math in inner loops.
- [x] **DStarLite pointer optimization** — All `s.G`/`s.RHS`/`s.InOpen`/`s.Parent` accesses in `TryRepair`, `TryExtractPath`, `CalculateKey`, `TryUpdateVertex`, `TryUpdateSuccessors` replaced with pre-extracted `float*`/`byte*`/`int*` pointers. Zero NativeArray indexing in hot loops.
- [x] **RSR pointer optimization** — `TryGetSuccessors` extracts `RectOfCell` pointer once; neighbor lookups via `roc[idx]` instead of `s.RectOfCell[idx]`.
- [x] **Subgoal pointer optimization** — `TryRelax` and `ExtractPath` take `float*`/`int*` raw pointers instead of `NativeArray<float/int>` refs. Eliminates safety handle overhead in A* inner loop.

## Future
- [ ] Verify all changes compile and 145+ tests pass in Unity
- [ ] Profile: benchmark Anya steppable vs monolithic, WFC observe-step throughput, CBS step latency
- [ ] HashLife: evaluate replacing `NativeParallelHashMap<ulong,int>` with custom open-addressing flat hash for Intern/ResultCache
- [ ] Subgoal: TryRelax inner loop could use SoA layout (gArr + parent as parallel arrays already, but subgoals Ptr access is still random)
