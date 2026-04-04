// <copyright file="Drawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary> Allows efficient drawing from burst jobs. </summary>
    public struct Drawer : IDrawWriter
    {
#if UNITY_EDITOR || BL_DEBUG
        private NativeThreadStream.Writer writer;
#endif

#if UNITY_EDITOR || BL_DEBUG
        internal Drawer(bool isEnabled, NativeThreadStream.Writer writer)
        {
            this.IsEnabled = isEnabled;
            this.writer = writer;
        }
#endif

#if UNITY_EDITOR || BL_DEBUG
        public bool IsEnabled { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IDrawWriter.Write<T>(T value)
        {
            this.writer.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IDrawWriter.WriteLarge<T>(NativeArray<T> array)
        {
            this.writer.WriteLarge(array);
        }
#else
        public bool IsEnabled => false;
#endif

        /// <summary> Draws a point. </summary>
        /// <param name="point"> The position of the point. </param>
        /// <param name="size"> The size of the point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Point(float3 point, float size, Color color, float duration = 0f)
        {
            DrawerImpl.Point(ref this, point, size, color, duration);
        }

        /// <summary> Draws a line. </summary>
        /// <param name="p0"> The starting point of the line. </param>
        /// <param name="p1"> The end point of the line. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Line(float3 p0, float3 p1, Color color, float duration = 0f)
        {
            DrawerImpl.Line(ref this, p0, p1, color, duration);
        }

        /// <summary> Draws a line. </summary>
        /// <param name="lines"> The line pairs to draw. Must be even length. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lines(NativeArray<float3> lines, Color color, float duration = 0f)
        {
            DrawerImpl.Lines(ref this, lines, color, duration);
        }

        /// <summary> Draws a ray. </summary>
        /// <param name="origin"> The starting point of the ray. </param>
        /// <param name="direction"> The direction the ray, the magnitude is how far it will travel. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ray(float3 origin, float3 direction, Color color, float duration = 0f)
        {
            DrawerImpl.Ray(ref this, origin, direction, color, duration);
        }

        /// <summary> Draws an arrow. </summary>
        /// <param name="origin"> The starting point of the arrow. </param>
        /// <param name="vector"> The distance and direction vector of the tip from the origin. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Arrow(float3 origin, float3 vector, Color color, float duration = 0f)
        {
            DrawerImpl.Arrow(ref this, origin, vector, color, duration);
        }

        /// <summary> Draws a plane. </summary>
        /// <param name="center"> The center. </param>
        /// <param name="direction"> The normal and size.  </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Plane(float3 center, float3 direction, Color color, float duration = 0f)
        {
            DrawerImpl.Plane(ref this, center, direction, color, duration);
        }

        /// <summary> Draws a circle. </summary>
        /// <param name="center"> The center of the circle. </param>
        /// <param name="direction"> The direction and radius. A value of (0,2,0) will draw a flat circle with a radius of 2. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Circle(float3 center, float3 direction, Color color, float duration = 0f)
        {
            DrawerImpl.Circle(ref this, center, direction, color, duration);
        }

        /// <summary> Draws an arc. </summary>
        /// <param name="center"> The center of the circle. </param>
        /// <param name="normal"> The normal of the arc. </param>
        /// <param name="arm"> The arm of the arc. </param>
        /// <param name="angle"> The angle of the arc. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Arc(float3 center, float3 normal, float3 arm, float angle, Color color, float duration = 0f)
        {
            DrawerImpl.Arc(ref this, center, normal, arm, angle, color, duration);
        }

        /// <summary> Draws a cone. </summary>
        /// <param name="point"> The apex of the cone. </param>
        /// <param name="direction"> The direction and size.. </param>
        /// <param name="angle"> The angle of the cone in radians. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cone(float3 point, float3 direction, float angle, Color color, float duration = 0f)
        {
            DrawerImpl.Cone(ref this, point, direction, angle, color, duration);
        }

        /// <summary> Draws a rectangular cuboid. </summary>
        /// <param name="center"> The center of the cuboid. </param>
        /// <param name="rotation"> The rotation of the cuboid. </param>
        /// <param name="size"> The size of the cuboid. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cuboid(float3 center, quaternion rotation, float3 size, Color color, float duration = 0f)
        {
            DrawerImpl.Cuboid(ref this, center, rotation, size, color, duration);
        }

        /// <summary> Draws a circular sector. </summary>
        /// <param name="point"> The centre of the circle. </param>
        /// <param name="forward"> The unit forward axis of the sector. </param>
        /// <param name="up"> The unit upward axis of the sector. </param>
        /// <param name="radius"> The radius of the circle. </param>
        /// <param name="angle"> The inner angle of the sector. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sector(float3 point, float3 forward, float3 up, float radius, float angle, Color color, float duration = 0f)
        {
            DrawerImpl.Sector(ref this, point, forward, up, radius, angle, color, duration);
        }

        /// <summary> Draws a triangle. </summary>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Triangle(float3 p0, float3 p1, float3 p2, Color color, float duration = 0f)
        {
            DrawerImpl.Triangle(ref this, p0, p1, p2, color, duration);
        }

        /// <summary> Draws a quad. </summary>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="p3"> Fourth point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Quad(float3 p0, float3 p1, float3 p2, float3 p3, Color color, float duration = 0f)
        {
            DrawerImpl.Quad(ref this, p0, p1, p2, p3, color, duration);
        }

        /// <summary> Draws a cylinder. Defaults to the Z forward direction. </summary>
        /// <param name="center"> The center of the cylinder. </param>
        /// <param name="rotation"> The rotation of the cylinder. Faces up on identity. </param>
        /// <param name="height"> Height of the cylinder. </param>
        /// <param name="radius"> Radius of the cylinder. </param>
        /// <param name="sideCount"> The number of sides to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cylinder(float3 center, quaternion rotation, float height, float radius, int sideCount, Color color, float duration = 0f)
        {
            DrawerImpl.Cylinder(ref this, center, rotation, height, radius, sideCount, color, duration);
        }

        /// <summary> Draws a capsule. </summary>
        /// <param name="center"> The center of the capsule. </param>
        /// <param name="rotation"> The rotation of the capsule. Faces up on identity. </param>
        /// <param name="height"> Height of the capsule. </param>
        /// <param name="radius"> Radius of the capsule. </param>
        /// <param name="sideCount"> The number of sides to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Capsule(float3 center, quaternion rotation, float height, float radius, int sideCount, Color color, float duration = 0f)
        {
            DrawerImpl.Capsule(ref this, center, rotation, height, radius, sideCount, color, duration);
        }

        /// <summary> Draws a sphere. </summary>
        /// <param name="center"> The center of the sphere. </param>
        /// <param name="radius"> Radius of the cylinder. </param>
        /// <param name="sideCount"> The number of sides to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sphere(float3 center, float radius, int sideCount, Color color, float duration = 0f)
        {
            DrawerImpl.Sphere(ref this, center, radius, sideCount, color, duration);
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text32(float3 position, FixedString32Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text32(ref this, position, text, color, size, duration);
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text64(float3 position, FixedString64Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text64(ref this, position, text, color, size, duration);
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text128(float3 position, FixedString128Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text128(ref this, position, text, color, size, duration);
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text512(float3 position, FixedString512Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text512(ref this, position, text, color, size, duration);
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text4096(float3 position, FixedString4096Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text4096(ref this, position, text, color, size, duration);
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text32(float3 position, quaternion rotation, FixedString32Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text32(ref this, position, rotation, text, color, size, duration);
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text64(float3 position, quaternion rotation, FixedString64Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text64(ref this, position, rotation, text, color, size, duration);
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text128(float3 position, quaternion rotation, FixedString128Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text128(ref this, position, rotation, text, color, size, duration);
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text512(float3 position, quaternion rotation, FixedString512Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text512(ref this, position, rotation, text, color, size, duration);
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Text4096(float3 position, quaternion rotation, FixedString4096Bytes text, Color color, float size = 16f, float duration = 0f)
        {
            DrawerImpl.Text4096(ref this, position, rotation, text, color, size, duration);
        }

        /// <summary> Draws a solid triangle. </summary>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SolidTriangle(float3 p0, float3 p1, float3 p2, Color color, float duration = 0f)
        {
            DrawerImpl.SolidTriangle(ref this, p0, p1, p2, color, duration);
        }

        /// <summary> Draws a solid quad. </summary>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="p3"> Forth point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SolidQuad(float3 p0, float3 p1, float3 p2, float3 p3, Color color, float duration = 0f)
        {
            DrawerImpl.SolidQuad(ref this, p0, p1, p2, p3, color, duration);
        }

        /// <summary> Draws a collection of solid triangles. </summary>
        /// <param name="triangles"> Collection of triangles point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SolidTriangles(NativeArray<float3x3> triangles, Color color, float duration = 0f)
        {
            DrawerImpl.SolidTriangles(ref this, triangles, color, duration);
        }
    }
}
