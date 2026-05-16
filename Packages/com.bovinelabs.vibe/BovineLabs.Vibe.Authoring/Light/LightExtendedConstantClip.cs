// <copyright file="LightExtendedConstantClip.cs" company="BovineLabs">
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
    /// Timeline clip that applies constant overrides to extended light properties.
    /// </summary>
    [Serializable]
    public class LightExtendedConstantClip : LightClipBase
    {
        [SerializeField]
        [Tooltip("Override the light range while this clip is active.")]
        private bool overrideRange = true;

        [SerializeField]
        [Tooltip("Range in Unity light units.")]
        [Min(0f)]
        private float range = 10f;

        [SerializeField]
        [Tooltip("Override the outer spot angle while this clip is active.")]
        private bool overrideSpotAngle;

        [SerializeField]
        [Tooltip("Outer spot angle in degrees.")]
        [Range(0f, 179f)]
        private float spotAngle = 30f;

        [SerializeField]
        [Tooltip("Override the inner spot angle while this clip is active.")]
        private bool overrideInnerSpotAngle;

        [SerializeField]
        [Tooltip("Inner spot angle in degrees.")]
        [Range(0f, 179f)]
        private float innerSpotAngle;

        [SerializeField]
        [Tooltip("Override the shadow strength while this clip is active.")]
        private bool overrideShadowStrength;

        [SerializeField]
        [Tooltip("Shadow strength multiplier.")]
        [Range(0f, 1f)]
        private float shadowStrength = 1f;

        [SerializeField]
        [Tooltip("Override the shadow bias while this clip is active.")]
        private bool overrideShadowBias;

        [SerializeField]
        [Tooltip("Shadow bias offset.")]
        [Min(0f)]
        private float shadowBias = 0.05f;

        [SerializeField]
        [Tooltip("Override the shadow normal bias while this clip is active.")]
        private bool overrideShadowNormalBias;

        [SerializeField]
        [Tooltip("Shadow normal bias offset.")]
        [Min(0f)]
        private float shadowNormalBias = 0.4f;

        [SerializeField]
        [Tooltip("Override the shadow near plane while this clip is active.")]
        private bool overrideShadowNearPlane;

        [SerializeField]
        [Tooltip("Shadow near plane offset.")]
        [Min(0f)]
        private float shadowNearPlane = 0.2f;

        [SerializeField]
        [Tooltip("Override the cookie size while this clip is active.")]
        private bool overrideCookieSize;

        [SerializeField]
        [Tooltip("Cookie size in world units.")]
        private Vector2 cookieSize = Vector2.one;

        [SerializeField]
        [Tooltip("Override the cookie texture while this clip is active.")]
        private bool overrideCookie;

        [SerializeField]
        [Tooltip("Cookie texture applied to the light.")]
        private Texture cookie;

        [SerializeField]
        [Tooltip("Override the light render mode while this clip is active.")]
        private bool overrideRenderMode;

        [SerializeField]
        [Tooltip("Render mode to apply when override is enabled.")]
        private LightRenderMode renderMode = LightRenderMode.Auto;

        [SerializeField]
        [Tooltip("Override the light shadow mode while this clip is active.")]
        private bool overrideShadows;

        [SerializeField]
        [Tooltip("Shadow mode to apply when override is enabled.")]
        private LightShadows shadows = LightShadows.None;

        [SerializeField]
        [Tooltip("Override the light type while this clip is active.")]
        private bool overrideType;

        [SerializeField]
        [Tooltip("Light type to apply when override is enabled.")]
        private LightType type = LightType.Point;

        [SerializeField]
        [Tooltip("Override the light culling mask while this clip is active.")]
        private bool overrideCullingMask;

        [SerializeField]
        [Tooltip("Culling mask applied to the light.")]
        private LayerMask cullingMask = ~0;

        [SerializeField]
        [Tooltip("Override the rendering layer mask while this clip is active.")]
        private bool overrideRenderingLayerMask;

        [SerializeField]
        [Tooltip("Rendering layer mask applied to the light.")]
        private int renderingLayerMask = 1;

        /// <inheritdoc/>
        public override ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        protected override LightClipType Type => LightClipType.ExtendedConstant;

        /// <inheritdoc/>
        protected override void Configure(
            Entity clipEntity, BakingContext context, ref BlobBuilder builder, ref LightClipBlobData clipData, ref LightClipData clipComponent)
        {
            ref var data = ref clipData.ExtendedConstant;
            data.OverrideRange = this.overrideRange;
            data.Range = math.max(0f, this.range);
            data.OverrideSpotAngle = this.overrideSpotAngle;
            data.SpotAngle = math.clamp(this.spotAngle, 0f, 179f);
            data.OverrideInnerSpotAngle = this.overrideInnerSpotAngle;
            var innerSpotLimit = data.OverrideSpotAngle ? data.SpotAngle : 179f;
            data.InnerSpotAngle = math.clamp(this.innerSpotAngle, 0f, innerSpotLimit);
            data.OverrideShadowStrength = this.overrideShadowStrength;
            data.ShadowStrength = math.saturate(this.shadowStrength);
            data.OverrideShadowBias = this.overrideShadowBias;
            data.ShadowBias = math.max(0f, this.shadowBias);
            data.OverrideShadowNormalBias = this.overrideShadowNormalBias;
            data.ShadowNormalBias = math.max(0f, this.shadowNormalBias);
            data.OverrideShadowNearPlane = this.overrideShadowNearPlane;
            data.ShadowNearPlane = math.max(0f, this.shadowNearPlane);
            data.OverrideCookieSize = this.overrideCookieSize;
            data.CookieSize = new float2(Mathf.Max(0f, this.cookieSize.x), Mathf.Max(0f, this.cookieSize.y));
            data.OverrideCookie = this.overrideCookie;
            data.OverrideRenderMode = this.overrideRenderMode;
            data.RenderMode = this.renderMode;
            data.OverrideShadows = this.overrideShadows;
            data.Shadows = this.shadows;
            data.OverrideType = this.overrideType;
            data.Type = this.type;
            data.OverrideCullingMask = this.overrideCullingMask;
            data.CullingMask = this.cullingMask.value;
            data.OverrideRenderingLayerMask = this.overrideRenderingLayerMask;
            data.RenderingLayerMask = this.renderingLayerMask;

            clipComponent.Cookie = this.cookie;
        }

        private void OnValidate()
        {
            this.range = Mathf.Max(0f, this.range);
            this.spotAngle = Mathf.Clamp(this.spotAngle, 0f, 179f);
            var innerSpotLimit = this.overrideSpotAngle ? this.spotAngle : 179f;
            this.innerSpotAngle = Mathf.Clamp(this.innerSpotAngle, 0f, innerSpotLimit);
            this.shadowStrength = Mathf.Clamp01(this.shadowStrength);
            this.shadowBias = Mathf.Max(0f, this.shadowBias);
            this.shadowNormalBias = Mathf.Max(0f, this.shadowNormalBias);
            this.shadowNearPlane = Mathf.Max(0f, this.shadowNearPlane);
            this.cookieSize.x = Mathf.Max(0f, this.cookieSize.x);
            this.cookieSize.y = Mathf.Max(0f, this.cookieSize.y);
        }
    }
}
#endif
