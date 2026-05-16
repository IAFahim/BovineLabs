// <copyright file="CMSplineDollyTargetClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Splines;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that assigns a spline dolly target without blending.
    /// </summary>
    [Serializable]
    public class CMSplineDollyTargetClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Spline container used while the clip is active.")]
        private ExposedReference<SplineContainer> spline;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            var splineEntity = Entity.Null;
            if (context.Director != null)
            {
                var resolved = context.Director.GetReferenceValue(this.spline.exposedName, out _) as SplineContainer;
                if (resolved != null)
                {
                    splineEntity = context.Baker.GetEntity(resolved, TransformUsageFlags.None);
                }
            }

            context.Baker.AddComponent(clipEntity, new CMSplineDollyTargetClipData { Spline = splineEntity });
        }
    }
}
#endif
