// <copyright file="CMVolumeSettingsClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Cinemachine
{
    using System;
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Cinemachine;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that blends Cinemachine volume settings weight & focus while applying activation-only options.
    /// </summary>
    [Serializable]
    public class CMVolumeSettingsClip : DOTSClip, ITimelineClipAsset
    {
        [SerializeField]
        [Tooltip("Blend weight applied to the volumetric profile.")]
        [Range(0f, 1f)]
        private float weight = 1f;

        [SerializeField]
        [Tooltip("Offset applied to the computed focus distance.")]
        private float focusOffset = 0f;

        [SerializeField]
        [Tooltip("Mode used to determine the focus target.")]
        private CinemachineVolumeSettings.FocusTrackingMode focusTracking =
            CinemachineVolumeSettings.FocusTrackingMode.LookAtTarget;

        [SerializeField]
        [Tooltip("If enabled, assigns a specific focus target while the clip is active.")]
        private bool overrideFocusTarget;

        [SerializeField]
        private ExposedReference<Transform> focusTarget;

        [SerializeField]
        [Tooltip("If enabled, swaps the volume profile while the clip is active.")]
        private bool overrideProfile;

        [SerializeField]
        private VolumeProfile profile;

        /// <inheritdoc/>
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            base.Bake(clipEntity, context);

            this.weight = Mathf.Clamp01(this.weight);
            context.Baker.AddComponent<CMVolumeSettingsAnimated>(clipEntity);

            var focusTargetEntity = Entity.Null;
            if (this.overrideFocusTarget && context.Director != null)
            {
                var resolved = context.Director.GetReferenceValue(this.focusTarget.exposedName, out _) as Transform;
                if (resolved != null)
                {
                    focusTargetEntity = context.Baker.GetEntity(resolved, TransformUsageFlags.Dynamic);
                }
            }

            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<CMVolumeSettingsClipBlob>();
            blob.Type = CMVolumeSettingsClipType.Animated;
            blob.Weight = this.weight;
            blob.FocusOffset = this.focusOffset;
            blob.FocusTracking = this.focusTracking;
            blob.FocusTarget = focusTargetEntity;
            blob.OverrideFocusTarget = this.overrideFocusTarget;
            blob.OverrideProfile = this.overrideProfile;

            var blobRef = builder.CreateBlobAssetReference<CMVolumeSettingsClipBlob>(Allocator.Persistent);
            context.Baker.AddBlobAsset(ref blobRef, out _);
            context.Baker.AddComponent(
                clipEntity,
                new CMVolumeSettingsClipData
                {
                    Value = blobRef,
                    Profile = this.overrideProfile ? this.profile : default,
                });
        }

        private void OnValidate()
        {
            this.weight = Mathf.Clamp01(this.weight);
        }
    }
}
#endif
