// <copyright file="VolumeSettingsTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Volume
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Volume;
    using UnityEngine.Rendering;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends volume settings clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(VolumeSettingsClip))]
    [TrackClipType(typeof(VolumeSettingsInitialClip))]
    [TrackBindingType(typeof(Volume))]
    [TrackColor(0.35f, 0.55f, 0.85f)]
    [DisplayName("DOTS/Rendering/Volume Settings Track")]
    public class VolumeSettingsTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<VolumeSettingsInitial>(context.TrackEntity);
        }
    }
}
#endif
#endif
