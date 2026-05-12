using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Rsr;

public class RsrTests
{
    [Test] public void Create_Dimensions()
    { var s = RsrApi.Create(5, 5, 100, Allocator.Temp); Assert.AreEqual(25, s.Grid.Length); RsrApi.Dispose(ref s); }

    [Test] public void Build_OpenGrid()
    {
        var s = RsrApi.Create(5, 5, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); for (int i = 0; i < blocked.Length; i++) blocked[i] = 0;
        RsrApi.Build(ref s, blocked);
        Assert.AreEqual(1, s.Rects.Length); // one big rectangle
        Assert.AreEqual(0, s.Rects[0].Min.x);
        Assert.AreEqual(4, s.Rects[0].Max.x);
        RsrApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void Build_WithWall()
    {
        var s = RsrApi.Create(5, 5, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); for (int i = 0; i < blocked.Length; i++) blocked[i] = 0;
        blocked[s.Grid.ToIndex(2, 2)] = 1;
        RsrApi.Build(ref s, blocked);
        Assert.Greater(s.Rects.Length, 1);
        RsrApi.Dispose(ref s); blocked.Dispose();
    }

    [Test] public void GetSuccessors_Interior()
    {
        var s = RsrApi.Create(5, 5, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); for (int i = 0; i < blocked.Length; i++) blocked[i] = 0;
        RsrApi.Build(ref s, blocked);
        var succ = new NativeList<int>(Allocator.Temp);
        // Center cell is interior of the single 5x5 rect
        RsrApi.GetSuccessors(ref s, s.Grid.ToIndex(2, 2), blocked, succ);
        // Interior cell should get perimeter successors
        Assert.Greater(succ.Length, 0);
        RsrApi.Dispose(ref s); blocked.Dispose(); succ.Dispose();
    }

    [Test] public void GetSuccessors_Perimeter()
    {
        var s = RsrApi.Create(5, 5, 100, Allocator.Temp);
        var blocked = new NativeArray<byte>(25, Allocator.Temp); for (int i = 0; i < blocked.Length; i++) blocked[i] = 0;
        RsrApi.Build(ref s, blocked);
        var succ = new NativeList<int>(Allocator.Temp);
        // Corner cell is on perimeter
        RsrApi.GetSuccessors(ref s, s.Grid.ToIndex(0, 0), blocked, succ);
        Assert.Greater(succ.Length, 0);
        RsrApi.Dispose(ref s); blocked.Dispose(); succ.Dispose();
    }

    [Test] public void Dispose_Double() { var s = RsrApi.Create(3, 3, 10, Allocator.Temp); RsrApi.Dispose(ref s); RsrApi.Dispose(ref s); }
}
