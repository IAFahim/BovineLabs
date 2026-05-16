// <copyright file="PositionOrbitClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
#if BL_REACTION
    using BovineLabs.Reaction.Data.Core;
#endif
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Orbits the bound transform around a target using optional angle and radius animation.
    /// </summary>
    [Serializable]
    public class PositionOrbitClip : PositionClipBase
    {
        private const float DegreesToRadians = math.PI / 180f;

#if BL_REACTION
        [Tooltip("Target binding used as the orbit pivot.")]
        [SerializeField]
        private Target target = Target.Target;
#endif

        [Tooltip("Offset applied to the pivot position before orbiting.")]
        [SerializeField]
        private SpaceVector3 pivotOffset = SpaceVector3.Local(Vector3.zero);

        [Tooltip("Axis of rotation expressed in the chosen transform space.")]
        [SerializeField]
        private SpaceVector3 axis = SpaceVector3.World(Vector3.up);

        [Tooltip("Use a manually specified initial offset instead of capturing the current position.")]
        [SerializeField]
        private bool useCustomInitialOffset;

        [Tooltip("Initial offset from the pivot when the clip activates.")]
        [SerializeField]
        private SpaceVector3 initialOffset = SpaceVector3.Local(Vector3.forward);

        [Min(0f)]
        [Tooltip("Base orbit radius applied in addition to the radius curve.")]
        [SerializeField]
        private float radius = 1f;

        [SerializeField]
        [Tooltip("Degrees per second applied in addition to the angle curve.")]
        private float angularSpeed = 90f;

        [Tooltip("Enable sampling of the angle curve.")]
        [SerializeField]
        private bool animateAngle;

        [Tooltip("Angle curve evaluated in degrees over the clip duration.")]
        [SerializeField]
        private AnimationCurve angleCurve = AnimationCurve.Linear(0f, 0f, 1f, 360f);

        [Tooltip("Stretch the angle curve so it spans the playable clip length.")]
        [SerializeField]
        private bool remapAngleToClipLength = true;

        [Tooltip("Enable sampling of the radius curve.")]
        [SerializeField]
        private bool animateRadius;

        [Tooltip("Radius curve evaluated over the clip duration.")]
        [SerializeField]
        private AnimationCurve radiusCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Tooltip("Stretch the radius curve so it spans the playable clip length.")]
        [SerializeField]
        private bool remapRadiusToClipLength;

        [Tooltip(Strings.UseClipActivationTooltip)]
        [SerializeField]
        private bool useClipActivation = true;

        protected override void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob)
        {
            blob.Type = PositionType.Orbit;
            blob.TransformOnClipActivation = this.useClipActivation;

            var axisValue = math.normalizesafe(this.axis.Value, math.up());

            blob.Orbit = new PositionClipBlob.OrbitData
            {
#if BL_REACTION
                Target = this.target,
#endif
                PivotSpace = this.pivotOffset.Space,
                AxisSpace = this.axis.Space,
                InitialOffsetSpace = this.initialOffset.Space,
                UseCustomInitialOffset = this.useCustomInitialOffset,
                Radius = math.max(0f, this.radius),
                AngularSpeed = math.radians(this.angularSpeed),
                PivotOffset = this.pivotOffset.Value,
                Axis = axisValue,
                InitialOffset = this.initialOffset.Value,
            };

            var hasCurve = BakeCurve(ref blob.Orbit.AngleCurve, this.animateAngle, this.angleCurve, DegreesToRadians, ref builder, this.remapAngleToClipLength, context.Clip);
            hasCurve |= BakeCurve(ref blob.Orbit.RadiusCurve, this.animateRadius, this.radiusCurve, 1f, ref builder, this.remapRadiusToClipLength, context.Clip);

            if (hasCurve)
            {
                context.Baker.AddComponent(clipEntity, ClipBlobCurveCache.Create());
            }
        }

        private static bool BakeCurve(ref BlobCurve blobCurve, bool enabled, AnimationCurve curve, float valueScale, ref BlobBuilder builder, bool remap,
            TimelineClip clip)
        {
            if (!enabled || curve == null || curve.length == 0)
            {
                return false;
            }

            AnimationCurve curveToBake = curve;

            if (remap && clip != null && CurveRemapUtility.TryRemapToClipLength(curve, (float)clip.clipIn, (float)(clip.duration * clip.timeScale), out var remapped))
            {
                curveToBake = remapped;
            }

            if (!Mathf.Approximately(valueScale, 1f))
            {
                curveToBake = ScaleCurve(curveToBake, valueScale);
            }

            BlobCurve.Construct(ref builder, ref blobCurve, curveToBake);
            return true;
        }

        private static AnimationCurve ScaleCurve(AnimationCurve source, float scale)
        {
            var keys = source.keys;
            var scaledKeys = new Keyframe[keys.Length];

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                key.value *= scale;

                if (!float.IsInfinity(key.inTangent))
                {
                    key.inTangent *= scale;
                }

                if (!float.IsInfinity(key.outTangent))
                {
                    key.outTangent *= scale;
                }

                scaledKeys[i] = key;
            }

            return new AnimationCurve(scaledKeys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode,
            };
        }

    }
}
