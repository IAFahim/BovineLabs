namespace BovineLabs.Timeline.UI.Data
{
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Entities;

    public struct EssenceUIComponent : IComponentData
    {
        public StatKey Stat;
        public IntrinsicKey Intrinsic;
        public ConditionKey Event;
    }
}