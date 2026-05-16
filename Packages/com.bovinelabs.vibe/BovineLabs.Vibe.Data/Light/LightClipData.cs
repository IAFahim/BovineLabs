// <copyright file="LightClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Light
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Identifies how a light clip should animate the bound light component.
    /// </summary>
    public enum LightClipType : byte
    {
        Constant,
        Initial,
        Flicker,
        ExtendedConstant,
        ExtendedCurve,
    }

    /// <summary>
    /// Preset shapes used when generating flicker patterns.
    /// </summary>
    public enum LightFlickerPreset : byte
    {
        Strobe,
        Buzz,
        Organic,
    }

    /// <summary>
    /// Component containing the blob asset that defines a light clip.
    /// </summary>
    public struct LightClipData : IComponentData
    {
        /// <summary>
        /// Blob holding the serialized configuration for the clip.
        /// </summary>
        public BlobAssetReference<LightClipBlobData> Value;

        /// <summary>
        /// Cookie texture override stored outside the blob.
        /// </summary>
        public UnityObjectRef<Texture> Cookie;
    }

    /// <summary>
    /// Union-style blob that stores the data required to evaluate a light clip.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct LightClipBlobData
    {
        [FieldOffset(0)]
        public LightClipType Type;

        [FieldOffset(4)]
        public LightConstantData Constant;

        [FieldOffset(4)]
        public LightFlickerData Flicker;

        [FieldOffset(4)]
        public LightExtendedData ExtendedConstant;

        [FieldOffset(4)]
        public LightExtendedCurveData ExtendedCurve;
    }

    /// <summary>
    /// Constant color/intensity overrides applied while the clip is active.
    /// </summary>
    public struct LightConstantData
    {
        public float3 Color;
        public float Intensity;
        public float ColorTemperature;
        public bool OverrideColor;
        public bool OverrideIntensity;
        public bool OverrideColorTemperature;
    }

    /// <summary>
    /// Parameters used to generate light flicker animation.
    /// </summary>
    public struct LightFlickerData
    {
        public LightFlickerPreset Preset;
        public float Speed;
        public float MinIntensityMultiplier;
        public float MaxIntensityMultiplier;
        public float DutyCycle;
        public uint Seed;
        public bool UseCustomCurve;
        public bool OverrideColor;
        public bool OverrideColorTemperature;
        public BlobCurve Curve;
        public float3 ColorA;
        public float3 ColorB;
        public float TemperatureA;
        public float TemperatureB;
    }

    /// <summary>
    /// Constant overrides applied to extended light properties.
    /// </summary>
    public struct LightExtendedData
    {
        public float Range;
        public float SpotAngle;
        public float InnerSpotAngle;
        public float ShadowStrength;
        public float ShadowBias;
        public float ShadowNormalBias;
        public float ShadowNearPlane;
        public float2 CookieSize;
        public LightRenderMode RenderMode;
        public LightShadows Shadows;
        public LightType Type;
        public int CullingMask;
        public int RenderingLayerMask;
        public bool OverrideRange;
        public bool OverrideSpotAngle;
        public bool OverrideInnerSpotAngle;
        public bool OverrideShadowStrength;
        public bool OverrideShadowBias;
        public bool OverrideShadowNormalBias;
        public bool OverrideShadowNearPlane;
        public bool OverrideCookieSize;
        public bool OverrideCookie;
        public bool OverrideRenderMode;
        public bool OverrideShadows;
        public bool OverrideType;
        public bool OverrideCullingMask;
        public bool OverrideRenderingLayerMask;
    }

    /// <summary>
    /// Curve-driven overrides for extended light properties.
    /// </summary>
    public struct LightExtendedCurveData
    {
        public LightExtendedCurve Range;
        public LightExtendedCurve SpotAngle;
        public LightExtendedCurve InnerSpotAngle;
        public LightExtendedCurve ShadowStrength;
    }

    /// <summary>
    /// Describes a curve-driven remap for a single extended light property.
    /// </summary>
    public struct LightExtendedCurve
    {
        public BlobCurve Curve;
        public float Min;
        public float Max;
        public bool Relative;
        public bool UseInitial;
        public bool OverrideValue;
    }
}
#endif
