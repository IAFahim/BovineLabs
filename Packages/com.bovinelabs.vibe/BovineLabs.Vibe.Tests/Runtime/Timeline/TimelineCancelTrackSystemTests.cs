// <copyright file="TimelineCancelTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Tests.Runtime.Timeline
{
    using BovineLabs.Bridge.Input;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Timeline;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Entities;

    public class TimelineCancelTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void OnUpdate_InputTriggered_DisablesDirectorTimelineActive()
        {
            var director = this.Manager.CreateEntity(typeof(TimelineActive));
            this.Manager.SetComponentEnabled<TimelineActive>(director, true);

            var clip = this.Manager.CreateEntity(typeof(TimelineCancelClipData), typeof(ClipActive), typeof(DirectorRoot));
            this.Manager.SetComponentData(clip, new DirectorRoot { Director = director });
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);

            var input = this.Manager.CreateEntity(typeof(InputCommon));
            this.Manager.SetComponentData(input, new InputCommon { AnyButtonPress = true });

            var system = this.World.CreateSystem<TimelineCancelTrackSystem>();
            this.RunSystem(system);

            Assert.IsFalse(this.Manager.IsComponentEnabled<TimelineActive>(director));
        }

        [Test]
        public void OnUpdate_InputNotTriggered_LeavesDirectorTimelineActive()
        {
            var director = this.Manager.CreateEntity(typeof(TimelineActive));
            this.Manager.SetComponentEnabled<TimelineActive>(director, true);

            var clip = this.Manager.CreateEntity(typeof(TimelineCancelClipData), typeof(ClipActive), typeof(DirectorRoot));
            this.Manager.SetComponentData(clip, new DirectorRoot { Director = director });
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);

            var input = this.Manager.CreateEntity(typeof(InputCommon));
            this.Manager.SetComponentData(input, new InputCommon { AnyButtonPress = false });

            var system = this.World.CreateSystem<TimelineCancelTrackSystem>();
            this.RunSystem(system);

            Assert.IsTrue(this.Manager.IsComponentEnabled<TimelineActive>(director));
        }
    }
}
#endif
