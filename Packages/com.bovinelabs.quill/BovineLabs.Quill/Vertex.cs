// <copyright file="Vertex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using Unity.Mathematics;
    using UnityEngine;

    internal struct LineVertex
    {
        public float4 Start;
        public float4 End;
        public float4 Color;
        public float4 Padding;
    }

    internal struct SolidVertex
    {
        public float4 Vertex0;
        public float4 Vertex1;
        public float4 Vertex2;
        public float4 Color;
    }

    internal struct TextVertex
    {
        public float4 Vertex0;
        public float4 Vertex1;
        public float4 Vertex2;
        public float4 Vertex3;
        public float4 Color;
        public float4 UV01;
        public float4 UV23;
        public float4 Padding;
    }
}
