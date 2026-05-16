// <copyright file="ClipCurveUtilityTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Utility
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Tests.TestDoubles;
    using NUnit.Framework;
    using Unity.IntegerTime;

    public class ClipCurveUtilityTests
    {
        [Test]
        public void EvaluateNormalized_NoCurve_ReturnsDefaultValue()
        {
            var emptyBlob = BlobCurveTestHelpers.CreateEmpty();
            try
            {
                ref var curve = ref emptyBlob.Value;
                var cache = BlobCurveCache.Empty;
                var timeTransform = new TimeTransform
                {
                    Start = new DiscreteTime(0.0),
                    End = new DiscreteTime(10.0),
                    ClipIn = DiscreteTime.Zero,
                    Scale = 1.0,
                };

                var value = ClipCurveUtility.EvaluateNormalized(ref curve, 5f, in timeTransform, ref cache, 42f);

                Assert.AreEqual(42f, value, 0.0001f);
            }
            finally
            {
                emptyBlob.Dispose();
            }
        }

        [Test]
        public void EvaluateNormalized_ZeroDuration_UsesTimeZeroOnCurve()
        {
            var owned = BlobCurveTestHelpers.CreateLinear(0f, 0f, 1f, 1f);
            try
            {
                ref var curve = ref owned.Blob.Value;
                var cache = BlobCurveCache.Empty;
                var expectedCache = BlobCurveCache.Empty;
                var timeTransform = new TimeTransform
                {
                    Start = new DiscreteTime(10.0),
                    End = new DiscreteTime(10.0),
                    ClipIn = new DiscreteTime(2.0),
                    Scale = 1.0,
                };

                var expected = curve.Evaluate(0f, ref expectedCache);
                var value = ClipCurveUtility.EvaluateNormalized(ref curve, 123f, in timeTransform, ref cache);

                Assert.AreEqual(expected, value, 0.0001f);
            }
            finally
            {
                owned.Dispose();
            }
        }

        [Test]
        public void EvaluateNormalized_ValidDuration_UsesNormalizedClipTime()
        {
            var owned = BlobCurveTestHelpers.CreateLinear(0f, 0f, 1f, 1f);
            try
            {
                ref var curve = ref owned.Blob.Value;
                var cache = BlobCurveCache.Empty;
                var expectedCache = BlobCurveCache.Empty;
                var timeTransform = new TimeTransform
                {
                    Start = new DiscreteTime(0.0),
                    End = new DiscreteTime(10.0),
                    ClipIn = new DiscreteTime(2.0),
                    Scale = 1.0,
                };

                var expected = curve.Evaluate(0.5f, ref expectedCache);
                var value = ClipCurveUtility.EvaluateNormalized(ref curve, 7f, in timeTransform, ref cache);

                Assert.AreEqual(expected, value, 0.0001f);
            }
            finally
            {
                owned.Dispose();
            }
        }
    }
}
