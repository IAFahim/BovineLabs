// <copyright file="CMFreeLookModifierClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Entities;

    /// <summary>
    /// Differentiates Cinemachine free look modifier clip behaviour.
    /// </summary>
    public enum CMFreeLookModifierClipType : byte
    {
        Animated,
        Initial,
    }

    /// <summary>
    /// Serialized configuration baked for each Cinemachine free look modifier clip.
    /// </summary>
    public struct CMFreeLookModifierClipData : IComponentData
    {
        public CMFreeLookModifierClipType Type;
        public float Easing;
    }
}
#endif
