using NUnit.Framework;
using Scripts.Data.Stats;
using Scripts.Stats;

namespace Scripts.Tests.Stats
{
    public class StatVisualUtilityTests
    {
        [Test]
        public void FormatsBoolInactive()
        {
            var stat = new StatBoolElement
            {
                View = StatBoolView.ActiveInactive,
                Value = 0,
            };

            var result = StatBoolFormatter.Format(stat);

            Assert.That(result, Is.EqualTo("IDLE"));
        }

        [Test]
        public void FormatsFloatDelta()
        {
            var stat = new StatFloatElement
            {
                View = StatFloatView.DeltaToLink,
                Value = 82f,
            };

            var result = StatFloatFormatter.Format(stat, 100f, true);

            Assert.That(result, Is.EqualTo("-18"));
        }

        [Test]
        public void FormatsIntPercentOfLink()
        {
            var stat = new StatIntElement
            {
                View = StatIntView.PercentOfLink,
                Value = 15,
            };

            var result = StatIntFormatter.Format(stat, 20, true);

            Assert.That(result, Is.EqualTo("75%"));
        }

        [Test]
        public void FormatsFloatSpeed()
        {
            var stat = new StatFloatElement
            {
                View = StatFloatView.Speed,
                Value = 6.5f,
            };

            var result = StatFloatFormatter.Format(stat, 0f, false);

            Assert.That(result, Is.EqualTo("6.5m/s"));
        }
    }
}
