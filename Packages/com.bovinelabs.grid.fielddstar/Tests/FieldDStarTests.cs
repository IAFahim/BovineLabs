using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.FieldDStar;

public class FieldDStarTests
{
    [Test] public void Create_Dimensions()
    { var s = FieldDStarApi.Create(5, 5, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); FieldDStarApi.Dispose(ref s); }

    [Test] public void Reset()
    {
        var s = FieldDStarApi.Create(5, 5, Allocator.Temp);
        s.G[0] = 5f;
        FieldDStarApi.Reset(ref s);
        for (int i = 0; i < s.Grid.Length; i++) Assert.IsTrue(float.IsPositiveInfinity(s.G[i]));
        FieldDStarApi.Dispose(ref s);
    }

    [Test] public void SetGoal()
    {
        var s = FieldDStarApi.Create(5, 5, Allocator.Temp);
        FieldDStarApi.Reset(ref s);
        FieldDStarApi.SetGoal(ref s, 24);
        Assert.AreEqual(0f, s.RHS[24], 0.001f);
        FieldDStarApi.Dispose(ref s);
    }

    [Test] public void Dispose_Double() { var s = FieldDStarApi.Create(3, 3, Allocator.Temp); FieldDStarApi.Dispose(ref s); FieldDStarApi.Dispose(ref s); }
}
