// <copyright file="NavigateClip.cs" company="BovineLabs">
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
    /// Timeline clip that navigates to an Anchor destination when activated.
    /// </summary>
    [Serializable]
    public class NavigateClip : AnchorNavClipBase
    {
        [SerializeField]
        [Tooltip("Anchor action to invoke when the clip activates.")]
        private AnchorAction action;

        /// <inheritdoc/>
        protected override AnchorNavClipData BuildClipData()
        {
            return new AnchorNavClipData
            {
                Action = AnchorNavClipAction.Navigate,
                Destination = this.action != null ? this.action.ActionName : string.Empty,
            };
        }
    }
}
#endif
