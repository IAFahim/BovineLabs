// <copyright file="CloseAllPopupsClip.cs" company="BovineLabs">
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
    /// Timeline clip that closes all Anchor popups when activated.
    /// </summary>
    [Serializable]
    public class CloseAllPopupsClip : AnchorNavClipBase
    {
        [SerializeField]
        [Tooltip("Exit animation used when dismissing popups.")]
        private AnchorNavAnimation exitAnimation;

        /// <inheritdoc/>
        protected override AnchorNavClipData BuildClipData()
        {
            return new AnchorNavClipData
            {
                Action = AnchorNavClipAction.CloseAllPopups,
                ExitAnimation = this.exitAnimation != null ? this.exitAnimation.ID : 0,
            };
        }
    }
}
#endif
