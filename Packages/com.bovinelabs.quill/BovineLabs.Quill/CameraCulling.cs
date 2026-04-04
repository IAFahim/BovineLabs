// <copyright file="CameraCulling.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Assertions;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    // basically the same as Core CameraPlanes
    [SuppressMessage("ReSharper", "UnassignedField.Global", Justification = "Reinterpret")]
    public unsafe struct CameraCulling : IComponentData, IEquatable<CameraCulling>
    {
        public float4 Left;
        public float4 Right;
        public float4 Bottom;
        public float4 Top;
        public float4 Near;
        public float4 Far;

        public static CameraCulling Create(Camera camera, Plane[] planes, float maxRange)
        {
            Check.Assume(planes.Length == 6);

            GeometryUtility.CalculateFrustumPlanes(camera.projectionMatrix * camera.worldToCameraMatrix, planes);

            // Copied from FrustumPlanes but avoiding array allocation each frame
            var cameraToWorld = camera.cameraToWorldMatrix;
            var eyePos = cameraToWorld.MultiplyPoint(Vector3.zero);
            var viewDir = new float3(cameraToWorld.m02, cameraToWorld.m12, cameraToWorld.m22);
            viewDir = -math.normalizesafe(viewDir);

            // Near Plane
            planes[4].SetNormalAndPosition(viewDir, eyePos);
            planes[4].distance -= camera.nearClipPlane;

            // Far plane
            planes[5].SetNormalAndPosition(-viewDir, eyePos);
            planes[5].distance += math.clamp(maxRange, camera.nearClipPlane, camera.farClipPlane);

            var culling = default(CameraCulling);

            for (var i = 0; i < 6; ++i)
            {
                culling[i] = new float4(planes[i].normal, planes[i].distance);
            }

            return culling;
        }

        public bool IsDefault => this.Equals(default);

        public float4 this[int index]
        {
            get
            {
                CheckRange(index);
                fixed (CameraCulling* v = &this)
                {
                    return ((float4*)v)[index];
                }
            }

            set
            {
                CheckRange(index);
                fixed (CameraCulling* v = &this)
                {
                    ((float4*)v)[index] = value;
                }
            }
        }

        public bool AnyIntersect(AABB a)
        {
            if (this.IsDefault)
            {
                return false;
            }

            var m = a.Center;
            var extent = a.Extents;

            for (var i = 0; i < 6; i++)
            {
                var normal = this[i].xyz;
                var dist = math.dot(normal, m) + this[i].w;
                var radius = math.dot(extent, math.abs(normal));
                if (dist + radius <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        public CameraCulling GetWithFarClipDistance(float newFar)
        {
            if (this.IsDefault)
            {
                return this;
            }

            var copy = this;

            // 0) Normalize for numerical stability
            for (var i = 0; i < 6; i++)
            {
                copy[i] = NormalizePlane(copy[i]);
            }

            // 1) Eye position = intersection of three side planes
            var eye = Intersect3(copy[0], copy[1], copy[3]); // Left, Right, Top (any 3 of L/R/T/B work)

            // 2) Forward from Near plane normal (Near points inward)
            var forward = math.normalize(copy[4].xyz);

            // 3) Point on the new far plane along the camera ray
            var pFar = eye + (forward * newFar);

            // 4) Far plane normal points opposite forward
            copy[5] = PlaneFromNormalPoint(-forward, pFar);

            return copy;
        }

        public bool Equals(CameraCulling other)
        {
            return this.Left.Equals(other.Left) && this.Right.Equals(other.Right) && this.Bottom.Equals(other.Bottom) && this.Top.Equals(other.Top) &&
                this.Near.Equals(other.Near) && this.Far.Equals(other.Far);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.Left.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Right.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Bottom.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Top.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Near.GetHashCode();
                hashCode = (hashCode * 397) ^ this.Far.GetHashCode();
                return hashCode;
            }
        }

        // plane: n.xyz, d.w  with equation dot(n, x) + d = 0
        private static float4 NormalizePlane(float4 p)
        {
            var invLen = math.rsqrt(math.max(1e-20f, math.dot(p.xyz, p.xyz)));
            return new float4(p.xyz * invLen, p.w * invLen);
        }

        // Intersection of 3 planes (Eric Lengyel-style), requires normals not coplanar
        private static float3 Intersect3(in float4 p1, in float4 p2, in float4 p3)
        {
            float3 n1 = p1.xyz, n2 = p2.xyz, n3 = p3.xyz;
            var n2xn3 = math.cross(n2, n3);
            var n3xn1 = math.cross(n3, n1);
            var n1xn2 = math.cross(n1, n2);

            var denom = math.dot(n1, n2xn3);

            // Caller should ensure denom != 0 (perspective frustum side planes are fine)
            var x = (-p1.w * n2xn3) + (-p2.w * n3xn1) + (-p3.w * n1xn2);
            return x / denom;
        }

        // Rebuild a plane from normal and point: d = -dot(n, point)
        private static float4 PlaneFromNormalPoint(float3 n, float3 point)
        {
            n = math.normalize(n);
            var d = -math.dot(n, point);
            return new float4(n, d);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckRange(int index)
        {
            if (index is < 0 or >= 6)
            {
                throw new IndexOutOfRangeException("Frustum Planes must be in range of 6");
            }
        }
    }
}
