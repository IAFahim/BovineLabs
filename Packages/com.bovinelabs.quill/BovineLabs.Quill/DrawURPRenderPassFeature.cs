// <copyright file="DrawURPRenderPassFeature.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
#define BL_DRAW_ENABLED
#endif

#if UNITY_URP && BL_DRAW_ENABLED
namespace BovineLabs.Quill
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.RenderGraphModule;
    using UnityEngine.Rendering.Universal;

    internal class DrawURPRenderPassFeature : ScriptableRendererFeature
    {
        private DrawRenderPass? scriptablePass;

        public override void Create()
        {
            this.scriptablePass = new DrawRenderPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing - 1,
            };
        }

        public void SetSystem(DrawSystem system)
        {
            this.scriptablePass!.System = system;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            this.AddRenderPasses(renderer);
        }

        public void AddRenderPasses(ScriptableRenderer renderer)
        {
            renderer.EnqueuePass(this.scriptablePass);
        }

        private void OnDisable()
        {
            this.scriptablePass?.Dispose();
        }

        private class DrawRenderPass : ScriptableRenderPass, IDisposable
        {
            private CommandBuffer? commandBuffer;

            public DrawRenderPass()
            {
                this.profilingSampler = new ProfilingSampler("Draw");
            }

            public DrawSystem? System { get; set; }

            public void Dispose()
            {
                this.commandBuffer?.Dispose();
                this.commandBuffer = null;
            }

            // [Obsolete]
            // public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            // {
            //     this.commandBuffer ??= new CommandBuffer();
            //
            //     this.commandBuffer.Clear();
            //     this.System?.SubmitFrame(renderingData.cameraData.camera!, new CommandBufferWrapper(this.commandBuffer));
            //     context.ExecuteCommandBuffer(this.commandBuffer);
            // }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                using var builder = renderGraph.AddRasterRenderPass("Draw", out Data passData, profilingSampler);

                if (Application.isEditor && (cameraData.cameraType & (CameraType.SceneView | CameraType.Preview)) != 0)
                {
                    builder.AllowGlobalStateModification(true);
                }

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                passData.Camera = cameraData.camera;

                builder.SetRenderFunc((Data data, RasterGraphContext context) =>
                    this.System?.SubmitFrame(data.Camera!, new RasterCommandBufferWrapper(context.cmd)));
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
            }

            private class Data
            {
                public Camera? Camera;
            }
        }
    }
}
#endif
