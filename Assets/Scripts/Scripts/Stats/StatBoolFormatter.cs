using Scripts.Data.Stats;

namespace Scripts.Stats
{
    internal static class StatBoolFormatter
    {
        internal static string Format(in StatBoolElement stat)
        {
            var active = stat.Value != 0;

            switch (stat.View)
            {
                case StatBoolView.OnOff:
                    return active ? "ON" : "OFF";
                case StatBoolView.ActiveInactive:
                    return active ? "ACTIVE" : "IDLE";
                case StatBoolView.ReadyUsed:
                    return active ? "READY" : "USED";
                case StatBoolView.LockedUnlocked:
                    return active ? "OPEN" : "LOCK";
                case StatBoolView.CompleteIncomplete:
                    return active ? "DONE" : "TODO";
                case StatBoolView.SeenUnseen:
                    return active ? "SEEN" : "HIDE";
                default:
                    return active ? "ON" : "OFF";
            }
        }
    }
}
