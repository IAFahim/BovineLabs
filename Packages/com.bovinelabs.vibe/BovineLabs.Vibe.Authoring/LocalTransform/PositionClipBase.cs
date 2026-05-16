// <copyright file="PositionClipBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.LocalTransform;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Base implementation shared by position clip authoring components.
    /// </summary>
    [Serializable]
    public abstract class PositionClipBase : DOTSClip, ITimelineClipAsset
    {
        public virtual ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public sealed override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent<PositionAnimated>(clipEntity);
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<PositionClipBlob>();
            this.Bake(clipEntity, context, ref blobBuilder, ref root);

            var blob = blobBuilder.CreateBlobAssetReference<PositionClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new PositionClipData { Value = blob });
            context.Baker.AddComponent<LocalTransformClipInitial>(clipEntity);

            this.PostBake(clipEntity, context, ref root);

            base.Bake(clipEntity, context);
        }

        protected abstract void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PositionClipBlob blob);

        protected virtual void PostBake(Entity clipEntity, BakingContext context, ref PositionClipBlob blob)
        {
        }
    }
}
