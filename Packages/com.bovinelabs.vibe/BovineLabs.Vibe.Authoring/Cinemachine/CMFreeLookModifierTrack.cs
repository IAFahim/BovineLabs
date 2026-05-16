// <copyright file="CMFreeLookModifierTrack.cs" company="BovineLabs">
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
    /// Timeline track that blends Cinemachine free look modifier clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CMFreeLookModifierClip))]
    [TrackClipType(typeof(CMFreeLookModifierInitialClip))]
    [TrackBindingType(typeof(CinemachineFreeLookModifier))]
    [TrackColor(0.55f, 0.4f, 0.7f)]
    [DisplayName("DOTS/Cinemachine/FreeLook Modifier Track")]
    public class CMFreeLookModifierTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<CMFreeLookModifierInitial>(context.TrackEntity);
        }
    }
}
#endif
