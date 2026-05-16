// <copyright file="NonUniformScaleTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that evaluates non-uniform scale clips in DOTS.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(SquashStretchConstantClip))]
    [TrackClipType(typeof(SquashStretchCurveClip))]
    [TrackClipType(typeof(SquashStretchShakeClip))]
    [TrackClipType(typeof(SquashStretchSpringClip))]
    [TrackClipType(typeof(NonUniformScaleInitialClip))]
    [TrackColor(0.25f, 0f, 0.5f)]
    [TrackBindingType(typeof(Transform))]
    [DisplayName("DOTS/Transform/Non-Uniform Scale Track")]
    public class NonUniformScaleTrack : DOTSTrack
    {
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddComponent<PostTransformMatrixInitial>(context.TrackEntity);
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic | TransformUsageFlags.NonUniformScale);
        }
    }
}
