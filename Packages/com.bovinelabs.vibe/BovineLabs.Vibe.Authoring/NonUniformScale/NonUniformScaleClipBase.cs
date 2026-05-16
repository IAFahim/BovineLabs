// <copyright file="NonUniformScaleClipBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.NonUniformScale
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Base implementation shared by non-uniform scale clip authoring components.
    /// </summary>
    [Serializable]
    public abstract class NonUniformScaleClipBase : DOTSClip, ITimelineClipAsset
    {
        public virtual ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public sealed override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent<NonUniformScaleAnimated>(clipEntity);
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic | TransformUsageFlags.NonUniformScale);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<NonUniformScaleClipBlob>();

            this.Bake(clipEntity, context, ref builder, ref blob);

            var blobRef = builder.CreateBlobAssetReference<NonUniformScaleClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new NonUniformScaleClipData { Value = blobRef });
            context.Baker.AddComponent<PostTransformMatrixClipInitial>(clipEntity);

            this.PostBake(clipEntity, context, ref blob);

            builder.Dispose();

            base.Bake(clipEntity, context);
        }

        protected abstract void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref NonUniformScaleClipBlob blob);

        protected virtual void PostBake(Entity clipEntity, BakingContext context, ref NonUniformScaleClipBlob blob)
        {
        }
    }
}
