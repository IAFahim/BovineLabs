using System;
using System.ComponentModel;
using BovineLabs.Timeline.Authoring;
using UnityEngine;
using UnityEngine.Timeline;

[Serializable]
[TrackClipType(typeof(NumberClip))]
[TrackBindingType(typeof(GameObject))]
[TrackColor(0.2f, 0.6f, 0.8f)]
[DisplayName("DOTS/Number Track")]
public class NumberTrack : DOTSTrack
{
}