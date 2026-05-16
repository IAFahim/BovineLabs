// <copyright file="PopBackStackClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe.Authoring.UI
{
    using System;
    using BovineLabs.Vibe.Data.UI;

    /// <summary>
    /// Timeline clip that pops the Anchor back stack when activated.
    /// </summary>
    [Serializable]
    public class PopBackStackClip : AnchorNavClipBase
    {
        /// <inheritdoc/>
        protected override AnchorNavClipData BuildClipData()
        {
            return new AnchorNavClipData { Action = AnchorNavClipAction.PopBackStack };
        }
    }
}
#endif
