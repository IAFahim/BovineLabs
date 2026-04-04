// <copyright file="DrawBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;

    internal unsafe struct DrawBuilder : IDisposable
    {
        private readonly SDFCharacter* characterInfo;

        private UnsafeList<LineVertex> lineVertices;
        private UnsafeList<TextVertex> textVertices;
        private UnsafeList<SolidVertex> solidVertices;

        public DrawBuilder(NativeArray<SDFCharacter> font, Allocator allocator)
        {
            this.characterInfo = (SDFCharacter*)font.GetUnsafeReadOnlyPtr();

            this.lineVertices = new UnsafeList<LineVertex>(0, allocator);
            this.textVertices = new UnsafeList<TextVertex>(0, allocator);
            this.solidVertices = new UnsafeList<SolidVertex>(0, allocator);

            this.CameraRotation = quaternion.identity;
        }

        public UnsafeList<LineVertex> LineVertices => this.lineVertices;

        public UnsafeList<SolidVertex> SolidVertices => this.solidVertices;

        public UnsafeList<TextVertex> TextVertices => this.textVertices;

        public quaternion CameraRotation { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.lineVertices.Dispose();
            this.textVertices.Dispose();
            this.solidVertices.Dispose();
        }

        public void Reset()
        {
            this.lineVertices.Clear();
            this.textVertices.Clear();
            this.solidVertices.Clear();
        }

        public void DrawLine(float3 a, float3 b, Color color)
        {
            this.lineVertices.Add(new LineVertex
            {
                Start = new float4(a.xyz, 1),
                End = new float4(b.xyz, 1),
                Color = UnsafeUtility.As<Color, float4>(ref color),
            });
        }

        public void DrawLines(NativeArray<float3> lines, Color color)
        {
            if (this.lineVertices.Capacity < this.lineVertices.Length + (lines.Length / 2))
            {
                this.lineVertices.SetCapacity(this.lineVertices.Length + (lines.Length / 2));
            }

            for (var i = 0; i < lines.Length; i += 2)
            {
                var a = lines[i];
                var b = lines[i + 1];

                this.lineVertices.AddNoResize(new LineVertex
                {
                    Start = new float4(a.xyz, 1),
                    End = new float4(b.xyz, 1),
                    Color = UnsafeUtility.As<Color, float4>(ref color),
                });
            }
        }

        public void DrawText(float3 position, byte* text, int length, float size, Color color)
        {
            this.DrawText3D(position, this.CameraRotation, text, length, size, color);
        }

        public void DrawText3D(float3 position, quaternion rotation, byte* text, int length, float size, Color color)
        {
            var right = math.mul(rotation, math.right());
            var up = math.mul(rotation, math.up());

            var fontWorldSize = size / 100f; // 100 pretty standard pixel per m scaling

            right *= fontWorldSize;
            up *= fontWorldSize;

            this.AddText(text, position, right, up, length, color);
        }

        public void DrawTriangle(float3 a, float3 b, float3 c, Color color)
        {
            this.solidVertices.Add(new SolidVertex
            {
                Vertex0 = new float4(a, 1),
                Vertex1 = new float4(b, 1),
                Vertex2 = new float4(c, 1),
                Color = UnsafeUtility.As<Color, float4>(ref color),
            });
        }

        private void AddText(byte* text, float3 position, float3 right, float3 up, int numCharacters, Color color)
        {
            const byte newLine = (byte)'\n';

            // Replace non-ASCII characters with '?'.
            for (var i = 0; i < numCharacters; i++)
            {
                var c = text + i;
                if (*c >= 128)
                {
                    *c = (byte)'?';
                }
            }

            float maxWidth = 0;
            float currentWidth = 0;
            float numLines = 1;

            // Calculate layout dimensions: max width and total number of lines.
            for (var i = 0; i < numCharacters; i++)
            {
                var characterInfoIndex = text[i];
                if (characterInfoIndex == newLine)
                {
                    maxWidth = math.max(maxWidth, currentWidth);
                    currentWidth = 0;
                    numLines++;
                }
                else
                {
                    currentWidth += this.characterInfo[characterInfoIndex].Width;
                }
            }

            maxWidth = math.max(maxWidth, currentWidth);

            // Adjust starting position based on pivot and layout.
            var pos = position;
            pos -= right * maxWidth * 0.5f;

            var lower = 1 - numLines;
            var yAdjustment = math.lerp(lower, 0.75f, 0.5f);
            pos -= up * yAdjustment;

            var lineStart = pos;

            // Process each character, compute vertex positions, and update bounds.
            for (var i = 0; i < numCharacters; i++)
            {
                var characterInfoIndex = text[i];

                // Handle new line: move to the beginning of the next line.
                if (characterInfoIndex == newLine)
                {
                    lineStart -= up;
                    pos = lineStart;
                    continue;
                }

                var ch = this.characterInfo[characterInfoIndex];

                // Compute vertex positions for the character quad.
                var v0 = pos + (ch.VertexTopLeft.x * right) + (ch.VertexTopLeft.y * up);
                var v1 = pos + (ch.VertexTopRight.x * right) + (ch.VertexTopRight.y * up);
                var v2 = pos + (ch.VertexBottomRight.x * right) + (ch.VertexBottomRight.y * up);
                var v3 = pos + (ch.VertexBottomLeft.x * right) + (ch.VertexBottomLeft.y * up);

                this.textVertices.Add(new TextVertex
                {
                    Vertex0 = new float4(v0, 1),
                    Vertex1 = new float4(v1, 1),
                    Vertex2 = new float4(v2, 1),
                    Vertex3 = new float4(v3, 1),
                    Color = UnsafeUtility.As<Color, float4>(ref color),
                    UV01 = new float4(ch.UVTopLeft, ch.UVTopRight),
                    UV23 = new float4(ch.UVBottomRight, ch.UVBottomLeft),
                });

                // Advance horizontal position by character width.
                pos += right * ch.Width;
            }
        }
    }
}
