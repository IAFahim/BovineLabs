namespace Scripts.Stats
{
    internal static class StatNameUtility
    {
        internal static string Compact(string value, int visibleColumns)
        {
            var compact = value
                .Replace("Chance", "Ch")
                .Replace("Damage", "Dmg")
                .Replace("Discount", "Disc")
                .Replace("Cooldown", "Cd")
                .Replace("Duration", "Dur")
                .Replace("Velocity", "Vel")
                .Replace("Range", "Rng")
                .Replace("Radius", "Rad")
                .Replace("Window", "Win")
                .Replace("Buffer", "Buf")
                .Replace("Active", "Act")
                .Replace("Complete", "Done")
                .Replace("Pressed", "Press")
                .Replace("Visible", "Vis")
                .Replace("Discount", "Disc")
                .Replace("Intrinsic", "Intr")
                .Trim();

            var max = visibleColumns >= 3 ? 8 : visibleColumns == 2 ? 11 : 16;
            if (compact.Length <= max)
            {
                return compact;
            }

            return compact.Substring(0, max - 1) + "…";
        }
    }
}
