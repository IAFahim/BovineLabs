// <copyright file="BlobCurveTestHelpers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.TestDoubles
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    public static class BlobCurveTestHelpers
    {
        public struct OwnedBlobCurveSampler : IDisposable
        {
            public BlobAssetReference<BlobCurve> Blob;
            public BlobCurveSampler Sampler;

            public OwnedBlobCurveSampler(BlobAssetReference<BlobCurve> blob)
            {
                this.Blob = blob;
                this.Sampler = new BlobCurveSampler(blob);
            }

            public void Dispose()
            {
                if (this.Blob.IsCreated)
                {
                    this.Blob.Dispose();
                }
            }
        }

        public static OwnedBlobCurveSampler CreateOwned(AnimationCurve curve)
        {
            var blob = BlobCurve.Create(curve);
            return new OwnedBlobCurveSampler(blob);
        }

        public static OwnedBlobCurveSampler CreateLinear(float time0, float value0, float time1, float value1)
        {
            return CreateOwned(AnimationCurve.Linear(time0, value0, time1, value1));
        }

        public static BlobAssetReference<BlobCurve> CreateEmpty()
        {
            var builder = new BlobBuilder(Allocator.Temp);
            builder.ConstructRoot<BlobCurve>();
            var blob = builder.CreateBlobAssetReference<BlobCurve>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }
    }
}
