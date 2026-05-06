using System;
using BovineLabs.Anchor;
using Unity.Collections;
using Unity.Entities;
using Unity.Properties;

namespace BovineLabs.Timeline.UI.Data.ViewModel
{
    public struct PlayerUIBlock : IEquatable<PlayerUIBlock>
    {
        public Entity PlayerEntity;
        public FixedString512Bytes StatsText;
        public FixedString512Bytes IntrinsicsText;
        public FixedString512Bytes EventsText;

        public bool Equals(PlayerUIBlock other) =>
            PlayerEntity == other.PlayerEntity &&
            StatsText == other.StatsText &&
            IntrinsicsText == other.IntrinsicsText &&
            EventsText == other.EventsText;
    }

    [IsService]
    public partial class EssenceUIViewModel : SystemObservableObject<EssenceUIViewModel.Data>
    {
        // FIX: Explicitly unwrap the ChangedList (.Value) and cast to UIArray
        [CreateProperty(ReadOnly = true)] 
        public UIArray<PlayerUIBlock> Players => (UIArray<PlayerUIBlock>)(MultiContainer<PlayerUIBlock>)this.Value.Players.Value;

        public partial struct Data
        {
            [SystemProperty] 
            private ChangedList<PlayerUIBlock> players;
        }
    }
}