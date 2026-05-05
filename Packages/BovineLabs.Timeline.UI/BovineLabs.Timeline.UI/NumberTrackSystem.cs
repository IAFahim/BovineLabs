namespace BovineLabs.Timeline.UI
{
    using BovineLabs.Anchor;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.UI.Data;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    [WorldSystemFilter(
        WorldSystemFilterFlags.LocalSimulation |
        WorldSystemFilterFlags.ClientSimulation |
        WorldSystemFilterFlags.ServerSimulation |
        WorldSystemFilterFlags.Presentation)]
    public partial struct NumberTrackSystem : ISystem, ISystemStartStop
    {
        private UIHelper<NumberViewModel, NumberViewModel.Data> uiHelper;

        public void OnCreate(ref SystemState state)
        {
            this.uiHelper =
                new UIHelper<NumberViewModel, NumberViewModel.Data>(ref state,
                    ComponentType.ReadOnly<NumberComponent>());
        }

        public void OnStartRunning(ref SystemState state) => this.uiHelper.Bind();
        public void OnStopRunning(ref SystemState state) => this.uiHelper.Unbind();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            bool isVisible = false;
            int currentNumber = 0;

            foreach (var number in SystemAPI.Query<RefRO<NumberComponent>>().WithAll<TimelineActive, ClipActive>())
            {
                isVisible = true;
                currentNumber = number.ValueRO.Value;
                break;
            }

            ref var data = ref this.uiHelper.Binding;
            data.IsVisible = isVisible;
            if (isVisible)
            {
                data.Number = currentNumber;
            }
        }
    }
}
