This is a large codebase — I'll go through every algorithm systematically and flag correctness bugs, not just superficial issues.

---

## Core (`com.bovinelabs.grid`)

**Grid2D, MinHeap, GridNeighbors, RangeI** — These are solid. MinHeap's decrease-key is correct, sift-up/down are correct, and the `Fill` in-place transform safety holds because parabola centers always map to their own original value.

---

## Algorithm-by-Algorithm Audit

### ❌ AnyaApi — Wrong algorithm entirely

`AnyaNode` has `Root`, `Row`, `XMin`, `XMax` fields — the real Anya interval-propagation structure. But `Search` ignores them completely and runs a plain A* with a grandparent line-of-sight check. The `Nodes` list is **never written to or read from**. This is closer to a broken Theta* than Anya.

Additionally, `parent[parent[u]]` can be -1 when `parent[u]` is the start node, then `s.Grid.ToCoord(-1)` produces garbage coordinates.

---

### ❌ BeliefApi — Wrong opposite-direction index

```cs
if (od == (dir ^ 1)) continue; // skip message from target direction
```

For 4 directions `{0=right, 1=down, 2=left, 3=up}`, the opposite of direction `d` is `(d + 2) % 4`. XOR with 1 gives: `0↔1`, `2↔3` — i.e. right↔down and left↔up. That is wrong in both the exclusion loop and the message write:

```cs
int msgIdx = neighbor * 4 * L + (dir ^ 1) * L; // also wrong
```

Both should use `(dir + 2) % 4`. As written, belief propagation passes messages into completely incorrect slots.

---

### ❌ CbsApi — Stub, not CBS

`AStar` does nothing but a blocked-cell check and returns `true`. No actual path is computed. `FindFirstConflict` always returns `false`. Every "path" is just `{start, goal}`. The entire conflict-resolution tree (the defining feature of CBS) is absent.

---

### ❌ CftpApi — Broken coupling

Coupling from the Past requires both chains to receive **identical** random inputs so they can coalesce under a monotone coupling. Here:

```cs
byte lowBit  = (byte)(u.RandomBits & 1);
byte highBit = (byte)((u.RandomBits >> 1) & 1);
s.Low[u.Cell]  = lowBit;
s.High[u.Cell] = highBit;
```

The two chains receive *different* bits from the same word. There is no monotone order being maintained, no domination relation, and no guarantee of coalescence. `Coalesced` returning true here would be coincidental, not correct.

---

### ❌ DStarLiteApi — Edge cost is guessed, blocked start not handled

`GetEdgeCost` doesn't receive grid width:

```cs
// We don't know width here, so just use heuristic
return 1f;
```

All edges cost 1.0, but the heuristic uses octile distance (which accounts for diagonals costing √2). This makes the heuristic inadmissible and can produce non-optimal paths.

The `Repair_BlockedStart` test expects `false`, but nothing in `Initialize` or `Repair` prevents a blocked start cell from getting a finite `RHS` through its unblocked neighbors. The test will likely fail.

---

### ❌ DynamicCutApi — Stub

`EditUnary` and `EditPairwise` only append to `DirtyNodes` without modifying any edge capacities in the underlying `GraphCutState`. `Repair` calls a full `MinCut` and clears `DirtyNodes` — the incremental structure exists only in name.

---

### ❌ FieldDStarApi — Broken flow extraction

`ExtractFlow` weights gradient directions by G values:

```cs
float weight = s.G[ni];
grad += dir * weight;
```

A lower G means closer to the goal, so this pushes the flow *away* from the goal. The flow field points in the wrong direction. It should use the gradient of the potential (finite differences of G), not a weighted sum.

`UpdateRHS` also has no guard for the goal node — goal's RHS should remain 0, but as written it will be overwritten if any neighbor has a lower `G + cost`.

---

### ❌ GraphCutApi — Max-flow never runs, flow never updated

The BFS in `MinCut` sets `found = true` inside the dequeue loop but never actually breaks or uses `found` to trace an augmenting path. Edge `.Flow` fields are initialized to 0 and **never modified**. The bottleneck is hardcoded to 1 regardless of actual residual capacity. Max-flow is not computed; the "cut" is based entirely on source adjacency, not any real flow.

`AlphaExpansion` also passes `labels[0]` as the second label to `ApplyCutLabels`, which should be each cell's current label — a single constant is wrong.

---

### ❌ HashlifeApi — Step is a stub

`StepPowerOfTwo` always returns the input node unchanged at every level ≥ 1. No Game-of-Life rule is applied. The recursion structure is present but the computation is missing.

---

### ❌ KasteleynApi — Orientation is incorrect

```cs
int sign = ((e.x + e.y) % 2 == 0) ? 1 : -1;
```

