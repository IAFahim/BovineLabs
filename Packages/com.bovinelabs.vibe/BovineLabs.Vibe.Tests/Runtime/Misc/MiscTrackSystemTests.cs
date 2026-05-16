// <copyright file="MiscTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Misc
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.Time;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
#if BOVINELABS_BRIDGE
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Vibe.Data.Camera;
#endif
    public class MiscTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void TimeScaleTrack_RestoreOnTrackDeactivate_AppliesInitialValue()
        {
            var original = Time.timeScale;
            Time.timeScale = 1f;

            var clipBlob = CreateBlobAssetReference(new TimeScaleClipBlob
            {
                TargetScale = 1f,
                ClampMin = -10f,
                ClampMax = 10f,
                UseCurve = false,
                RestoreOnDeactivate = false,
            });
            var track = this.Manager.CreateEntity(typeof(TimeScaleInitial), typeof(TrackResetOnDeactivate), typeof(TimelineActive), typeof(TimelineActivePrevious));
            this.Manager.SetComponentData(track, new TimeScaleInitial { Value = 0.35f });
            this.Manager.SetComponentEnabled<TimelineActive>(track, false);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, true);

            try
            {
                var gate = this.Manager.CreateEntity(typeof(TimeScaleClipData));
                this.Manager.SetComponentData(gate, new TimeScaleClipData { Value = clipBlob });

                var system = this.World.CreateSystem<TimeScaleTrackSystem>();
                this.RunSystem(system);

                Assert.AreEqual(0.35f, Time.timeScale, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
                Time.timeScale = original;
            }
        }

        [Test]
        public void TimeScaleTrack_RestoreOnClipDeactivate_UsesTrackInitial()
        {
            var original = Time.timeScale;
            Time.timeScale = 0.9f;

            var track = this.Manager.CreateEntity(typeof(TimeScaleInitial));
            this.Manager.SetComponentData(track, new TimeScaleInitial { Value = 0.42f });

            var clipBlob = CreateBlobAssetReference(new TimeScaleClipBlob
            {
                TargetScale = 1.5f,
                ClampMin = -10f,
                ClampMax = 10f,
                UseCurve = false,
                RestoreOnDeactivate = true,
            });

            try
            {
                var clip = this.Manager.CreateEntity(typeof(TimeScaleClipData), typeof(Clip), typeof(ClipActive), typeof(ClipActivePrevious));
                this.Manager.SetComponentData(clip, new TimeScaleClipData { Value = clipBlob });
                this.Manager.SetComponentData(clip, new Clip { Track = track });
                this.Manager.SetComponentEnabled<ClipActive>(clip, false);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, true);

                var system = this.World.CreateSystem<TimeScaleTrackSystem>();
                this.RunSystem(system);

                Assert.AreEqual(0.42f, Time.timeScale, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
                Time.timeScale = original;
            }
        }

        [Test]
        public void TimeScaleTrack_NegativeBlendOutput_ClampsToZero()
        {
            var original = Time.timeScale;
            Time.timeScale = 1f;

            var clipBlob = CreateBlobAssetReference(new TimeScaleClipBlob
            {
                TargetScale = -2f,
                ClampMin = -10f,
                ClampMax = 10f,
                UseCurve = false,
                RestoreOnDeactivate = false,
            });

            try
            {
                var clip = this.Manager.CreateEntity(
                    typeof(TimeScaleAnimated),
                    typeof(TimeScaleClipData),
                    typeof(Clip),
                    typeof(TrackBinding),
                    typeof(TimelineActive),
                    typeof(ClipActive),
                    typeof(ClipActivePrevious));

                this.Manager.SetComponentData(clip, new TimeScaleAnimated { Value = 0f });
                this.Manager.SetComponentData(clip, new TimeScaleClipData { Value = clipBlob });
                this.Manager.SetComponentData(clip, new Clip { Track = Entity.Null });
                this.Manager.SetComponentData(clip, new TrackBinding { Value = Entity.Null });
                this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActive>(clip, true);
                this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

                var system = this.World.CreateSystem<TimeScaleTrackSystem>();
                this.RunSystem(system);

                Assert.AreEqual(0f, Time.timeScale, 0.0001f);
            }
            finally
            {
                clipBlob.Dispose();
                Time.timeScale = original;
            }
        }

#if BOVINELABS_BRIDGE
        [Test]
        public void CameraMatrixShiftTrack_NullBindingUsesCameraMain_AndRestoresOnDeactivate()
        {
            var initialOffset = new float2(0.2f, -0.1f);
            var animatedOffset = new float2(0.6f, 0.4f);

            var camera = this.Manager.CreateEntity(typeof(CameraMain), typeof(CameraViewSpaceOffset));
            this.Manager.SetComponentData(camera, new CameraViewSpaceOffset { ProjectionCenterOffset = initialOffset });

            var track = this.Manager.CreateEntity(typeof(CameraMatrixShiftInitial), typeof(TrackBinding), typeof(TimelineActive), typeof(TimelineActivePrevious));
            this.Manager.SetComponentData(track, new CameraMatrixShiftInitial
            {
                Offset = new CameraViewSpaceOffset { ProjectionCenterOffset = float2.zero },
            });
            this.Manager.SetComponentData(track, new TrackBinding { Value = Entity.Null });
            this.Manager.SetComponentEnabled<TimelineActive>(track, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, false);

            var clip = this.Manager.CreateEntity(
                typeof(CameraMatrixShiftAnimated),
                typeof(CameraMatrixShiftClipData),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious));

            this.Manager.SetComponentData(clip, new CameraMatrixShiftAnimated
            {
                Value = new CameraMatrixShiftBlend { ProjectionCenterOffset = float2.zero },
            });
            this.Manager.SetComponentData(clip, new CameraMatrixShiftClipData
            {
                Type = CameraMatrixShiftClipType.Animated,
                ProjectionCenterOffset = animatedOffset,
            });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = Entity.Null });
            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);

            var system = this.World.CreateSystem<CameraMatrixShiftTrackSystem>();
            this.RunSystem(system);

            var writtenOffset = this.Manager.GetComponentData<CameraViewSpaceOffset>(camera).ProjectionCenterOffset;
            AssertFloat2(writtenOffset, animatedOffset);

            this.Manager.AddComponent<TrackResetOnDeactivate>(track);
            this.Manager.SetComponentEnabled<TimelineActive>(track, false);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(track, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, false);

            this.RunSystem(system);

            var restoredOffset = this.Manager.GetComponentData<CameraViewSpaceOffset>(camera).ProjectionCenterOffset;
            AssertFloat2(restoredOffset, initialOffset);
        }
#endif
        private static BlobAssetReference<T> CreateBlobAssetReference<T>(in T value)
            where T : unmanaged
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<T>();
            root = value;
            var blob = builder.CreateBlobAssetReference<T>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

#if BOVINELABS_BRIDGE
        private static void AssertFloat2(float2 actual, float2 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
        }
#endif
    }
}
