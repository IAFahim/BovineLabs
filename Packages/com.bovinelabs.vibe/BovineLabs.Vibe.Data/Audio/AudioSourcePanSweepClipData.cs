// <copyright file="AudioSourcePanSweepClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using Unity.Entities;

    /// <summary>
    /// Serialized configuration baked for audio source pan sweep clips.
    /// </summary>
    public struct AudioSourcePanSweepClipData : IComponentData
    {
        public BlobAssetReference<AudioSourcePanSweepClipBlob> Value;
    }

    /// <summary>
    /// Blob holding the sweep curve configuration.
    /// </summary>
    public struct AudioSourcePanSweepClipBlob
    {
        public AudioCurveSweepData Sweep;
    }
}
#endif
