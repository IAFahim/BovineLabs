// <copyright file="HDRPMaterialPropertyTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Authoring.Rendering
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Rendering;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that drives HDRP material properties.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(HDRPMaterialPropertyClip))]
    [TrackBindingType(typeof(Renderer))]
    [TrackColor(0.65f, 0.4f, 0.2f)]
    [DisplayName("DOTS/Rendering/HDRP Material Property Track")]
    public class HDRPMaterialPropertyTrack : DOTSTrack
    {
        [SerializeField]
        [Tooltip("When enabled, add/remove material property components as clips become active.")]
        private bool manageComponents = true;

        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            var flags = HDRPMaterialPropertyFlags.None;
            foreach (var clipInfo in context.SharedContextValues.ClipEntities)
            {
                if (clipInfo.Clip.asset is not HDRPMaterialPropertyClip materialClip)
                {
                    continue;
                }

                flags |= materialClip.Properties;
            }

            context.Baker.AddComponent(context.TrackEntity, new HDRPMaterialPropertyInitial { Flags = flags });
            context.Baker.AddComponent(context.TrackEntity, new HDRPMaterialPropertyTrackComponents { ManageComponents = this.manageComponents });
        }
    }
}
#endif
