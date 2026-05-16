// <copyright file="MaterialPropertyTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Rendering
{
#if UNITY_URP
    using BovineLabs.Vibe.Data.Rendering;
    using Unity.Rendering;
#endif
#if UNITY_HDRP
    using BovineLabs.Vibe.Data.Rendering;
    using Unity.Rendering;
#endif
#if UNITY_URP
    public class URPMaterialPropertyTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void URPMaterialTrack_ClipWritesOnlyEnabledFlags()
        {
            var bound = this.Manager.CreateEntity(typeof(URPMaterialPropertyBumpScale), typeof(URPMaterialPropertyMetallic));
            this.Manager.SetComponentData(bound, new URPMaterialPropertyBumpScale { Value = 1f });
            this.Manager.SetComponentData(bound, new URPMaterialPropertyMetallic { Value = 0.3f });

            var track = this.Manager.CreateEntity(
                typeof(URPMaterialPropertyInitial),
                typeof(URPMaterialPropertyTrackComponents),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new URPMaterialPropertyInitial
            {
                Flags = URPMaterialPropertyFlags.BumpScale | URPMaterialPropertyFlags.Metallic,
                Value = default,
            });
            this.Manager.SetComponentData(track, new URPMaterialPropertyTrackComponents { AddComponents = false });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);

            var clip = this.Manager.CreateEntity(
                typeof(URPMaterialPropertyAnimated),
                typeof(URPMaterialPropertyClipData),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, new URPMaterialPropertyAnimated { Value = default });
            this.Manager.SetComponentData(clip, new URPMaterialPropertyClipData
            {
                Flags = URPMaterialPropertyFlags.BumpScale,
                BumpScale = 2f,
                Metallic = 0.9f,
            });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

#if BL_CORE_EXTENSIONS
            var commandBufferSystem = this.World.CreateSystem<InstantiateCommandBufferSystem>();
#else
            var commandBufferSystem = this.World.CreateSystem<Unity.Entities.EndSimulationEntityCommandBufferSystem>();
#endif
            var system = this.World.CreateSystem<URPMaterialPropertyTrackSystem>();
            this.RunSystem(system);
            this.RunSystem(commandBufferSystem);

            var bump = this.Manager.GetComponentData<URPMaterialPropertyBumpScale>(bound);
            var metallic = this.Manager.GetComponentData<URPMaterialPropertyMetallic>(bound);
            Assert.AreEqual(2f, bump.Value, 0.0001f);
            Assert.AreEqual(0.3f, metallic.Value, 0.0001f);
        }

        [Test]
        public void URPMaterialTrack_NoClipFlags_DoesNotWrite()
        {
            var bound = this.Manager.CreateEntity(typeof(URPMaterialPropertyBumpScale));
            this.Manager.SetComponentData(bound, new URPMaterialPropertyBumpScale { Value = 1f });

            var track = this.Manager.CreateEntity(
                typeof(URPMaterialPropertyInitial),
                typeof(URPMaterialPropertyTrackComponents),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new URPMaterialPropertyInitial
            {
                Flags = URPMaterialPropertyFlags.BumpScale,
                Value = default,
            });
            this.Manager.SetComponentData(track, new URPMaterialPropertyTrackComponents { AddComponents = false });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);

            var clip = this.Manager.CreateEntity(
                typeof(URPMaterialPropertyAnimated),
                typeof(URPMaterialPropertyClipData),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, new URPMaterialPropertyAnimated { Value = default });
            this.Manager.SetComponentData(clip, new URPMaterialPropertyClipData
            {
                Flags = URPMaterialPropertyFlags.None,
                BumpScale = 99f,
            });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

#if BL_CORE_EXTENSIONS
            var commandBufferSystem = this.World.CreateSystem<InstantiateCommandBufferSystem>();
#else
            var commandBufferSystem = this.World.CreateSystem<Unity.Entities.EndSimulationEntityCommandBufferSystem>();
#endif
            var system = this.World.CreateSystem<URPMaterialPropertyTrackSystem>();
            this.RunSystem(system);
            this.RunSystem(commandBufferSystem);

            var bump = this.Manager.GetComponentData<URPMaterialPropertyBumpScale>(bound);
            Assert.AreEqual(1f, bump.Value, 0.0001f);
        }
    }
#endif
#if UNITY_HDRP
    public class HDRPMaterialPropertyTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void HDRPMaterialTrack_ClipWritesOnlyEnabledFlags()
        {
            var bound = this.Manager.CreateEntity(typeof(HDRPMaterialPropertyAlphaCutoff), typeof(HDRPMaterialPropertyMetallic));
            this.Manager.SetComponentData(bound, new HDRPMaterialPropertyAlphaCutoff { Value = 0.2f });
            this.Manager.SetComponentData(bound, new HDRPMaterialPropertyMetallic { Value = 0.7f });

            var track = this.Manager.CreateEntity(
                typeof(HDRPMaterialPropertyInitial),
                typeof(HDRPMaterialPropertyTrackComponents),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(track, new HDRPMaterialPropertyInitial
            {
                Flags = HDRPMaterialPropertyFlags.AlphaCutoff | HDRPMaterialPropertyFlags.Metallic,
                Value = default,
            });
            this.Manager.SetComponentData(track, new HDRPMaterialPropertyTrackComponents { ManageComponents = false });
            this.Manager.SetComponentData(track, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);

            var clip = this.Manager.CreateEntity(
                typeof(HDRPMaterialPropertyAnimated),
                typeof(HDRPMaterialPropertyClipData),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, new HDRPMaterialPropertyAnimated { Value = default });
            this.Manager.SetComponentData(clip, new HDRPMaterialPropertyClipData
            {
                Flags = HDRPMaterialPropertyFlags.AlphaCutoff,
                AlphaCutoff = 0.8f,
                Metallic = 0.1f,
                BaseColor = new float4(1f),
            });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = bound });
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

#if BL_CORE_EXTENSIONS
            var commandBufferSystem = this.World.CreateSystem<InstantiateCommandBufferSystem>();
#else
            var commandBufferSystem = this.World.CreateSystem<Unity.Entities.EndSimulationEntityCommandBufferSystem>();
#endif
            var system = this.World.CreateSystem<HDRPMaterialPropertyTrackSystem>();
            this.RunSystem(system);
            this.RunSystem(commandBufferSystem);

            var alpha = this.Manager.GetComponentData<HDRPMaterialPropertyAlphaCutoff>(bound);
            var metallic = this.Manager.GetComponentData<HDRPMaterialPropertyMetallic>(bound);
            Assert.AreEqual(0.8f, alpha.Value, 0.0001f);
            Assert.AreEqual(0.7f, metallic.Value, 0.0001f);
        }
    }
#endif
}
