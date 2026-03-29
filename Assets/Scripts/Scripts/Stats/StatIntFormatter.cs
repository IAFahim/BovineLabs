using System.Globalization;
using Scripts.Data.Stats;

namespace Scripts.Stats
{
    internal static class StatIntFormatter
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        internal static string Format(in StatIntElement stat, int linkValue, bool hasLink)
        {
            switch (stat.View)
            {
                case StatIntView.Number:
                case StatIntView.Count:
                case StatIntView.Tier:
                case StatIntView.Level:
                    return stat.Value.ToString(Culture);
                case StatIntView.SignedNumber:
                    return stat.Value >= 0 ? "+" + stat.Value.ToString(Culture) : stat.Value.ToString(Culture);
                case StatIntView.CurrentOfLink:
                    return hasLink ? stat.Value.ToString(Culture) + "/" + linkValue.ToString(Culture) : stat.Value.ToString(Culture);
                case StatIntView.MissingFromLink:
                    return hasLink ? (linkValue - stat.Value).ToString(Culture) : stat.Value.ToString(Culture);
                case StatIntView.PercentOfLink:
                    return hasLink && linkValue != 0 ? (((float)stat.Value / linkValue) * 100f).ToString("0.#", Culture) + "%" : "0%";
                case StatIntView.DeltaToLink:
                    if (!hasLink)
                    {
                        return stat.Value >= 0 ? "+" + stat.Value.ToString(Culture) : stat.Value.ToString(Culture);
                    }

                    var delta = stat.Value - linkValue;
                    return delta >= 0 ? "+" + delta.ToString(Culture) : delta.ToString(Culture);
                default:
                    return stat.Value.ToString(Culture);
            }
        }
    }
}
