// <copyright file="DrawHDRPCustomPass.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
#define BL_DRAW_ENABLED
#endif

#if UNITY_HDRP && BL_DRAW_ENABLED
namespace BovineLabs.Quill
{
    using UnityEngine.Profiling;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;

    internal class DrawHDRPCustomPass : CustomPass
    {
        private readonly DrawSystem? system;

        public DrawHDRPCustomPass(DrawSystem system)
        {
            this.system = system;
        }

        protected override void Execute(CustomPassContext context)
        {
            if (this.system == null)
            {
                return;
            }

            Profiler.BeginSample("Draw");
            this.system.SubmitFrame(context.hdCamera.camera, new CommandBufferWrapper(context.cmd));
            Profiler.EndSample();
        }

        protected override void Cleanup()
        {
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
        }
    }
}
#endif
