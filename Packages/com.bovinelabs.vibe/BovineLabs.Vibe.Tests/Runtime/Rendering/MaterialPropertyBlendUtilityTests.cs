// <copyright file="MaterialPropertyBlendUtilityTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Rendering
{
    using NUnit.Framework;
    using Unity.Mathematics;

    public class MaterialPropertyBlendUtilityTests
    {
        [Test]
        public void BlendValueFloat_EnabledMatrixBehavesAsExpected()
        {
            var bothEnabled = MaterialPropertyBlendUtility.BlendValue(1f, true, 5f, true, 0.25f, out var bothEnabledFlag);
            var onlyA = MaterialPropertyBlendUtility.BlendValue(1f, true, 5f, false, 0.25f, out var onlyAFlag);
            var onlyB = MaterialPropertyBlendUtility.BlendValue(1f, false, 5f, true, 0.25f, out var onlyBFlag);
            var none = MaterialPropertyBlendUtility.BlendValue(1f, false, 5f, false, 0.25f, out var noneFlag);

            Assert.IsTrue(bothEnabledFlag);
            Assert.AreEqual(2f, bothEnabled, 0.0001f);
            Assert.IsTrue(onlyAFlag);
            Assert.AreEqual(1f, onlyA, 0.0001f);
            Assert.IsTrue(onlyBFlag);
            Assert.AreEqual(5f, onlyB, 0.0001f);
            Assert.IsFalse(noneFlag);
            Assert.AreEqual(0f, none, 0.0001f);
        }

        [Test]
        public void BlendValueFloat3_EnabledMatrixBehavesAsExpected()
        {
            var a = new float3(1f, 2f, 3f);
            var b = new float3(5f, 6f, 7f);

            var bothEnabled = MaterialPropertyBlendUtility.BlendValue(a, true, b, true, 0.25f, out var bothEnabledFlag);
            var onlyA = MaterialPropertyBlendUtility.BlendValue(a, true, b, false, 0.25f, out var onlyAFlag);
            var onlyB = MaterialPropertyBlendUtility.BlendValue(a, false, b, true, 0.25f, out var onlyBFlag);
            var none = MaterialPropertyBlendUtility.BlendValue(a, false, b, false, 0.25f, out var noneFlag);

            Assert.IsTrue(bothEnabledFlag);
            AssertFloat3(new float3(2f, 3f, 4f), bothEnabled);
            Assert.IsTrue(onlyAFlag);
            AssertFloat3(a, onlyA);
            Assert.IsTrue(onlyBFlag);
            AssertFloat3(b, onlyB);
            Assert.IsFalse(noneFlag);
            AssertFloat3(default, none);
        }

        [Test]
        public void BlendValueFloat4_EnabledMatrixBehavesAsExpected()
        {
            var a = new float4(1f, 2f, 3f, 4f);
            var b = new float4(5f, 6f, 7f, 8f);

            var bothEnabled = MaterialPropertyBlendUtility.BlendValue(a, true, b, true, 0.25f, out var bothEnabledFlag);
            var onlyA = MaterialPropertyBlendUtility.BlendValue(a, true, b, false, 0.25f, out var onlyAFlag);
            var onlyB = MaterialPropertyBlendUtility.BlendValue(a, false, b, true, 0.25f, out var onlyBFlag);
            var none = MaterialPropertyBlendUtility.BlendValue(a, false, b, false, 0.25f, out var noneFlag);

            Assert.IsTrue(bothEnabledFlag);
            AssertFloat4(new float4(2f, 3f, 4f, 5f), bothEnabled);
            Assert.IsTrue(onlyAFlag);
            AssertFloat4(a, onlyA);
            Assert.IsTrue(onlyBFlag);
            AssertFloat4(b, onlyB);
            Assert.IsFalse(noneFlag);
            AssertFloat4(default, none);
        }

        [Test]
        public void AddValueFloat_EnabledMatrixBehavesAsExpected()
        {
            var bothEnabled = MaterialPropertyBlendUtility.AddValue(1f, true, 5f, true, out var bothEnabledFlag);
            var onlyA = MaterialPropertyBlendUtility.AddValue(1f, true, 5f, false, out var onlyAFlag);
            var onlyB = MaterialPropertyBlendUtility.AddValue(1f, false, 5f, true, out var onlyBFlag);
            var none = MaterialPropertyBlendUtility.AddValue(1f, false, 5f, false, out var noneFlag);

            Assert.IsTrue(bothEnabledFlag);
            Assert.AreEqual(6f, bothEnabled, 0.0001f);
            Assert.IsTrue(onlyAFlag);
            Assert.AreEqual(1f, onlyA, 0.0001f);
            Assert.IsTrue(onlyBFlag);
            Assert.AreEqual(5f, onlyB, 0.0001f);
            Assert.IsFalse(noneFlag);
            Assert.AreEqual(0f, none, 0.0001f);
        }

        [Test]
        public void AddValueFloat3_EnabledMatrixBehavesAsExpected()
        {
            var a = new float3(1f, 2f, 3f);
            var b = new float3(5f, 6f, 7f);

            var bothEnabled = MaterialPropertyBlendUtility.AddValue(a, true, b, true, out var bothEnabledFlag);
            var onlyA = MaterialPropertyBlendUtility.AddValue(a, true, b, false, out var onlyAFlag);
            var onlyB = MaterialPropertyBlendUtility.AddValue(a, false, b, true, out var onlyBFlag);
            var none = MaterialPropertyBlendUtility.AddValue(a, false, b, false, out var noneFlag);

            Assert.IsTrue(bothEnabledFlag);
            AssertFloat3(new float3(6f, 8f, 10f), bothEnabled);
            Assert.IsTrue(onlyAFlag);
            AssertFloat3(a, onlyA);
            Assert.IsTrue(onlyBFlag);
            AssertFloat3(b, onlyB);
            Assert.IsFalse(noneFlag);
            AssertFloat3(default, none);
        }

        [Test]
        public void AddValueFloat4_EnabledMatrixBehavesAsExpected()
        {
            var a = new float4(1f, 2f, 3f, 4f);
            var b = new float4(5f, 6f, 7f, 8f);

            var bothEnabled = MaterialPropertyBlendUtility.AddValue(a, true, b, true, out var bothEnabledFlag);
            var onlyA = MaterialPropertyBlendUtility.AddValue(a, true, b, false, out var onlyAFlag);
            var onlyB = MaterialPropertyBlendUtility.AddValue(a, false, b, true, out var onlyBFlag);
            var none = MaterialPropertyBlendUtility.AddValue(a, false, b, false, out var noneFlag);

            Assert.IsTrue(bothEnabledFlag);
            AssertFloat4(new float4(6f, 8f, 10f, 12f), bothEnabled);
            Assert.IsTrue(onlyAFlag);
            AssertFloat4(a, onlyA);
            Assert.IsTrue(onlyBFlag);
            AssertFloat4(b, onlyB);
            Assert.IsFalse(noneFlag);
            AssertFloat4(default, none);
        }

        private static void AssertFloat3(float3 expected, float3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }

        private static void AssertFloat4(float4 expected, float4 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
            Assert.AreEqual(expected.w, actual.w, 0.0001f);
        }
    }
}
