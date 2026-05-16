// <copyright file="AudioSourceTriggerClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Supported actions for triggered audio source clips.
    /// </summary>
    public enum AudioSourceTriggerAction : byte
    {
        Play,
        Pause,
        Unpause,
        Stop,
    }

    /// <summary>
    /// Serialized configuration baked for audio source trigger clips.
    /// </summary>
    public struct AudioSourceTriggerClipData : IComponentData
    {
        public BlobAssetReference<AudioSourceTriggerClipBlob> Value;
    }

    /// <summary>
    /// Serialized configuration blob for audio source trigger clips.
    /// </summary>
    public struct AudioSourceTriggerClipBlob
    {
        public AudioSourceTriggerAction Action;
        public float MinVolume;
        public float MaxVolume;
        public float MinPitch;
        public float MaxPitch;
        public uint Seed;
        public bool ForceRestart;
    }

    /// <summary>
    /// Randomized audio clip list used by trigger clips.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct AudioSourceTriggerClipEntry : IBufferElementData
    {
        public UnityObjectRef<AudioClip> Clip;
    }
}
#endif
