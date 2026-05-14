using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public unsafe partial class AnyaValidationTests
{
    [Test]
    [Category("LineOfSight")]
    public void LineOfSight_BlockedEndpoint_ReturnsFalse()
    {
        var blocked = NewEmptyBlocked(3, 3, Allocator.Temp);

        try
        {
            blocked[2 + 2 * 3] = 1;

            var ptr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            Assert.IsFalse(AnyaApi.LineOfSight(3, 3, ptr, new int2(0, 0), new int2(2, 2)));
        }
        finally
        {
            blocked.Dispose();
        }
    }

    [Test]
    [Category("LineOfSight")]
    public void LineOfSight_DiagonalCornerCutPolicy_IsConservative()
    {
        var blocked = NewEmptyBlocked(2, 2, Allocator.Temp);

        try
        {
            blocked[1 + 0 * 2] = 1;

            var ptr = (byte*)blocked.GetUnsafeReadOnlyPtr();
            Assert.IsFalse(
                AnyaApi.LineOfSight(2, 2, ptr, new int2(0, 0), new int2(1, 1)),
                "LOS must not skip side cells on diagonal corner crossings.");
        }
        finally
        {
            blocked.Dispose();
        }
    }
}
