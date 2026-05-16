// <copyright file="ScaleTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that evaluates DOTS scale clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(ScaleConstantClip))]
    [TrackClipType(typeof(ScaleOffsetClip))]
    [TrackClipType(typeof(ScaleCurveClip))]
    [TrackClipType(typeof(ScaleShakeClip))]
    [TrackClipType(typeof(ScaleWiggleClip))]
    [TrackClipType(typeof(ScaleSpringClip))]
    [TrackClipType(typeof(ScaleInitialClip))]
    [TrackColor(0, 0, 0.25f)]
    [TrackBindingType(typeof(Transform))]
    [DisplayName("DOTS/Transform/Scale Track")]
    public class ScaleTrack : DOTSTrack
    {
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic);
            context.Baker.AddComponent<LocalTransformInitial>(context.TrackEntity);
        }
    }
}
