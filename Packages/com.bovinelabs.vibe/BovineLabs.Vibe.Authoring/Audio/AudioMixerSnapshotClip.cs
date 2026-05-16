#if BOVINELABS_BRIDGE
// // <copyright file="AudioMixerSnapshotClip.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Vibe.Authoring.Audio
// {
//     using System;
//     using BovineLabs.Timeline.Authoring;
//     using BovineLabs.Vibe.Data.Audio;
//     using Unity.Entities;
//     using UnityEngine;
//     using UnityEngine.Audio;
//     using UnityEngine.Timeline;
//
//     /// <summary>
//     /// Timeline clip that transitions to an audio mixer snapshot.
//     /// </summary>
//     [Serializable]
//     public class AudioMixerSnapshotClip : DOTSClip, ITimelineClipAsset
//     {
//         [SerializeField]
//         [Tooltip("Snapshot to transition to when the clip activates.")]
//         private AudioMixerSnapshot snapshot;
//
//         [SerializeField]
//         [Tooltip("Snapshot to return to when the clip deactivates.")]
//         private AudioMixerSnapshot originalSnapshot;
//
//         [SerializeField]
//         [Tooltip("Duration of the snapshot transition in seconds.")]
//         [Min(0f)]
//         private float transitionDuration = 0.5f;
//
//         /// <inheritdoc/>
//         public ClipCaps clipCaps => ClipCaps.None;
//
//         /// <inheritdoc/>
//         public override void Bake(Entity clipEntity, BakingContext context)
//         {
//             base.Bake(clipEntity, context);
//
//             context.Baker.AddComponent(
//                 clipEntity,
//                 new AudioMixerSnapshotClipData
//                 {
//                     Snapshot = this.snapshot,
//                     OriginalSnapshot = this.originalSnapshot,
//                     TransitionDuration = Mathf.Max(0f, this.transitionDuration),
//                 });
//         }
//
//         private void OnValidate()
//         {
//             this.transitionDuration = Mathf.Max(0f, this.transitionDuration);
//         }
//     }
// }
#endif
