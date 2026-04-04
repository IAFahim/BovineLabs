// <copyright file="DrawerImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using System.Diagnostics;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Quill.Drawers;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    internal static class DrawerImpl
    {
        /// <summary> Draws a point. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="point"> The position of the point. </param>
        /// <param name="size"> The size of the point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Point<T>(ref T drawer, float3 point, float size, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Point, duration));
            drawer.Write(new PointDrawer
            {
                Position = point,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a line. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="p0"> The starting point of the line. </param>
        /// <param name="p1"> The end point of the line. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Line<T>(ref T drawer, float3 p0, float3 p1, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Line, duration));
            drawer.Write(new LineDrawer
            {
                P0 = p0,
                P1 = p1,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a line. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="lines"> The line pairs to draw. Must be even length. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Lines<T>(ref T drawer, NativeArray<float3> lines, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            Check.Assume(lines.Length % 2 == 0, "Odd number of line segments");

            drawer.Write(new DrawHeader(DrawType.Lines, duration));
            drawer.Write(new LinesDrawer
            {
                Color = color,
                Count = lines.Length,
            });

            drawer.WriteLarge(lines);
#endif
        }

        /// <summary> Draws a ray. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="origin"> The starting point of the ray. </param>
        /// <param name="direction"> The direction the ray, the magnitude is how far it will travel. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Ray<T>(ref T drawer, float3 origin, float3 direction, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Line, duration));
            drawer.Write(new LineDrawer
            {
                P0 = origin,
                P1 = origin + direction,
                Color = color,
            });
#endif
        }

        /// <summary> Draws an arrow. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="origin"> The starting point of the arrow. </param>
        /// <param name="vector"> The distance and direction vector of the tip from the origin. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Arrow<T>(ref T drawer, float3 origin, float3 vector, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Arrow, duration));
            drawer.Write(new ArrowDrawer
            {
                X0 = origin,
                X1 = origin + vector,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a plane. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="center"> The center. </param>
        /// <param name="direction"> The normal and size.  </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Plane<T>(ref T drawer, float3 center, float3 direction, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Plane, duration));
            drawer.Write(new PlaneDrawer
            {
                Center = center,
                Direction = direction,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a circle. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="center"> The center of the circle. </param>
        /// <param name="direction"> The direction and size. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Circle<T>(ref T drawer, float3 center, float3 direction, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Circle, duration));
            drawer.Write(new CircleDrawer
            {
                Center = center,
                Direction = direction,
                Color = color,
            });
#endif
        }

        /// <summary> Draws an arc. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="center"> The center of the circle. </param>
        /// <param name="normal"> The normal of the arc. </param>
        /// <param name="arm"> The arm of the arc. </param>
        /// <param name="angle"> The angle of the arc. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Arc<T>(ref T drawer, float3 center, float3 normal, float3 arm, float angle, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Arc, duration));
            drawer.Write(new ArcDrawer
            {
                Center = center,
                Normal = normal,
                Arm = arm,
                Angle = angle,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a cone. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="point"> The apex of the cone. </param>
        /// <param name="direction"> The direction and size.. </param>
        /// <param name="angle"> The angle of the cone in radians. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Cone<T>(ref T drawer, float3 point, float3 direction, float angle, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Cone, duration));
            drawer.Write(new ConeDrawer
            {
                Point = point,
                Direction = direction,
                Angle = angle,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a rectangular cuboid. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="center"> The center of the cuboid. </param>
        /// <param name="rotation"> The rotation of the cuboid. </param>
        /// <param name="size"> The size of the cuboid. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Cuboid<T>(ref T drawer, float3 center, quaternion rotation, float3 size, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Cuboid, duration));
            drawer.Write(new CuboidDrawer
            {
                Size = size,
                Center = center,
                Rotation = rotation,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a circular sector. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="point"> The centre of the circle. </param>
        /// <param name="forward"> The unit forward axis of the sector. </param>
        /// <param name="up"> The unit upward axis of the sector. </param>
        /// <param name="radius"> The radius of the circle. </param>
        /// <param name="angle"> The inner angle of the sector. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Sector<T>(ref T drawer, float3 point, float3 forward, float3 up, float radius, float angle, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Sector, duration));
            drawer.Write(new SectorDrawer
            {
                Point = point,
                Forward = forward,
                Up = up,
                Radius = radius,
                Angle = angle,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a triangle. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Triangle<T>(ref T drawer, float3 p0, float3 p1, float3 p2, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Triangle, duration));
            drawer.Write(new TriangleDrawer
            {
                P0 = p0,
                P1 = p1,
                P2 = p2,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a quad. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="p3"> Fourth point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Quad<T>(ref T drawer, float3 p0, float3 p1, float3 p2, float3 p3, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Quad, duration));
            drawer.Write(new QuadDrawer
            {
                P0 = p0,
                P1 = p1,
                P2 = p2,
                P3 = p3,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a cylinder. Defaults to the Z forward direction. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="center"> The center of the cylinder. </param>
        /// <param name="rotation"> The rotation of the cylinder. Faces up on identity. </param>
        /// <param name="height"> Height of the cylinder. </param>
        /// <param name="radius"> Radius of the cylinder. </param>
        /// <param name="sideCount"> The number of sides to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Cylinder<T>(
            ref T drawer, float3 center, quaternion rotation, float height, float radius, int sideCount, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Cylinder, duration));
            drawer.Write(new CylinderDrawer
            {
                Center = center,
                Rotation = rotation,
                Height = height,
                Radius = radius,
                SideCount = sideCount,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a capsule. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="center"> The center of the capsule. </param>
        /// <param name="rotation"> The rotation of the capsule. Faces up on identity. </param>
        /// <param name="height"> Height of the capsule. </param>
        /// <param name="radius"> Radius of the capsule. </param>
        /// <param name="sideCount"> The number of sides to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Capsule<T>(
            ref T drawer, float3 center, quaternion rotation, float height, float radius, int sideCount, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Capsule, duration));
            drawer.Write(new CapsuleDrawer
            {
                Center = center,
                Rotation = rotation,
                Height = height,
                Radius = radius,
                SideCount = sideCount,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a sphere. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="center"> The center of the sphere. </param>
        /// <param name="radius"> Radius of the cylinder. </param>
        /// <param name="sideCount"> The number of sides to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Sphere<T>(ref T drawer, float3 center, float radius, int sideCount, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Sphere, duration));
            drawer.Write(new SphereDrawer
            {
                Center = center,
                Radius = radius,
                SideCount = sideCount,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text32<T>(ref T drawer, float3 position, FixedString32Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text2D32, duration));
            drawer.Write(new TextDrawer2D<FixedString32Bytes>
            {
                Position = position,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text64<T>(ref T drawer, float3 position, FixedString64Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text2D64, duration));
            drawer.Write(new TextDrawer2D<FixedString64Bytes>
            {
                Position = position,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text128<T>(ref T drawer, float3 position, FixedString128Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text2D128, duration));
            drawer.Write(new TextDrawer2D<FixedString128Bytes>
            {
                Position = position,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text512<T>(ref T drawer, float3 position, FixedString512Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text2D512, duration));
            drawer.Write(new TextDrawer2D<FixedString512Bytes>
            {
                Position = position,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text that always looks at camera. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="text"> The text to draw.</param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text4096<T>(ref T drawer, float3 position, FixedString4096Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text2D4096, duration));
            drawer.Write(new TextDrawer2D<FixedString4096Bytes>
            {
                Position = position,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text32<T>(
            ref T drawer, float3 position, quaternion rotation, FixedString32Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text3D32, duration));
            drawer.Write(new TextDrawer3D<FixedString32Bytes>
            {
                Position = position,
                Rotation = rotation,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text64<T>(
            ref T drawer, float3 position, quaternion rotation, FixedString64Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text3D64, duration));
            drawer.Write(new TextDrawer3D<FixedString64Bytes>
            {
                Position = position,
                Rotation = rotation,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text128<T>(
            ref T drawer, float3 position, quaternion rotation, FixedString128Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text3D128, duration));
            drawer.Write(new TextDrawer3D<FixedString128Bytes>
            {
                Position = position,
                Rotation = rotation,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text512<T>(
            ref T drawer, float3 position, quaternion rotation, FixedString512Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text3D512, duration));
            drawer.Write(new TextDrawer3D<FixedString512Bytes>
            {
                Position = position,
                Rotation = rotation,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws text with a specific rotation. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="position"> The position. </param>
        /// <param name="rotation"> The rotation of the text. </param>
        /// <param name="text"> The text to draw. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="size"> The size of the text. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void Text4096<T>(
            ref T drawer, float3 position, quaternion rotation, FixedString4096Bytes text, Color color, float size = 16f, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.Text3D4096, duration));
            drawer.Write(new TextDrawer3D<FixedString4096Bytes>
            {
                Position = position,
                Rotation = rotation,
                Text = text,
                Size = size,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a solid triangle. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void SolidTriangle<T>(ref T drawer, float3 p0, float3 p1, float3 p2, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.SolidTriangle, duration));
            drawer.Write(new SolidTriangleDrawer
            {
                P0 = p0,
                P1 = p1,
                P2 = p2,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a solid quad. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="p0"> First point. </param>
        /// <param name="p1"> Second point. </param>
        /// <param name="p2"> Third point. </param>
        /// <param name="p3"> Forth point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void SolidQuad<T>(ref T drawer, float3 p0, float3 p1, float3 p2, float3 p3, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.SolidQuad, duration));
            drawer.Write(new SolidQuadDrawer
            {
                P0 = p0,
                P1 = p1,
                P2 = p2,
                P3 = p3,
                Color = color,
            });
#endif
        }

        /// <summary> Draws a collection of solid triangles. </summary>
        /// <param name="drawer"> The drawer. </param>
        /// <param name="triangles"> Collection of triangles point. </param>
        /// <param name="color"> The Color to draw with. </param>
        /// <param name="duration"> How long to draw this. </param>
        /// <typeparam name="T"> The drawer type. </typeparam>
        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public static void SolidTriangles<T>(ref T drawer, NativeArray<float3x3> triangles, Color color, float duration = 0f)
            where T : unmanaged, IDrawWriter
        {
#if UNITY_EDITOR || BL_DEBUG
            if (!drawer.IsEnabled)
            {
                return;
            }

            drawer.Write(new DrawHeader(DrawType.SolidTriangles, duration));
            drawer.Write(new SolidTrianglesDrawer
            {
                Color = color,
                Count = triangles.Length,
            });

            drawer.WriteLarge(triangles);
#endif
        }
    }
}
