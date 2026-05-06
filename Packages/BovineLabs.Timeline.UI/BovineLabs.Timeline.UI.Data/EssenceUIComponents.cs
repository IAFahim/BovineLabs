using BovineLabs.Essence.Data;
using BovineLabs.Reaction.Data.Conditions;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.UI.Data
{
    // The configured elements from the Timeline Clip
    public struct ClipStat : IBufferElementData { public StatKey Key; public FixedString32Bytes Name; }
    public struct ClipIntrinsic : IBufferElementData { public IntrinsicKey Key; public FixedString32Bytes Name; }
    public struct ClipEvent : IBufferElementData { public ConditionKey Key; public FixedString32Bytes Name; public float Duration; }

    // System-managed buffer to keep 1-frame events alive for their Display Duration
    public struct ActiveUIEvent : IBufferElementData
    {
        public ConditionKey Key;
        public FixedString32Bytes Name;
        public int Value;
        public float TimeRemaining;
    }
}