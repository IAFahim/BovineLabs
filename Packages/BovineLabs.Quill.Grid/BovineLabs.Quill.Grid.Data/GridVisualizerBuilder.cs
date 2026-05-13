using BovineLabs.Core.EntityCommands;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridVisualizerBuilder
    {
        private const int CellBuffer = 1 << 0;
        private const int LineBuffer = 1 << 1;
        private const int TextBuffer = 1 << 2;
        private const int IntervalBuffer = 1 << 3;
        private const int ArrowBuffer = 1 << 4;
        private const int PathBuffer = 1 << 5;
        private const int AgentPathBuffer = 1 << 6;
        private const int ConflictBuffer = 1 << 7;
        private const int ConstraintBuffer = 1 << 8;
        private const int VectorFieldBuffer = 1 << 9;
        private const int BlockedBuffer = 1 << 10;
        private const int CellVisualStateBuffer = 1 << 11;

        private bool hasGlobal;
        private bool hasGrid;
        private bool hasConfig;
        private bool hasStyle;
        private bool hasPillarStyle;

        private GridVisualizerGlobal global;
        private GridVisualizerData grid;
        private GridAlgorithmVisualConfig config;
        private GridVisualizerStyle style;
        private GridPillarStyleConfig pillarStyle;
        private int bufferMask;

        public GridVisualizerBuilder WithGlobal(
            bool enabled = true,
            GridVisualizerMode mode = GridVisualizerMode.Live,
            int currentFrame = 0,
            int maxFrames = 0)
        {
            this.hasGlobal = true;
            this.global = new GridVisualizerGlobal
            {
                Enabled = enabled,
                CurrentFrame = currentFrame,
                MaxFrames = maxFrames,
                Mode = mode
            };

            return this;
        }

        public GridVisualizerBuilder WithGrid(float cellSize, float3 origin, int width, int height)
        {
            this.hasGrid = true;
            this.grid = new GridVisualizerData
            {
                CellSize = cellSize,
                Origin = origin,
                GridWidth = width,
                GridHeight = height
            };

            return this;
        }

        public GridVisualizerBuilder WithConfig(
            FixedString64Bytes algorithmName,
            FixedString32Bytes category = default,
            bool drawGrid = true,
            bool drawObstacles = true,
            bool drawFrontier = true,
            bool drawClosed = true,
            bool drawPath = true,
            bool drawLabels = true,
            bool drawHeatmap = true,
            bool drawIntervals = false,
            bool drawConstraints = false,
            bool drawConflicts = false,
            bool drawMessages = false,
            bool drawVectorField = false,
            bool drawTimeline = false)
        {
            this.hasConfig = true;
            this.config = new GridAlgorithmVisualConfig
            {
                AlgorithmName = algorithmName,
                Category = category,
                DrawGrid = drawGrid,
                DrawObstacles = drawObstacles,
                DrawFrontier = drawFrontier,
                DrawClosed = drawClosed,
                DrawPath = drawPath,
                DrawLabels = drawLabels,
                DrawHeatmap = drawHeatmap,
                DrawIntervals = drawIntervals,
                DrawConstraints = drawConstraints,
                DrawConflicts = drawConflicts,
                DrawMessages = drawMessages,
                DrawVectorField = drawVectorField,
                DrawTimeline = drawTimeline
            };

            return this;
        }

        public GridVisualizerBuilder WithVisualBuffers()
        {
            this.bufferMask |= CellBuffer | LineBuffer | TextBuffer | IntervalBuffer | ArrowBuffer | PathBuffer |
                AgentPathBuffer | ConflictBuffer | ConstraintBuffer | VectorFieldBuffer | BlockedBuffer;
            return this;
        }

        public GridVisualizerBuilder WithCellBuffer()
        {
            this.bufferMask |= CellBuffer;
            return this;
        }

        public GridVisualizerBuilder WithLineBuffer()
        {
            this.bufferMask |= LineBuffer;
            return this;
        }

        public GridVisualizerBuilder WithTextBuffer()
        {
            this.bufferMask |= TextBuffer;
            return this;
        }

        public GridVisualizerBuilder WithIntervalBuffer()
        {
            this.bufferMask |= IntervalBuffer;
            return this;
        }

        public GridVisualizerBuilder WithArrowBuffer()
        {
            this.bufferMask |= ArrowBuffer;
            return this;
        }

        public GridVisualizerBuilder WithPathBuffer()
        {
            this.bufferMask |= PathBuffer;
            return this;
        }

        public GridVisualizerBuilder WithAgentPathBuffer()
        {
            this.bufferMask |= AgentPathBuffer;
            return this;
        }

        public GridVisualizerBuilder WithConflictBuffer()
        {
            this.bufferMask |= ConflictBuffer;
            return this;
        }

        public GridVisualizerBuilder WithConstraintBuffer()
        {
            this.bufferMask |= ConstraintBuffer;
            return this;
        }

        public GridVisualizerBuilder WithVectorFieldBuffer()
        {
            this.bufferMask |= VectorFieldBuffer;
            return this;
        }

        public GridVisualizerBuilder WithBlockedBuffer()
        {
            this.bufferMask |= BlockedBuffer;
            return this;
        }

        public GridVisualizerBuilder WithCellVisualStateBuffer()
        {
            this.bufferMask |= CellVisualStateBuffer;
            return this;
        }

        public GridVisualizerBuilder WithStyle(GridVisualStyleType type)
        {
            this.hasStyle = true;
            this.style = new GridVisualizerStyle { Type = type };
            return this;
        }

        public GridVisualizerBuilder WithPillarStyle(in GridPillarStyleConfig styleConfig)
        {
            this.hasPillarStyle = true;
            this.pillarStyle = styleConfig;
            return this;
        }

        public void ApplyTo<T>(ref T commands)
            where T : struct, IEntityCommands
        {
            if (this.hasGlobal)
            {
                commands.AddComponent(this.global);
            }

            if (this.hasGrid)
            {
                commands.AddComponent(this.grid);
            }

            if (this.hasConfig)
            {
                commands.AddComponent(this.config);
            }

            if (this.hasStyle)
            {
                commands.AddComponent(this.style);
            }

            if (this.hasPillarStyle)
            {
                commands.AddComponent(this.pillarStyle);
            }

            this.AddBuffers(ref commands);
        }

        private void AddBuffers<T>(ref T commands)
            where T : struct, IEntityCommands
        {
            if ((this.bufferMask & CellBuffer) != 0) commands.AddBuffer<GridCellVisual>();
            if ((this.bufferMask & LineBuffer) != 0) commands.AddBuffer<GridLineVisual>();
            if ((this.bufferMask & TextBuffer) != 0) commands.AddBuffer<GridTextVisual>();
            if ((this.bufferMask & IntervalBuffer) != 0) commands.AddBuffer<GridIntervalVisual>();
            if ((this.bufferMask & ArrowBuffer) != 0) commands.AddBuffer<GridArrowVisual>();
            if ((this.bufferMask & PathBuffer) != 0) commands.AddBuffer<GridPathVisual>();
            if ((this.bufferMask & AgentPathBuffer) != 0) commands.AddBuffer<GridAgentPathVisual>();
            if ((this.bufferMask & ConflictBuffer) != 0) commands.AddBuffer<GridConflictVisual>();
            if ((this.bufferMask & ConstraintBuffer) != 0) commands.AddBuffer<GridConstraintVisual>();
            if ((this.bufferMask & VectorFieldBuffer) != 0) commands.AddBuffer<GridVectorFieldVisual>();
            if ((this.bufferMask & BlockedBuffer) != 0) commands.AddBuffer<GridBlockedData>();
            if ((this.bufferMask & CellVisualStateBuffer) != 0) commands.AddBuffer<GridCellVisualState>();
        }
    }
}
