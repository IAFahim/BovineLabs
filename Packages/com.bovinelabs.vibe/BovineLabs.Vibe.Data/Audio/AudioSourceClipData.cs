// <copyright file="AudioSourceClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Serialized configuration baked for audio source clip clips.
    /// </summary>
    public struct AudioSourceClipData : IComponentData
    {
        public UnityObjectRef<AudioClip> Clip;
    }
}
#endif
