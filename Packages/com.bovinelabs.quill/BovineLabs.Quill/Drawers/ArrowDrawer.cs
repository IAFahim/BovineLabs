// <copyright file="ArrowDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using BovineLabs.Core.Utility;
    using Unity.Mathematics;
    using UnityEngine;

    internal struct ArrowDrawer : IDrawer
    {
        public float3 X0;
        public float3 X1;
        public Color Color;

        public void Draw(ref DrawBuilder builder)
        {
            if (math.all(this.X0 == this.X1))
            {
                return;
            }

            var v = this.X1 - this.X0;
            var length = mathex.NormalizeWithLength(v, out var dir);
            mathex.CalculatePerpendicularNormalized(dir, out var p, out var q);

            var scale = length * 0.2f;

            builder.DrawLine(this.X0, this.X1, this.Color);

            builder.DrawLine(this.X1, this.X1 + ((p - dir) * scale), this.Color);
            builder.DrawLine(this.X1, this.X1 - ((p + dir) * scale), this.Color);
            builder.DrawLine(this.X1, this.X1 + ((q - dir) * scale), this.Color);
            builder.DrawLine(this.X1, this.X1 - ((q + dir) * scale), this.Color);
        }
    }
}
