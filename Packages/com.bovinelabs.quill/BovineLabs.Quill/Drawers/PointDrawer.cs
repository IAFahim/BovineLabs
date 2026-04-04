// <copyright file="PointDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct PointDrawer : IDrawer
    {
        public float3 Position;
        public float Size;
        public Color Color;

        public void Draw(ref DrawBuilder drawer)
        {
            drawer.DrawLine(this.Position - new float3(this.Size, 0, 0), this.Position + new float3(this.Size, 0, 0), this.Color);
            drawer.DrawLine(this.Position - new float3(0, this.Size, 0), this.Position + new float3(0, this.Size, 0), this.Color);
            drawer.DrawLine(this.Position - new float3(0, 0, this.Size), this.Position + new float3(0, 0, this.Size), this.Color);
        }
    }
}
