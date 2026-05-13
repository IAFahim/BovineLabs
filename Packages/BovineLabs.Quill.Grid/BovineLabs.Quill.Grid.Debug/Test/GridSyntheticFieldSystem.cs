using BovineLabs.Core;
using BovineLabs.Quill.Grid.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation |
                       WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation |
                       WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct GridSyntheticFieldSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridSyntheticFieldConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (visualizer, global, config, members, cells, vectors, texts) in
                     SystemAPI.Query<
                         GridVisualizerData,
                         GridVisualizerGlobal,
                         GridSyntheticFieldConfig,
                         DynamicBuffer<GridSyntheticMember>,
                         DynamicBuffer<GridCellVisual>,
                         DynamicBuffer<GridVectorFieldVisual>,
                         DynamicBuffer<GridTextVisual>>())
            {
                if (!global.Enabled)
                {
                    continue;
                }

                cells.Clear();
                vectors.Clear();
                texts.Clear();

                var width = visualizer.GridWidth;
                var height = visualizer.GridHeight;
                var length = width * height;
                var converter = new GridCoordinateConverter(
                    visualizer.Origin,
                    visualizer.CellSize,
                    visualizer.GridWidth,
                    visualizer.GridHeight);
                var labelStride = math.max(1, width / 4);

                for (var index = 0; index < length; index++)
                {
                    var cell = new int2(index % width, index / width);
                    var cellCenter = new float2(cell.x + 0.5f, cell.y + 0.5f);

                    var value = CalculateFieldValue(
                        cellCenter,
                        members,
                        config,
                        time,
                        out var direction);

                    cells.Add(new GridCellVisual(
                        index,
                        value,
                        GridCellVisual.LayerHeatmap));

                    if (config.WriteVectorField && math.lengthsq(direction) > 0.0001f)
                    {
                        vectors.Add(new GridVectorFieldVisual(
                            index,
                            math.normalizesafe(direction),
                            math.saturate(value)));
                    }

                    if (config.WriteLabels && cell.x % labelStride == 0 && cell.y % labelStride == 0)
                    {
                        var world = converter.CellCenter(index);

                        world.y += 0.4f;

                        var text = new FixedString64Bytes();
                        text.Append((int)math.round(value * 100f));

                        texts.Add(new GridTextVisual(
                            world,
                            text,
                            new float4(1f, 1f, 1f, 1f)));
                    }
                }
            }
        }

        private static float CalculateFieldValue(
            float2 cellCenter,
            DynamicBuffer<GridSyntheticMember> members,
            GridSyntheticFieldConfig config,
            float time,
            out float2 direction)
        {
            var total = 0f;
            direction = float2.zero;

            for (var i = 0; i < members.Length; i++)
            {
                var member = members[i];

                var memberPos = member.BasePosition;
                if (config.AnimateMembers)
                {
                    var phase = time * config.MemberMotionSpeed + member.Phase;
                    memberPos += new float2(
                        math.cos(phase),
                        math.sin(phase * 0.73f)) * member.MotionRadius;
                }

                var toCell = cellCenter - memberPos;
                var dist = math.length(toCell);
                var t = math.saturate(1f - dist / member.Radius);

                var influence = math.pow(t, math.max(0.001f, config.MemberFalloffPower)) * member.Strength;
                total += influence;

                if (dist > 0.001f)
                {
                    direction += math.normalize(toCell) * influence;
                }
            }

            var wave = math.sin((cellCenter.x * 0.45f) + (cellCenter.y * 0.28f) + time * 1.5f);
            total += (wave * 0.5f + 0.5f) * config.BaseWaveStrength;

            return math.saturate(total);
        }
    }
}
