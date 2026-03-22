namespace Scripts.Tests.SlabAllocatorExample
{
    using Scripts.Data.SlabAllocatorExample;
    using BovineLabs.Testing;
    using NUnit.Framework;

    public class SlabAllocatorSystemTests : ECSTestsFixture
    {
        [Test]
        public void SlabAllocatorConfigCreation()
        {
            var config = new SlabAllocatorConfig
            {
                SlabSize = 16,
                MaxSlabs = 10,
                AllocationInterval = 0.1f
            };

            Assert.AreEqual(16, config.SlabSize);
            Assert.AreEqual(10, config.MaxSlabs);
            Assert.AreEqual(0.1f, config.AllocationInterval);
        }

        [Test]
        public void SlabAllocatorControllerCreation()
        {
            var controller = new SlabAllocatorController
            {
                Timer = 0.5f,
                TotalAllocations = 100,
                TotalDeallocations = 50
            };

            Assert.AreEqual(0.5f, controller.Timer);
            Assert.AreEqual(100, controller.TotalAllocations);
            Assert.AreEqual(50, controller.TotalDeallocations);
        }

        [Test]
        public void SlabItemCreation()
        {
            var item = new SlabItem
            {
                SlabIndex = 5,
                ItemIndex = 3
            };

            Assert.AreEqual(5, item.SlabIndex);
            Assert.AreEqual(3, item.ItemIndex);
        }

        [Test]
        public void SlabItemIndexCalculation()
        {
            var slabSize = 8;
            var slabIndex = 3;
            var itemIndex = 5;
            var expected = slabIndex * slabSize + itemIndex;

            Assert.AreEqual(29, expected);
        }
    }
}
