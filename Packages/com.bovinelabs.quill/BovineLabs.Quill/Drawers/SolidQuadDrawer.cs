// <copyright file="SolidQuadDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct SolidQuadDrawer : IDrawer
    {
        public float3 P0;
        public float3 P1;
        public float3 P2;
        public float3 P3;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            builder.DrawTriangle(this.P0, this.P1, this.P2, this.Color);
            builder.DrawTriangle(this.P2, this.P3, this.P0, this.Color);
        }
    }
}
