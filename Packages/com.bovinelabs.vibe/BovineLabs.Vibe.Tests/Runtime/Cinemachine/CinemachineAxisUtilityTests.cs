// <copyright file="CinemachineAxisUtilityTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Tests.Runtime.Cinemachine
{
    using NUnit.Framework;
    using Unity.Cinemachine;
    using UnityEngine;

    public class CinemachineAxisUtilityTests
    {
        [Test]
        public void SanitizeAxis_ClampsRangeWrapAndRecentering()
        {
            var axis = new InputAxis
            {
                Wrap = true,
                Range = new Vector2(10f, 0f),
                Center = -2f,
                Value = 25f,
                Recentering = new InputAxis.RecenteringSettings
                {
                    Enabled = true,
                    Wait = -3f,
                    Time = -2f,
                },
            };

            CinemachineAxisUtility.SanitizeAxis(ref axis);

            Assert.AreEqual(10f, axis.Range.x, 0.0001f);
            Assert.AreEqual(10f, axis.Range.y, 0.0001f);
            Assert.AreEqual(10f, axis.Center, 0.0001f);
            Assert.AreEqual(10f, axis.Value, 0.0001f);
            Assert.AreEqual(0f, axis.Recentering.Wait, 0.0001f);
            Assert.AreEqual(0f, axis.Recentering.Time, 0.0001f);
        }

        [Test]
        public void SanitizeAxis_WrapsWhenRangeIsValid()
        {
            var axis = new InputAxis
            {
                Wrap = true,
                Range = new Vector2(0f, 10f),
                Center = -3f,
                Value = 12f,
            };

            CinemachineAxisUtility.SanitizeAxis(ref axis);

            Assert.AreEqual(7f, axis.Center, 0.0001f);
            Assert.AreEqual(2f, axis.Value, 0.0001f);
        }

        [Test]
        public void SanitizeAxis_DoesNotWrapWhenRangeIsTooSmall()
        {
            var axis = new InputAxis
            {
                Wrap = true,
                Range = new Vector2(2f, 2.00001f),
                Center = 0f,
                Value = 100f,
            };

            CinemachineAxisUtility.SanitizeAxis(ref axis);

            Assert.AreEqual(2f, axis.Center, 0.0001f);
            Assert.AreEqual(2.00001f, axis.Value, 0.0001f);
        }

        [Test]
        public void ClampTiltRange_ClampsToMinus90To90AndRepairsOrder()
        {
            var axis = new InputAxis
            {
                Range = new Vector2(95f, -120f),
            };

            CinemachineAxisUtility.ClampTiltRange(ref axis);

            Assert.AreEqual(90f, axis.Range.x, 0.0001f);
            Assert.AreEqual(90f, axis.Range.y, 0.0001f);
        }

        [Test]
        public void ClampRadialRangeMin_EnforcesPositiveMinimum()
        {
            var axis = new InputAxis
            {
                Range = new Vector2(-5f, -1f),
            };

            CinemachineAxisUtility.ClampRadialRangeMin(ref axis);

            Assert.AreEqual(0.0001f, axis.Range.x, 0.0001f);
            Assert.AreEqual(0.0001f, axis.Range.y, 0.0001f);
        }
    }
}
#endif
