// <copyright file="GlobalDraw.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

#if UNITY_EDITOR
    [Configurable]
#endif
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:Element parameters should be documented", Justification = "Using see cref")]
    public static class GlobalDraw
    {
#if UNITY_EDITOR
        private const string DrawGlobalEnabled = "draw.enabled-global";
        private const string DrawGlobalDescEnabled = "Enable the global drawer in the editor.";

        [ConfigVar(DrawGlobalEnabled, false, DrawGlobalDescEnabled)]
        internal static readonly SharedStatic<bool> Enabled = SharedStatic<bool>.GetOrCreate<EnabledType>();

        internal static readonly SharedStatic<DrawerUnsafe> Draw = SharedStatic<DrawerUnsafe>.GetOrCreate<DrawThreadType>();
#endif

        /// <summary> <see cref="Drawer.Point"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Point(float3 point, float size, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Point(ref Draw.Data, point, size, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Line"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Line(float3 p0, float3 p1, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Line(ref Draw.Data, p0, p1, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Lines"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Lines(NativeArray<float3> lines, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Lines(ref Draw.Data, lines, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Ray"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Ray(float3 origin, float3 direction, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Ray(ref Draw.Data, origin, direction, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Arrow"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Arrow(float3 origin, float3 vector, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Arrow(ref Draw.Data, origin, vector, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Plane"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Plane(float3 center, float3 direction, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Plane(ref Draw.Data, center, direction, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Circle"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Circle(float3 center, float3 direction, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Circle(ref Draw.Data, center, direction, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Arc"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Arc(float3 center, float3 normal, float3 arm, float angle, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Arc(ref Draw.Data, center, normal, arm, angle, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Cone"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Cone(float3 point, float3 direction, float angle, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Cone(ref Draw.Data, point, direction, angle, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Cuboid"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Cuboid(float3 center, quaternion rotation, float3 size, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Cuboid(ref Draw.Data, center, rotation, size, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Sector"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Sector(float3 point, float3 forward, float3 up, float radius, float angle, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Sector(ref Draw.Data, point, forward, up, radius, angle, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Triangle"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Triangle(float3 p0, float3 p1, float3 p2, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Triangle(ref Draw.Data, p0, p1, p2, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Quad"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Quad(float3 p0, float3 p1, float3 p2, float3 p3, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Quad(ref Draw.Data, p0, p1, p2, p3, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Cylinder"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Cylinder(float3 center, quaternion rotation, float height, float radius, int sideCount, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Cylinder(ref Draw.Data, center, rotation, height, radius, sideCount, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Capsule"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Capsule(float3 center, quaternion rotation, float height, float radius, int sideCount, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Capsule(ref Draw.Data, center, rotation, height, radius, sideCount, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Sphere"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Sphere(float3 center, float radius, int sideCount, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Sphere(ref Draw.Data, center, radius, sideCount, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text32(float3, FixedString32Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text32(float3 position, FixedString32Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text32(ref Draw.Data, position, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text64(float3, FixedString64Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text64(float3 position, FixedString64Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text64(ref Draw.Data, position, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text128(float3, FixedString128Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text128(float3 position, FixedString128Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text128(ref Draw.Data, position, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text512(float3, FixedString512Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text512(float3 position, FixedString512Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text512(ref Draw.Data, position, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text4096(float3, FixedString4096Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text4096(float3 position, FixedString4096Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text4096(ref Draw.Data, position, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text32(float3, quaternion, FixedString32Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text32(float3 position, quaternion rotation, FixedString32Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text32(ref Draw.Data, position, rotation, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text64(float3, quaternion, FixedString64Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text64(float3 position, quaternion rotation, FixedString64Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text64(ref Draw.Data, position, rotation, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text128(float3, quaternion, FixedString128Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text128(float3 position, quaternion rotation, FixedString128Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text128(ref Draw.Data, position, rotation, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text512(float3, quaternion, FixedString512Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text512(float3 position, quaternion rotation, FixedString512Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text512(ref Draw.Data, position, rotation, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.Text4096(float3, quaternion, FixedString4096Bytes, Color, float, float)"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Text4096(float3 position, quaternion rotation, FixedString4096Bytes text, Color color, float size = 16f, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.Text4096(ref Draw.Data, position, rotation, text, color, size, duration);
#endif
        }

        /// <summary> <see cref="Drawer.SolidTriangle"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void SolidTriangle(float3 p0, float3 p1, float3 p2, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.SolidTriangle(ref Draw.Data, p0, p1, p2, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.SolidQuad"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void SolidQuad(float3 p0, float3 p1, float3 p2, float3 p3, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.SolidQuad(ref Draw.Data, p0, p1, p2, p3, color, duration);
#endif
        }

        /// <summary> <see cref="Drawer.SolidTriangles"/>. </summary>
        [Conditional("UNITY_EDITOR")]
        public static void SolidTriangles(NativeArray<float3x3> triangles, Color color, float duration = 0f)
        {
#if UNITY_EDITOR
            DrawerImpl.SolidTriangles(ref Draw.Data, triangles, color, duration);
#endif
        }

#if UNITY_EDITOR
        private struct EnabledType
        {
        }

        private struct DrawThreadType
        {
        }
#endif
    }
}
