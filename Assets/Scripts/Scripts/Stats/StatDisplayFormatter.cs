using System.Globalization;
using Scripts.Data.Stats;

namespace Scripts.Stats
{
    internal static class StatDisplayFormatter
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        internal static string Format(in StatValueElement stat, float linkValue, bool hasLink)
        {
            return stat.Style switch
            {
                StatDisplayStyle.Number => FormatCompact(stat.Value),
                StatDisplayStyle.Integer => FormatWhole(stat.Value),
                StatDisplayStyle.SignedNumber => FormatSignedCompact(stat.Value),
                StatDisplayStyle.SignedInteger => FormatSignedWhole(stat.Value),
                StatDisplayStyle.Percentage => FormatCompact(stat.Value * 100f) + "%",
                StatDisplayStyle.PercentageWhole => FormatWhole(stat.Value) + "%",
                StatDisplayStyle.Multiplier => "x" + FormatCompact(stat.Value),
                StatDisplayStyle.MultiplierSigned => FormatMultiplierSigned(stat.Value),
                StatDisplayStyle.Seconds => FormatCompact(stat.Value) + "s",
                StatDisplayStyle.Milliseconds => FormatCompact(stat.Value) + "ms",
                StatDisplayStyle.DistanceMeters => FormatCompact(stat.Value) + "m",
                StatDisplayStyle.SpeedPerSecond => FormatCompact(stat.Value) + "m/s",
                StatDisplayStyle.RatePerMinute => FormatCompact(stat.Value) + "/m",
                StatDisplayStyle.CurrentOfLink => hasLink ? FormatCompact(stat.Value) + "/" + FormatCompact(linkValue) : FormatCompact(stat.Value),
                StatDisplayStyle.MissingFromLink => hasLink ? FormatCompact(linkValue - stat.Value) : FormatCompact(stat.Value),
                StatDisplayStyle.PercentageOfLink => hasLink ? FormatLinkedPercent(stat.Value, linkValue) : FormatCompact(stat.Value),
                StatDisplayStyle.DeltaToLink => hasLink ? FormatSignedCompact(stat.Value - linkValue) : FormatSignedCompact(stat.Value),
                _ => FormatCompact(stat.Value),
            };
        }

        private static string FormatLinkedPercent(float value, float linkValue)
        {
            if (linkValue == 0f)
            {
                return "0%";
            }

            return FormatCompact((value / linkValue) * 100f) + "%";
        }

        private static string FormatMultiplierSigned(float value)
        {
            return value >= 0f
                ? "+x" + FormatCompact(value)
                : "-x" + FormatCompact(-value);
        }

        private static string FormatCompact(float value)
        {
            return value.ToString("0.##", Culture);
        }

        private static string FormatWhole(float value)
        {
            return value.ToString("0", Culture);
        }

        private static string FormatSignedCompact(float value)
        {
            return value >= 0f
                ? "+" + FormatCompact(value)
                : FormatCompact(value);
        }

        private static string FormatSignedWhole(float value)
        {
            return value >= 0f
                ? "+" + FormatWhole(value)
                : FormatWhole(value);
        }
    }
}
