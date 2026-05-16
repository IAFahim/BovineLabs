// <copyright file="PositionTrack.cs" company="BovineLabs">
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
    /// Timeline track that evaluates DOTS position clips.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(PositionWorldClip))]
    [TrackClipType(typeof(PositionOffsetClip))]
#if BL_REACTION
    [TrackClipType(typeof(PositionTargetClip))]
#endif
    [TrackClipType(typeof(PositionCurveClip))]
    [TrackClipType(typeof(PositionShakeClip))]
    [TrackClipType(typeof(PositionWiggleClip))]
    [TrackClipType(typeof(PositionSpringClip))]
    [TrackClipType(typeof(PositionOrbitClip))]
    [TrackClipType(typeof(PositionInitialClip))]
    [TrackColor(0.25f, 0.25f, 0)]
    [TrackBindingType(typeof(Transform))]
    [DisplayName("DOTS/Transform/Position Track")]
    public class PositionTrack : DOTSTrack
    {
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic);
            context.Baker.AddComponent<LocalTransformInitial>(context.TrackEntity);
        }
    }
}
