// <copyright file="CMVolumeSettingsTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends Cinemachine volume settings clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMVolumeSettingsClip))]
    [TrackClipType(typeof(CMVolumeSettingsInitialClip))]
    [TrackBindingType(typeof(CinemachineVolumeSettings))]
    [TrackColor(0.6f, 0.6f, 0.2f)]
    [DisplayName("DOTS/Cinemachine/Volume Settings Track")]
    public class CMVolumeSettingsTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMVolumeSettingsInitial>(context.TrackEntity);
        }
    }
}
#endif
