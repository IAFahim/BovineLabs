// <copyright file="LightTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Light
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Light;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that blends light clips and writes into ECS light data.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(LightConstantClip))]
    [TrackClipType(typeof(LightInitialClip))]
    [TrackClipType(typeof(LightFlickerClip))]
    [TrackClipType(typeof(LightExtendedConstantClip))]
    [TrackClipType(typeof(LightExtendedCurveClip))]
    [TrackBindingType(typeof(Light))]
    [TrackColor(1f, 0.8f, 0.2f)]
    [DisplayName("DOTS/Rendering/Light Track")]
    public class LightTrack : DOTSTrack
    {
        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<LightInitial>(context.TrackEntity);
            context.Baker.AddComponent<LightExtendedInitial>(context.TrackEntity);
        }
    }
}
#endif
