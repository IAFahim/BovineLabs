namespace Scripts.Tests.NativeArrayExample
{
    using Scripts.Data.NativeArrayExample;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;

    public class NativeArraySystemTests : ECSTestsFixture
    {
        [Test]
        public void NativeArrayConfigCreation()
        {
            var config = new NativeArrayConfig
            {
                ArraySize = 50,
                UpdateInterval = 0.1f,
                EntitiesPerRow = 10
            };

            Assert.AreEqual(50, config.ArraySize);
            Assert.AreEqual(0.1f, config.UpdateInterval);
            Assert.AreEqual(10, config.EntitiesPerRow);
        }

        [Test]
        public void NativeArrayControllerCreation()
        {
            var controller = new NativeArrayController
            {
                Timer = 2.0f,
                TotalUpdates = 25
            };

            Assert.AreEqual(2.0f, controller.Timer);
            Assert.AreEqual(25u, controller.TotalUpdates);
        }

        [Test]
        public void ArrayItemCreation()
        {
            var item = new ArrayItem
            {
                ArrayIndex = 42,
                Value = 0.75f
            };

            Assert.AreEqual(42, item.ArrayIndex);
            Assert.AreEqual(0.75f, item.Value);
        }

        [Test]
        public void NativeArrayAllocationDeallocation()
        {
            var array = new NativeArray<float>(10, Allocator.Temp);

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i * 0.1f;
            }

            Assert.AreEqual(10, array.Length);
            Assert.AreEqual(0.5f, array[5]);

            array.Dispose();
        }

        [Test]
        public void ArrayItemIndexToRowColCalculation()
        {
            var entitiesPerRow = 10;
            var index = 42;

            var row = index / entitiesPerRow;
            var col = index % entitiesPerRow;

            Assert.AreEqual(4, row);
            Assert.AreEqual(2, col);
        }
    }
}
