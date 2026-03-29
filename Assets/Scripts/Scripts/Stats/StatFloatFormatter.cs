using System.Globalization;
using Scripts.Data.Stats;

namespace Scripts.Stats
{
    internal static class StatFloatFormatter
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        internal static string Format(in StatFloatElement stat, float linkValue, bool hasLink)
        {
            switch (stat.View)
            {
                case StatFloatView.Number:
                    return stat.Value.ToString("0.##", Culture);
                case StatFloatView.SignedNumber:
                    return stat.Value >= 0f ? "+" + stat.Value.ToString("0.##", Culture) : stat.Value.ToString("0.##", Culture);
                case StatFloatView.Percent:
                    return (stat.Value * 100f).ToString("0.#", Culture) + "%";
                case StatFloatView.Multiplier:
                    return "x" + stat.Value.ToString("0.##", Culture);
                case StatFloatView.Seconds:
                    return stat.Value.ToString("0.##", Culture) + "s";
                case StatFloatView.Milliseconds:
                    return stat.Value.ToString("0.#", Culture) + "ms";
                case StatFloatView.Distance:
                    return stat.Value.ToString("0.##", Culture) + "m";
                case StatFloatView.Speed:
                    return stat.Value.ToString("0.##", Culture) + "m/s";
                case StatFloatView.Rate:
                    return stat.Value.ToString("0.#", Culture) + "/m";
                case StatFloatView.CurrentOfLink:
                    return hasLink ? stat.Value.ToString("0.##", Culture) + "/" + linkValue.ToString("0.##", Culture) : stat.Value.ToString("0.##", Culture);
                case StatFloatView.MissingFromLink:
                    return hasLink ? (linkValue - stat.Value).ToString("0.##", Culture) : stat.Value.ToString("0.##", Culture);
                case StatFloatView.PercentOfLink:
                    return hasLink && linkValue != 0f ? ((stat.Value / linkValue) * 100f).ToString("0.#", Culture) + "%" : "0%";
                case StatFloatView.DeltaToLink:
                    if (!hasLink)
                    {
                        return stat.Value >= 0f ? "+" + stat.Value.ToString("0.##", Culture) : stat.Value.ToString("0.##", Culture);
                    }

                    var delta = stat.Value - linkValue;
                    return delta >= 0f ? "+" + delta.ToString("0.##", Culture) : delta.ToString("0.##", Culture);
                default:
                    return stat.Value.ToString("0.##", Culture);
            }
        }
    }
}
