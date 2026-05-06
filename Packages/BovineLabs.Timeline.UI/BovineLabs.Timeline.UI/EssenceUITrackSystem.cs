using BovineLabs.Anchor;
using BovineLabs.Essence.Data;
using BovineLabs.Reaction.Data.Conditions;
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.Essence;
using BovineLabs.Timeline.UI.Data;
using BovineLabs.Timeline.UI.Data.ViewModel;
using Unity.Burst;
using Unity.Entities;

namespace BovineLabs.Timeline.UI
{
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    [UpdateAfter(typeof(TimelineEssenceStatSystem))]
    [UpdateAfter(typeof(TimelineEssenceIntrinsicSystem))]
    [UpdateAfter(typeof(TimelineEssenceEventSystem))]
    [WorldSystemFilter(
        WorldSystemFilterFlags.LocalSimulation |
        WorldSystemFilterFlags.ClientSimulation |
        WorldSystemFilterFlags.ServerSimulation |
        WorldSystemFilterFlags.Presentation
    )]
    public partial struct EssenceUITrackSystem : ISystem, ISystemStartStop
    {
        private UIHelper<EssenceUIViewModel, EssenceUIViewModel.Data> uiHelper;

        public void OnCreate(ref SystemState state)
        {
            uiHelper =
                new UIHelper<EssenceUIViewModel, EssenceUIViewModel.Data>(ref state,
                    ComponentType.ReadOnly<EssenceUIComponent>());
        }

        public void OnStartRunning(ref SystemState state)
        {
            uiHelper.Bind();
        }

        public void OnStopRunning(ref SystemState state)
        {
            uiHelper.Unbind();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency.Complete();

            var isVisible = false;
            float statVal = 0;
            var intrinsicVal = 0;
            var eventVal = 0;
            var hasEvent = false;

            var statsLookup = SystemAPI.GetBufferLookup<Stat>(true);
            var intrinsicsLookup = SystemAPI.GetBufferLookup<Intrinsic>(true);
            var eventsLookup = SystemAPI.GetBufferLookup<ConditionEvent>(true);

            foreach (var (clipData, trackBinding) in SystemAPI.Query<RefRO<EssenceUIComponent>, RefRO<TrackBinding>>())
            {
                isVisible = true;
                var target = trackBinding.ValueRO.Value;

                if (statsLookup.TryGetBuffer(target, out var stats))
                    statVal = stats.GetValueFloat(clipData.ValueRO.Stat);

                if (intrinsicsLookup.TryGetBuffer(target, out var intrinsics))
                    intrinsicVal = intrinsics.GetValue(clipData.ValueRO.Intrinsic);

                if (eventsLookup.TryGetBuffer(target, out var events))
                    if (events.AsMap().TryGetValue(clipData.ValueRO.Event, out var evValue))
                    {
                        hasEvent = true;
                        eventVal = evValue;
                    }

                break;
            }

            ref var data = ref uiHelper.Binding;
            data.IsVisible = isVisible;

            if (isVisible)
            {
                data.StatValue = statVal;
                data.IntrinsicValue = intrinsicVal;
                data.HasEvent = hasEvent;
                data.EventValue = eventVal;
            }
        }
    }
}