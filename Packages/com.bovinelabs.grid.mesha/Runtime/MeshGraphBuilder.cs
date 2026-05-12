using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Grid.MeshA
{
    [BurstCompile]
    public static class MeshGraphBuilder
    {
        public const int NumHeadings = 8;

        public static MeshGraphData Build(in PrimitiveSet primSet, Allocator allocator)
        {
            var mesh = new MeshGraphData(NumHeadings, NumHeadings, allocator);

            // Step 1: Map each heading to its initial config (identity mapping)
            for (int theta = 0; theta < NumHeadings; theta++)
            {
                mesh.InitialConfigByTheta[theta] = theta;
                mesh.ThetaByInitialConfig[theta] = theta;
            }

            // Step 2: Build successors for each config into flat arrays
            // First pass: count successors per config
            var tempLists = new NativeArray<NativeList<SuccessorTransition>>(NumHeadings, Allocator.Temp);
            for (int i = 0; i < NumHeadings; i++)
                tempLists[i] = new NativeList<SuccessorTransition>(4, Allocator.Temp);

            for (int theta = 0; theta < NumHeadings; theta++)
            {
                if (primSet.PrimsByHeading.TryGetFirstValue(theta, out int primIdx, out var it))
                {
                    do
                    {
                        var prim = primSet.Primitives[primIdx];
                        int nextConfig = prim.GoalTheta;
                        tempLists[theta].Add(new SuccessorTransition(
                            prim.GoalOffset.x, prim.GoalOffset.y, nextConfig, prim.Id));
                    } while (primSet.PrimsByHeading.TryGetNextValue(out primIdx, ref it));
                }
            }

            // Compute total and offsets
            int total = 0;
            for (int i = 0; i < NumHeadings; i++) total += tempLists[i].Length;

            mesh.SuccessorsFlat = new NativeArray<SuccessorTransition>(total, allocator);
            int offset = 0;
            for (int i = 0; i < NumHeadings; i++)
            {
                mesh.SuccOffsets[i] = offset;
                mesh.SuccCounts[i] = tempLists[i].Length;
                for (int j = 0; j < tempLists[i].Length; j++)
                    mesh.SuccessorsFlat[offset + j] = tempLists[i][j];
                offset += tempLists[i].Length;
                tempLists[i].Dispose();
            }
            tempLists.Dispose();

            return mesh;
        }
    }
}
