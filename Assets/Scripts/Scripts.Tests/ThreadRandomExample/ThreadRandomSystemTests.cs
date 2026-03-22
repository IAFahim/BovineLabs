namespace Scripts.Tests.ThreadRandomExample
{
    using Scripts.Data.ThreadRandomExample;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;

    public class ThreadRandomSystemTests : ECSTestsFixture
    {
        [Test]
        public void ThreadRandomConfigCreation()
        {
            var config = new ThreadRandomConfig
            {
                SpawnInterval = 0.1f,
                SpawnRadius = 10f,
                MaxEntities = 50
            };

            Assert.AreEqual(0.1f, config.SpawnInterval);
            Assert.AreEqual(10f, config.SpawnRadius);
            Assert.AreEqual(50, config.MaxEntities);
        }

        [Test]
        public void ThreadRandomControllerCreation()
        {
            var controller = new ThreadRandomController
            {
                Timer = 1.5f,
                TotalSpawned = 100
            };

            Assert.AreEqual(1.5f, controller.Timer);
            Assert.AreEqual(100, controller.TotalSpawned);
        }

        [Test]
        public void RandomSpawnedEntityCreation()
        {
            var entity = new RandomSpawnedEntity
            {
                RandomPosition = new float3(1, 2, 3),
                RandomColor = new float4(0.5f, 0.5f, 0.5f, 1f),
                LifeRemaining = 5f
            };

            Assert.AreEqual(new float3(1, 2, 3), entity.RandomPosition);
            Assert.AreEqual(new float4(0.5f, 0.5f, 0.5f, 1f), entity.RandomColor);
            Assert.AreEqual(5f, entity.LifeRemaining);
        }
    }
}
