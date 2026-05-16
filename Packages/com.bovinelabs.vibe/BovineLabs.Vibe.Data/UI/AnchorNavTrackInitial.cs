// <copyright file="AnchorNavTrackInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe.Data.UI
{
    using Unity.Entities;

    /// <summary>
    /// Stores a handle to the Anchor navigation state captured when the track activates.
    /// </summary>
    public struct AnchorNavTrackInitial : IComponentData
    {
        public int StateHandle;
    }
}
#endif
