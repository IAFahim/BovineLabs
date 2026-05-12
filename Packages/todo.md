There is no single “top 2D grid algorithm” because papers target different problem models: single-agent grid, any-angle Euclidean shortest path, lattice/motion-primitive planning, multi-agent pathfinding, dynamic obstacles, weighted grids, etc.

But for **serious 2026 academic output**, the strongest candidates I found are:

## 1. **MeshA*** — best if you mean *grid path planning with motion primitives*

**Paper:** *MeshA*: Efficient Path Planning with Motion Primitives*
**Venue:** AAAI 2026, published March 14, 2026.
**Authors:** Marat Agranovskiy, Konstantin Yakovlev. ([ojs.aaai.org][1])

This is the one I would put at the top for your use case if your grid is not just “move N/E/S/W/diagonal,” but has **short kinodynamic motion primitives**: dash arcs, swept cells, turn-radius moves, charge moves, animation-root-motion-like transitions, etc.

The problem: normal lattice A* over motion primitives explodes because each state has a huge branching factor. MeshA* changes the search structure: instead of naively searching the full primitive lattice, it searches over grid cells while simultaneously fitting valid primitive sequences into those cells. The paper claims it preserves **completeness and optimality**, while giving about **1.5×–2× runtime reduction** over conventional lattice-based planning. ([ojs.aaai.org][1])

For a competitive programmer / engine-dev lens, the interesting part is not “A* but faster.” It is that the state abstraction changes from primitive-sequence lattice to a mesh/grid-cell fitting problem while keeping guarantees.

**Why it matters for games:**
For your DOTS/Timeline/physics movement system, this is more relevant than plain JPS/Theta*/Anya because your agents likely have move primitives, animation constraints, attack movement, charge/flee/strafe behaviors, and swept occupancy. MeshA* is closer to “pathfinding for actual moves” than “pathfinding for a point on a grid.”

**When I would use it:**
Use it when moves are not atomic grid steps. Example: enemy has a 0.4s lunge primitive occupying a swept capsule over several cells. MeshA* is a serious direction.

---

## 2. **EHL*** — best if you mean *ultrafast optimal Euclidean path queries*

**Paper:** *EHL*: Memory-Budgeted Indexing for Ultrafast Optimal Euclidean Pathfinding*
**Venue:** AAAI 2026, published March 14, 2026.
**Authors:** Jinchun Du, Bojie Shen, Muhammad Aamir Cheema. ([ojs.aaai.org][2])

This is not “grid A*” in the normal CP sense. It targets the **Euclidean Shortest Path Problem** with polygonal obstacles, using a grid-backed index/hub-labeling style approach. The original EHL was already state-of-the-art for ultra-fast optimal queries, but it had brutal memory costs, sometimes tens of GB on large maps. EHL* introduces a memory-budgeted index that can reduce memory by **10×–20×** without much query-time loss. ([ojs.aaai.org][2])

For a high-level algorithmic view: it is closer to **preprocessed shortest-path oracle** than online search. It pays preprocessing/indexing cost, then answers path queries extremely fast.

**Why it matters for games:**
If your map is mostly static and you need many queries per frame or per tick, this is much more interesting than rerunning A*/JPS/Theta*. Think: enemy crowd path queries over fixed dungeon geometry, base layouts, or precomputed realm maps.

**When I would use it:**
Use it for static or semi-static maps where memory is acceptable and query speed dominates. Not ideal for destructible/dynamic obstacles unless you partition dynamic areas separately.

---

## Honorable mention: **Efficient Hierarchical Any-Angle Path Planning on Multi-Resolution Grids**

This one appeared on arXiv in February 2026, but the arXiv page says it was accepted to **RSS 2025**, so it is not truly a “2026 paper” by venue. Still, it is academically strong. It targets 2D/3D occupancy maps, uses multi-resolution representations, and aims to keep any-angle completeness/optimality properties while fixing scalability issues in high-res maps. ([arXiv][3])

The key idea: any-angle paths are shorter/smoother because they are not constrained to grid edges; the paper exploits multi-resolution structure to keep this tractable. ([arXiv][4])

For your use case, this is worth reading if you want hierarchical grid/path layers: coarse far-field, fine local cells, then any-angle smoothing/taut-path behavior.

---

# My ranking for a serious 2D grid/pathfinding person

| Rank | Algorithm                                           | Best for                                          | Why it is not silly                                                                        |
| ---: | --------------------------------------------------- | ------------------------------------------------- | ------------------------------------------------------------------------------------------ |
|    1 | **MeshA***                                          | Grid + motion primitives                          | Preserves completeness/optimality, attacks branching factor in lattice planning, AAAI 2026 |
|    2 | **EHL***                                            | Static-map optimal Euclidean shortest-path oracle | 10×–20× memory reduction over EHL while keeping ultra-fast optimal queries                 |
|    3 | **Hierarchical Any-Angle Multi-Resolution Planner** | Large occupancy maps, smoother paths              | Strong any-angle + hierarchy idea, but RSS 2025 despite 2026 arXiv release                 |

# For your game/system specifically

Use this mental split:

**Local tactical grid behavior:**
Do not use EHL*. Use something closer to **MeshA*** or your own primitive-aware grid planner, because local enemies need motion constraints, swept occupancy, attack arcs, fear/curiosity/decoy weights, timeline clips, and dynamic costs.

**Static long-range navigation:**
Use Recast/navmesh or hierarchical grid. EHL*/Polyanya/Anya-family ideas are relevant if you want optimal any-angle paths over static geometry.

**Crowd/many enemies:**
Do not chase single-agent optimality too hard. Use hierarchical planning + flow fields / influence fields / reservation-lite local avoidance. For dense multi-agent research, 2026 also has MAPF work like *Graph Attention-Guided Search for Dense Multi-Agent Pathfinding*, but that is a different problem class: multi-agent coordination, not a clean single 2D grid algorithm. ([ojs.aaai.org][5])

My blunt pick: **read MeshA* first**. It is the most directly useful if your grid system is meant to drive real game movement primitives rather than abstract tile walking.

[1]: https://ojs.aaai.org/index.php/AAAI/article/view/41004 "
		MeshA*: Efficient Path Planning with Motion Primitives
							\| Proceedings of the AAAI Conference on Artificial Intelligence
			"
[2]: https://ojs.aaai.org/index.php/AAAI/article/view/41015 "
		EHL*: Memory-Budgeted Indexing for Ultrafast Optimal Euclidean Pathfinding
							\| Proceedings of the AAAI Conference on Artificial Intelligence
			"
[3]: https://arxiv.org/abs/2602.21174 "[2602.21174] Efficient Hierarchical Any-Angle Path Planning on Multi-Resolution 3D Grids"
[4]: https://arxiv.org/html/2602.21174v1 "Efficient Hierarchical Any-Angle Path Planning on Multi-Resolution 3D Grids"
[5]: https://ojs.aaai.org/index.php/AAAI/article/view/40192/44153 "Graph Attention-Guided Search for Dense Multi-Agent Pathfinding"
