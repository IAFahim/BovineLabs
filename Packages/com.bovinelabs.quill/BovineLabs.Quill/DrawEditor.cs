// <copyright file="DrawEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
#if UNITY_EDITOR
    using UnityEditor;
#endif

#if UNITY_EDITOR
    [Configurable]
#endif
    public static class DrawEditor
    {
#if UNITY_EDITOR
        private const string DrawInPlayMode = "draw.editor-playmode";

        [ConfigVar(DrawInPlayMode, false, "Should the DrawEditor.Update execute while in play mode? Global draw needs to be enabled as well.")]
        private static readonly SharedStatic<bool> EnabledInPlayMode = SharedStatic<bool>.GetOrCreate<Type>();
#endif

        private static bool initialized;

        public static event Action? Update
        {
            add
            {
                if (!initialized)
                {
                    initialized = true;
#if UNITY_EDITOR
                    EditorApplication.update += EditorUpdated;
#endif
                }

                UpdateInternal += value;
            }
            remove => UpdateInternal -= value;
        }

        // stop build warning
#pragma warning disable CS0067
        private static event Action? UpdateInternal;
#pragma warning restore CS0067

#if UNITY_EDITOR
        private static void EditorUpdated()
        {
            if (!FrameUtility.IsNewFrame<Type>())
            {
                return;
            }

            if (EditorApplication.isPlaying && !EnabledInPlayMode.Data)
            {
                return;
            }

            UpdateInternal?.Invoke();
        }
#endif

        private struct Type
        {
        }
    }
}
