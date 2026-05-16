// <copyright file="AnchorNavClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe.Data.UI
{
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Navigation action to invoke when an Anchor navigation clip becomes active.
    /// </summary>
    public enum AnchorNavClipAction : byte
    {
        Navigate,
        ClearNavigation,
        ClearBackStack,
        PopBackStack,
        PopBackStackToPanel,
        CloseAllPopups,
        ClosePopup,
    }

    /// <summary>
    /// Serialized configuration baked for Anchor navigation clips.
    /// </summary>
    public struct AnchorNavClipData : IComponentData
    {
        public FixedString32Bytes Destination;

        public int ExitAnimation;

        public AnchorNavClipAction Action;
    }
}
#endif
