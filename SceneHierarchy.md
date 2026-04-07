# Scene Hierarchy

```
Scene (Root)
├── Camera
├── UI [GameObject]
│   └── UIDocument [Component: UnityEngine.UIElements.UIDocument]
│       ├── Panel Settings → PanelSettings asset (Assets/Settings/PanelSettings.asset)
│       └── Source Asset   → assigned at runtime by SetupExamplesEditor
│
└── Sub Scene [GameObject]
    └── Sub Scene [SubScene Component] ← ECS authoring lives here
```
