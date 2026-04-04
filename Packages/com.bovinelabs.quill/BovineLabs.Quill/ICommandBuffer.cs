// <copyright file="ICommandBuffer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using UnityEngine;
    using UnityEngine.Rendering;

    public interface ICommandBuffer
    {
        void DrawProcedural(Material material, MeshTopology meshTopology, int vertexCount);
    }

    public readonly struct CommandBufferWrapper : ICommandBuffer
    {
        private readonly CommandBuffer commandBuffer;

        public CommandBufferWrapper(CommandBuffer commandBuffer)
        {
            this.commandBuffer = commandBuffer;
        }

        public void DrawProcedural(Material material, MeshTopology meshTopology, int vertexCount)
        {
            this.commandBuffer.DrawProcedural(Matrix4x4.identity, material, 0, meshTopology, vertexCount);
        }
    }

#if UNITY_URP
    public readonly struct RasterCommandBufferWrapper : ICommandBuffer
    {
        private readonly RasterCommandBuffer commandBuffer;

        public RasterCommandBufferWrapper(RasterCommandBuffer commandBuffer)
        {
            this.commandBuffer = commandBuffer;
        }

        public void DrawProcedural(Material material, MeshTopology meshTopology, int vertexCount)
        {
            this.commandBuffer.DrawProcedural(Matrix4x4.identity, material, 0, meshTopology, vertexCount);
        }
    }
#endif
}
