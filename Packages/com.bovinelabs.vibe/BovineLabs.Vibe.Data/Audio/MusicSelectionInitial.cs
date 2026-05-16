// <copyright file="MusicSelectionInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Audio
{
    using Unity.Entities;

    /// <summary>
    /// Captures the initial music selection when the track activates.
    /// </summary>
    public struct MusicSelectionInitial : IComponentData
    {
        public int TrackId;
    }
}
#endif
