namespace Scripts.Data.Stats
{
    public enum StatDisplayStyle : byte
    {
        Number = 0,
        Integer = 1,
        SignedNumber = 2,
        SignedInteger = 3,
        Percentage = 4,
        PercentageWhole = 5,
        Multiplier = 6,
        MultiplierSigned = 7,
        Seconds = 8,
        Milliseconds = 9,
        DistanceMeters = 10,
        SpeedPerSecond = 11,
        RatePerMinute = 12,
        CurrentOfLink = 13,
        MissingFromLink = 14,
        PercentageOfLink = 15,
        DeltaToLink = 16,
    }
}
