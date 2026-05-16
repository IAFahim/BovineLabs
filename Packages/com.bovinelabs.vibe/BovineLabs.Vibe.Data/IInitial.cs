// <copyright file="IInitial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data
{
    using Unity.Entities;

    public interface IInitial<T> : IComponentData
        where T : unmanaged, IComponentData
    {
        T Value { get; set; }
    }
}
