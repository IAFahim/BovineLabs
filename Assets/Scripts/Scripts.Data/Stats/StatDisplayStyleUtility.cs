namespace Scripts.Data.Stats
{
    public static class StatDisplayStyleUtility
    {
        public static bool RequiresLink(StatDisplayStyle style)
        {
            return style == StatDisplayStyle.CurrentOfLink
                || style == StatDisplayStyle.MissingFromLink
                || style == StatDisplayStyle.PercentageOfLink
                || style == StatDisplayStyle.DeltaToLink;
        }
    }
}
