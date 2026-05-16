// <copyright file="PhysicsImpulseClipBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS

namespace BovineLabs.Vibe.Authoring.Physics
{
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Physics;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Shared authoring functionality for physics impulse clips.
    /// </summary>
    public abstract class PhysicsImpulseClipBase : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public virtual ClipCaps clipCaps => ClipCaps.None;

        /// <inheritdoc/>
        public sealed override void Bake(Entity clipEntity, BakingContext context)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<PhysicsImpulseClipBlob>();
            this.Bake(clipEntity, context, ref builder, ref blob);

            var blobReference = builder.CreateBlobAssetReference<PhysicsImpulseClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobReference, out _);
            context.Baker.AddComponent(clipEntity, new PhysicsImpulseClipData { Value = blobReference });

            base.Bake(clipEntity, context);
        }

        /// <summary>
        /// Implemented by subclasses to populate the impulse blob data.
        /// </summary>
        protected abstract void Bake(Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref PhysicsImpulseClipBlob blob);
    }
}

#endif
