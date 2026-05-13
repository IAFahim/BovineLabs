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

## Future
- [ ] **Anya completeness** — Interval splitting at blocked cells (create `[L,x)` and `[x+1,R)` sub-intervals instead of `continue`-ing past blocked cells). Will close remaining Anya reachability gaps found by fuzz testing.
- [ ] **Anya optimality** — Fix corner detection to handle all wall configurations. Currently misses some corners causing suboptimal paths on dense obstacle maps.
- [ ] **MeshA hash map elimination** — Strip `NativeHashMap` from MeshAStarJob, replace with flat arrays + bit-packed closed set
- [ ] **EHL SIMD Jaccard** — Replace iterative overlap computation with `math.countbits()` SIMD bitmasks
