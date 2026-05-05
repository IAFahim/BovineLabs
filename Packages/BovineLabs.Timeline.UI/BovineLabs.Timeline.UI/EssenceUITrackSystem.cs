// namespace BovineLabs.Timeline.UI
// {
//     using BovineLabs.Anchor;
//     using BovineLabs.Timeline.UI.Data.ViewModel;
//     using BovineLabs.Essence.Data;
//     using BovineLabs.Reaction.Data.Conditions;
//     using BovineLabs.Timeline;
//     using BovineLabs.Timeline.Data;
//     using BovineLabs.Timeline.UI.Data;
//     using Unity.Burst;
//     using Unity.Entities;
//
//     [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
//     [WorldSystemFilter(
//         WorldSystemFilterFlags.LocalSimulation |
//         WorldSystemFilterFlags.ClientSimulation |
//         WorldSystemFilterFlags.ServerSimulation |
//         WorldSystemFilterFlags.Presentation
//     )]
//     public partial struct EssenceUITrackSystem : ISystem, ISystemStartStop
//     {
//         private UIHelper<EssenceUIViewModel, EssenceUIViewModel.Data> uiHelper;
//
//         public void OnCreate(ref SystemState state)
//         {
//             this.uiHelper =
//                 new UIHelper<EssenceUIViewModel, EssenceUIViewModel.Data>(ref state,
//                     ComponentType.ReadOnly<EssenceUIComponent>());
//         }
//
//         public void OnStartRunning(ref SystemState state) => this.uiHelper.Bind();
//         public void OnStopRunning(ref SystemState state) => this.uiHelper.Unbind();
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             bool isVisible = false;
//             float statVal = 0;
//             int intrinsicVal = 0;
//             int eventVal = 0;
//             bool hasEvent = false;
//
//             // Get access to the target entity's Essence/Reaction buffers
//             var statsLookup = SystemAPI.GetBufferLookup<Stat>(isReadOnly: true);
//             var intrinsicsLookup = SystemAPI.GetBufferLookup<Intrinsic>(isReadOnly: true);
//             var eventsLookup = SystemAPI.GetBufferLookup<ConditionEvent>(isReadOnly: true);
//
//             foreach (var (clipData, trackBinding) in SystemAPI.Query<RefRO<EssenceUIComponent>, RefRO<TrackBinding>>())
//             {
//                 isVisible = true;
//                 Entity target = trackBinding.ValueRO.Value;
//
//                 // 1. Read Stat
//                 if (statsLookup.TryGetBuffer(target, out var stats))
//                 {
//                     statVal = stats.GetValueFloat(clipData.ValueRO.Stat);
//                 }
//
//                 // 2. Read Intrinsic
//                 if (intrinsicsLookup.TryGetBuffer(target, out var intrinsics))
//                 {
//                     intrinsicVal = intrinsics.GetValue(clipData.ValueRO.Intrinsic);
//                 }
//
//                 // 3. Read Event (Events are transient per-frame in Reaction!)
//                 if (eventsLookup.TryGetBuffer(target, out var events))
//                 {
//                     if (events.AsMap().TryGetValue(clipData.ValueRO.Event, out var evValue))
//                     {
//                         hasEvent = true;
//                         eventVal = evValue;
//                     }
//                 }
//
//                 break; // Just grab the first active one for the UI overlay
//             }
//
//             // Write to the View Model memory
//             ref var data = ref this.uiHelper.Binding;
//             data.IsVisible = isVisible;
//
//             if (isVisible)
//             {
//                 data.StatValue = statVal;
//                 data.IntrinsicValue = intrinsicVal;
//                 data.HasEvent = hasEvent;
//                 data.EventValue = eventVal;
//             }
//         }
//     }
// }