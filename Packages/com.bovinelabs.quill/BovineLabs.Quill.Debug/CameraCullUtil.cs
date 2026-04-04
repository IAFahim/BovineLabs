// // <copyright file="CameraCullUtil.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Quill.Debug
// {
//     using BovineLabs.Core.Camera;
//     using Unity.Burst;
//     using Unity.Collections;
//     using Unity.Entities;
//
//     public struct CameraCullUtil
//     {
//         private static readonly SharedStatic<bool> IsSceneEnabled = SharedStatic<bool>.GetOrCreate<DrawSystem.DrawSceneEnabledTagType>();
//
//         private EntityQuery cameraQuery;
// #if UNITY_EDITOR
//         private EntityQuery sceneQuery;
// #endif
//
//         public void OnCreate(ref SystemState state)
//         {
//             var builder = new EntityQueryBuilder(Allocator.Temp);
//             this.cameraQuery = builder.WithAll<CameraFrustumPlanes, CameraMain>().Build(ref state);
//
// #if UNITY_EDITOR
//             builder.Reset();
//             this.sceneQuery = builder.WithAll<CameraFrustumPlanes, CameraScene>().Build(ref state);
// #endif
//         }
//
//         public CameraFrustumPlanes GetFrustumPlanes()
//         {
//             CameraFrustumPlanes cameraFrustum;
//
// #if UNITY_EDITOR
//             if (IsSceneEnabled.Data)
//             {
//                 this.sceneQuery.TryGetSingleton(out cameraFrustum);
//             }
//             else
// #endif
//             {
//                 this.cameraQuery.TryGetSingleton(out cameraFrustum);
//             }
//
//             return cameraFrustum;
//         }
//     }
// }