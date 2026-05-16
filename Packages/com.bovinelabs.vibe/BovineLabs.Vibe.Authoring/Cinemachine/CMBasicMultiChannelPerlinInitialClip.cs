// <copyright file="CMBasicMultiChannelPerlinInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Clip that restores Cinemachine basic multi channel perlin to its captured initial values.
    /// </summary>
    [Serializable]
    public class CMBasicMultiChannelPerlinInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMBasicMultiChannelPerlinAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMBasicMultiChannelPerlinClipBlob>();
            blob.Type = CMBasicMultiChannelPerlinClipType.Initial;
            var blobRef = builder.CreateBlobAssetReference<CMBasicMultiChannelPerlinClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new CMBasicMultiChannelPerlinClipData { Value = blobRef });
        }
    }
}
#endif
