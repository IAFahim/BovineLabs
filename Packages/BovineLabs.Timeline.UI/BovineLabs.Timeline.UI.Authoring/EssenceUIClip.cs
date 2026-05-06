using System;
using BovineLabs.Essence.Authoring;
using BovineLabs.Reaction.Authoring.Conditions;
using BovineLabs.Timeline.Authoring;
using BovineLabs.Timeline.UI.Data;
using Unity.Entities;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.UI.Authoring
{
    [Serializable]
    public class EssenceUIClip : DOTSClip, ITimelineClipAsset
    {
        public StatSchemaObject Stat;
        public IntrinsicSchemaObject Intrinsic;
        public ConditionEventObject Event;
        public override double duration => 1;

        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent(clipEntity, new EssenceUIComponent
            {
                Stat = Stat != null ? Stat.Key : 0,
                Intrinsic = Intrinsic != null ? Intrinsic.Key : 0,
                Event = Event != null ? Event.Key : 0
            });

            base.Bake(clipEntity, context);
        }
    }
}