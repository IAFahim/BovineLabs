// <copyright file="URPMaterialPropertyTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe.Authoring.Rendering
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Rendering;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline track that drives URP material properties.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(URPMaterialPropertyClip))]
    [TrackBindingType(typeof(Renderer))]
    [TrackColor(0.2f, 0.6f, 0.2f)]
    [DisplayName("DOTS/Rendering/URP Material Property Track")]
    public class URPMaterialPropertyTrack : DOTSTrack
    {
        [SerializeField]
        [Tooltip("When enabled, add/remove material property components if required as clips become active.")]
        private bool addComponents = true;

        /// <inheritdoc/>
        protected override void Bake(BakingContext context)
        {
            var flags = URPMaterialPropertyFlags.None;
            foreach (var clipInfo in context.SharedContextValues.ClipEntities)
            {
                if (clipInfo.Clip.asset is not URPMaterialPropertyClip materialClip)
                {
                    continue;
                }

                flags |= materialClip.Properties;
            }

            context.Baker.AddComponent(context.TrackEntity, new URPMaterialPropertyInitial { Flags = flags });
            context.Baker.AddComponent(context.TrackEntity, new URPMaterialPropertyTrackComponents { AddComponents = this.addComponents });
        }
    }
}
#endif
