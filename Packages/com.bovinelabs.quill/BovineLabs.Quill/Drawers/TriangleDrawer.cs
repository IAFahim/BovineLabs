// <copyright file="TriangleDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct TriangleDrawer : IDrawer
    {
        public float3 P0;
        public float3 P1;
        public float3 P2;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            // Draw the rightmost boundary
            builder.DrawLine(this.P0, this.P1, this.Color);
            builder.DrawLine(this.P1, this.P2, this.Color);
            builder.DrawLine(this.P2, this.P0, this.Color);
        }
    }
}
