// <copyright file="DrawHeader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using Unity.Mathematics;

    internal readonly struct DrawHeader
    {
        public readonly DrawType DrawType;
        public readonly half Duration;

        public DrawHeader(DrawType drawType, float duration)
        {
            this.DrawType = drawType;
            this.Duration = (half)duration;
        }
    }
}
