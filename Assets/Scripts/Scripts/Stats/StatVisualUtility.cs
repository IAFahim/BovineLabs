using Scripts.Data.Stats;
using UnityEngine;

namespace Scripts.Stats
{
    internal static class StatVisualUtility
    {
        internal static bool UsesBar(in StatValueElement stat, bool hasLink)
        {
            if (hasLink)
            {
                return stat.Style == StatDisplayStyle.CurrentOfLink
                    || stat.Style == StatDisplayStyle.MissingFromLink
                    || stat.Style == StatDisplayStyle.PercentageOfLink;
            }

            return stat.Style == StatDisplayStyle.Percentage || stat.Style == StatDisplayStyle.PercentageWhole;
        }

        internal static float GetBarRatio(in StatValueElement stat, float linkValue, bool hasLink)
        {
            var value = stat.Value;

            switch (stat.Style)
            {
                case StatDisplayStyle.CurrentOfLink:
                case StatDisplayStyle.PercentageOfLink:
                    return NormalizeRatio(value, linkValue, hasLink);
                case StatDisplayStyle.MissingFromLink:
                    return NormalizeRatio(linkValue - value, linkValue, hasLink);
                case StatDisplayStyle.Percentage:
                    return Mathf.Clamp01(value);
                case StatDisplayStyle.PercentageWhole:
                    return Mathf.Clamp01(value / 100f);
                default:
                    return 0f;
            }
        }

        private static float NormalizeRatio(float value, float linkValue, bool hasLink)
        {
            if (!hasLink || linkValue <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(value / linkValue);
        }
    }
}
