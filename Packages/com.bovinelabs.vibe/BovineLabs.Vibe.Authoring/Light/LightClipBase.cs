// <copyright file="LightClipBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Light
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Light;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Base implementation shared by light clip authoring components.
    /// </summary>
    [Serializable]
    public abstract class LightClipBase : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public virtual ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public sealed override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent<LightAnimated>(clipEntity);
            context.Baker.AddComponent<LightExtendedAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var clipData = ref builder.ConstructRoot<LightClipBlobData>();
            clipData.Type = this.Type;

            var clipComponent = new LightClipData();
            this.Configure(clipEntity, context, ref builder, ref clipData, ref clipComponent);

            var blob = builder.CreateBlobAssetReference<LightClipBlobData>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            clipComponent.Value = blob;
            context.Baker.AddComponent(clipEntity, clipComponent);

            base.Bake(clipEntity, context);
        }

        /// <summary>
        /// Gets the clip type recorded on the baked component.
        /// </summary>
        protected abstract LightClipType Type { get; }

        /// <summary>
        /// Allows derived clips to populate custom data.
        /// </summary>
        protected virtual void Configure(
            Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref LightClipBlobData clipData, ref LightClipData clipComponent)
        {
        }
    }
}
#endif
