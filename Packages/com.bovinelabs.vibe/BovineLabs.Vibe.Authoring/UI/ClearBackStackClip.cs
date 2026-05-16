// <copyright file="ClearBackStackClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe.Authoring.UI
{
    using System;
    using BovineLabs.Vibe.Data.UI;

    /// <summary>
    /// Timeline clip that clears the Anchor back stack when activated.
    /// </summary>
    [Serializable]
    public class ClearBackStackClip : AnchorNavClipBase
    {
        /// <inheritdoc/>
        protected override AnchorNavClipData BuildClipData()
        {
            return new AnchorNavClipData { Action = AnchorNavClipAction.ClearBackStack };
        }
    }
}
#endif
