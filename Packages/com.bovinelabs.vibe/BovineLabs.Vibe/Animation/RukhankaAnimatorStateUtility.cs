// <copyright file="RukhankaAnimatorStateUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Animation
{
    using System;
    using Rukhanka;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    internal static class RukhankaAnimatorStateUtility
    {
        private const float DefaultStateDuration = 1f;

        public static int GetStateIndex(in BlobAssetReference<ControllerBlob> controllerBlob, int layerIndex, uint stateHash)
        {
            if (!controllerBlob.IsCreated || stateHash == 0)
            {
                return -1;
            }

            if (layerIndex < 0 || layerIndex >= controllerBlob.Value.layers.Length)
            {
                return -1;
            }

            return ScriptedAnimator.GetStateIndexInControllerLayer(controllerBlob, layerIndex, stateHash);
        }

        public static float GetStateDurationSeconds(
            int layerIndex,
            int stateIndex,
            in BlobAssetReference<ControllerBlob> controllerBlob,
            in BlobAssetReference<ControllerAnimationsBlob> controllerAnimationsBlob,
            in NativeArray<AnimatorControllerLayerComponent> layers,
            in NativeArray<AnimatorControllerParameterComponent> parameters,
            in BlobDatabaseSingleton blobDatabase)
        {
            if (!controllerBlob.IsCreated || !controllerAnimationsBlob.IsCreated)
            {
                return DefaultStateDuration;
            }

            if (layerIndex < 0 || stateIndex < 0 || layerIndex >= controllerBlob.Value.layers.Length)
            {
                return DefaultStateDuration;
            }

            ref var layer = ref controllerBlob.Value.layers[layerIndex];
            if (stateIndex >= layer.states.Length)
            {
                return DefaultStateDuration;
            }

            ref var state = ref layer.states[stateIndex];
            var motionDuration = CalculateMotionDuration(ref state.motion, parameters, controllerAnimationsBlob, blobDatabase, 1f);

            if (layer.syncedLayerIndex >= 0 && layer.syncedLayerIndex < controllerBlob.Value.layers.Length)
            {
                ref var baseState = ref controllerBlob.Value.layers[layer.syncedLayerIndex].states[stateIndex];
                var baseDuration = CalculateMotionDuration(ref baseState.motion, parameters, controllerAnimationsBlob, blobDatabase, 1f);
                var weight = layerIndex < layers.Length ? layers[layerIndex].weight : 1f;
                var weightedDuration = math.lerp(baseDuration, motionDuration, weight);
                motionDuration = layer.syncedTiming > 0 ? weightedDuration : baseDuration;
            }
            else if (layer.syncedTiming >= 0 && layer.syncedTiming < controllerBlob.Value.layers.Length)
            {
                ref var syncedState = ref controllerBlob.Value.layers[layer.syncedTiming].states[stateIndex];
                var syncedDuration = CalculateMotionDuration(ref syncedState.motion, parameters, controllerAnimationsBlob, blobDatabase, 1f);
                var weight = layer.syncedTiming < layers.Length ? layers[layer.syncedTiming].weight : 1f;
                motionDuration = math.lerp(motionDuration, syncedDuration, weight);
            }

            var speedMultiplier = 1f;
            if (state.speedMultiplierParameterIndex >= 0 && state.speedMultiplierParameterIndex < parameters.Length)
            {
                speedMultiplier = parameters[state.speedMultiplierParameterIndex].FloatValue;
            }

            var speed = state.speed * speedMultiplier;
            var duration = motionDuration / speed;
            return math.select(DefaultStateDuration, duration, math.isfinite(duration));
        }

        private static float CalculateMotionDuration(
            ref MotionBlob motionBlob,
            in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
            in BlobAssetReference<ControllerAnimationsBlob> controllerAnimationsBlob,
            in BlobDatabaseSingleton blobDatabase,
            float weight)
        {
            if (weight == 0f)
            {
                return 0f;
            }

            switch (motionBlob.type)
            {
                case MotionBlob.Type.None:
                    return DefaultStateDuration;
                case MotionBlob.Type.AnimationClip:
                    if (controllerAnimationsBlob.Value.animations.Length <= motionBlob.animationIndex ||
                        motionBlob.animationIndex < 0 ||
                        !blobDatabase.animations.IsCreated)
                    {
                        return DefaultStateDuration;
                    }

                    var animationHash = controllerAnimationsBlob.Value.animations[motionBlob.animationIndex];
                    var animBlob = blobDatabase.GetAnimationClipBlob(animationHash);
                    if (animBlob != BlobAssetReference<AnimationClipBlob>.Null)
                    {
                        return animBlob.Value.length * weight;
                    }

                    return DefaultStateDuration;
            }

            var childMotions = GetChildMotionsList(ref motionBlob, runtimeParams);
            return CalculateBlendTreeMotionDuration(childMotions, ref motionBlob.blendTree.motions, runtimeParams, controllerAnimationsBlob, blobDatabase, weight);
        }

        private static float CalculateBlendTreeMotionDuration(
            NativeList<MotionIndexAndWeight> miwArr,
            ref BlobArray<ChildMotionBlob> motions,
            in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
            in BlobAssetReference<ControllerAnimationsBlob> controllerAnimationsBlob,
            in BlobDatabaseSingleton blobDatabase,
            float weight)
        {
            if (!miwArr.IsCreated || miwArr.IsEmpty)
            {
                if (miwArr.IsCreated)
                {
                    miwArr.Dispose();
                }

                return DefaultStateDuration;
            }

            var weightSum = 0.0f;
            for (var i = 0; i < miwArr.Length; ++i)
            {
                weightSum += miwArr[i].weight;
            }

            if (weightSum < 1f)
            {
                for (var i = 0; i < miwArr.Length; ++i)
                {
                    var miw = miwArr[i];
                    miw.weight = miw.weight / weightSum;
                    miwArr[i] = miw;
                }
            }

            var duration = 0.0f;
            for (var i = 0; i < miwArr.Length; ++i)
            {
                var miw = miwArr[i];
                ref var motion = ref motions[miw.motionIndex];
                duration += CalculateMotionDuration(ref motion.motion, runtimeParams, controllerAnimationsBlob, blobDatabase, weight * miw.weight) / motion.timeScale;
            }

            miwArr.Dispose();
            return duration;
        }

        private static NativeList<MotionIndexAndWeight> GetChildMotionsList(ref MotionBlob motionBlob, in NativeArray<AnimatorControllerParameterComponent> runtimeParams)
        {
            NativeList<MotionIndexAndWeight> blendTreeMotionsAndWeights = default;

            switch (motionBlob.type)
            {
                case MotionBlob.Type.None:
                case MotionBlob.Type.AnimationClip:
                    return blendTreeMotionsAndWeights;
                case MotionBlob.Type.BlendTreeDirect:
                    blendTreeMotionsAndWeights = GetBlendTreeDirectCurrentMotions(ref motionBlob, runtimeParams);
                    break;
                case MotionBlob.Type.BlendTree1D:
                    blendTreeMotionsAndWeights = GetBlendTree1DCurrentMotions(ref motionBlob, runtimeParams);
                    break;
                case MotionBlob.Type.BlendTree2DSimpleDirectional:
                case MotionBlob.Type.BlendTree2DFreeformCartesian:
                case MotionBlob.Type.BlendTree2DFreeformDirectional:
                    blendTreeMotionsAndWeights = GetBlendTree2DCurrentMotions(ref motionBlob, runtimeParams, motionBlob.type);
                    break;
            }

            return blendTreeMotionsAndWeights;
        }

        private static NativeList<MotionIndexAndWeight> GetBlendTree1DCurrentMotions(
            ref MotionBlob motionBlob,
            in NativeArray<AnimatorControllerParameterComponent> runtimeParams)
        {
            ref var motions = ref motionBlob.blendTree.motions;
            if (motions.Length == 0)
            {
                return default;
            }

            Span<float> thresholds = stackalloc float[motions.Length];
            for (var i = 0; i < motions.Length; ++i)
            {
                thresholds[i] = motions[i].threshold;
            }

            var parameterValue = GetParameterFloat(motionBlob.blendTree.blendParameterIndex, runtimeParams);
            return ComputeBlendTree1D(thresholds, parameterValue);
        }

        private static NativeList<MotionIndexAndWeight> GetBlendTreeDirectCurrentMotions(
            ref MotionBlob motionBlob,
            in NativeArray<AnimatorControllerParameterComponent> runtimeParams)
        {
            ref var motions = ref motionBlob.blendTree.motions;
            var rv = new NativeList<MotionIndexAndWeight>(motions.Length, Allocator.Temp);

            var weightSum = 0.0f;
            for (var i = 0; i < motions.Length; ++i)
            {
                ref var motion = ref motions[i];
                var weight = motion.directBlendParameterIndex >= 0
                    ? GetParameterFloat(motion.directBlendParameterIndex, runtimeParams)
                    : 0f;
                if (weight > 0f)
                {
                    var miw = new MotionIndexAndWeight { motionIndex = i, weight = weight };
                    weightSum += miw.weight;
                    rv.Add(miw);
                }
            }

            if (motionBlob.blendTree.normalizeBlendValues && weightSum > 1f)
            {
                for (var i = 0; i < rv.Length; ++i)
                {
                    var miw = rv[i];
                    miw.weight = miw.weight / weightSum;
                    rv[i] = miw;
                }
            }

            return rv;
        }

        private static NativeList<MotionIndexAndWeight> GetBlendTree2DCurrentMotions(
            ref MotionBlob motionBlob,
            in NativeArray<AnimatorControllerParameterComponent> runtimeParams,
            MotionBlob.Type blendTreeType)
        {
            ref var motions = ref motionBlob.blendTree.motions;
            if (motions.Length == 0)
            {
                return default;
            }

            var pX = GetParameterFloat(motionBlob.blendTree.blendParameterIndex, runtimeParams);
            var pY = GetParameterFloat(motionBlob.blendTree.blendParameterYIndex, runtimeParams);
            var point = new float2(pX, pY);

            Span<BlendTree2DMotionElement> positions = stackalloc BlendTree2DMotionElement[motions.Length];
            var validMotionCount = 0;
            for (var i = 0; i < motions.Length; ++i)
            {
                ref var motion = ref motions[i];
                if (motion.motion.type == MotionBlob.Type.None)
                {
                    continue;
                }

                positions[validMotionCount++] = new BlendTree2DMotionElement { pos = motion.position2D, motionIndex = i };
            }

            positions = positions.Slice(0, validMotionCount);

            return blendTreeType switch
            {
                MotionBlob.Type.BlendTree2DSimpleDirectional => ComputeBlendTree2DSimpleDirectional(positions, point),
                MotionBlob.Type.BlendTree2DFreeformCartesian => ComputeBlendTree2DFreeformCartesian(positions, point),
                MotionBlob.Type.BlendTree2DFreeformDirectional => ComputeBlendTree2DFreeformDirectional(positions, point),
                _ => default,
            };
        }

        private static NativeList<MotionIndexAndWeight> ComputeBlendTree1D(in ReadOnlySpan<float> blendTreeThresholds, float blendTreeParameter)
        {
            if (blendTreeThresholds.Length == 0)
            {
                return default;
            }

            if (blendTreeThresholds.Length == 1)
            {
                var single = new NativeList<MotionIndexAndWeight>(1, Allocator.Temp);
                single.Add(new MotionIndexAndWeight { motionIndex = 0, weight = 1f });
                return single;
            }

            var i0 = 0;
            var i1 = 0;
            var found = false;
            for (var i = 0; i < blendTreeThresholds.Length && !found; ++i)
            {
                var threshold = blendTreeThresholds[i];
                i0 = i1;
                i1 = i;
                if (threshold > blendTreeParameter)
                {
                    found = true;
                }
            }

            if (!found)
            {
                i1 = blendTreeThresholds.Length - 1;
                i0 = i1 - 1;
            }

            var motion0Threshold = blendTreeThresholds[i0];
            var motion1Threshold = blendTreeThresholds[i1];
            var factor = math.saturate((blendTreeParameter - motion0Threshold) / (motion1Threshold - motion0Threshold));

            var rv = new NativeList<MotionIndexAndWeight>(2, Allocator.Temp);
            rv.Add(new MotionIndexAndWeight { motionIndex = i0, weight = 1 - factor });
            rv.Add(new MotionIndexAndWeight { motionIndex = i1, weight = factor });
            return rv;
        }

        private static NativeList<MotionIndexAndWeight> ComputeBlendTree2DSimpleDirectional(
            in ReadOnlySpan<BlendTree2DMotionElement> blendTreePositions,
            float2 blendTreeParameter)
        {
            var rv = new NativeList<MotionIndexAndWeight>(Allocator.Temp);

            if (blendTreePositions.Length < 2)
            {
                if (blendTreePositions.Length == 1)
                {
                    rv.Add(new MotionIndexAndWeight { weight = 1, motionIndex = 0 });
                }

                return rv;
            }

            HandleCentroidCase(ref rv, blendTreeParameter, blendTreePositions);
            if (rv.Length > 0)
            {
                return rv;
            }

            var centerPtIndex = -1;
            var dotProductsAndWeights = new NativeList<MotionIndexAndWeight>(blendTreePositions.Length, Allocator.Temp);
            for (var i = 0; i < blendTreePositions.Length; ++i)
            {
                var motionDir = blendTreePositions[i].pos;
                if (!math.any(motionDir))
                {
                    centerPtIndex = i;
                    continue;
                }

                var angle = math.atan2(motionDir.y, motionDir.x);
                var miw = new MotionIndexAndWeight { motionIndex = blendTreePositions[i].motionIndex, weight = angle };
                dotProductsAndWeights.Add(miw);
            }

            var pointAngle = math.atan2(blendTreeParameter.y, blendTreeParameter.x);
            dotProductsAndWeights.Sort();

            MotionIndexAndWeight d0 = default;
            MotionIndexAndWeight d1 = default;
            var index = 0;
            for (; index < dotProductsAndWeights.Length; ++index)
            {
                var d = dotProductsAndWeights[index];
                if (d.weight < pointAngle)
                {
                    var ld0 = index == 0 ? dotProductsAndWeights.Length - 1 : index - 1;
                    d1 = d;
                    d0 = dotProductsAndWeights[ld0];
                    break;
                }
            }

            if (index == dotProductsAndWeights.Length)
            {
                d0 = dotProductsAndWeights[dotProductsAndWeights.Length - 1];
                d1 = dotProductsAndWeights[0];
            }

            var p0 = blendTreePositions[d0.motionIndex].pos;
            var p1 = blendTreePositions[d1.motionIndex].pos;
            var (l0, l1, l2) = CalculateBarycentric(p0, p1, blendTreeParameter);

            var m0Weight = l1;
            var m1Weight = l2;
            if (l0 < 0)
            {
                var sum = m0Weight + m1Weight;
                m0Weight /= sum;
                m1Weight /= sum;
            }

            l0 = math.saturate(l0);

            var evenlyDistributedMotionWeight = centerPtIndex < 0 ? (1.0f / blendTreePositions.Length) * l0 : 0f;

            rv.Add(new MotionIndexAndWeight { motionIndex = d0.motionIndex, weight = m0Weight + evenlyDistributedMotionWeight });
            rv.Add(new MotionIndexAndWeight { motionIndex = d1.motionIndex, weight = m1Weight + evenlyDistributedMotionWeight });

            if (evenlyDistributedMotionWeight > 0f)
            {
                for (var i = 0; i < blendTreePositions.Length; ++i)
                {
                    if (i == d0.motionIndex || i == d1.motionIndex)
                    {
                        continue;
                    }

                    rv.Add(new MotionIndexAndWeight { motionIndex = blendTreePositions[i].motionIndex, weight = evenlyDistributedMotionWeight });
                }
            }

            if (centerPtIndex >= 0)
            {
                rv.Add(new MotionIndexAndWeight { motionIndex = centerPtIndex, weight = l0 });
            }

            dotProductsAndWeights.Dispose();
            return rv;
        }

        private static NativeList<MotionIndexAndWeight> ComputeBlendTree2DFreeformCartesian(
            in ReadOnlySpan<BlendTree2DMotionElement> blendTreePositions,
            float2 blendTreeParameter)
        {
            Span<float> hpArr = stackalloc float[blendTreePositions.Length];
            var hpSum = 0.0f;

            for (var i = 0; i < blendTreePositions.Length; ++i)
            {
                var pi = blendTreePositions[i].pos;
                var pip = blendTreeParameter - pi;

                var w = 1.0f;
                for (var j = 0; j < blendTreePositions.Length && w > 0; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var pj = blendTreePositions[j].pos;
                    var pipj = pj - pi;
                    var f = math.dot(pip, pipj) / math.lengthsq(pipj);
                    var hj = math.max(1 - f, 0);
                    w = math.min(hj, w);
                }

                hpSum += w;
                hpArr[i] = w;
            }

            var rv = new NativeList<MotionIndexAndWeight>(blendTreePositions.Length, Allocator.Temp);
            for (var i = 0; i < blendTreePositions.Length; ++i)
            {
                var w = hpArr[i] / hpSum;
                if (w > 0)
                {
                    rv.Add(new MotionIndexAndWeight { motionIndex = blendTreePositions[i].motionIndex, weight = w });
                }
            }

            return rv;
        }

        private static NativeList<MotionIndexAndWeight> ComputeBlendTree2DFreeformDirectional(
            in ReadOnlySpan<BlendTree2DMotionElement> blendTreePositions,
            float2 blendTreeParameter)
        {
            var pointLength = math.length(blendTreeParameter);

            Span<float> hpArr = stackalloc float[blendTreePositions.Length];
            var hpSum = 0.0f;

            for (var i = 0; i < blendTreePositions.Length; ++i)
            {
                var pi = blendTreePositions[i].pos;
                var lpi = math.length(pi);

                var w = 1.0f;
                for (var j = 0; j < blendTreePositions.Length && w > 0; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var pj = blendTreePositions[j].pos;
                    var lpj = math.length(pj);

                    var pRcpMiddle = math.rcp((lpj + lpi) * 0.5f);
                    var lpip = (pointLength - lpi) * pRcpMiddle;
                    var lpipj = (lpj - lpi) * pRcpMiddle;
                    var angleWeights = CalcAngleWeights(pi, pj, blendTreeParameter);

                    var pip = new float2(lpip, angleWeights.y);
                    var pipj = new float2(lpipj, angleWeights.x);

                    var f = math.dot(pip, pipj) / math.lengthsq(pipj);
                    var hj = math.saturate(1 - f);
                    w = math.min(hj, w);
                }

                hpSum += w;
                hpArr[i] = w;
            }

            var rv = new NativeList<MotionIndexAndWeight>(blendTreePositions.Length, Allocator.Temp);
            for (var i = 0; i < blendTreePositions.Length; ++i)
            {
                var w = hpArr[i] / hpSum;
                if (w > 0)
                {
                    rv.Add(new MotionIndexAndWeight { motionIndex = blendTreePositions[i].motionIndex, weight = w });
                }
            }

            return rv;
        }

        private static void HandleCentroidCase(
            ref NativeList<MotionIndexAndWeight> rv,
            float2 point,
            in ReadOnlySpan<BlendTree2DMotionElement> blendTreePositions)
        {
            if (math.any(point))
            {
                return;
            }

            var index = 0;
            for (; index < blendTreePositions.Length && math.any(blendTreePositions[index].pos); ++index)
            {
            }

            if (index < blendTreePositions.Length)
            {
                rv.Add(new MotionIndexAndWeight { motionIndex = index, weight = 1 });
            }
            else
            {
                var weight = 1.0f / blendTreePositions.Length;
                for (var i = 0; i < blendTreePositions.Length; ++i)
                {
                    rv.Add(new MotionIndexAndWeight { motionIndex = i, weight = weight });
                }
            }
        }

        private static (float, float, float) CalculateBarycentric(float2 p1, float2 p2, float2 pt)
        {
            var np2 = new float2(0 - p2.y, p2.x - 0);
            var np1 = new float2(0 - p1.y, p1.x - 0);

            var l1 = math.dot(pt, np2) / math.dot(p1, np2);
            var l2 = math.dot(pt, np1) / math.dot(p2, np1);
            var l0 = 1 - l1 - l2;
            return (l0, l1, l2);
        }

        private static float CalcAngle(float2 a, float2 b)
        {
            var cross = a.x * b.y - a.y * b.x;
            var dot = math.dot(a, b);
            var tanA = new float2(cross, dot);
            return math.atan2(tanA.x, tanA.y);
        }

        private static float2 CalcAngleWeights(float2 i, float2 j, float2 s)
        {
            float2 rv = 0;
            if (!math.any(i))
            {
                rv.x = CalcAngle(j, s);
                rv.y = 0;
            }
            else if (!math.any(j))
            {
                rv.x = CalcAngle(i, s);
                rv.y = rv.x;
            }
            else
            {
                rv.x = CalcAngle(i, j);
                rv.y = math.any(s) ? CalcAngle(i, s) : rv.x;
            }

            return rv;
        }

        private static float GetParameterFloat(int index, in NativeArray<AnimatorControllerParameterComponent> runtimeParams)
        {
            return index >= 0 && index < runtimeParams.Length ? runtimeParams[index].FloatValue : 0f;
        }

        private struct BlendTree2DMotionElement
        {
            public float2 pos;
            public int motionIndex;
        }

        private struct MotionIndexAndWeight : IComparable<MotionIndexAndWeight>
        {
            public int motionIndex;
            public float weight;

            public int CompareTo(MotionIndexAndWeight other)
            {
                if (this.weight < other.weight)
                {
                    return 1;
                }

                if (this.weight > other.weight)
                {
                    return -1;
                }

                return 0;
            }
        }
    }
}

#endif
