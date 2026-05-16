// <copyright file="MusicSelectionClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using Unity.Entities;

    /// <summary>
    /// Serialized configuration baked for music selection clips.
    /// </summary>
    public struct MusicSelectionClipData : IComponentData
    {
        public int TrackId;
    }
}
#endif
