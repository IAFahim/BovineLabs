using BovineLabs.Core;
using BovineLabs.Quill;
using BovineLabs.Reaction.Data.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Targets.Debug
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct TargetsDebugSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();

            state.Dependency = new DrawTargetsJob
            {
                Drawer = drawer,
                LtwLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                TargetsCustomLookup = SystemAPI.GetComponentLookup<TargetsCustom>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct DrawTargetsJob : IJobEntity
        {
            public Drawer Drawer;
            [ReadOnly] public ComponentLookup<LocalToWorld> LtwLookup;
            [ReadOnly] public ComponentLookup<TargetsCustom> TargetsCustomLookup;

            // Vibrant, distinct colors for each target type
            private static readonly Color ColorOwner = new Color(0.2f, 0.8f, 1.0f); // Cyan
            private static readonly Color ColorSource = new Color(1.0f, 0.6f, 0.1f); // Orange
            private static readonly Color ColorTarget = new Color(1.0f, 0.2f, 0.4f); // Red/Pink
            private static readonly Color ColorCustom0 = new Color(0.4f, 1.0f, 0.4f); // Green
            private static readonly Color ColorCustom1 = new Color(0.8f, 0.3f, 1.0f); // Purple

            private void Execute(Entity entity, in LocalToWorld ltw, in BovineLabs.Reaction.Data.Core.Targets targets)
            {
                int nullCount = 0; // Used to stack "Missing" text cleanly
                
                // 1. Draw Standard Targets
                DrawTether(entity, ltw.Position, targets.Owner, "Owner", ColorOwner, 0, ref nullCount);
                DrawTether(entity, ltw.Position, targets.Source, "Source", ColorSource, 1, ref nullCount);
                DrawTether(entity, ltw.Position, targets.Target, "Target", ColorTarget, 2, ref nullCount);

                // 2. Draw Custom Targets (if they exist)
                if (TargetsCustomLookup.TryGetComponent(entity, out var custom))
                {
                    DrawTether(entity, ltw.Position, custom.Target0, "Custom0", ColorCustom0, 3, ref nullCount);
                    DrawTether(entity, ltw.Position, custom.Target1, "Custom1", ColorCustom1, 4, ref nullCount);
                }
            }

            private void DrawTether(Entity self, float3 selfPos, Entity target, FixedString32Bytes label, Color color, int index, ref int nullCount)
            {
                // Handle Null / Unassigned
                if (target == Entity.Null)
                {
                    // Draw a dimmed, stacked text above the entity indicating what's missing
                    var dimColor = color;
                    dimColor.a = 0.4f;
                    float3 nullPos = selfPos + new float3(0, 0.8f + (nullCount * 0.25f), 0);
                    Drawer.Text32(nullPos, $"[No {label}]", dimColor, 10f);
                    nullCount++;
                    return;
                }

                // Handle Missing Transforms
                if (!LtwLookup.TryGetComponent(target, out var targetLtw))
                {
                    float3 errPos = selfPos + new float3(0, 0.8f + (nullCount * 0.25f), 0);
                    Drawer.Text32(errPos, $"[{label} has no Transform]", Color.red, 10f);
                    nullCount++;
                    return;
                }

                var targetPos = targetLtw.Position;

                // Handle Self-Looping (The target IS this entity)
                if (self == target || math.all(selfPos == targetPos))
                {
                    DrawSelfLoop(selfPos, label, color, index);
                    return;
                }

                // Handle Standard Connections (Beautiful Bezier Curves)
                DrawCurvedTether(selfPos, targetPos, label, color, index);
            }

            private void DrawCurvedTether(float3 start, float3 end, FixedString32Bytes label, Color color, int index)
            {
                var distance = math.distance(start, end);
                var mid = (start + end) * 0.5f;
                
                // Arc upwards based on distance. Offset slightly by index so lines don't perfectly overlap
                mid.y += (distance * 0.2f) + (index * 0.1f); 

                const int segments = 16;
                var lines = new NativeList<float3>(segments * 2, Allocator.Temp);
                var prev = start;

                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    // Quadratic Bezier interpolation
                    float3 current = math.lerp(math.lerp(start, mid, t), math.lerp(mid, end, t), t);
                    
                    lines.Add(prev);
                    lines.Add(current);
                    prev = current;
                }

                Drawer.Lines(lines.AsArray(), color);
                
                // Draw an arrowhead at the exact end pointing in the trajectory of the final segment
                var dir = math.normalize(end - lines[lines.Length - 4]);
                Drawer.Arrow(end - (dir * 0.1f), dir * 0.25f, color);

                // Draw the label at the peak of the arc
                Drawer.Text32(mid + new float3(0, 0.2f, 0), label, color, 11f);
                lines.Dispose();
            }

            private void DrawSelfLoop(float3 pos, FixedString32Bytes label, Color color, int index)
            {
                // Create a beautiful cubic bezier "leaf" shape overhead
                float height = 1.0f + (index * 0.3f);
                float spread = 0.5f + (index * 0.1f);

                float3 p0 = pos;
                float3 p1 = pos + new float3(spread, height, 0);
                float3 p2 = pos + new float3(-spread, height, 0);
                float3 p3 = pos;

                const int segments = 16;
                var lines = new NativeList<float3>(segments * 2, Allocator.Temp);
                var prev = p0;

                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    float u = 1 - t;
                    
                    // Cubic Bezier Formula
                    float3 current = (u * u * u * p0) + 
                                     (3 * u * u * t * p1) + 
                                     (3 * u * t * t * p2) + 
                                     (t * t * t * p3);

                    lines.Add(prev);
                    lines.Add(current);
                    prev = current;
                }

                Drawer.Lines(lines.AsArray(), color);

                // Arrow at the end of the loop to show direction
                var dir = math.normalize(p3 - lines[lines.Length - 4]);
                Drawer.Arrow(pos - (dir * 0.05f), dir * 0.2f, color);

                // Label at the very top of the loop
                float3 topPos = pos + new float3(0, height + 0.1f, 0);
                Drawer.Text32(topPos, label, color, 10f);
                lines.Dispose();
            }
        }
    }
}