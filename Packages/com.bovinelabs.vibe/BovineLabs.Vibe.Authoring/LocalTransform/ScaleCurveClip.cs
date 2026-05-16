// <copyright file="ScaleCurveClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Drives the binding scale by sampling a single animation curve.
    /// </summary>
    [Serializable]
    public class ScaleCurveClip : ScaleClipBase
    {
        [Tooltip("Curve evaluated to produce the scale offset over time.")]
        [SerializeField]
        private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 0);

        [Tooltip(Strings.UseClipActivationTooltip)]
        [SerializeField]
        private bool useClipActivation = true;

        [Tooltip(Strings.CurveStretchTooltip)]
        [SerializeField]
        private bool remapCurveToClipLength;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref ScaleClipBlob blob)
        {
            blob.Type = ScaleType.Curve;
            blob.Curve = new ScaleClipBlob.CurveData
            {
                TransformOnClipActivation = this.useClipActivation,
            };

            if (this.curve == null || this.curve.length == 0)
            {
                return;
            }

            var curveToBake = this.curve;

            if (this.remapCurveToClipLength)
            {
                var clip = context.Clip!;
                var clipDuration = (float)(clip.duration * clip.timeScale);
                if (CurveRemapUtility.TryRemapToClipLength(this.curve, (float)clip.clipIn, clipDuration, out var remappedCurve))
                {
                    curveToBake = remappedCurve;
                }
            }

            BlobCurve.Construct(ref builder, ref blob.Curve.Curve, curveToBake);
            context.Baker.AddComponent(clipEntity, ClipBlobCurveCache.Create());
        }
    }
}
