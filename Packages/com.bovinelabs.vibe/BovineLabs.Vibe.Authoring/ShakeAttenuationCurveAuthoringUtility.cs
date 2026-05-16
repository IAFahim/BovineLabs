// <copyright file="ShakeAttenuationCurveAuthoringUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    internal static class ShakeAttenuationCurveAuthoringUtility
    {
        public static bool TryConstructAttenuationCurve(
            ref BlobBuilder builder, ref BlobCurve blobCurve, bool useCurve, AnimationCurve attenuationCurve, bool remapToClipLength, TimelineClip clip)
        {
            if (!useCurve || attenuationCurve == null || attenuationCurve.length == 0)
            {
                return false;
            }

            var curveToBake = attenuationCurve;

            if (remapToClipLength && clip != null)
            {
                var clipDuration = (float)(clip.duration * clip.timeScale);
                if (clipDuration > Mathf.Epsilon && CurveRemapUtility.IsClampWrapMode(attenuationCurve) &&
                    CurveRemapUtility.TryRemapToClipLength(attenuationCurve, (float)clip.clipIn, clipDuration, out var clipSpaceCurve))
                {
                    curveToBake = NormalizeClipCurve(clipSpaceCurve, (float)clip.clipIn, clipDuration);
                }
                else
                {
                    curveToBake = NormalizeCurve(curveToBake);
                }
            }
            else
            {
                curveToBake = NormalizeCurve(curveToBake);
            }

            BlobCurve.Construct(ref builder, ref blobCurve, curveToBake);
            return true;
        }

        private static AnimationCurve NormalizeCurve(AnimationCurve curve)
        {
            var keys = curve.keys;
            if (keys.Length == 0)
            {
                return curve;
            }

            var firstTime = keys[0].time;
            var lastTime = keys[^1].time;
            var duration = lastTime - firstTime;

            var remappedKeys = new Keyframe[keys.Length];
            if (Mathf.Approximately(duration, 0f))
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    key.time = 0f;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    remappedKeys[i] = key;
                }
            }
            else
            {
                var inverseDuration = 1f / duration;
                for (var i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    key.time = (key.time - firstTime) * inverseDuration;

                    if (!float.IsInfinity(key.inTangent))
                    {
                        key.inTangent /= inverseDuration;
                    }

                    if (!float.IsInfinity(key.outTangent))
                    {
                        key.outTangent /= inverseDuration;
                    }

                    remappedKeys[i] = key;
                }
            }

            return new AnimationCurve(remappedKeys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode,
            };
        }

        private static AnimationCurve NormalizeClipCurve(AnimationCurve curve, float clipIn, float clipDuration)
        {
            var keys = curve.keys;
            if (keys.Length == 0)
            {
                return curve;
            }

            var remappedKeys = new Keyframe[keys.Length];
            if (clipDuration <= Mathf.Epsilon)
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    key.time = 0f;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    remappedKeys[i] = key;
                }
            }
            else
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    key.time = (key.time - clipIn) / clipDuration;

                    if (!float.IsInfinity(key.inTangent))
                    {
                        key.inTangent *= clipDuration;
                    }

                    if (!float.IsInfinity(key.outTangent))
                    {
                        key.outTangent *= clipDuration;
                    }

                    remappedKeys[i] = key;
                }
            }

            return new AnimationCurve(remappedKeys)
            {
                preWrapMode = curve.preWrapMode,
                postWrapMode = curve.postWrapMode,
            };
        }
    }
}
