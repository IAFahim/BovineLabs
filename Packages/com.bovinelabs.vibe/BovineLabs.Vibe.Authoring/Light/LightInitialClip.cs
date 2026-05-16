// <copyright file="LightInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Light
{
    using System;
    using BovineLabs.Vibe.Data.Light;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that restores the bound light to its captured initial state.
    /// </summary>
    [Serializable]
    public class LightInitialClip : LightClipBase
    {
        /// <inheritdoc/>
        public override ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        protected override LightClipType Type => LightClipType.Initial;
    }
}
#endif
