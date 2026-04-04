// <copyright file="FrameUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;
    using UnityEngine.Assertions;

    /// <summary>
    /// Utility to ensure only updating once per frame. This is useful for fixed update or EditorApplication.update can trigger dozens of times per update,
    /// this util helps track that and avoid drawing multiple times. Can only be used from main thread.
    /// </summary>
    public static class FrameUtility
    {
        private static readonly SharedStatic<UnsafeHashMap<long, int>> LastUpdates = SharedStatic<UnsafeHashMap<long, int>>.GetOrCreate<FrameType>();

        /// <summary> Has the frame changed since the last time the type called this method. </summary>
        /// <typeparam name="T"> Type used for tracking. </typeparam>
        /// <returns> True if new frame else false. </returns>
        public static bool IsNewFrame<T>()
        {
#if UNITY_EDITOR
            Assert.IsTrue(UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread(), "Can only be called from main thread");
#endif

            var id = BurstRuntime.GetHashCode64<T>();

            if (!LastUpdates.Data.TryGetValue(id, out var lastFrame))
            {
                lastFrame = -1;
            }

            if (Time.frameCount != lastFrame)
            {
                LastUpdates.Data[id] = Time.frameCount;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (LastUpdates.Data.IsCreated)
            {
                return;
            }

            LastUpdates.Data = new UnsafeHashMap<long, int>(0, Allocator.Domain);
        }

        private struct FrameType
        {
        }
    }
}
