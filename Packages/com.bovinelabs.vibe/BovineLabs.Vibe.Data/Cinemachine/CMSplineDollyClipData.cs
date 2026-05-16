// <copyright file="CMSplineDollyClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;

    /// <summary>
    /// Differentiates spline dolly clip behaviour.
    /// </summary>
    public enum CMSplineDollyClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each spline dolly clip.
    /// </summary>
    public struct CMSplineDollyClipData : IComponentData
    {
        public CMSplineDollyClipType Type;
        public float Position;
    }
}
#endif
