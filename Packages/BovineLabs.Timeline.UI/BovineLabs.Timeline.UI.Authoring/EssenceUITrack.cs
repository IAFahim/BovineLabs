using System;
using System.ComponentModel;
using BovineLabs.Essence.Authoring;
using BovineLabs.Timeline.Authoring;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.UI.Authoring
{
    [Serializable]
    [TrackClipType(typeof(EssenceUIClip))]
    [TrackBindingType(typeof(StatAuthoring))]
    [TrackColor(0.8f, 0.2f, 0.8f)]
    [DisplayName("DOTS/Essence UI Track")]
    public class EssenceUITrack : DOTSTrack
    {
    }
}