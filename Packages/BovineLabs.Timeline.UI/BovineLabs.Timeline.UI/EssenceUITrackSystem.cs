using BovineLabs.Anchor;
using BovineLabs.Essence.Data;
using BovineLabs.Reaction.Data.Conditions;
using BovineLabs.Timeline;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.UI.Data;
using BovineLabs.Timeline.UI.Data.ViewModel;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace BovineLabs.Timeline.UI
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.Presentation)]
    public partial struct EssenceUITrackSystem : ISystem, ISystemStartStop
    {
        private UIHelper<EssenceUIViewModel, EssenceUIViewModel.Data> uiHelper;
        private NativeList<PlayerUIBlock> players;

        public void OnCreate(ref SystemState state)
        {
            uiHelper = new UIHelper<EssenceUIViewModel, EssenceUIViewModel.Data>(ref state,
                ComponentType.ReadOnly<ClipStat>());
            this.players = new NativeList<PlayerUIBlock>(4, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (this.players.IsCreated) this.players.Dispose();
        }

        public void OnStartRunning(ref SystemState state) => uiHelper.Bind();
        public void OnStopRunning(ref SystemState state) => uiHelper.Unbind();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            float dt = SystemAPI.Time.DeltaTime;

            var statsLookup = SystemAPI.GetBufferLookup<Stat>(true);
            var intrinsicsLookup = SystemAPI.GetBufferLookup<Intrinsic>(true);
            var conditionEventsLookup = SystemAPI.GetBufferLookup<ConditionEvent>(true);

            this.players.Clear();

            foreach (var (clipStats, clipIntrinsics, clipEvents, _activeEvents, trackBinding) in
                     SystemAPI
                         .Query<DynamicBuffer<ClipStat>, DynamicBuffer<ClipIntrinsic>, DynamicBuffer<ClipEvent>,
                             DynamicBuffer<ActiveUIEvent>, RefRO<TrackBinding>>()
                         .WithAll<TimelineActive, ClipActive>())
            {
                var activeEvents = _activeEvents;

                Entity player = trackBinding.ValueRO.Value;
                var block = new PlayerUIBlock { PlayerEntity = player };

                if (statsLookup.TryGetBuffer(player, out var stats))
                {
                    foreach (var s in clipStats)
                    {
                        block.StatsText.Append(s.Name);
                        block.StatsText.Append(": ");
                        block.StatsText.Append(stats.GetValueFloat(s.Key));
                        block.StatsText.Append("\n");
                    }
                }

                if (intrinsicsLookup.TryGetBuffer(player, out var intrinsics))
                {
                    foreach (var i in clipIntrinsics)
                    {
                        block.IntrinsicsText.Append(i.Name);
                        block.IntrinsicsText.Append(": ");
                        block.IntrinsicsText.Append(intrinsics.GetValue(i.Key));
                        block.IntrinsicsText.Append("\n");
                    }
                }

                for (int e = activeEvents.Length - 1; e >= 0; e--)
                {
                    var ev = activeEvents[e];
                    ev.TimeRemaining -= dt;
                    if (ev.TimeRemaining <= 0)
                        activeEvents.RemoveAtSwapBack(e);
                    else
                        activeEvents[e] = ev;
                }

                if (conditionEventsLookup.TryGetBuffer(player, out var conditionEvents))
                {
                    var eventMap = conditionEvents.AsMap();
                    foreach (var ce in clipEvents)
                    {
                        if (eventMap.TryGetValue(ce.Key, out int val))
                        {
                            bool found = false;
                            for (int e = 0; e < activeEvents.Length; e++)
                            {
                                if (activeEvents[e].Key.Equals(ce.Key))
                                {
                                    var ev = activeEvents[e];
                                    ev.TimeRemaining = ce.Duration;
                                    ev.Value = val;
                                    activeEvents[e] = ev;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                                activeEvents.Add(new ActiveUIEvent
                                    { Key = ce.Key, Name = ce.Name, Value = val, TimeRemaining = ce.Duration });
                        }
                    }
                }

                foreach (var ev in activeEvents)
                {
                    block.EventsText.Append(ev.Name);
                    block.EventsText.Append("! (");
                    block.EventsText.Append(ev.Value);
                    block.EventsText.Append(")\n");
                }

                this.players.Add(block);
            }

            uiHelper.Binding.Players = this.players;
        }
    }
}
