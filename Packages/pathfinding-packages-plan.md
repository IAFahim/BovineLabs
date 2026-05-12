# Pathfinding Packages Implementation Plan

## Goal
Create 3 Unity DOTS packages using Burst + NativeCollections for state-of-the-art pathfinding:
1. `com.bovinelabs.grid.mesha` - MeshA* (AAAI 2026)
2. `com.bovinelabs.grid.ehl` - EHL* (AAAI 2026)
3. `com.bovinelabs.grid.wavestar` - Multi-res Theta* (RSS 2025)

Plus the shared base package `com.bovinelabs.grid`.

## Location
All packages go in: `/home/l/Github/bovinelabs-core-internals/BovineLabs/Packages/com.bovinelabs.grid.<name>/`

## Package Structure (each)
```
com.bovinelabs.grid.<name>/
  package.json
  Runtime/
    com.bovinelabs.grid.<name>.asmdef
    <Algorithm>Api.cs        # Public API entry point
    <Algorithm>Core.cs       # Burst-compiled core solver
    <Algorithm>Types.cs      # Native structs, enums
  Tests/
    com.bovinelabs.grid.<name>.tests.asmdef
    <Algorithm>Tests.cs
```

## Dependencies
- com.bovinelabs.grid (base: shared types, NativeGrid2D, IPathfinder)
- com.unity.collections (NativeList, NativeHashMap, NativeQueue, NativeParallelHashMap)
- com.unity.burst (BurstCompile, ReadOnly, WriteOnly)
- com.unity.mathematics (float3, int2, math.*)

## Burst Conventions
- All job structs use [BurstCompile(CompileSynchronously = true)]
- NativeArray/NativeList with [ReadOnly]/[WriteOnly] attributes
- Allocator.Temp for single-frame, Allocator.Persistent for cached indices
- No managed allocations in hot paths
- Unity.Mathematics everywhere (no System.Numerics)

## Algorithm Details

### MeshA* (com.bovinelabs.grid.mesha)
- Paper: AAAI 2026, Agranovskiy & Yakovlev
- Core idea: search over grid cells (extended cells) instead of primitive lattice states
- Key data: MotionPrimitives (swept cells, arc length, heading change), MeshInfo (config transitions)
- Two phases: build mesh graph from primitives, then A* on extended cells
- Reference C++ impl: ~/pathfinding-research/MeshAStar/

### EHL* (com.bovinelabs.grid.ehl)
- Paper: AAAI 2026, Du, Shen, Cheema
- Core idea: Hub-labeling oracle for Euclidean shortest paths with memory budgeting
- Offline phase: build visibility graph → hub labels → merge grid cells under memory budget
- Online phase: O(|L_s| + |L_t|) query via sorted label intersection
- No public reference implementation -- coding from paper
- Paper text: ~/pathfinding-research/EHLStar_full_text.txt

### Wavestar (com.bovinelabs.grid.wavestar)
- Paper: RSS 2025 / arXiv 2602.21174, Reijgwart et al. (ETH Zurich)
- Core idea: multi-resolution any-angle Theta* on octree occupancy maps
- Key data: OctreeIndex, MultiResCostField, subvolume predecessor+g-cost
- Reference C++ impl: ~/pathfinding-research/wavestar/
- Paper text: ~/pathfinding-research/Wavestar_full_text.txt

## Unity Version
6000.5.0b1 (Unity 6)
