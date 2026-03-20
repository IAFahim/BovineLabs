using NUnit.Framework;
using Unity.Mathematics;
using Assert = NUnit.Framework.Assert;

namespace Examples._Shared.Testing
{
    public static class AssertMathHelpers
    {
        public static void AreApproximatelyEqual(float3 expected, float3 actual, float delta)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(delta));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(delta));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(delta));
        }

        public static void AreApproximatelyEqual(quaternion expected, quaternion actual, float delta)
        {
            Assert.That(actual.value.x, Is.EqualTo(expected.value.x).Within(delta));
            Assert.That(actual.value.y, Is.EqualTo(expected.value.y).Within(delta));
            Assert.That(actual.value.z, Is.EqualTo(expected.value.z).Within(delta));
            Assert.That(actual.value.w, Is.EqualTo(expected.value.w).Within(delta));
        }
    }
}
