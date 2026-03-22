namespace Scripts.Tests.BitArray256Example
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;

    public class BitArray256SystemTests : ECSTestsFixture
    {
        [Test]
        public void BitArray256IndexerGetSet()
        {
            var bits = new BitArray256();

            bits[0] = true;
            bits[50] = true;
            bits[128] = true;
            bits[255] = true;

            Assert.IsTrue(bits[0]);
            Assert.IsTrue(bits[50]);
            Assert.IsTrue(bits[128]);
            Assert.IsTrue(bits[255]);

            Assert.IsFalse(bits[1]);
            Assert.IsFalse(bits[100]);
            Assert.IsFalse(bits[254]);
        }

        [Test]
        public void BitArray256CountBits()
        {
            var bits = new BitArray256();

            bits[0] = true;
            bits[1] = true;
            bits[2] = true;

            Assert.AreEqual(3, bits.CountBits());

            bits[1] = false;
            Assert.AreEqual(2, bits.CountBits());

            bits[100] = true;
            bits[200] = true;
            Assert.AreEqual(4, bits.CountBits());
        }

        [Test]
        public void BitArray256BitwiseOperations()
        {
            var bits1 = new BitArray256();
            var bits2 = new BitArray256();

            bits1[0] = true;
            bits1[1] = true;
            bits1[2] = true;

            bits2[1] = true;
            bits2[2] = true;
            bits2[3] = true;

            var orResult = bits1 | bits2;
            Assert.AreEqual(4, orResult.CountBits());

            var andResult = bits1 & bits2;
            Assert.AreEqual(2, andResult.CountBits());

            var notResult = ~bits1;
            Assert.AreEqual(253, notResult.CountBits());
        }
    }
}
