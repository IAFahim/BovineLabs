#if BOVINELABS_BRIDGE
// // <copyright file="AudioMixerSnapshotTrackSystem.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Vibe
// {
//     using BovineLabs.Bridge.Data.Audio;
//     using BovineLabs.Timeline;
//     using BovineLabs.Timeline.Data;
//     using BovineLabs.Vibe.Data.Audio;
//     using Unity.Burst;
//     using Unity.Collections;
//     using Unity.Entities;
//     using UnityEngine.Audio;
//
//     /// <summary>
//     /// Triggers audio mixer snapshot transitions for timeline clips.
//     /// </summary>
//     [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
//     public partial struct AudioMixerSnapshotTrackSystem : ISystem
//     {
//         /// <inheritdoc/>
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<AudioMixerSnapshotClipData>();
//         }
//
//         /// <inheritdoc/>
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             state.Dependency = new ClipActivateJob
//             {
//                 Snapshots = SystemAPI.GetComponentLookup<AudioMixerSnapshotData>(),
//             }.ScheduleParallel(state.Dependency);
//         }
//
//         [BurstCompile]
//         [WithAll(typeof(ClipActive))]
//         [WithDisabled(typeof(ClipActivePrevious))]
//         private partial struct ClipActivateJob : IJobEntity
//         {
//             [NativeDisableParallelForRestriction]
//             public ComponentLookup<AudioMixerSnapshotData> Snapshots;
//
//             private void Execute(in AudioMixerSnapshotClipData clipData, in TrackBinding trackBinding)
//             {
//                 if (!this.Snapshots.TryGetRefRW(trackBinding.Value, out var snapshotData))
//                 {
//                     return;
//                 }
//
//                 snapshotData.ValueRW.Snapshot = clipData.Snapshot;
//                 snapshotData.ValueRW.OriginalSnapshot = clipData.OriginalSnapshot;
//                 snapshotData.ValueRW.TransitionDuration = clipData.TransitionDuration;
//                 snapshotData.ValueRW.TransitionId = snapshotData.ValueRO.TransitionId + 1u;
//             }
//         }
//     }
// }
#endif
