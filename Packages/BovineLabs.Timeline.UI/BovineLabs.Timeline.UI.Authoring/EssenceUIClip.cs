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

        public ClipCaps clipCaps => ClipCaps.None;
        public override double duration => 1;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent(clipEntity, new EssenceUIComponent 
            { 
                Stat = this.Stat != null ? this.Stat.Key : 0,
                Intrinsic = this.Intrinsic != null ? this.Intrinsic.Key : 0,
                Event = this.Event != null ? this.Event.Key : 0
            });
            
            base.Bake(clipEntity, context);
        }
    }
}