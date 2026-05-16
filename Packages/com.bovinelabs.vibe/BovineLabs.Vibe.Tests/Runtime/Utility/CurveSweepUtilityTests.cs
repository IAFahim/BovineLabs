// <copyright file="CurveSweepUtilityTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Utility
{
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Tests.TestDoubles;
    using NUnit.Framework;

    public class CurveSweepUtilityTests
    {
        [Test]
        public void Evaluate_OrdersMinMaxAndUsesNormalizedCurveValue()
        {
            var owned = BlobCurveTestHelpers.CreateLinear(0f, 0f, 1f, 1f);
            try
            {
                ref var curve = ref owned.Blob.Value;
                var cache = ClipBlobCurveCache.Create();

                var value = CurveSweepUtility.Evaluate(ref curve, 10f, 2f, false, 0.25f, ref cache, 99f);

                Assert.AreEqual(4f, value, 0.0001f);
            }
            finally
            {
                owned.Dispose();
            }
        }

        [Test]
        public void Evaluate_RelativeAndAbsoluteModes_RespectBaseValue()
        {
            var owned = BlobCurveTestHelpers.CreateLinear(0f, 0f, 1f, 1f);
            try
            {
                ref var curve = ref owned.Blob.Value;
                var relativeCache = ClipBlobCurveCache.Create();
                var absoluteCache = ClipBlobCurveCache.Create();

                var relative = CurveSweepUtility.Evaluate(ref curve, 0f, 2f, true, 0.5f, ref relativeCache, 10f);
                var absolute = CurveSweepUtility.Evaluate(ref curve, 0f, 2f, false, 0.5f, ref absoluteCache, 10f);

                Assert.AreEqual(11f, relative, 0.0001f);
                Assert.AreEqual(1f, absolute, 0.0001f);
            }
            finally
            {
                owned.Dispose();
            }
        }

        [Test]
        public void Evaluate_NoCurve_ReturnsBaseValue()
        {
            var emptyBlob = BlobCurveTestHelpers.CreateEmpty();
            try
            {
                ref var curve = ref emptyBlob.Value;
                var cache = ClipBlobCurveCache.Create();

                var value = CurveSweepUtility.Evaluate(ref curve, -5f, 5f, true, 0.7f, ref cache, 7.5f);

                Assert.AreEqual(7.5f, value, 0.0001f);
            }
            finally
            {
                emptyBlob.Dispose();
            }
        }
    }
}
