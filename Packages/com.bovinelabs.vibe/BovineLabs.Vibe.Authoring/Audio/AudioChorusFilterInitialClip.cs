// <copyright file="AudioChorusFilterInitialClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Audio
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Audio;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// Clip that restores audio chorus filter values to their captured initial values.
    /// </summary>
    [Serializable]
    public class AudioChorusFilterInitialClip : DOTSClip, ITimelineClipAsset
    {
        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            context.Baker.AddComponent<AudioChorusFilterAnimated>(clipEntity);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AudioChorusFilterClipBlob>();
            root.Type = AudioChorusFilterClipType.Initial;

            var blob = builder.CreateBlobAssetReference<AudioChorusFilterClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blob, out _);
            context.Baker.AddComponent(clipEntity, new AudioChorusFilterClipData { Value = blob });
        }
    }
}
#endif
