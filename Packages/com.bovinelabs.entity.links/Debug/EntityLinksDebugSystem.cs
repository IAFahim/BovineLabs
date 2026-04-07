#if UNITY_EDITOR || BL_DEBUG
using BovineLabs.Core;
using BovineLabs.Quill;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace BovineLabs.EntityLinks.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct EntityLinksDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DrawSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();

            state.Dependency = new DrawEntityLinksJob
            {
                Drawer = drawer,
                LtwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct DrawEntityLinksJob : IJobEntity
        {
            public Drawer Drawer;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;

            private void Execute(Entity entity, in LocalToWorld ltw, in DynamicBuffer<EntityLookupResolveResult> results)
            {
                var nullCount = 0;

                for (var i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    var label = EntityLinkKeys.KeyToName(result.Key);
                    var color = GetColorForKey(result.Key);

                    var fullLabel = new FixedString64Bytes(label);
                    fullLabel.Append(" -> ");
                    fullLabel.Append(result.AssignedTo.ToString());

                    this.DrawTether(entity, ltw.Position, result.Value, fullLabel, color, i, ref nullCount);
                }
            }

            private static Color GetColorForKey(byte key)
            {
                var h = (key * 0.618033988749895f) % 1.0f;
                return Color.HSVToRGB(h, 0.8f, 0.9f);
            }

            private void DrawTether(Entity self, float3 selfPos, Entity target, FixedString64Bytes label, Color color, int index, ref int nullCount)
            {
                if (target == Entity.Null)
                {
                    var dimColor = color;
                    dimColor.a = 0.4f;
                    var nullPos = selfPos + new float3(0f, 0.8f + (nullCount * 0.25f), 0f);
                    this.Drawer.Text64(nullPos, $"[Failed: {label}]", dimColor, 10f);
                    nullCount++;
                    return;
                }

                if (!this.LtwLookup.TryGetComponent(target, out var targetLtw))
                {
                    var errPos = selfPos + new float3(0f, 0.8f + (nullCount * 0.25f), 0f);
                    this.Drawer.Text64(errPos, $"[{label} missing Transform]", Color.red, 10f);
                    nullCount++;
                    return;
                }

                var targetPos = targetLtw.Position;

                if (self == target || math.all(selfPos == targetPos))
                {
                    this.DrawSelfLoop(selfPos, label, color, index);
                    return;
                }

                this.DrawCurvedTether(selfPos, targetPos, label, color, index);
            }

            private void DrawCurvedTether(float3 start, float3 end, FixedString64Bytes label, Color color, int index)
            {
                var distance = math.distance(start, end);
                var mid = (start + end) * 0.5f;

                mid.y += (distance * 0.2f) + (index * 0.1f);

                const int segments = 16;
                var lines = new NativeList<float3>(segments * 2, Allocator.Temp);
                var prev = start;

                for (var i = 1; i <= segments; i++)
                {
                    var t = i / (float)segments;
                    var current = math.lerp(math.lerp(start, mid, t), math.lerp(mid, end, t), t);

                    lines.Add(prev);
                    lines.Add(current);
                    prev = current;
                }

                this.Drawer.Lines(lines.AsArray(), color);

                var dir = math.normalize(end - lines[lines.Length - 4]);
                this.Drawer.Arrow(end - (dir * 0.1f), dir * 0.25f, color);

                this.Drawer.Text64(mid + new float3(0f, 0.2f, 0f), label, color, 11f);
                lines.Dispose();
            }

            private void DrawSelfLoop(float3 pos, FixedString64Bytes label, Color color, int index)
            {
                var height = 1.0f + (index * 0.3f);
                var spread = 0.5f + (index * 0.1f);

                var p0 = pos;
                var p1 = pos + new float3(spread, height, 0f);
                var p2 = pos + new float3(-spread, height, 0f);
                var p3 = pos;

                const int segments = 16;
                var lines = new NativeList<float3>(segments * 2, Allocator.Temp);
                var prev = p0;

                for (var i = 1; i <= segments; i++)
                {
                    var t = i / (float)segments;
                    var u = 1f - t;

                    var current = (u * u * u * p0) +
                                     (3f * u * u * t * p1) +
                                     (3f * u * t * t * p2) +
                                     (t * t * t * p3);

                    lines.Add(prev);
                    lines.Add(current);
                    prev = current;
                }

                this.Drawer.Lines(lines.AsArray(), color);

                var dir = math.normalize(p3 - lines[lines.Length - 4]);
                this.Drawer.Arrow(pos - (dir * 0.05f), dir * 0.2f, color);

                var topPos = pos + new float3(0f, height + 0.1f, 0f);
                this.Drawer.Text64(topPos, label, color, 10f);
                lines.Dispose();
            }
        }
    }
}
#endif