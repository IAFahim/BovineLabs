// <copyright file="LightConstantClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Authoring.Light
{
    using System;
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Vibe.Data.Light;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Timeline clip that applies constant color and intensity overrides to a light.
    /// </summary>
    [Serializable]
    public class LightConstantClip : LightClipBase
    {
        [SerializeField]
        [Tooltip("Override the light color while this clip is active.")]
        private bool overrideColor = true;

        [SerializeField]
        [Tooltip("RGB color applied when override is enabled.")]
        private Color color = Color.white;

        [SerializeField]
        [Tooltip("Override the light intensity while this clip is active.")]
        private bool overrideIntensity = true;

        [SerializeField]
        [Tooltip("Intensity in Unity light units.")]
        [Min(0f)]
        private float intensity = 1f;

        [SerializeField]
        [Tooltip("Override the color temperature while this clip is active.")]
        private bool overrideColorTemperature;

        [SerializeField]
        [Tooltip("Color temperature in Kelvin.")]
        [Min(0f)]
        private float colorTemperature = 6500f;

        /// <inheritdoc/>
        public override ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        protected override LightClipType Type => LightClipType.Constant;

        /// <inheritdoc/>
        protected override void Configure(
            Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref LightClipBlobData clipData, ref LightClipData clipComponent)
        {
            ref var data = ref clipData.Constant;
            data.OverrideColor = this.overrideColor;
            data.Color = new float3(this.color.r, this.color.g, this.color.b);
            data.OverrideIntensity = this.overrideIntensity;
            data.Intensity = math.max(0f, this.intensity);
            data.OverrideColorTemperature = this.overrideColorTemperature;
            data.ColorTemperature = math.max(0f, this.colorTemperature);
        }

        private void OnValidate()
        {
            this.intensity = math.max(0f, this.intensity);
            this.colorTemperature = math.max(0f, this.colorTemperature);
        }
    }
}
#endif
