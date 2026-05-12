using NUnit.Framework;
using BovineLabs.Grid;

public class RangeITests
{
    [Test] public void Constructor() { var r = new RangeI(5, 3); Assert.AreEqual(5, r.Offset); Assert.AreEqual(3, r.Count); }
    [Test] public void End() { Assert.AreEqual(15, new RangeI(10, 5).End); }
    [Test] public void End_ZeroCount() { Assert.AreEqual(7, new RangeI(7, 0).End); }
    [Test] public void Contains_Inside() { Assert.IsTrue(new RangeI(3, 4).Contains(3)); Assert.IsTrue(new RangeI(3, 4).Contains(6)); }
    [Test] public void Contains_AtEnd() { Assert.IsFalse(new RangeI(3, 4).Contains(7)); }
    [Test] public void Contains_Before() { Assert.IsFalse(new RangeI(3, 4).Contains(2)); }
    [Test] public void Contains_EmptyRange() { var r = new RangeI(5, 0); Assert.IsFalse(r.Contains(5)); }
    [Test] public void Contains_ZeroOffset() { var r = new RangeI(0, 10); Assert.IsTrue(r.Contains(0)); Assert.IsTrue(r.Contains(9)); Assert.IsFalse(r.Contains(10)); }
}
