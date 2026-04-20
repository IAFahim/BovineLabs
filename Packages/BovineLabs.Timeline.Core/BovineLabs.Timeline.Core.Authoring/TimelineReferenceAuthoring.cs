using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Timeline.Core.Data.Builders;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Timeline.Core.Authoring
{
    [DisallowMultipleComponent]
    public class TimelineReferenceAuthoring : MonoBehaviour
    {
        private class TimelineReferenceBaker : Baker<TimelineReferenceAuthoring>
        {
            public override void Bake(TimelineReferenceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var commands = new BakerCommands(this, entity);
                var builder = new TimelineReferenceBuilder();
                builder.ApplyTo(ref commands);
            }
        }
    }
}