// <copyright file="TextDrawer3D.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    internal struct TextDrawer3D<T> : IDrawer
        where T : unmanaged, IUTF8Bytes, INativeList<byte>
    {
        public float3 Position;
        public quaternion Rotation;
        public T Text;
        public float Size;
        public Color Color;

        public unsafe void Draw(ref DrawBuilder builder)
        {
            builder.DrawText3D(this.Position, this.Rotation, this.Text.GetUnsafePtr(), this.Text.Length, this.Size, this.Color);
        }
    }
}
