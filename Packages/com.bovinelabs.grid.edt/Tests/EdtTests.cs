using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using BovineLabs.Grid;
using BovineLabs.Grid.Edt;

public class EdtTests
{
    [Test] public void Init_ObstacleIsZero()
    { var b = new NativeArray<byte>(new byte[] { 1, 0, 0 }, Allocator.Temp); var d = new NativeArray<float>(3, Allocator.Temp); EdtApi.InitFromBlocked(in b, ref d); Assert.AreEqual(0f, d[0], 0.001f); b.Dispose(); d.Dispose(); }

    [Test] public void Init_FreeIsInf()
    { var b = new NativeArray<byte>(new byte[] { 0, 0 }, Allocator.Temp); var d = new NativeArray<float>(2, Allocator.Temp); EdtApi.InitFromBlocked(in b, ref d); Assert.IsTrue(float.IsPositiveInfinity(d[0])); b.Dispose(); d.Dispose(); }

    [Test] public void Transform1D_Single()
    {
        int len = 5;
        var f = new NativeArray<float>(len, Allocator.Temp); var o = new NativeArray<float>(len, Allocator.Temp);
        var v = new NativeArray<int>(len, Allocator.Temp); var z = new NativeArray<float>(len + 1, Allocator.Temp);
        f[0] = 0f; for (int i = 1; i < len; i++) f[i] = float.PositiveInfinity;
        EdtApi.Transform1D(in f, ref o, ref v, ref z, len);
        Assert.AreEqual(0f, o[0], 0.001f); Assert.AreEqual(1f, o[1], 0.001f); Assert.AreEqual(4f, o[2], 0.001f); Assert.AreEqual(9f, o[3], 0.001f); Assert.AreEqual(16f, o[4], 0.001f);
        f.Dispose(); o.Dispose(); v.Dispose(); z.Dispose();
    }

    [Test] public void Transform1D_TwoSources()
    {
        int len = 5;
        var f = new NativeArray<float>(len, Allocator.Temp); var o = new NativeArray<float>(len, Allocator.Temp);
        var v = new NativeArray<int>(len, Allocator.Temp); var z = new NativeArray<float>(len + 1, Allocator.Temp);
        f[0] = 0f; f[1] = float.PositiveInfinity; f[2] = 0f; f[3] = float.PositiveInfinity; f[4] = float.PositiveInfinity;
        EdtApi.Transform1D(in f, ref o, ref v, ref z, len);
        Assert.AreEqual(0f, o[0], 0.001f); Assert.AreEqual(1f, o[1], 0.001f); Assert.AreEqual(0f, o[2], 0.001f); Assert.AreEqual(1f, o[3], 0.001f); Assert.AreEqual(4f, o[4], 0.001f);
        f.Dispose(); o.Dispose(); v.Dispose(); z.Dispose();
    }

    [Test] public void Transform1D_AllObstacles()
    {
        int len = 4;
        var f = new NativeArray<float>(len, Allocator.Temp); var o = new NativeArray<float>(len, Allocator.Temp);
        var v = new NativeArray<int>(len, Allocator.Temp); var z = new NativeArray<float>(len + 1, Allocator.Temp);
        for (int i = 0; i < len; i++) f[i] = 0f;
        EdtApi.Transform1D(in f, ref o, ref v, ref z, len);
        for (int i = 0; i < len; i++) Assert.AreEqual(0f, o[i], 0.001f);
        f.Dispose(); o.Dispose(); v.Dispose(); z.Dispose();
    }

    [Test] public void Transform1D_AllFree()
    {
        int len = 3;
        var f = new NativeArray<float>(len, Allocator.Temp); var o = new NativeArray<float>(len, Allocator.Temp);
        var v = new NativeArray<int>(len, Allocator.Temp); var z = new NativeArray<float>(len + 1, Allocator.Temp);
        for (int i = 0; i < len; i++) f[i] = float.PositiveInfinity;
        EdtApi.Transform1D(in f, ref o, ref v, ref z, len);
        for (int i = 0; i < len; i++) Assert.IsTrue(float.IsPositiveInfinity(o[i]));
        f.Dispose(); o.Dispose(); v.Dispose(); z.Dispose();
    }

    [Test] public void Transform1D_Length1()
    {
        var f = new NativeArray<float>(1, Allocator.Temp); var o = new NativeArray<float>(1, Allocator.Temp);
        var v = new NativeArray<int>(1, Allocator.Temp); var z = new NativeArray<float>(2, Allocator.Temp);
        f[0] = 0f; EdtApi.Transform1D(in f, ref o, ref v, ref z, 1); Assert.AreEqual(0f, o[0], 0.001f);
        f.Dispose(); o.Dispose(); v.Dispose(); z.Dispose();
    }

