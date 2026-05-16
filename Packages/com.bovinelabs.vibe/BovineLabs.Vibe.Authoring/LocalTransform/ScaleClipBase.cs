// <copyright file="ScaleClipBase.cs" company="BovineLabs">
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
    /// Base implementation shared by scale clip authoring components.
    /// </summary>
    [Serializable]
    public abstract class ScaleClipBase : DOTSClip, ITimelineClipAsset
    {
        public virtual ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public sealed override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent<ScaleAnimated>(clipEntity);
            context.Baker.AddTransformUsageFlags(context.Binding!.Target, TransformUsageFlags.Dynamic);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var root = ref blobBuilder.ConstructRoot<ScaleClipBlob>();
            this.Bake(clipEntity, context, ref blobBuilder, ref root);

            var blob = blobBuilder.CreateBlobAssetReference<ScaleClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new ScaleClipData { Value = blob });
            context.Baker.AddComponent<LocalTransformClipInitial>(clipEntity);

            this.PostBake(clipEntity, context, ref root);

            base.Bake(clipEntity, context);
        }

        protected abstract void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref ScaleClipBlob blob);

        protected virtual void PostBake(Entity clipEntity, BakingContext context, ref ScaleClipBlob blob)
        {
        }
    }
}
