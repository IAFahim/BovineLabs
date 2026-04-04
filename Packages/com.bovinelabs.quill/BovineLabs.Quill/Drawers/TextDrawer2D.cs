// <copyright file="TextDrawer2D.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    internal struct TextDrawer2D<T> : IDrawer
        where T : unmanaged, IUTF8Bytes, INativeList<byte>
    {
        public float3 Position;
        public float Size;
        public Color Color;
        public T Text;

        public unsafe void Draw(ref DrawBuilder builder)
        {
            builder.DrawText(this.Position, this.Text.GetUnsafePtr(), this.Text.Length, this.Size, this.Color);
        }
    }
}
