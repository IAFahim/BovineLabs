// <copyright file="TimeScaleInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.Time
{
    using Unity.Entities;

    /// <summary>
    /// Captures the global time scale when the track activates.
    /// </summary>
    public struct TimeScaleInitial : IComponentData
    {
        public float Value;
    }
}
