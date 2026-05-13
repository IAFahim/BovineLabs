# BovineLabs Quill Grid

Grid-oriented algorithm visualization framework for Unity ECS/DOTS, built on BovineLabs Quill.

## Architecture

4-layer design: algorithms never draw directly.

```
Algorithm State → Capture/Record → Visual Buffers → Quill Render Systems
```

### Assemblies

| Assembly | Purpose |
|---|---|
| `BovineLabs.Quill.Grid.Data` | ECS components, buffer elements, coordinate helpers, palette |
| `BovineLabs.Quill.Grid.Debug` | Render systems (Editor/Debug only) |
| `BovineLabs.Quill.Grid.Authoring` | Baker + MonoBehaviour for scene setup |

### Visual Blocks

| Buffer | Renders |
|---|---|
| `GridCellVisual` | Colored cells (obstacle, frontier, closed, heatmap, path, conflict, constraint) |
| `GridLineVisual` | Arbitrary lines with color |
| `GridTextVisual` | Floating labels |
| `GridIntervalVisual` | Anya intervals with root points and parent lines |
| `GridArrowVisual` | Directional arrows (messages, gradients) |
| `GridPathVisual` | Path segments (from/to cell coords) |
| `GridAgentPathVisual` | Multi-agent time-stepped paths |
| `GridConflictVisual` | CBS conflict markers |
| `GridConstraintVisual` | CBS constraint cells |
| `GridVectorFieldVisual` | Vector/gradient field arrows |
| `GridBlockedData` | Obstacle occupancy grid |

### Usage

1. Add `GridVisualizerAuthoring` to a GameObject in a subscene
2. Algorithm systems write to `DynamicBuffer<Grid*Visual>` on the same entity
3. Debug render systems read buffers and draw via Quill `Drawer`
4. Control visibility through `GridAlgorithmVisualConfig` toggles

### Algorithm Integration

Algorithm packages reference `BovineLabs.Quill.Grid.Data` and use `GridVisualRecorder` helpers:

```csharp
GridVisualRecorder.RecordBlockedCells(ref blockedBuffer, blockedArray, width, height);
GridVisualRecorder.RecordPath(ref pathBuffer, pathList);
GridVisualRecorder.RecordScalarField(ref cellBuffer, distanceField, count, GridCellVisual.LayerHeatmap);
```

Or write directly to buffers for algorithm-specific data:

```csharp
buffer.Add(new GridIntervalVisual(y, xl, xr, root, cost, parentIdx, isExpanded));
```

## Package name

`com.bovinelabs.quill.grid`

## Dependencies

- `com.bovinelabs.core`
- `com.bovinelabs.quill`
- `com.bovinelabs.grid` (core grid types: Grid2D, MinHeap, HeapNode)
- `com.unity.burst`
- `com.unity.collections`
- `com.unity.entities`
- `com.unity.mathematics`
