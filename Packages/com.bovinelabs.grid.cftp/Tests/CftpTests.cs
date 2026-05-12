using NUnit.Framework;
using Unity.Collections;
using BovineLabs.Grid.Cftp;

public class CftpTests
{
    [Test] public void Create_Dimensions()
    { var s = CftpApi.Create(3, 3, 100, Allocator.Temp); Assert.AreEqual(9, s.Grid.Length); CftpApi.Dispose(ref s); }

    [Test] public void InitializeExtremes()
    {
        var s = CftpApi.Create(3, 3, 100, Allocator.Temp);
        CftpApi.InitializeExtremes(ref s);
        for (int i = 0; i < s.Grid.Length; i++) { Assert.AreEqual(0, s.Low[i]); Assert.AreEqual(1, s.High[i]); }
        CftpApi.Dispose(ref s);
    }

    [Test] public void Coalesced_NotInitially()
    {
        var s = CftpApi.Create(3, 3, 100, Allocator.Temp);
        CftpApi.InitializeExtremes(ref s);
        Assert.IsFalse(CftpApi.Coalesced(ref s));
        CftpApi.Dispose(ref s);
    }

    [Test] public void GenerateUpdates()
    {
        var s = CftpApi.Create(3, 3, 1000, Allocator.Temp);
        var rng = new Unity.Mathematics.Random(42);
        CftpApi.GeneratePastUpdates(ref s, ref rng, 5);
        Assert.AreEqual(45, s.Updates.Length); // 5 rounds * 9 cells
        CftpApi.Dispose(ref s);
    }

    [Test] public void Dispose_Double() { var s = CftpApi.Create(3, 3, 10, Allocator.Temp); CftpApi.Dispose(ref s); CftpApi.Dispose(ref s); }
}
