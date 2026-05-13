using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation |
                       WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation |
                       WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    [UpdateAfter(typeof(GridSyntheticFieldSystem))]
    public partial struct GridCellVisualStateTransitionSystem : ISystem
    {
        private BufferLookup<GridCellVisual> cellLookup;
        private BufferLookup<GridCellVisualState> stateLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridCellVisualState>();
            this.cellLookup = state.GetBufferLookup<GridCellVisual>(true);
            this.stateLookup = state.GetBufferLookup<GridCellVisualState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.cellLookup.Update(ref state);
            this.stateLookup.Update(ref state);

            var dt = SystemAPI.Time.DeltaTime;
            var cellLookupLocal = this.cellLookup;
            var stateLookupLocal = this.stateLookup;

            foreach (var (visualizer, global, style, pillar, entity) in
                     SystemAPI.Query<
                             GridVisualizerData,
                             GridVisualizerGlobal,
                             GridVisualizerStyle,
                             GridPillarStyleConfig>()
                         .WithEntityAccess())
            {
                if (!global.Enabled)
                {
                    continue;
                }

                if (style.Type != GridVisualStyleType.Pillar)
                {
                    continue;
                }

                if (!cellLookupLocal.HasBuffer(entity) || !stateLookupLocal.HasBuffer(entity))
                {
                    continue;
                }

                var cells = cellLookupLocal[entity];
                var states = stateLookupLocal[entity];

                var count = visualizer.GridWidth * visualizer.GridHeight;
                if (states.Length != count)
                {
                    states.ResizeUninitialized(count);

                    for (var i = 0; i < count; i++)
                    {
                        states[i] = new GridCellVisualState
                        {
                            TargetDepth = 0f,
                            CurrentDepth = 0f,
                            TargetColor = pillar.BaseColor,
                            CurrentColor = pillar.BaseColor
                        };
                    }
                }

                for (var i = 0; i < count; i++)
                {
                    var s = states[i];
                    s.TargetDepth = 0f;
                    s.TargetColor = pillar.BaseColor;
                    states[i] = s;
                }

                for (var i = 0; i < cells.Length; i++)
                {
                    var cell = cells[i];
                    if (cell.Cell < 0 || cell.Cell >= count)
                    {
                        continue;
                    }

                    var value = math.saturate(cell.Value);
                    var color = math.lerp(pillar.ColdColor, pillar.HotColor, value);

                    var s = states[cell.Cell];
                    var targetDepth = value * pillar.MaxDepth;
                    if (targetDepth < s.TargetDepth)
                    {
                        continue;
                    }

                    s.TargetDepth = targetDepth;
                    s.TargetColor = color;
                    states[cell.Cell] = s;
                }

                var lerpT = math.saturate(dt * pillar.TransitionSpeed);

                for (var i = 0; i < count; i++)
                {
                    var s = states[i];
                    s.CurrentDepth = math.lerp(s.CurrentDepth, s.TargetDepth, lerpT);
                    s.CurrentColor = math.lerp(s.CurrentColor, s.TargetColor, lerpT);
                    states[i] = s;
                }
            }
        }
    }
}