    [Test] public void Transform1D_Length0()
    {
        var f = new NativeArray<float>(1, Allocator.Temp); var o = new NativeArray<float>(1, Allocator.Temp);
        var v = new NativeArray<int>(1, Allocator.Temp); var z = new NativeArray<float>(2, Allocator.Temp);
        Assert.DoesNotThrow(() => EdtApi.Transform1D(in f, ref o, ref v, ref z, 0));
        f.Dispose(); o.Dispose(); v.Dispose(); z.Dispose();
    }

    [Test] public void Build_CenterObstacle()
    {
        var s = EdtApi.Create(3, 3, Allocator.Temp); var b = new NativeArray<byte>(9, Allocator.Temp); var d = new NativeArray<float>(9, Allocator.Temp);
        b.Fill((byte)0); b[4] = 1;
        EdtApi.Build(ref s, in b, ref d);
        Assert.AreEqual(0f, d[4], 0.001f); Assert.AreEqual(1f, d[1], 0.001f); Assert.AreEqual(2f, d[0], 0.001f); Assert.AreEqual(2f, d[8], 0.001f);
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }

    [Test] public void Build_CornerObstacle()
    {
        var s = EdtApi.Create(3, 3, Allocator.Temp); var b = new NativeArray<byte>(9, Allocator.Temp); var d = new NativeArray<float>(9, Allocator.Temp);
        b.Fill((byte)0); b[0] = 1;
        EdtApi.Build(ref s, in b, ref d);
        Assert.AreEqual(0f, d[0], 0.001f); Assert.AreEqual(1f, d[1], 0.001f); Assert.AreEqual(8f, d[8], 0.01f);
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }

    [Test] public void Build_AllObstacles()
    {
        var s = EdtApi.Create(4, 4, Allocator.Temp); var b = new NativeArray<byte>(16, Allocator.Temp); var d = new NativeArray<float>(16, Allocator.Temp);
        b.Fill((byte)1); EdtApi.Build(ref s, in b, ref d);
        for (int i = 0; i < 16; i++) Assert.AreEqual(0f, d[i], 0.001f);
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }

    [Test] public void Build_NoObstacles()
    {
        var s = EdtApi.Create(3, 3, Allocator.Temp); var b = new NativeArray<byte>(9, Allocator.Temp); var d = new NativeArray<float>(9, Allocator.Temp);
        b.Fill((byte)0); EdtApi.Build(ref s, in b, ref d);
        for (int i = 0; i < 9; i++) Assert.IsTrue(float.IsPositiveInfinity(d[i]));
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }

    [Test] public void Build_Line()
    {
        var s = EdtApi.Create(5, 1, Allocator.Temp); var b = new NativeArray<byte>(5, Allocator.Temp); var d = new NativeArray<float>(5, Allocator.Temp);
        b.Fill((byte)0); b[0] = 1; EdtApi.Build(ref s, in b, ref d);
        Assert.AreEqual(0f, d[0], 0.001f); Assert.AreEqual(1f, d[1], 0.001f); Assert.AreEqual(16f, d[4], 0.001f);
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }

    [Test] public void Build_Symmetric()
    {
        var s = EdtApi.Create(5, 1, Allocator.Temp); var b = new NativeArray<byte>(5, Allocator.Temp); var d = new NativeArray<float>(5, Allocator.Temp);
        b.Fill((byte)0); b[0] = 1; b[4] = 1; EdtApi.Build(ref s, in b, ref d);
        Assert.AreEqual(4f, d[2], 0.001f);
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }

    [Test] public void Build_OffCenter()
    {
        var s = EdtApi.Create(5, 5, Allocator.Temp); var b = new NativeArray<byte>(25, Allocator.Temp); var d = new NativeArray<float>(25, Allocator.Temp);
        b.Fill((byte)0); b[s.Grid.ToIndex(1, 1)] = 1; EdtApi.Build(ref s, in b, ref d);
        Assert.AreEqual(8f, d[s.Grid.ToIndex(3, 3)], 0.01f); Assert.AreEqual(9f, d[s.Grid.ToIndex(4, 1)], 0.01f);
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }

    [Test] public void ToDistance_Sqrt()
    {
        var d2 = new NativeArray<float>(new float[] { 0f, 1f, 4f, 9f }, Allocator.Temp); var d = new NativeArray<float>(4, Allocator.Temp);
        EdtApi.ToDistance(in d2, ref d);
        Assert.AreEqual(0f, d[0], 0.001f); Assert.AreEqual(2f, d[2], 0.001f);
        d2.Dispose(); d.Dispose();
    }

    [Test] public void Dispose_DoubleSafe() { var s = EdtApi.Create(3, 3, Allocator.Temp); EdtApi.Dispose(ref s); EdtApi.Dispose(ref s); }

    [Test] public void Build_1x1()
    {
        var s = EdtApi.Create(1, 1, Allocator.Temp); var b = new NativeArray<byte>(new byte[] { 1 }, Allocator.Temp); var d = new NativeArray<float>(1, Allocator.Temp);
        EdtApi.Build(ref s, in b, ref d); Assert.AreEqual(0f, d[0], 0.001f);
        EdtApi.Dispose(ref s); b.Dispose(); d.Dispose();
    }
}
