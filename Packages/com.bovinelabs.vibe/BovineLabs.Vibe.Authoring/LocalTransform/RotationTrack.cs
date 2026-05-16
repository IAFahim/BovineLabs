// <copyright file="RotationTrack.cs" company="BovineLabs">
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
    /// Timeline track that evaluates DOTS rotation clips.
    /// </summary>
    [Serializable]
#if BL_REACTION
    [TrackClipType(typeof(RotationLookAtTargetClip))]
#endif
    [TrackClipType(typeof(RotationLookAtStartClip))]
    [TrackClipType(typeof(RotationLookAtDirectionClip))]
    [TrackClipType(typeof(RotationLookAtRotationClip))]
    [TrackClipType(typeof(RotationShakeClip))]
    [TrackClipType(typeof(RotationWiggleClip))]
    [TrackClipType(typeof(RotationSpringClip))]
    [TrackClipType(typeof(RotationInitialClip))]
    [TrackColor(0, 0.25f, 0)]
    [TrackBindingType(typeof(Transform))]
    [DisplayName("DOTS/Transform/Rotation Track")]
    public class RotationTrack : DOTSTrack
    {
        protected override void Bake(BakingContext context)
        {
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic);
            context.Baker.AddComponent<LocalTransformInitial>(context.TrackEntity);
        }
    }
}
