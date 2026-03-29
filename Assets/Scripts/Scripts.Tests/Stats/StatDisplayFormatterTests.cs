using NUnit.Framework;
using Scripts.Data.Stats;
using Scripts.Stats;

namespace Scripts.Tests.Stats
{
    public class StatDisplayFormatterTests
    {
        [Test]
        public void FormatsFloatPercentOfLink()
        {
            var stat = new StatFloatElement
            {
                View = StatFloatView.PercentOfLink,
                Value = 75f,
            };

            var result = StatFloatFormatter.Format(stat, 150f, true);

            Assert.That(result, Is.EqualTo("50%"));
        }

        [Test]
        public void FormatsFloatCurrentOfLink()
        {
            var stat = new StatFloatElement
            {
                View = StatFloatView.CurrentOfLink,
                Value = 75f,
            };

            var result = StatFloatFormatter.Format(stat, 100f, true);

            Assert.That(result, Is.EqualTo("75/100"));
        }

        [Test]
        public void FormatsIntCurrentOfLink()
        {
            var stat = new StatIntElement
            {
                View = StatIntView.CurrentOfLink,
                Value = 14,
            };

            var result = StatIntFormatter.Format(stat, 20, true);

            Assert.That(result, Is.EqualTo("14/20"));
        }

        [Test]
        public void FormatsIntSigned()
        {
            var stat = new StatIntElement
            {
                View = StatIntView.SignedNumber,
                Value = 7,
            };

            var result = StatIntFormatter.Format(stat, 0, false);

            Assert.That(result, Is.EqualTo("+7"));
        }

        [Test]
        public void FormatsBoolReadyUsed()
        {
            var stat = new StatBoolElement
            {
                View = StatBoolView.ReadyUsed,
                Value = 1,
            };

            var result = StatBoolFormatter.Format(stat);

            Assert.That(result, Is.EqualTo("READY"));
        }
    }
}