`e.x` and `e.y` are compact vertex IDs (0, 1, 2, …), not grid coordinates. A Kasteleyn orientation on a grid graph requires assigning signs based on spatial position and edge direction (e.g. all horizontal edges +1, vertical edges alternating by row). Using sequential IDs produces an orientation that is not guaranteed to be Kasteleyn, so the determinant formula gives incorrect matching counts.

---

### ❌ SubgoalApi — Corner detection looks at wrong direction, search ignores graph

```cs
int2 diag = p + Grid2D.Directions8[d * 2];
```

For `d` in `0..3`, `d * 2` gives `0, 2, 4, 6` — which are exactly the cardinal directions in `Directions8` (right, up-right… wait, let's check: `Directions8 = {(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1),(0,-1),(1,-1)}`). Indices 0,2,4,6 = `(1,0),(0,1),(-1,0),(0,-1)` — the same four cardinals as `Directions4`. The "diagonal" check is checking a cardinal direction, so no actual corner detection is happening.

`Search` builds a subgoal graph but then runs plain A* over the full grid, never consulting `Subgoals`, `Edges`, or `EdgeRanges`.

---

### ❌ WfcApi — Same opposite-direction bug as BeliefApi

```cs
int oppDir = d ^ 1;
neighborPossible |= s.Compatibility[cp * 4 + oppDir];
```

Same issue: `^1` gives wrong opposite for 4-direction indices. Should be `(d + 2) % 4`.

---

### ⚠️ Partially Implemented / Minor Issues

**ContinuumCrowdApi** — `Divergence` is allocated but never computed. `BuildFlow` and potential solve are correct.

**DominoApi** — `FlipAt` always returns `false`. Height function uses `(d==0||d==1)?1:-1` which doesn't implement actual height-function semantics for dominoes (height should change by ±1 crossing a domino boundary, ±3 otherwise).

**MorseApi** — `TraceManifolds` can loop if `Descending` has a cycle on flat regions. `Simplify` iterates critical points but performs no cancellation. `PairByPersistence` only pairs maxima, not saddle-minimum pairs.

**RsrApi** — `GetSuccessors` returns plain 4-neighbors, ignoring the entire RSR rectangle structure. The rectangle-expansion greedy algorithm (expand width first, then height) can leave valid cells uncovered.

**SippApi** — `BuildSafeIntervals` assigns every cell one interval `[0, ∞)`. No dynamic obstacles are ever encoded. The algorithm degrades to plain A*.

**ThinningApi** — `Frontier.Contains` is O(n). `IsSimplePoint` allocates a managed `byte[]` inside a method that could be Burst-compiled. The check `fgCount >= 2` is not the standard simplicity criterion.

---

## Summary Table

| Package | Status | Core Issue |
|---|---|---|
| `anya` | ❌ Wrong algorithm | Not Anya; `AnyaNode` unused; broken parent traversal |
| `belief` | ❌ Correctness bug | Opposite direction `^1` instead of `+2 mod 4` |
| `cbs` | ❌ Stub | No A*, no conflict detection |
| `cftp` | ❌ Correctness bug | Chains not coupled; coalescence not guaranteed |
| `continuum` | ⚠️ Incomplete | Divergence unused |
| `cpd` | ✅ Mostly correct | Run compression assumes contiguous ordering |
| `domino` | ⚠️ Incomplete | `FlipAt` always false; height function wrong |
| `dstarlite` | ❌ Correctness bug | Edge costs all=1; blocked start not rejected |
| `dynamiccut` | ❌ Stub | Dirty nodes ignored; not incremental |
| `edt` | ✅ Correct | — |
| `fastmarching` | ✅ Correct | — |
| `fastsweeping` | ✅ Correct | — |
| `fielddstar` | ❌ Correctness bug | Flow extraction wrong; goal RHS overwritten |
| `graphcut` | ❌ Broken | Flow never updated; max-flow not computed |
| `hashlife` | ❌ Stub | Step is a no-op |
| `jps` | ✅ Mostly correct | Recursion depth risk on large open grids |
| `kasteleyn` | ❌ Correctness bug | Orientation uses IDs not grid coords |
| `morse` | ⚠️ Incomplete | Simplify is no-op; potential infinite loop |
| `rsr` | ⚠️ Incomplete | `GetSuccessors` ignores RSR structure |
| `sandpile` | ✅ Correct | — |
| `sipp` | ❌ Stub | Safe intervals always `[0,∞)` |
| `subgoal` | ❌ Correctness bug | Corner detection wrong; search ignores graph |
| `thinning` | ⚠️ Mostly correct | Managed alloc; O(n) contains |
| `watershed` | ✅ Mostly correct | Height=0 cells skipped (design choice) |
| `wfc` | ❌ Correctness bug | Opposite direction `^1` instead of `+2 mod 4` |
| `wilson` | ✅ Correct | — |
