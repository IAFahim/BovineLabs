// <copyright file="VolumeScreenSpaceLensFlareInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Volume
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Volume;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Clip that restores screen space lens flare settings to their captured initial values.
    /// </summary>
    [Serializable]
    public class VolumeScreenSpaceLensFlareInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<VolumeScreenSpaceLensFlareAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<VolumeScreenSpaceLensFlareClipBlob>();
            blob.Type = VolumeScreenSpaceLensFlareClipType.Initial;

            var blobRef = builder.CreateBlobAssetReference<VolumeScreenSpaceLensFlareClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new VolumeScreenSpaceLensFlareClipData { Value = blobRef });
        }
    }
}
#endif
#endif
