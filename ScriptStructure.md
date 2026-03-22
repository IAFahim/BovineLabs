## File: Scripts.Authoring/AssemblyInfo.cs
```csharp
using System.Runtime.CompilerServices;
using Unity.Entities;

[assembly: DisableAutoTypeRegistration]

[assembly: InternalsVisibleTo("Scripts.Editor")]
[assembly: InternalsVisibleTo("Scripts.Tests")]
```

## File: Scripts.Authoring/Scripts.Authoring.asmdef
```authoring.asmdef
{
    "name": "Scripts.Authoring",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Authoring",
        "BovineLabs.Core.Extensions",
        "BovineLabs.Core.Extensions.Authoring",
        "Scripts.Data",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_EDITOR"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## File: Scripts.Data/AssemblyInfo.cs
```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts")]
[assembly: InternalsVisibleTo("Scripts.Authoring")]
[assembly: InternalsVisibleTo("Scripts.Debug")]
[assembly: InternalsVisibleTo("Scripts.Editor")]
[assembly: InternalsVisibleTo("Scripts.Tests")]
```

## File: Scripts.Data/Scripts.Data.asmdef
```data.asmdef
{
    "name": "Scripts.Data",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Extensions",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## File: Scripts.Debug/Scripts.Debug.asmdef
```debug.asmdef
{
    "name": "Scripts.Debug",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Editor",
        "Scripts",
        "Scripts.Data",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Mathematics",
        "Unity.Transforms",
        "Unity.UIElements",
        "Unity.UIElements.Toolkit"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}

```

## File: Scripts.Editor/Scripts.Editor.asmdef
```editor.asmdef
{
    "name": "Scripts.Editor",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Editor",
        "BovineLabs.Core.Extensions",
        "BovineLabs.Core.Extensions.Editor",
        "Scripts",
        "Scripts.Authoring",
        "Scripts.Data",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms",
        "Unity.UIElements",
        "Unity.UIElements.Toolkit"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## File: Scripts.Editor/SetupExamplesEditor.cs
```csharp
namespace Scripts.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SetupExamplesEditor : EditorWindow
    {
        [Serializable]
        public struct SetupConfig
        {
            public string gameObjectName;

            /// <summary>
            /// MonoScript assets to AddComponent to the created GameObject.
            /// Resolved to concrete Types via MonoScript.GetClass().
            /// </summary>
            public MonoScript[] scripts;
        }

        [SerializeField] private List<SetupConfig> configs = new();

        private const string UIGameObjectName = "UI";
        private const string PanelSettingsFilter = "t:PanelSettings";
        private const string SubSceneName = "Sub Scene";

        [MenuItem("Setup/Examples")]
        public static void ShowWindow()
        {
            var window = GetWindow<SetupExamplesEditor>("Setup Examples");
            window.minSize = new Vector2(360, 300);
            window.Show();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 8;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingBottom = 8;

            var so = new SerializedObject(this);

            var header = new Label("Example Setup Configs")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginBottom = 6,
                },
            };
            root.Add(header);

            var hint = new HelpBox(
                "Add a row per example. Drag MonoScript assets into the Scripts list.\n" +
                "Click Setup Scene to create GameObjects inside the Sub Scene.",
                HelpBoxMessageType.Info);
            root.Add(hint);

            var configsField = new PropertyField(so.FindProperty("configs"), "Configs");
            configsField.style.marginTop = 6;
            configsField.Bind(so);
            root.Add(configsField);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            root.Add(spacer);

            var setupBtn = new Button(OnSetupClicked)
            {
                text = "▶  Setup Scene",
                style =
                {
                    height = 32,
                    marginTop = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                },
            };
            root.Add(setupBtn);

            var clearBtn = new Button(OnClearClicked)
            {
                text = "✕  Clear Generated Objects",
                style = { height = 26, marginTop = 4 },
            };
            root.Add(clearBtn);
        }

        private void OnSetupClicked()
        {
            if (configs == null || configs.Count == 0)
            {
                EditorUtility.DisplayDialog("Setup Examples", "No configs defined. Add at least one entry.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Setup ECS Examples");
            var group = Undo.GetCurrentGroup();

            var uiDoc = EnsureUIDocument();
            var subScene = EnsureSubSceneContainer();

            foreach (var config in configs)
            {
                if (string.IsNullOrWhiteSpace(config.gameObjectName))
                {
                    Debug.LogWarning("[SetupExamples] Skipping config with empty gameObjectName.");
                    continue;
                }

                CreateConfiguredGameObject(config, subScene.transform);
            }

            Undo.CollapseUndoOperations(group);
            EditorUtility.SetDirty(uiDoc.gameObject);

            Debug.Log($"[SetupExamples] ✅ Created {configs.Count} example(s). Assign prefabs and press Play.");
        }

        private void OnClearClicked()
        {
            if (!EditorUtility.DisplayDialog(
                    "Clear Generated Objects",
                    "This will destroy all GameObjects inside Sub Scene that match config names. Continue?",
                    "Yes", "Cancel"))
            {
                return;
            }

            Undo.SetCurrentGroupName("Clear ECS Examples");
            var group = Undo.GetCurrentGroup();

            var subScene = GameObject.Find(SubSceneName);
            if (subScene != null)
            {
                foreach (var config in configs)
                {
                    var child = subScene.transform.Find(config.gameObjectName);
                    if (child != null)
                    {
                        Undo.DestroyObjectImmediate(child.gameObject);
                    }
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        /// <summary>
        /// Finds or creates the UI GameObject at scene root and ensures it has
        /// a UIDocument with a PanelSettings asset attached.
        /// </summary>
        private static UIDocument EnsureUIDocument()
        {
            var uiGo = GameObject.Find(UIGameObjectName);
            if (uiGo == null)
            {
                uiGo = new GameObject(UIGameObjectName);
                Undo.RegisterCreatedObjectUndo(uiGo, "Create UI GameObject");
            }

            var uiDoc = uiGo.GetComponent<UIDocument>();
            if (uiDoc == null)
            {
                uiDoc = Undo.AddComponent<UIDocument>(uiGo);
            }

            if (uiDoc.panelSettings == null)
            {
                var guids = AssetDatabase.FindAssets(PanelSettingsFilter);
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    uiDoc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                    Debug.Log($"[SetupExamples] Auto-assigned PanelSettings from: {path}");
                }
                else
                {
                    Debug.LogWarning("[SetupExamples] No PanelSettings asset found. Create one at Assets/Settings/PanelSettings.asset.");
                }
            }

            return uiDoc;
        }

        /// <summary>
        /// Finds or creates the Sub Scene container GameObject.
        /// </summary>
        private static GameObject EnsureSubSceneContainer()
        {
            var subScene = GameObject.Find(SubSceneName);
            if (subScene == null)
            {
                subScene = new GameObject(SubSceneName);
                Undo.RegisterCreatedObjectUndo(subScene, "Create Sub Scene");
            }

            return subScene;
        }

        /// <summary>
        /// Creates a child GameObject with the given config's scripts added as components.
        /// Skips creation if a child with the same name already exists.
        /// </summary>
        private static void CreateConfiguredGameObject(SetupConfig config, Transform parent)
        {
            var existing = parent.Find(config.gameObjectName);
            if (existing != null)
            {
                Debug.LogWarning($"[SetupExamples] '{config.gameObjectName}' already exists. Skipping.");
                return;
            }

            var go = new GameObject(config.gameObjectName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {config.gameObjectName}");
            go.transform.SetParent(parent, false);

            if (config.scripts == null)
            {
                return;
            }

            foreach (var monoScript in config.scripts)
            {
                if (monoScript == null)
                {
                    continue;
                }

                var type = monoScript.GetClass();
                if (type == null)
                {
                    Debug.LogWarning($"[SetupExamples] Could not resolve type for script '{monoScript.name}'. Skipping.");
                    continue;
                }

                if (!typeof(Component).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"[SetupExamples] '{type.Name}' is not a Component. Skipping.");
                    continue;
                }

                Undo.AddComponent(go, type);
            }

            Debug.Log($"[SetupExamples] Created '{config.gameObjectName}' with {config.scripts.Length} script(s).");
        }
    }
}
```

## File: Scripts.Tests/AssemblyInfo.cs
```csharp
using Unity.Entities;

[assembly: DisableAutoCreation]
```

## File: Scripts.Tests/Scripts.Tests.asmdef
```tests.asmdef
{
    "name": "Scripts.Tests",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Core",
        "BovineLabs.Core.Extensions",
        "BovineLabs.Testing",
        "Scripts",
        "Scripts.Data",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.PerformanceTesting",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [
        "TestAssemblies"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## File: Scripts/AssemblyInfo.cs
```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Debug")]
[assembly: InternalsVisibleTo("Scripts.Editor")]
[assembly: InternalsVisibleTo("Scripts.Tests")]
```

## File: Scripts/Scripts.asmdef
```asmdef
{
    "name": "Scripts",
    "rootNamespace": "",
    "references": [
        "BovineLabs.Anchor",
        "BovineLabs.Core",
        "BovineLabs.Core.Extensions",
        "Scripts.Data",
        "Unity.AppUI",
        "Unity.AppUI.MVVM",
        "Unity.AppUI.Navigation",
        "Unity.Burst",
        "Unity.Collections",
        "Unity.Entities",
        "Unity.Entities.Graphics",
        "Unity.Entities.Hybrid",
        "Unity.InputSystem",
        "Unity.Mathematics",
        "Unity.Mathematics.Extensions",
        "Unity.Physics",
        "Unity.Transforms"
    ],
    "optionalUnityReferences": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

_Project Structure:_
```text
Scripts.Authoring/AssemblyInfo.cs
Scripts.Authoring/Scripts.Authoring.asmdef
Scripts.Data/AssemblyInfo.cs
Scripts.Data/Scripts.Data.asmdef
Scripts.Debug/Scripts.Debug.asmdef
Scripts.Editor/Scripts.Editor.asmdef
Scripts.Editor/SetupExamplesEditor.cs
Scripts.Tests/AssemblyInfo.cs
Scripts.Tests/Scripts.Tests.asmdef
Scripts/AssemblyInfo.cs
Scripts/Scripts.asmdef
```
