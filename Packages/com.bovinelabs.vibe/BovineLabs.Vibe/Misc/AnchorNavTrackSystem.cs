// <copyright file="AnchorNavTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR
namespace BovineLabs.Vibe
{
    using BovineLabs.Anchor.Nav;
    using BovineLabs.Core;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.UI;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary>
    /// Invokes Anchor navigation actions when timeline clips become active.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | Worlds.Menu)]
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public partial struct AnchorNavTrackSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AnchorNavClipData>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var initial in SystemAPI
                .Query<RefRW<AnchorNavTrackInitial>>()
                .WithAll<TrackResetOnDeactivate, TimelineActive>()
                .WithDisabled<TimelineActivePrevious>())
            {
                initial.ValueRW.StateHandle = AnchorNavHost.Burst.SaveStateHandle();
            }

            foreach (var initial in SystemAPI
                .Query<RefRW<AnchorNavTrackInitial>>()
                .WithAll<TrackResetOnDeactivate, TimelineActivePrevious>()
                .WithDisabled<TimelineActive>())
            {
                var handle = initial.ValueRW.StateHandle;
                Check.Assume(handle != 0);
                AnchorNavHost.Burst.ReleaseStateHandle(handle);
                initial.ValueRW.StateHandle = 0;
            }

            foreach (var clipData in SystemAPI.Query<RefRO<AnchorNavClipData>>().WithAll<ClipActive>().WithDisabled<ClipActivePrevious>())
            {
                ref readonly var cd = ref clipData.ValueRO;

                switch (cd.Action)
                {
                    case AnchorNavClipAction.Navigate:
                        if (cd.Destination.Length != 0)
                        {
                            AnchorNavHost.Burst.Navigate(cd.Destination);
                        }

                        break;
                    case AnchorNavClipAction.ClearNavigation:
                        AnchorNavHost.Burst.ClearNavigation(cd.ExitAnimation);
                        break;
                    case AnchorNavClipAction.ClearBackStack:
                        AnchorNavHost.Burst.ClearBackStack();
                        break;
                    case AnchorNavClipAction.PopBackStack:
                        AnchorNavHost.Burst.PopBackStack();
                        break;
                    case AnchorNavClipAction.PopBackStackToPanel:
                        AnchorNavHost.Burst.PopBackStackToPanel();
                        break;
                    case AnchorNavClipAction.CloseAllPopups:
                        AnchorNavHost.Burst.CloseAllPopups(cd.ExitAnimation);
                        break;
                    case AnchorNavClipAction.ClosePopup:
                        if (cd.Destination.Length != 0)
                        {
                            AnchorNavHost.Burst.ClosePopup(cd.Destination, cd.ExitAnimation);
                        }

                        break;
                }
            }
        }
    }
}
#endif
