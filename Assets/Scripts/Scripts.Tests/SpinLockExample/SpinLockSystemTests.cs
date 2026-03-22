namespace Scripts.Tests.SpinLockExample
{
    using Scripts.Data.SpinLockExample;
    using BovineLabs.Testing;
    using NUnit.Framework;

    public class SpinLockSystemTests : ECSTestsFixture
    {
        [Test]
        public void SpinLockConfigCreation()
        {
            var config = new SpinLockConfig
            {
                WriterCount = 5,
                ReaderCount = 10,
                LockInterval = 0.1f
            };

            Assert.AreEqual(5, config.WriterCount);
            Assert.AreEqual(10, config.ReaderCount);
            Assert.AreEqual(0.1f, config.LockInterval);
        }

        [Test]
        public void SpinLockControllerCreation()
        {
            var controller = new SpinLockController
            {
                Timer = 1.0f,
                TotalLockAttempts = 100,
                SuccessfulLocks = 80,
                FailedLocks = 20
            };

            Assert.AreEqual(1.0f, controller.Timer);
            Assert.AreEqual(100, controller.TotalLockAttempts);
            Assert.AreEqual(80, controller.SuccessfulLocks);
            Assert.AreEqual(20, controller.FailedLocks);
        }

        [Test]
        public void SharedResourceCreation()
        {
            var resource = new SharedResource
            {
                Value = 42,
                LastWriterId = 123
            };

            Assert.AreEqual(42, resource.Value);
            Assert.AreEqual(123u, resource.LastWriterId);
        }

        [Test]
        public void SpinLockWriterReaderCreation()
        {
            var writer = new SpinLockWriter
            {
                WriterId = 1,
                LockAttemptTime = 0.5f
            };

            var reader = new SpinLockReader
            {
                ReaderId = 2,
                LockAttemptTime = 0.6f
            };

            Assert.AreEqual(1, writer.WriterId);
            Assert.AreEqual(0.5f, writer.LockAttemptTime);
            Assert.AreEqual(2, reader.ReaderId);
            Assert.AreEqual(0.6f, reader.LockAttemptTime);
        }
    }
}
