// <copyright file="SetupExamplesEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace Scripts.Editor
{
    using Scripts.Authoring.BitArray256Example;
    using Scripts.Authoring.NativeArrayExample;
    using Scripts.Authoring.SlabAllocatorExample;
    using Scripts.Authoring.SpinLockExample;
    using Scripts.Authoring.ThreadRandomExample;
    using UnityEditor;
    using UnityEngine;

    public class SetupExamplesEditor : EditorWindow
    {
        [MenuItem("Setup")]
        public static void SetupAllExamples()
        {
            CreateBitArray256Example();
            CreateThreadRandomExample();
            CreateSlabAllocatorExample();
            CreateSpinLockExample();
            CreateNativeArrayExample();

            Debug.Log("✅ All examples have been set up! Now assign prefabs and press Play.");
        }

        private static void CreateBitArray256Example()
        {
            var go = new GameObject("BitArray256System");
            var authoring = go.AddComponent<BitArray256Authoring>();
            authoring.ToggleInterval = 0.1f;
            authoring.GridSpacing = 1.2f;

            Debug.Log("✅ BitArray256 created. Assign a Cube Prefab to see 256 cubes!");
        }

        private static void CreateThreadRandomExample()
        {
            var go = new GameObject("ThreadRandomSystem");
            var authoring = go.AddComponent<ThreadRandomAuthoring>();
            authoring.SpawnInterval = 0.2f;
            authoring.SpawnRadius = 5f;
            authoring.MaxEntities = 50;

            Debug.Log("✅ ThreadRandom created. Assign a Sphere Prefab!");
        }

        private static void CreateSlabAllocatorExample()
        {
            var go = new GameObject("SlabAllocatorSystem");
            var authoring = go.AddComponent<SlabAllocatorAuthoring>();
            authoring.SlabSize = 8;
            authoring.MaxSlabs = 10;
            authoring.AllocationInterval = 0.15f;

            Debug.Log("✅ SlabAllocator created. Assign a Box Prefab!");
        }

        private static void CreateSpinLockExample()
        {
            var go = new GameObject("SpinLockSystem");
            var authoring = go.AddComponent<SpinLockAuthoring>();
            authoring.WriterCount = 3;
            authoring.ReaderCount = 3;
            authoring.LockInterval = 0.3f;

            Debug.Log("✅ SpinLock created. Assign Red Sphere (Writer) and Blue Sphere (Reader)!");
        }

        private static void CreateNativeArrayExample()
        {
            var go = new GameObject("NativeArraySystem");
            var authoring = go.AddComponent<NativeArrayAuthoring>();
            authoring.ArraySize = 50;
            authoring.UpdateInterval = 0.2f;
            authoring.EntitiesPerRow = 10;

            Debug.Log("✅ NativeArray created. Assign a Capsule Prefab!");
        }
    }
}
