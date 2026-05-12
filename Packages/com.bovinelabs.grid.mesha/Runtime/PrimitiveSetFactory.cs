using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA
{
    /// <summary>
    /// Builds a simple set of motion primitives for 8-directional grid movement.
    /// Each primitive moves 1-2 cells in a cardinal or diagonal direction.
    /// For production use, replace with your own kinodynamically-feasible primitives.
    /// </summary>
    [BurstCompile]
    public static class PrimitiveSetFactory
    {
        /// <summary>
        /// Create a basic 8-direction primitive set with 1-cell moves.
        /// Each heading maps to one primitive.
        /// 8 headings: N=0, NE=1, E=2, SE=3, S=4, SW=5, W=6, NW=7
        /// </summary>
        public static PrimitiveSet CreateCardinal8(Allocator allocator)
        {
            var set = new PrimitiveSet(8, allocator);
            int2[] offsets = {
                new int2(0, -1),   // N
                new int2(1, -1),   // NE
                new int2(1, 0),    // E
                new int2(1, 1),    // SE
                new int2(0, 1),    // S
                new int2(-1, 1),   // SW
                new int2(-1, 0),   // W
                new int2(-1, -1),  // NW
            };

            for (int theta = 0; theta < 8; theta++)
            {
                var offset = offsets[theta];
                float length = math.length(new float2(offset.x, offset.y));

                var sweptI = new NativeArray<int>(1, allocator);
                var sweptJ = new NativeArray<int>(1, allocator);
                sweptI[0] = offset.x;
                sweptJ[0] = offset.y;

                var prim = new MotionPrimitive(
                    id: theta,
                    startTheta: theta,
                    goalOffset: offset,
                    goalTheta: theta,
                    arcLength: length,
                    headingChange: 0f,
                    sweptI: sweptI,
                    sweptJ: sweptJ
                );
                set.Add(prim);
            }
            return set;
        }

        /// <summary>
        /// Create a richer primitive set: 8 headings × 3 primitives each (short/medium/long).
        /// Short = 1 cell, Medium = 2 cells straight, Long = 2 cells with slight turn.
        /// Total: 24 primitives.
        /// </summary>
        public static PrimitiveSet CreateExtended8(Allocator allocator)
        {
            var set = new PrimitiveSet(24, allocator);
            int2[] dirs = {
                new int2(0, -1), new int2(1, -1), new int2(1, 0), new int2(1, 1),
                new int2(0, 1), new int2(-1, 1), new int2(-1, 0), new int2(-1, -1),
            };

            int primId = 0;
            for (int theta = 0; theta < 8; theta++)
            {
                var dir = dirs[theta];
                float baseLen = math.length(new float2(dir.x, dir.y));

                // Short: 1-cell move in current heading
                var si1 = new NativeArray<int>(1, allocator);
                var sj1 = new NativeArray<int>(1, allocator);
                si1[0] = dir.x; sj1[0] = dir.y;
                set.Add(new MotionPrimitive(primId++, theta, dir, theta, baseLen, 0f, si1, sj1));

                // Medium: 2-cell straight move
                var si2 = new NativeArray<int>(2, allocator);
                var sj2 = new NativeArray<int>(2, allocator);
                si2[0] = dir.x; sj2[0] = dir.y;
                si2[1] = dir.x * 2; sj2[1] = dir.y * 2;
                set.Add(new MotionPrimitive(primId++, theta, dir * 2, theta, baseLen * 2, 0f, si2, sj2));

                // Long: 2-cell move ending one heading CW from current
                int nextTheta = (theta + 1) % 8;
                var nextDir = dirs[nextTheta];
                var endOff = dir + nextDir;
                var si3 = new NativeArray<int>(3, allocator);
                var sj3 = new NativeArray<int>(3, allocator);
                si3[0] = dir.x; sj3[0] = dir.y;
                si3[1] = dir.x + nextDir.x; sj3[1] = dir.y + nextDir.y;
                si3[2] = endOff.x; sj3[2] = endOff.y;
                float arcLen = math.length(new float2(endOff.x, endOff.y));
                set.Add(new MotionPrimitive(primId++, theta, endOff, nextTheta, arcLen, math.PI / 4f, si3, sj3));
            }
            return set;
        }
    }
}
