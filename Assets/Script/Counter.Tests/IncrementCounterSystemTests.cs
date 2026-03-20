using BovineLabs.Testing;
using Examples._01_AssemblyArchitecture.Script.Counter.Data;
using Examples._01_AssemblyArchitecture.Script.Counter.Systems;
using NUnit.Framework;
using Unity.Entities;

namespace Examples._01_AssemblyArchitecture.Script.Counter.Tests
{
    public class IncrementCounterSystemTests : ECSTestsFixture
    {
        [Test]
        [TestLeakDetection]
        public void IncrementCounterSystem_IncrementsCounterValue()
        {
            var entity = Manager.CreateEntity(typeof(CounterComponent));
            Manager.SetComponentData(entity, new CounterComponent { Value = 0 });

            var system = World.GetOrCreateSystem<IncrementCounterSystem>();
            system.Update(World.Unmanaged);

            Manager.CompleteAllTrackedJobs();

            var counter = Manager.GetComponentData<CounterComponent>(entity);
            Assert.AreEqual(1, counter.Value);
        }

        [Test]
        [TestLeakDetection]
        public void IncrementCounterSystem_MultipleIncrements_CorrectTotal()
        {
            var entity = Manager.CreateEntity(typeof(CounterComponent));
            Manager.SetComponentData(entity, new CounterComponent { Value = 5 });

            var system = World.GetOrCreateSystem<IncrementCounterSystem>();

            for (int i = 0; i < 10; i++)
            {
                system.Update(World.Unmanaged);
            }

            Manager.CompleteAllTrackedJobs();

            var counter = Manager.GetComponentData<CounterComponent>(entity);
            Assert.AreEqual(15, counter.Value);
        }

        [Test]
        [TestLeakDetection]
        public void IncrementCounterSystem_MultipleCounters_AllIncremented()
        {
            var entity1 = Manager.CreateEntity(typeof(CounterComponent));
            Manager.SetComponentData(entity1, new CounterComponent { Value = 0 });

            var entity2 = Manager.CreateEntity(typeof(CounterComponent));
            Manager.SetComponentData(entity2, new CounterComponent { Value = 100 });

            var system = World.GetOrCreateSystem<IncrementCounterSystem>();
            system.Update(World.Unmanaged);

            Manager.CompleteAllTrackedJobs();

            var counter1 = Manager.GetComponentData<CounterComponent>(entity1);
            var counter2 = Manager.GetComponentData<CounterComponent>(entity2);

            Assert.AreEqual(1, counter1.Value);
            Assert.AreEqual(101, counter2.Value);
        }
    }
}
