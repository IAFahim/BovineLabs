// <copyright file="ClipTransformSelectionTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.Runtime.Utility
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using BovineLabs.Vibe.Tests.Fixtures;
    using NUnit.Framework;
    using Unity.Mathematics;
    using Unity.Transforms;

    public class ClipTransformSelectionTests : VibeEcsTestsFixture
    {
        [Test]
        public void SelectLocalTransform_UsesClipInitialWhenRequested()
        {
            var trackEntity = this.Manager.CreateEntity(typeof(LocalTransformInitial));
            var trackInitial = LocalTransform.FromPositionRotationScale(new float3(8f, 8f, 8f), quaternion.identity, 8f);
            this.Manager.SetComponentData(trackEntity, new LocalTransformInitial { Value = trackInitial });

            var clipInitial = LocalTransform.FromPositionRotationScale(new float3(1f, 2f, 3f), quaternion.identity, 2f);
            var clip = new Clip { Track = trackEntity };
            var trackInitialLookup = this.Manager.GetComponentLookup<LocalTransformInitial>(true);

            var selected = ClipTransformSelection.SelectLocalTransform(true, in clipInitial, ref trackInitialLookup, in clip);

            Assert.AreEqual(clipInitial.Position, selected.Position);
            Assert.AreEqual(clipInitial.Scale, selected.Scale);
            Assert.AreEqual(clipInitial.Rotation.value, selected.Rotation.value);
        }

        [Test]
        public void SelectLocalTransform_UsesTrackInitialWhenClipInitialDisabled()
        {
            var trackEntity = this.Manager.CreateEntity(typeof(LocalTransformInitial));
            var trackInitial = LocalTransform.FromPositionRotationScale(new float3(5f, 6f, 7f), quaternion.identity, 3f);
            this.Manager.SetComponentData(trackEntity, new LocalTransformInitial { Value = trackInitial });

            var clipInitial = LocalTransform.FromPositionRotationScale(new float3(1f, 1f, 1f), quaternion.identity, 1f);
            var clip = new Clip { Track = trackEntity };
            var trackInitialLookup = this.Manager.GetComponentLookup<LocalTransformInitial>(true);

            var selected = ClipTransformSelection.SelectLocalTransform(false, in clipInitial, ref trackInitialLookup, in clip);

            Assert.AreEqual(trackInitial.Position, selected.Position);
            Assert.AreEqual(trackInitial.Scale, selected.Scale);
            Assert.AreEqual(trackInitial.Rotation.value, selected.Rotation.value);
        }

        [Test]
        public void SelectPostTransformMatrix_UsesClipInitialWhenRequested()
        {
            var trackEntity = this.Manager.CreateEntity(typeof(PostTransformMatrixInitial));
            var trackInitial = new PostTransformMatrix { Value = float4x4.Scale(new float3(4f, 4f, 4f)) };
            this.Manager.SetComponentData(trackEntity, new PostTransformMatrixInitial { Value = trackInitial });

            var clipInitial = new PostTransformMatrix { Value = float4x4.Scale(new float3(1f, 2f, 3f)) };
            var clip = new Clip { Track = trackEntity };
            var trackInitialLookup = this.Manager.GetComponentLookup<PostTransformMatrixInitial>(true);

            var selected = ClipTransformSelection.SelectPostTransformMatrix(true, in clipInitial, ref trackInitialLookup, in clip);

            Assert.AreEqual(clipInitial.Value, selected.Value);
        }

        [Test]
        public void SelectPostTransformMatrix_UsesTrackInitialWhenClipInitialDisabled()
        {
            var trackEntity = this.Manager.CreateEntity(typeof(PostTransformMatrixInitial));
            var trackInitial = new PostTransformMatrix { Value = float4x4.Scale(new float3(7f, 8f, 9f)) };
            this.Manager.SetComponentData(trackEntity, new PostTransformMatrixInitial { Value = trackInitial });

            var clipInitial = new PostTransformMatrix { Value = float4x4.Scale(new float3(1f, 1f, 1f)) };
            var clip = new Clip { Track = trackEntity };
            var trackInitialLookup = this.Manager.GetComponentLookup<PostTransformMatrixInitial>(true);

            var selected = ClipTransformSelection.SelectPostTransformMatrix(false, in clipInitial, ref trackInitialLookup, in clip);

            Assert.AreEqual(trackInitial.Value, selected.Value);
        }
    }
}
