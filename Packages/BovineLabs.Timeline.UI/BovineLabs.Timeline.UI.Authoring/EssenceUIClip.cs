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
    public struct EventUIConfig
    {
        public ConditionEventObject Event;
        public float DisplayDuration; // How long to buffer the event on screen
    }

    [Serializable]
    public class EssenceUIClip : DOTSClip, ITimelineClipAsset
    {
        public StatSchemaObject[] Stats;
        public IntrinsicSchemaObject[] Intrinsics;
        public EventUIConfig[] Events;
        
        public override double duration => 1;
        public ClipCaps clipCaps => ClipCaps.None;

        public override void Bake(Entity clipEntity, BakingContext context)
        {
            var statBuffer = context.Baker.AddBuffer<ClipStat>(clipEntity);
            if (Stats != null)
                foreach (var s in Stats)
                    if (s != null) statBuffer.Add(new ClipStat { Key = s.Key, Name = s.name });

            var intBuffer = context.Baker.AddBuffer<ClipIntrinsic>(clipEntity);
            if (Intrinsics != null)
                foreach (var i in Intrinsics)
                    if (i != null) intBuffer.Add(new ClipIntrinsic { Key = i.Key, Name = i.name });

            var evBuffer = context.Baker.AddBuffer<ClipEvent>(clipEntity);
            if (Events != null)
                foreach (var e in Events)
                    if (e.Event != null) evBuffer.Add(new ClipEvent { Key = e.Event.Key, Name = e.Event.name, Duration = e.DisplayDuration });

            // Empty buffer for the system to populate at runtime
            context.Baker.AddBuffer<ActiveUIEvent>(clipEntity);
            
            base.Bake(clipEntity, context);
        }
    }
}