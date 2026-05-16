// <copyright file="ClipBlobCache.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Stores reusable curve caches for timeline clip evaluation.
    /// </summary>
    public struct ClipBlobCurveCache : IComponentData
    {
        /// <summary>
        /// Cache used for the first curve bound to the clip.
        /// </summary>
        public BlobCurveCache Cache0;

        /// <summary>
        /// Cache used for the second curve bound to the clip.
        /// </summary>
        public BlobCurveCache Cache1;

        /// <summary>
        /// Cache used for the third curve bound to the clip.
        /// </summary>
        public BlobCurveCache Cache2;

        /// <summary>
        /// Resets all cached state so subsequent evaluations perform a fresh lookup.
        /// </summary>
        public void Reset()
        {
            this.Cache0 = BlobCurveCache.Empty;
            this.Cache1 = BlobCurveCache.Empty;
            this.Cache2 = BlobCurveCache.Empty;
        }

        /// <summary>
        /// Creates a cache with all entries reset.
        /// </summary>
        /// <returns>A cache with no stored indices.</returns>
        public static ClipBlobCurveCache Create()
        {
            var cache = default(ClipBlobCurveCache);
            cache.Reset();
            return cache;
        }
    }
}
