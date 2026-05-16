// <copyright file="ClosePopupClip.cs" company="BovineLabs">
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
    /// Timeline clip that closes a specific Anchor popup when activated.
    /// </summary>
    [Serializable]
    public class ClosePopupClip : AnchorNavClipBase
    {
        [SerializeField]
        [Tooltip("Anchor action whose destination should be closed.")]
        private AnchorAction action;

        [SerializeField]
        [Tooltip("Exit animation used when dismissing the popup.")]
        private AnchorNavAnimation exitAnimation;

        /// <inheritdoc/>
        protected override AnchorNavClipData BuildClipData()
        {
            return new AnchorNavClipData
            {
                Action = AnchorNavClipAction.ClosePopup,
                Destination = this.action != null ? this.action.Action.Destination : string.Empty,
                ExitAnimation = this.exitAnimation != null ? this.exitAnimation.ID : 0,
            };
        }
    }
}
#endif
