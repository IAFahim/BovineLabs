# Entity Links

Type-safe entity linking for Unity DOTS using the BovineLabs K System.

## Overview

This package replaces manual `ScriptableObject` references on Authoring components with centralized, type-safe key-based entity linking. Keys are defined once in a Settings file and rendered as dropdown menus in the Inspector via the `[K]` attribute.

## Setup

1. Open **BovineLabs -> Settings** from the Unity menu bar.
2. Under the **Core** group, find **EntityLinkKeys**.
3. Add your named keys (e.g. "Player" = 0, "Weapon" = 1, "Shield" = 2).
4. On your GameObjects, add `LinkComponentAuthoring` or `AutoLinkEntityBufferAuthoring` and select the key from the dropdown.

## Components

### EntityLinkComponent

A single `IComponentData` holding a byte key:

```csharp
public struct EntityLinkComponent : IComponentData
{
    public byte Key;
}
```

### AutoEntityLinkBuffer

A dynamic buffer of keyed entity references:

```csharp
public struct AutoEntityLinkBuffer : IBufferElementData
{
    public byte Key;
    public Entity Value;
}
```

## Authoring

### LinkComponentAuthoring

Adds `EntityLinkComponent` to the baked entity. Uses `[K]` for Inspector dropdown:

```csharp
public class LinkComponentAuthoring : MonoBehaviour
{
    [K(nameof(EntityLinkKeys))]
    public byte Key;
}
```

### AutoLinkEntityBufferAuthoring

Adds an empty `AutoEntityLinkBuffer` to the baked entity. Populate at runtime or via additional systems.

## Burst-Compatible Key Lookup

From any Bursted job or system:

```csharp
byte playerKey = EntityLinkKeys.NameToKey("Player");
```

## Why Not ScriptableObjects?

| Concern           | ScriptableObject approach  | K System approach         |
|-------------------|---------------------------|--------------------------|
| Null references   | Runtime NullRef possible   | Byte default = 0, safe   |
| Asset clutter     | One .asset per key         | Single settings file     |
| Burst compatible  | No (managed objects)       | Yes                      |
| Inspector UX      | Drag-and-drop              | Dropdown menu            |
