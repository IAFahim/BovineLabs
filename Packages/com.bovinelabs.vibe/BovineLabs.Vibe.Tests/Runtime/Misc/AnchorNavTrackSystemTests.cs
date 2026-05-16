// <copyright file="AnchorNavTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ANCHOR

namespace BovineLabs.Vibe.Tests.Runtime.Misc
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.UI;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class AnchorNavTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void TrackActivate_WithoutActiveApp_SavesZeroStateHandle()
        {
            this.CreateActionClip(AnchorNavClipAction.ClearBackStack, default);

            var track = this.Manager.CreateEntity(
                typeof(AnchorNavTrackInitial),
                typeof(TrackResetOnDeactivate),
                typeof(TimelineActive),
                typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new AnchorNavTrackInitial { StateHandle = 123 });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);

            var system = this.World.CreateSystem<AnchorNavTrackSystem>();
            this.RunSystem(system);

            var initial = this.Manager.GetComponentData<AnchorNavTrackInitial>(track);
            Assert.AreEqual(0, initial.StateHandle);
        }

        [TestCase(AnchorNavClipAction.Navigate, "panel")]
        [TestCase(AnchorNavClipAction.ClearNavigation, "")]
        [TestCase(AnchorNavClipAction.ClearBackStack, "")]
        [TestCase(AnchorNavClipAction.PopBackStack, "")]
        [TestCase(AnchorNavClipAction.PopBackStackToPanel, "")]
        [TestCase(AnchorNavClipAction.CloseAllPopups, "")]
        [TestCase(AnchorNavClipAction.ClosePopup, "panel")]
        public void ClipActions_WithoutActiveApp_DoNotThrow(AnchorNavClipAction action, string destination)
        {
            this.CreateActionClip(action, new FixedString32Bytes(destination));

            var system = this.World.CreateSystem<AnchorNavTrackSystem>();

            Assert.DoesNotThrow(() => this.RunSystem(system));
        }

        private Entity CreateActionClip(AnchorNavClipAction action, in FixedString32Bytes destination)
        {
            var clip = this.Manager.CreateEntity(
                typeof(AnchorNavClipData),
                typeof(ClipActive),
                typeof(ClipActivePrevious));

            this.Manager.SetComponentData(
                clip,
                new AnchorNavClipData
                {
                    Action = action,
                    Destination = destination,
                    ExitAnimation = 3,
                });
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);
            return clip;
        }
    }
}

#endif
