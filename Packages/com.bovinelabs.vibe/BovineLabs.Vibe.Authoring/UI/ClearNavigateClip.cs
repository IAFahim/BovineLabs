// <copyright file="ClearNavigateClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe.Authoring.UI
{
    using System;
    using BovineLabs.Anchor.Nav;
    using BovineLabs.Vibe.Data.UI;
    using UnityEngine;

    /// <summary>
    /// Timeline clip that clears Anchor navigation when activated.
    /// </summary>
    [Serializable]
    public class ClearNavigateClip : AnchorNavClipBase
    {
        [SerializeField]
        [Tooltip("Exit animation used when clearing navigation.")]
        private AnchorNavAnimation exitAnimation;

        /// <inheritdoc/>
        protected override AnchorNavClipData BuildClipData()
        {
            return new AnchorNavClipData
            {
                Action = AnchorNavClipAction.ClearNavigation,
                ExitAnimation = this.exitAnimation != null ? this.exitAnimation.ID : -1,
            };
        }
    }
}
#endif
