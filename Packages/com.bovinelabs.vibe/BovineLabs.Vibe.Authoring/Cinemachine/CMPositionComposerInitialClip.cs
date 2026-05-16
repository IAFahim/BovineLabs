// <copyright file="CMPositionComposerInitialClip.cs" company="BovineLabs">
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
    /// Clip that restores the Cinemachine position composer to its captured initial values.
    /// </summary>
    [Serializable]
    public class CMPositionComposerInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<CMPositionComposerAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMPositionComposerClipBlob>();
            blob.Type = CMPositionComposerClipType.Initial;
            var blobRef = builder.CreateBlobAssetReference<CMPositionComposerClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(clipEntity, new CMPositionComposerClipData { Value = blobRef });
        }
    }
}
#endif
