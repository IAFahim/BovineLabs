// <copyright file="LineDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct LineDrawer : IDrawer
    {
        public float3 P0;
        public float3 P1;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            builder.DrawLine(this.P0, this.P1, this.Color);
        }
    }
}
