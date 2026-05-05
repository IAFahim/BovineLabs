using System;
using System.ComponentModel;
using BovineLabs.Timeline.Authoring;
using UnityEngine;
using UnityEngine.Timeline;

namespace BovineLabs.Timeline.UI.Authoring
{
    [Serializable]
    [TrackClipType(typeof(NumberClip))]
    [TrackBindingType(typeof(GameObject))]
    [TrackColor(1f, 1f, 1f)]
    [DisplayName("DOTS/Number Track")]
    public class NumberTrack : DOTSTrack
    {
    }
}
