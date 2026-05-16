// <copyright file="AudioSourceClipInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Captures the initial audio source clip when the track activates.
    /// </summary>
    public struct AudioSourceClipInitial : IComponentData
    {
        public UnityObjectRef<AudioClip> Clip;
    }
}
#endif
