using BovineLabs.Grid.Anya;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

public unsafe partial class AnyaValidationTests
{
    [Test]
    [Category("Incremental")]
    public void IncrementalSearch_MatchesBatchSearch_OnObstacleMap()
    {
        Assert.IsTrue(AnyaApi.TryCreate(12, 12, 10000, Allocator.Temp, out var batch));
        Assert.IsTrue(AnyaApi.TryCreate(12, 12, 10000, Allocator.Temp, out var incremental));

        var blocked = NewEmptyBlocked(12, 12, Allocator.Temp);
        var batchPath = new NativeList<int2>(Allocator.Temp);
        var incPath = new NativeList<int2>(Allocator.Temp);

        try
        {
            for (var y = 0; y < 12; y++)
                if (y != 6)
                    blocked[5 + y * 12] = 1;

            var startA = new int2(1, 1);
            var goalA = new int2(10, 10);
            Assert.IsTrue(AnyaApi.TrySearch(ref batch, blocked, ref startA, ref goalA, ref batchPath));

            var startB = new int2(1, 1);
            var goalB = new int2(10, 10);
            Assert.IsTrue(AnyaApi.TryInitSearch(ref incremental, blocked, ref startB, ref goalB));

            var stepCount = 0;
            while (incremental is { SearchComplete: 0, Heap: { IsEmpty: false } })
            {
                Assert.IsTrue(AnyaApi.TryStepSearch(ref incremental, blocked), "Step failed.");
                stepCount++;
                Assert.Less(stepCount, 10000, "Incremental search did not terminate.");
            }

            Assert.Greater(stepCount, 0, "This test must require at least one incremental step.");
            Assert.IsTrue(AnyaApi.TryExtractPath(ref incremental, ref incPath));

            AssertPathValidAndVisible(incremental.Grid, blocked, incPath, startB, goalB);
            Assert.AreEqual(PathCost(batchPath), PathCost(incPath), CostTolerance);
        }
        finally
        {
            blocked.Dispose();
            batchPath.Dispose();
            incPath.Dispose();
            AnyaApi.Dispose(ref batch);
            AnyaApi.Dispose(ref incremental);
        }
    }
}