using System;
using BovineLabs.Timeline.Authoring;
using Scripts.Data;
using Unity.Entities;
using UnityEngine.Timeline;

[Serializable]
public class NumberClip : DOTSClip, ITimelineClipAsset
{
    public int Number;
    public ClipCaps clipCaps => ClipCaps.None;

    public override void Bake(Entity clipEntity, BakingContext context)
    {
        // Add our component with the authored number to the clip entity
        context.Baker.AddComponent(clipEntity, new NumberComponent { Value = this.Number });
        base.Bake(clipEntity, context);
    }
}