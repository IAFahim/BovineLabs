// <copyright file="ClipBlobCurveCacheTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Data
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Vibe.Data;
    using NUnit.Framework;
    using Unity.Mathematics;

    public class ClipBlobCurveCacheTests
    {
        [Test]
        public void Create_InitializesAllCachesToEmpty()
        {
            var cache = ClipBlobCurveCache.Create();

            AssertCacheEmpty(cache.Cache0);
            AssertCacheEmpty(cache.Cache1);
            AssertCacheEmpty(cache.Cache2);
        }

        [Test]
        public void Reset_ClearsAllCaches()
        {
            var cache = new ClipBlobCurveCache
            {
                Cache0 = new BlobCurveCache { Index = 1, NeighborhoodTimes = new float2(1f, 2f) },
                Cache1 = new BlobCurveCache { Index = 2, NeighborhoodTimes = new float2(3f, 4f) },
                Cache2 = new BlobCurveCache { Index = 3, NeighborhoodTimes = new float2(5f, 6f) },
            };

            cache.Reset();

            AssertCacheEmpty(cache.Cache0);
            AssertCacheEmpty(cache.Cache1);
            AssertCacheEmpty(cache.Cache2);
        }

        private static void AssertCacheEmpty(BlobCurveCache cache)
        {
            Assert.AreEqual(int.MinValue, cache.Index);
            Assert.IsTrue(float.IsNaN(cache.NeighborhoodTimes.x));
            Assert.IsTrue(float.IsNaN(cache.NeighborhoodTimes.y));
        }
    }
}
