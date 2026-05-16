#if BOVINELABS_BRIDGE
// // <copyright file="AudioMixerSnapshotTrack.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Vibe.Authoring.Audio
// {
//     using System;
//     using System.ComponentModel;
//     using BovineLabs.Bridge.Data.Audio;
//     using BovineLabs.Timeline.Authoring;
//     using UnityEngine;
//     using UnityEngine.Timeline;
//
//     /// <summary>
//     /// Timeline track that transitions audio mixer snapshots.
//     /// </summary>
//     [Serializable]
//     [TrackClipType(typeof(AudioMixerSnapshotClip))]
//     [TrackBindingType(typeof(AudioListener))]
//     [TrackColor(0.3f, 0.45f, 0.75f)]
//     [DisplayName("DOTS/Audio/Audio Mixer Snapshot Track")]
//     public class AudioMixerSnapshotTrack : DOTSTrack
//     {
//         /// <inheritdoc/>
//         protected override void Bake(BakingContext context)
//         {
//             if (context.Binding != null && context.Binding.Target != Unity.Entities.Entity.Null)
//             {
//                 context.Baker.AddComponent<AudioMixerSnapshotData>(context.Binding.Target);
//             }
//         }
//     }
// }
#endif
