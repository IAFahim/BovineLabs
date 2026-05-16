// <copyright file="AudioCurveSweepUtilityTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Tests.Runtime.Audio
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Audio;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    public class AudioCurveSweepUtilityTests
    {
        [Test]
        public void Evaluate_ForwardsParametersToCurveSweepUtility()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioCurveSweepBlob>();
            BlobCurve.Construct(ref builder, ref root.Sweep.Curve, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            root.Sweep.Min = 8f;
            root.Sweep.Max = 2f;
            root.Sweep.Relative = true;
            var blob = builder.CreateBlobAssetReference<AudioCurveSweepBlob>(Allocator.Persistent);
            builder.Dispose();

            try
            {
                ref var sweep = ref blob.Value.Sweep;
                var utilityCache = ClipBlobCurveCache.Create();
                var expectedCache = ClipBlobCurveCache.Create();

                var actual = AudioCurveSweepUtility.Evaluate(ref sweep, 0.25f, ref utilityCache, 10f);
                var expected = CurveSweepUtility.Evaluate(ref sweep.Curve, sweep.Min, sweep.Max, sweep.Relative, 0.25f, ref expectedCache, 10f);

                Assert.AreEqual(expected, actual, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        [Test]
        public void Evaluate_NoCurve_RespectsBaseValue()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioCurveSweepBlob>();
            root.Sweep.Min = 0f;
            root.Sweep.Max = 1f;
            root.Sweep.Relative = true;
            var blob = builder.CreateBlobAssetReference<AudioCurveSweepBlob>(Allocator.Persistent);
            builder.Dispose();

            try
            {
                ref var sweep = ref blob.Value.Sweep;
                var cache = ClipBlobCurveCache.Create();

                var value = AudioCurveSweepUtility.Evaluate(ref sweep, 0.5f, ref cache, 5.5f);

                Assert.AreEqual(5.5f, value, 0.0001f);
            }
            finally
            {
                blob.Dispose();
            }
        }

        private struct AudioCurveSweepBlob
        {
            public AudioCurveSweepData Sweep;
        }
    }
}
#endif
