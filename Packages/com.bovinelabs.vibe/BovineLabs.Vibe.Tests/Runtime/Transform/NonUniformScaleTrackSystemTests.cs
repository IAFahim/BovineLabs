// <copyright file="NonUniformScaleTrackSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Transform
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.IntegerTime;
    using Unity.Mathematics;
    using Unity.Transforms;

    public class NonUniformScaleTrackSystemTests : VibeEcsTestsFixture
    {
        [Test]
        public void SquashStretchAbsolute_XAxisPreserveVolume_AppliesExpectedMultiplier()
        {
            var target = this.CreatePostTransformEntity(new float3(1f, 1f, 1f));
            var track = this.CreateTrack(target);
            var clipData = CreateSquashStretchAbsoluteClip(
                SquashStretchNonUniformScaleAxis.X,
                preserveVolume: true,
                transformOnClipActivation: false,
                volumeExponent: 1f,
                amount: 1f);

            try
            {
                this.CreateClip(track, target, clipData, localTimeSeconds: 0.25);
                var system = this.World.CreateSystem<NonUniformScaleTrackSystem>();
                this.RunSystem(system);

                var scale = GetScale(this.Manager.GetComponentData<PostTransformMatrix>(target));
                AssertFloat3(scale, new float3(2f, 0.5f, 0.5f));
            }
            finally
            {
                clipData.Dispose();
            }
        }

        [Test]
        public void SquashStretchAbsolute_InvalidAxis_FallsBackToZAxisBranch()
        {
            var target = this.CreatePostTransformEntity(new float3(1f, 1f, 1f));
            var track = this.CreateTrack(target);
            var clipData = CreateSquashStretchAbsoluteClip(
                (SquashStretchNonUniformScaleAxis)255,
                preserveVolume: true,
                transformOnClipActivation: false,
                volumeExponent: 1f,
                amount: 1f);

            try
            {
                this.CreateClip(track, target, clipData, localTimeSeconds: 0.25);
                var system = this.World.CreateSystem<NonUniformScaleTrackSystem>();
                this.RunSystem(system);

                var scale = GetScale(this.Manager.GetComponentData<PostTransformMatrix>(target));
                AssertFloat3(scale, new float3(0.5f, 0.5f, 2f));
            }
            finally
            {
                clipData.Dispose();
            }
        }

        private static void AssertFloat3(float3 actual, float3 expected, float tolerance = 0.0001f)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
            Assert.AreEqual(expected.z, actual.z, tolerance);
        }

        private static BlobAssetReference<NonUniformScaleClipBlob> CreateSquashStretchAbsoluteClip(
            SquashStretchNonUniformScaleAxis axis,
            bool preserveVolume,
            bool transformOnClipActivation,
            float volumeExponent,
            float amount)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<NonUniformScaleClipBlob>();
            root.Type = NonUniformScaleType.SquashStretchAbsolute;
            root.SquashStretch = new NonUniformScaleClipBlob.SquashStretchData
            {
                Axis = axis,
                PreserveVolume = preserveVolume,
                TransformOnClipActivation = transformOnClipActivation,
                VolumeExponent = volumeExponent,
            };
            root.SquashStretchAbsolute = new NonUniformScaleClipBlob.SquashStretchAbsoluteData { Amount = amount };
            var blob = builder.CreateBlobAssetReference<NonUniformScaleClipBlob>(Allocator.Persistent);
            builder.Dispose();
            return blob;
        }

        private static float3 GetScale(in PostTransformMatrix matrix)
        {
            return new float3(matrix.Value.c0.x, matrix.Value.c1.y, matrix.Value.c2.z);
        }

        private Entity CreatePostTransformEntity(in float3 scale)
        {
            var entity = this.Manager.CreateEntity(typeof(PostTransformMatrix));
            this.Manager.SetComponentData(entity, new PostTransformMatrix { Value = float4x4.Scale(scale) });
            return entity;
        }

        private Entity CreateTrack(Entity target)
        {
            var entity = this.Manager.CreateEntity(
                typeof(PostTransformMatrixInitial),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(TimelineActivePrevious));

            this.Manager.SetComponentData(entity, new TrackBinding { Value = target });
            this.Manager.SetComponentData(entity, new PostTransformMatrixInitial
            {
                Value = new PostTransformMatrix { Value = float4x4.identity },
            });
            this.Manager.SetComponentEnabled<TimelineActive>(entity, true);
            this.Manager.SetComponentEnabled<TimelineActivePrevious>(entity, false);
            return entity;
        }

        private Entity CreateClip(Entity track, Entity target, BlobAssetReference<NonUniformScaleClipBlob> data, double localTimeSeconds)
        {
            var clip = this.Manager.CreateEntity(
                typeof(NonUniformScaleAnimated),
                typeof(NonUniformScaleClipData),
                typeof(PostTransformMatrixClipInitial),
                typeof(Clip),
                typeof(TrackBinding),
                typeof(TimelineActive),
                typeof(ClipActive),
                typeof(ClipActivePrevious),
                typeof(LocalTime),
                typeof(TimeTransform),
                typeof(ClipBlobCurveCache));

            this.Manager.SetComponentData(clip, new NonUniformScaleAnimated
            {
                BaseScale = new float3(1f, 1f, 1f),
                Value = new float3(1f, 1f, 1f),
            });
            this.Manager.SetComponentData(clip, new NonUniformScaleClipData { Value = data });
            this.Manager.SetComponentData(clip, new PostTransformMatrixClipInitial
            {
                Value = new PostTransformMatrix { Value = float4x4.identity },
            });
            this.Manager.SetComponentData(clip, new Clip { Track = track });
            this.Manager.SetComponentData(clip, new TrackBinding { Value = target });
            this.Manager.SetComponentData(clip, new LocalTime { Value = new DiscreteTime(localTimeSeconds) });
            this.Manager.SetComponentData(clip, new TimeTransform
            {
                Start = new DiscreteTime(0.0),
                End = new DiscreteTime(1.0),
                ClipIn = DiscreteTime.Zero,
                Scale = 1.0,
            });
            this.Manager.SetComponentData(clip, ClipBlobCurveCache.Create());

            this.Manager.SetComponentEnabled<TimelineActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActive>(clip, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(clip, false);
            return clip;
        }
    }
}
