# BovineLabs Timeline – Full Code Audit TODO

> Every item below is self-contained. Each entry states the **file**, the **exact problem**, and the **exact fix**.
> Severity: 🔴 Critical | 🟠 Bug | 🟡 Inconsistency/Missing | 🔵 Dead Code | ⚪ Polish

---

## 🔴 CRITICAL — Will cause crashes or silent incorrect behavior

---

### [C-01] [DONE] `UnsafeComponentLookup` fields never updated in 5 track systems
**Files:**
- `PhysicsLinearPIDTrackSystem.cs`
- `PhysicsAngularPIDTrackSystem.cs`
- `PhysicsDragTrackSystem.cs`
- `PhysicsForceTrackSystem.cs`
- `PhysicsVelocityTrackSystem.cs`

**Problem:**
Each system caches an `UnsafeComponentLookup<T>` field in `OnCreate` but never calls `.Update(ref state)` in `OnUpdate`. In DOTS, every cached lookup must be updated each frame to refresh its internal version number. Using a stale lookup can cause safety violations, incorrect reads/writes, or crashes when entity archetypes change.

**Exact evidence – `PhysicsLinearPIDTrackSystem.cs`:**
```csharp
// OnCreate
_activePidLookup = state.GetUnsafeComponentLookup<ActiveLinearPid>();
_stateLookup     = state.GetUnsafeComponentLookup<PhysicsLinearPIDState>();

// OnUpdate — _stateLookup is updated, _activePidLookup is NOT
_stateLookup.Update(ref state);  // ✅ correct
// _activePidLookup.Update(ref state);  ← MISSING
```

**Fix for each system:**

`PhysicsLinearPIDTrackSystem.OnUpdate` — add at the top:
```csharp
_activePidLookup.Update(ref state);
```

`PhysicsAngularPIDTrackSystem.OnUpdate` — add at the top (after existing `_stateLookup.Update`):
```csharp
_activePidLookup.Update(ref state);
```

`PhysicsDragTrackSystem.OnUpdate` — add at the top:
```csharp
_activeLookup.Update(ref state);
```

`PhysicsForceTrackSystem.OnUpdate` — add at the top:
```csharp
_activeLookup.Update(ref state);
```

`PhysicsVelocityTrackSystem.OnUpdate` — add at the top:
```csharp
_activeLookup.Update(ref state);
```

---

### [C-02] [DONE] `InputTransducerSystem` requires `InputConsumerRoute` which is never added to any entity
**Files:**
- `InputTransducerSystem.cs` (runtime)
- `InputConsumerBuilder.cs` (authoring)
- `InputConsumerAuthoring.cs` (authoring)

**Problem:**
`TransduceJob` in `InputTransducerSystem` has `in InputConsumerRoute route` as a parameter. ECS source generation therefore requires every matched entity to have the `InputConsumerRoute` component. However, `InputConsumerBuilder.ApplyTo` never adds this component, meaning **the TransduceJob will never match any entity** and the entire input transduction system is silently dead.

**Evidence:**
```csharp
// InputConsumerBuilder.ApplyTo — InputConsumerRoute is NOT added
builder.AddComponent(new PlayerId { Value = PlayerId });
builder.AddComponent<InputConsumerTag>();
builder.AddComponent(new InputSource { Provider = Entity.Null });
builder.AddComponent(new PlayerMoveInput());
// Missing: builder.AddComponent(new InputConsumerRoute { Target = ... });
```

```csharp
// TransduceJob requires it:
private void Execute(in InputSource source, in InputConsumerRoute route) // ← never matched
```

**Fix — `InputConsumerAuthoring.cs` Baker:**
Decide on the route target. If it should default to the entity itself:
```csharp
// In Baker.Bake, after building via InputConsumerBuilder:
AddComponent(entity, new InputConsumerRoute { Target = entity });
```

**Fix — `InputConsumerBuilder.ApplyTo`:**
```csharp
public void ApplyTo<T>(ref T builder) where T : struct, IEntityCommands
{
    builder.AddComponent(new PlayerId { Value = PlayerId });
    builder.AddComponent<InputConsumerTag>();
    builder.AddComponent(new InputSource { Provider = Entity.Null });
    builder.AddComponent(new PlayerMoveInput());
    builder.AddComponent(new InputConsumerRoute { Target = Entity.Null }); // ← ADD THIS
}
```
The `Target` should be patched at runtime (already done by `PlayerInputSubscriptionSystem` pattern) or set during baking.

---

### [C-03] [DONE] `routeEventsTo` field in `InputConsumerAuthoring` is declared but completely ignored in baking
**File:** `InputConsumerAuthoring.cs`

**Problem:**
```csharp
[Tooltip("Where to route transduced hardware input events. Defaults to self.")]
public EntityLinkSchema routeEventsTo;
```
The baker captures `var targetEntity = entity` but never uses `routeEventsTo` to resolve anything. This field serves no purpose and the tooltip is misleading.

**Fix — Option A (implement it):**
```csharp
public override void Bake(InputConsumerAuthoring authoring)
{
    var entity = GetEntity(TransformUsageFlags.None);
    
    Entity routeTarget = entity;
    if (authoring.routeEventsTo != null)
    {
        // resolve via EntityLink or direct binding
        // routeTarget = GetEntity(authoring.routeEventsTo, TransformUsageFlags.None);
    }

    var commands = new BakerCommands(this, entity);
    var builder = new InputConsumerBuilder().WithPlayerId(authoring.PlayerId);
    builder.ApplyTo(ref commands);

    AddComponent(entity, new InputConsumerRoute { Target = routeTarget }); // ← use resolved target
}
```

**Fix — Option B (remove dead field):**
Delete `public EntityLinkSchema routeEventsTo;` and the `[Tooltip]` above it.

---

## 🟠 BUGS — Incorrect behavior, no crash

---

### [B-01] [DONE] `PhysicsTimelineBakingSystem` uses inconsistent guard conditions
**File:** `PhysicsTimelineBakingSystem.cs`

**Problem:**
The guard condition for each block checks a *different* component than what is actually added. This means if one of the checked components exists from another source, the sibling component is silently not added — creating an entity with a missing component that the runtime system requires.

| Block | Guards with `HasComponent<>` | Actually adds |
|---|---|---|
| Linear PID | `PhysicsLinearPIDState` | `ActiveLinearPid` + `PhysicsLinearPIDState` |
| Angular PID | `PhysicsAngularPIDState` | `ActiveAngularPid` + `PhysicsAngularPIDState` |
| Force | `ActiveForce` | `ActiveForce` + `PhysicsForceState` |
| Velocity | `ActiveVelocity` | `ActiveVelocity` + `PhysicsVelocityState` |
| Drag | `ActiveDrag` | `ActiveDrag` only |

For Linear and Angular PID, the guard checks the *state* component, not the *active enableable* component. If `PhysicsLinearPIDState` somehow already exists, `ActiveLinearPid` will never be added.

**Fix:** Make all guards check the same component that is the primary enableable marker:
```csharp
// Linear PID — change guard from PhysicsLinearPIDState to ActiveLinearPid
if (target != Entity.Null && !em.HasComponent<ActiveLinearPid>(target))
{
    ecb.AddComponent<ActiveLinearPid>(target);
    ecb.SetComponentEnabled<ActiveLinearPid>(target, false);
    ecb.AddComponent<PhysicsLinearPIDState>(target);
}

// Angular PID — same fix
if (target != Entity.Null && !em.HasComponent<ActiveAngularPid>(target))
{
    ecb.AddComponent<ActiveAngularPid>(target);
    ecb.SetComponentEnabled<ActiveAngularPid>(target, false);
    ecb.AddComponent<PhysicsAngularPIDState>(target);
}
```
Force, Velocity, Drag blocks already use the correct component as their guard — no change needed there.

---

### [B-02] [DONE] `PhysicsTriggerInstantiateSystem` uses `AddNoResize` on a pooled list with unbounded input
**File:** `PhysicsTriggerInstantiateSystem.cs`

**Problem:**
```csharp
using var spawnsPool = PooledNativeList<SpawnData>.Make();
var spawns = spawnsPool.List;
// ...
spawns.AddNoResize(new SpawnData { ... }); // ← throws if capacity exceeded
```
`PooledNativeList<T>.Make()` returns a list with a fixed internal capacity. `AddNoResize` throws `InvalidOperationException` if more spawns are requested than that capacity. In scenes with many simultaneous trigger events this will crash in-editor and in builds.

**Fix:** Replace `AddNoResize` with `Add` throughout `InstantiateGatherJob.Execute`:
```csharp
// Change all occurrences:
spawns.Add(new SpawnData { ... }); // ← use Add, not AddNoResize
```
If `PooledNativeList` does not support resizing, replace with a local `NativeList<SpawnData>` with a generous initial capacity:
```csharp
var spawns = new NativeList<SpawnData>(32, Allocator.Temp);
// ... use spawns.Add(...)
spawns.Dispose(); // at end of Execute
```

---

### [B-03] [DONE] `NeighborDebugSystem.OnUpdate` early-returns before `transformLookup.Update`
**File:** `NeighborDebugSystem.cs`

**Problem:**
```csharp
public void OnUpdate(ref SystemState state)
{
    return; // early return for "intentional" disable
    this.transformLookup.Update(ref state); // ← UNREACHABLE — never called
    // ... rest of system
}
```
The `transformLookup.Update` call is dead code. While the system is disabled via the early return, when this system is re-enabled (by removing the `return`), `transformLookup` will be stale from the frame it was last updated in `OnCreate`. This will cause safety errors on first real execution.

Also, the comment has a typo: `"intnetoally retuned"` should be `"intentionally returned"`.

**Fix:**
```csharp
public void OnUpdate(ref SystemState state)
{
    // Intentionally disabled for now
    return;
}
```
Move `transformLookup.Update(ref state)` to just BEFORE the early return, or restructure:
```csharp
public void OnUpdate(ref SystemState state)
{
    this.transformLookup.Update(ref state); // always update
    return; // disabled for now — remove this line to re-enable
    // ...
}
```

---

### [B-04] [DONE] `WorldTimeScaleSystem.OnUpdate` missing `[BurstCompile]`
**File:** `WorldTimeScaleSystem.cs`

**Problem:**
```csharp
// OnCreate has no [BurstCompile] either — but that's acceptable
[BurstCompile]
public void OnUpdate(ref SystemState state) // ← [BurstCompile] IS present on the method
```
Wait — actually re-checking: the system IS an `ISystem partial struct` but the `OnUpdate` does NOT have `[BurstCompile]`. The method uses `SystemAPI.Query` with `ref` accessors which *can* Burst-compile. Without it, this runs on the managed thread with JIT.

**Fix:**
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // existing body unchanged
}
```

---

### [B-05] [DONE] `HitStopSystem.TriggerJob` has an unguarded parallel write race condition
**File:** `HitStopSystem.cs`

**Problem:**
```csharp
[NativeDisableParallelForRestriction] public ComponentLookup<HitStopState> States;
// ...
States.SetComponentEnabled(target, true);
States[target] = newState; // ← direct write in parallel job
```
If two different `HitStopConfig` entities both fire `OnHit` and both resolve to the *same* target entity in the same frame, `States[target] = newState` will be called concurrently from different threads. `NativeDisableParallelForRestriction` suppresses the safety check but does **not** prevent the race. The last writer wins non-deterministically.

**Fix:** Route this through an `EntityCommandBuffer.ParallelWriter` for the write-back, using `ECB.SetComponent` and `ECB.SetComponentEnabled`. The `ECB` is already available in `TriggerJob`:
```csharp
// Instead of:
States.SetComponentEnabled(target, true);
States[target] = newState;
// Use:
ECB.SetComponentEnabled<HitStopState>(sortKey, target, true);
ECB.SetComponent(sortKey, target, newState);
```
Remove `States` from `TriggerJob` entirely since it's only used for this write. (The ECB already handles the non-HasComponent branch with `AddComponent`.)

---

## 🟡 INCONSISTENCIES AND MISSING PATTERNS

---

### [I-01] [DONE] `BlendTree2DTrack.OnValidate` does not call `EditorUtility.SetDirty`
**File:** `BlendTree2DTrack.cs`

**Problem:**
```csharp
private void OnValidate()
{
    foreach (var motion in Motions) motion.CalcDirection();
    // directionCalc is a serialized public field — mutation in OnValidate
    // is not marked dirty, so it may not save to disk reliably
}
```

**Fix:**
```csharp
private void OnValidate()
{
    var changed = false;
    foreach (var motion in Motions)
    {
        var prev = motion.directionCalc;
        motion.CalcDirection();
        if (prev != motion.directionCalc) changed = true;
    }
#if UNITY_EDITOR
    if (changed) UnityEditor.EditorUtility.SetDirty(this);
#endif
}
```

---

### [I-02] [DONE] `TimelineTimeScaleApplySystem` reads `HitStopState` without checking enabled state
**File:** `TimelineTimeScaleApplySystem.cs`

**Problem:**
```csharp
if (HitStops.TryGetComponent(entity, out var hitStop) && hitStop.RemainingTime > 0f)
```
`ComponentLookup.TryGetComponent` does NOT check whether an `IEnableableComponent` is enabled — it returns `true` and the component data if the component exists at all, regardless of enabled state. The `RemainingTime > 0f` check is an accidental safeguard (since `RemainingTime` becomes negative when the hit-stop ends), but this is fragile and relies on an undocumented invariant.

**Fix:**
```csharp
if (HitStops.HasComponent(entity) && 
    HitStops.IsComponentEnabled(entity) && 
    HitStops[entity].RemainingTime > 0f)
{
    timeScale = 0.0001f;
}
```
This makes the intent explicit and is robust against any future change to how `RemainingTime` is reset.

---

### [I-03] [DONE] `PhysicsMath` helper methods always return `bool` but always return `true`
**File:** `PhysicsMath.cs`

**Problem:**
```csharp
public static bool TryApplyLinearForce(...) { ... return true; }
public static bool TryApplyAngularTorque(...) { ... return true; }
public static bool TryComputeExponentialDecay(...) { ... return true; }
public static bool TryResolveSpaceVector(...) { ... return true; }
// All call sites use: if (TryApply...) { ... }
```
Every `Try*` method unconditionally returns `true`. This is misleading — callers wrap the call in `if (Try...)` checks that can never be false, adding noise. Either make them `void` or add actual failure conditions.

**Fix — Option A (simplest, make them void):**
```csharp
public static void ApplyLinearForce(in PhysicsVelocity velocityIn, in PhysicsMass mass, float3 force, float deltaTime, out PhysicsVelocity velocityOut)
public static void ApplyAngularTorque(in PhysicsVelocity velocityIn, in PhysicsMass mass, in LocalTransform transform, float3 torque, float deltaTime, out PhysicsVelocity velocityOut)
// etc.
```
Update all call sites to remove the `if (Try...)` wrapper.

**Fix — Option B (add real failure cases):**
Return `false` from `TryApplyLinearForce` when `deltaTime <= 0f` or mass is zero, etc. This makes the `Try*` naming meaningful.

---

### [I-04] [DONE] `TimelineTimeScaleClip` overrides `TrackBinding` without clear documentation
**File:** `TimelineTimeScaleClip.cs`

**Problem:**
```csharp
public override void Bake(Entity clipEntity, BakingContext context)
{
    context.Baker.SetComponent(clipEntity, new TrackBinding { Value = context.Timer }); // ← overrides default binding
    // ...
}
```
The base `DOTSClip.Bake` sets `TrackBinding.Value = context.Target`. This `SetComponent` call silently overrides it. There is no comment explaining why the timer entity is used instead of the bound target. Future maintainers may not realize this is intentional.

**Fix:** Add a comment:
```csharp
// Time scale clips target the timeline's clock entity (context.Timer), not the bound
// StatAuthoring entity — the multiplier is applied per-timeline, not per-bound-entity.
context.Baker.SetComponent(clipEntity, new TrackBinding { Value = context.Timer });
```

---

### [I-05] [DONE] `ActionTickDistributionSystem.ApplyIntrinsicJob` deduplication is O(n²)
**File:** `ActionTickDistributionSystem.cs`

**Problem:**
```csharp
var values = new NativeList<IntrinsicAmount>(Allocator.Temp);
// ...
var existingIndex = values.IndexOf(value); // ← linear scan every iteration
if (Hint.Unlikely(existingIndex == -1)) values.Add(value);
else values.ElementAt(existingIndex).Amount += value.Amount;
```
`NativeList.IndexOf` scans linearly. For an entity that receives many tick events in one frame (e.g. high TPS + large delta), this is O(n²) over the deduplicated intrinsic keys.

**Fix:** Use a `NativeHashMap<IntrinsicKey, int>` as an index:
```csharp
var valueMap = new NativeHashMap<IntrinsicKey, int>(8, Allocator.Temp);
var values   = new NativeList<IntrinsicAmount>(8, Allocator.Temp);

while (GroupChanges.TryGetNextValue(out value, ref it))
{
    if (valueMap.TryGetValue(value.Intrinsic, out var idx))
        values.ElementAt(idx).Amount += value.Amount;
    else
    {
        valueMap.Add(value.Intrinsic, values.Length);
        values.Add(value);
    }
}
```
Apply the same pattern to `ApplyEventJob`.

---

### [I-06] [DONE] `EntityLinkRootAuthoring` baker loops call `DependsOn` inside nested loops — excessive tracking
**File:** `EntityLinkRootAuthoring.cs`

**Problem:**
```csharp
foreach (var source in sources)
{
    DependsOn(source);          // O(sources × schemas)
    foreach (var schema in schemas)
        DependsOn(schema);
}
```
Excessive `DependsOn` calls slow down incremental baking. Schemas are small ScriptableObjects that rarely change; tracking every one individually on every source causes unnecessary cache invalidation.

**Fix:** Call `DependsOn(schema)` once per unique schema, not once per source×schema:
```csharp
var seenSchemas = new HashSet<EntityLinkSchema>();
foreach (var source in sources)
{
    DependsOn(source);
    schemas.Clear();
    source.AddSchemas(schemas);
    foreach (var schema in schemas)
    {
        if (seenSchemas.Add(schema)) DependsOn(schema);
        // ...
    }
}
```

---

### [I-07] [DONE] `ConditionKeyDrawer` cache is invalidated on every property change
**File:** `ConditionKeyDrawer.cs`

**Problem:**
```csharp
if (!ReferenceEquals(next, current) && valueProp != null)
{
    valueProp.intValue = next != null ? next.Key : 0;
    s_Cache = null; // ← invalidate entire cache on every edit
}
```
Setting `s_Cache = null` forces a full `AssetDatabase.FindAssets` scan on the next `OnGUI` call. In a prefab with many condition key fields, each change causes N×FindAssets calls.

**Fix:** Only invalidate when the specific key is being changed, or use `AssetPostprocessor` to invalidate on import instead of on every draw:
```csharp
// Remove the cache-null line — stale entries resolve to the correct SO via key int lookup.
// Cache misses are already handled by BuildCache().
valueProp.intValue = next != null ? next.Key : 0;
// s_Cache = null; ← REMOVE
```
Add invalidation only on asset import via:
```csharp
// In OnPostprocessAllAssets when a ConditionEventObject is imported:
s_Cache = null;
```

---

### [I-08] [DONE] `SmearVelocityAuthoring` uses `TransformUsageFlags.Dynamic` unnecessarily
**File:** `SmearVelocityAuthoring.cs`

**Problem:**
```csharp
var entity = GetEntity(TransformUsageFlags.Dynamic);
```
`SmearVelocity` is a material property only — it reads from `PhysicsVelocity` at runtime. The entity does not need full dynamic transform tracking (`LocalTransform` + `LocalToWorld` + transform hierarchy update). `Dynamic` adds unnecessary components to the archetype.

**Fix:**
```csharp
var entity = GetEntity(TransformUsageFlags.None);
```
If rendering requires `LocalToWorld` for HDRP/URP material properties, use `TransformUsageFlags.Renderable` instead.

---

## 🔵 DEAD CODE — Can be safely removed

---

### [D-01] [DONE] `InputCancelWindow` (IEnableableComponent) is defined but never used
**File:** `InputCancelWindow.cs`

**Problem:**
```csharp
public struct InputCancelWindow : IComponentData, IEnableableComponent
{
    public BitArray256 AllowedMask;
}
```
The runtime system `InputCancelWindowSystem` uses `InputCancelWindowConfig` (from `TimelineInputComponents.cs`), not `InputCancelWindow`. This struct is never added to any entity and is never queried. It is pure dead code.

**Fix:** Delete `InputCancelWindow.cs` entirely, or if it was intended to replace `InputCancelWindowConfig`, migrate the system to use it and delete `InputCancelWindowConfig`.

---

### [D-02] [DONE] `InputButtonDownBuffer`, `InputButtonHeldBuffer`, `InputButtonUpBuffer` are defined but never used
**File:** `InputBuffered.cs`

**Problem:**
```csharp
public struct InputButtonDownBuffer : IBufferElementData { public byte ActionId; }
public struct InputButtonHeldBuffer : IBufferElementData { public byte ActionId; }
public struct InputButtonUpBuffer : IBufferElementData   { public byte ActionId; }
```
These three buffer types are never added to any entity and never queried in any system. All input state is handled via `InputState` (BitArray256 fields) and `InputAxisBuffer`.

**Fix:** Delete these three structs from `InputBuffered.cs`. Keep `InputAxisBuffer` — it IS used.

---

### [D-03] [DONE] `SchemaIconPostprocessor` static constructor has a leftover development comment
**File:** `SchemaIconPostprocessor.cs`

**Problem:**
```csharp
static SchemaIconPostprocessor() // <-- add this block
{
```
The `// <-- add this block` comment is a development note that was never removed.

**Fix:** Delete the comment:
```csharp
static SchemaIconPostprocessor()
{
```

---

### [D-04] [DONE] `NeighborDebugSystem.OnUpdate` body is entirely unreachable
**File:** `NeighborDebugSystem.cs`

**Problem:**
The entire `OnUpdate` body after `return;` — 70+ lines — is unreachable code that still adds noise and confusion. The `transformLookup.Update` call is also dead (see [B-03]).

**Fix:** Either delete the body entirely (keeping only the early return with a comment), or move it behind a `#if BL_SPATIAL_DEBUG` conditional compile flag:
```csharp
[BurstCompile]
public void OnUpdate(ref SystemState state)
{
    // Intentionally disabled — spatial grid debug rendering is in progress.
    return;
}
```

---

### [D-05] [DONE] `WorldTimeScaleSystem` is not used by any track system but `WorldTimeScaleAnimated` has no animator
**File:** `WorldTimeScaleSystem.cs`

**Problem:**
`WorldTimeScaleSystem` reads `WorldTimeScaleAnimated` from clips and blends it into `WorldTimeScale`. However, there is no `TrackBlendImpl` or animation channel integration — `WorldTimeScaleAnimated.Value` is set only in `WorldTimeScaleClip.Bake` as a constant. There is no `IAnimatedComponent` or `[CreateProperty]` on `WorldTimeScaleAnimated`, meaning it will never receive blended values from the timeline animation system.

**Fix — Option A (make it a proper animated component like the others):**
```csharp
// In WorldTimeScaleAnimated.cs:
public struct WorldTimeScaleAnimated : IAnimatedComponent<float>
{
    public float AuthoredData;
    [CreateProperty] public float Value { get; set; }
}
```
Then the `WorldTimeScaleSystem` should use the `.Value` property for the blend result.

**Fix — Option B (it's intentionally not blended):**
Document explicitly why `WorldTimeScaleAnimated` is not an `IAnimatedComponent` — it reads `Value` directly from the authored constant and relies on `ClipWeight` for blending in `WorldTimeScaleSystem.OnUpdate`. Add a comment to both files explaining this.

---

## ⚪ POLISH / NAMING

---

### [P-01] [DONE] `MuliInputSettings` is a typo — should be `MultiInputSettings`
**Files:** `MuliInputSettings.cs` and all references

**Problem:** The class is named `MuliInputSettings` (missing the 't'). This propagates to every method that references it: `MuliInputSettings.GetIndex(...)`, `MuliInputSettings.KeyToName(...)`, etc.

**Fix:** Rename the class and file. Since this is a `SettingsGroup` ScriptableObject, check whether the `[SettingsGroup("Input")]` attribute serializes the type name — if so, existing saved settings may need migration. Scope the rename carefully. A safe approach is to add a `[Obsolete]` alias:
```csharp
[Obsolete("Use MultiInputSettings")]
public class MuliInputSettings : MultiInputSettings { }
```

---

### [P-02] [DONE] `BlendTree2DTrack.OnValidate` — `CalcDirection` result is not shown as read-only in Inspector
**File:** `BlendTree2DTrack.cs`

**Problem:**
`directionCalc` is a public serialized field intended to be read-only (auto-computed from `degreeCalc` and `rangeCalc`). It's exposed in the inspector as editable, which allows users to override the computed value — but it will be overwritten on the next `OnValidate` call.

**Fix:** Add `[HideInInspector]` or use `[ReadOnly]` from BovineLabs' `InspectorReadOnly` attribute (already used in `EntityLinkSchema.cs`):
```csharp
[Tooltip("Computed direction vector (auto-calculated from degree and range).")]
[BovineLabs.Core.PropertyDrawers.InspectorReadOnly]
public Vector2 directionCalc;
```

---

### [P-03] [DONE] `ActionTickDistributionSystem.GetKeysJob` and `GetEventKeysJob` are near-identical structs
**File:** `ActionTickDistributionSystem.cs`

**Problem:**
```csharp
private struct GetKeysJob : IJob
{
    public NativeList<Entity> UniqueKeys;
    [ReadOnly] public NativeParallelMultiHashMap<Entity, IntrinsicAmount>.ReadOnly GroupChanges;
    public void Execute() { GroupChanges.GetUniqueKeyArray(UniqueKeys); }
}

private struct GetEventKeysJob : IJob
{
    public NativeList<Entity> UniqueKeys;
    [ReadOnly] public NativeParallelMultiHashMap<Entity, EventAmount>.ReadOnly GroupChanges;
    public void Execute() { GroupChanges.GetUniqueKeyArray(UniqueKeys); }
}
```
These are identical in structure — only the value type differs. This is duplication.

**Fix:** If the `GetUniqueKeyArray` extension method is generic, merge into one generic job. If not, at minimum add a comment noting these are intentionally parallel structures.

---

### [P-04] [DONE] `EntityLinkResolver.TryResolve` has two overloads that can easily be confused
**File:** `EntityLinkResolver.cs`

**Problem:**
```csharp
// Overload A — takes root entity directly
public static bool TryResolve(Entity root, ushort key, in BufferLookup<EntityLink> links, out Entity result)

// Overload B — takes any entity and resolves root internally
public static bool TryResolve(Entity entity, ushort key, in ComponentLookup<EntityLinkSource> sources, in BufferLookup<EntityLink> links, out Entity result)
```
Passing a non-root entity to Overload A silently fails to find the link (returns false or finds nothing). There is no hint in the name to distinguish "expects a root" vs "will resolve root for you."

**Fix:** Rename Overload A to `TryResolveFromRoot` to make the distinction explicit:
```csharp
public static bool TryResolveFromRoot(Entity root, ushort key, in BufferLookup<EntityLink> links, out Entity result)
```
Update all call sites. The call in `EntityLinkMutateSystem` and `EntityLinkParentSystem` where `root` is already resolved should use `TryResolveFromRoot`.

---

### [P-05] [DONE] `BlendTree2DTrack.Bake` early-exit path is inconsistent with other tracks
**File:** `BlendTree2DTrack.cs`

**Problem:**
```csharp
if (rigDef == null)
{
    base.Bake(context); // calls base and returns — no track-specific components added
    return;
}
```
`RukhankaAnimationTrack.Bake` has the same pattern. While functionally correct (the track degrades gracefully), it silently produces a half-baked track entity with no `BlendAnimationTree2DTrackData`. This is hard to debug — no error or warning is logged.

**Fix:** Log a warning before the graceful degradation:
```csharp
if (rigDef == null)
{
    Debug.LogWarning($"[BlendTree2DTrack] '{name}' has no RigDefinitionAuthoring binding — animation data will not be baked.");
    base.Bake(context);
    return;
}
```

---

### [P-06] [DONE] `TimelineAnimationUnificationSystem.UnifyAnimationsJob` uses O(n×m) inner search
**File:** `TimelineAnimationUnificationSystem.cs`

**Problem:**
```csharp
for (var i = 0; i < blendEntries.Length; i++)
{
    for (var j = 0; j < smoothEntries.Length; j++) // ← O(blendEntries × smoothEntries)
        if (smoothEntries[j].ClipHash == request.ClipHash && ...)
```
For a character with many simultaneous clips (blend tree with 8 motions × multiple tracks), this inner loop runs hundreds of iterations per frame.

**Fix:** Build a temporary `NativeHashMap<(Hash128 ClipHash, int LayerIndex, uint MotionId), int>` index of `smoothEntries` before the outer loop:
```csharp
var smoothIndex = new NativeHashMap<uint, int>(smoothEntries.Length, Allocator.Temp);
for (var j = 0; j < smoothEntries.Length; j++)
    smoothIndex.TryAdd(smoothEntries[j].MotionId, j);

for (var i = 0; i < blendEntries.Length; i++)
{
    var request = blendEntries[i];
    if (smoothIndex.TryGetValue(request.MotionId, out var j))
    {
        // update existing entry at j
    }
    else
    {
        smoothEntries.Add(new SmoothBlendGroupEntry { ... });
        smoothIndex.Add(request.MotionId, smoothEntries.Length - 1);
    }
}
smoothIndex.Dispose();
```
`MotionId` is already computed as a unique hash combining track entity, layer index, and clip hash — use it as the key directly.

---

### [P-07] [DONE] `EventsDirty` component used in `PlayerInputBridge.cs` but not visible in provided code
**File:** `PlayerInputBridge.cs`

**Problem:**
```csharp
entityManager.AddComponent<EventsDirty>(providerEntity);
entityManager.SetComponentEnabled<EventsDirty>(providerEntity, false);
```
`EventsDirty` is referenced but not defined in any of the provided files. If this is an internal BovineLabs type, its assembly must be referenced. If it's supposed to be in this package, it's missing.

**Fix:** Locate and confirm `EventsDirty` is defined in the BovineLabs.Reaction or BovineLabs.Core namespace and is accessible from this assembly. If it's missing, add:
```csharp
// In an appropriate Data file:
public struct EventsDirty : IComponentData, IEnableableComponent { }
```

---

## SUMMARY TABLE

| ID | Severity | File(s) | Issue |
|---|---|---|---|
| C-01 | 🔴 Critical | 5 track systems | `UnsafeComponentLookup` fields not updated in OnUpdate |
| C-02 | 🔴 Critical | InputTransducerSystem + Builder | `InputConsumerRoute` never added — transduction is dead |
| C-03 | 🔴 Critical | InputConsumerAuthoring | `routeEventsTo` field baked nowhere |
| B-01 | 🟠 Bug | PhysicsTimelineBakingSystem | Wrong guard component for PID systems |
| B-02 | 🟠 Bug | PhysicsTriggerInstantiateSystem | `AddNoResize` on unbounded list — crash risk |
| B-03 | 🟠 Bug | NeighborDebugSystem | `transformLookup.Update` is unreachable dead code |
| B-04 | 🟠 Bug | WorldTimeScaleSystem | Missing `[BurstCompile]` on OnUpdate |
| B-05 | 🟠 Bug | HitStopSystem | Race condition on parallel write to same target |
| I-01 | 🟡 Missing | BlendTree2DTrack | `OnValidate` doesn't call `SetDirty` |
| I-02 | 🟡 Missing | TimelineTimeScaleApplySystem | HitStopState enabled state not checked |
| I-03 | 🟡 Inconsistency | PhysicsMath | `Try*` methods always return `true` |
| I-04 | 🟡 Missing | TimelineTimeScaleClip | Undocumented TrackBinding override |
| I-05 | 🟡 Performance | ActionTickDistributionSystem | O(n²) deduplication in ApplyIntrinsicJob |
| I-06 | 🟡 Performance | EntityLinkRootAuthoring | Excessive `DependsOn` calls in nested loops |
| I-07 | 🟡 Performance | ConditionKeyDrawer | Overly aggressive cache invalidation |
| I-08 | 🟡 Inconsistency | SmearVelocityAuthoring | Wrong TransformUsageFlags |
| D-01 | 🔵 Dead Code | InputCancelWindow.cs | Entire struct unused |
| D-02 | 🔵 Dead Code | InputBuffered.cs | 3 buffer types unused |
| D-03 | 🔵 Dead Code | SchemaIconPostprocessor.cs | Leftover dev comment |
| D-04 | 🔵 Dead Code | NeighborDebugSystem.cs | 70+ lines unreachable |
| D-05 | 🔵 Design Gap | WorldTimeScaleAnimated | Not implementing IAnimatedComponent |
| P-01 | ⚪ Polish | MuliInputSettings | Typo in class name |
| P-02 | ⚪ Polish | BlendTree2DTrack | `directionCalc` should be read-only in inspector |
| P-03 | ⚪ Polish | ActionTickDistributionSystem | Duplicated key-extraction job structs |
| P-04 | ⚪ Polish | EntityLinkResolver | Ambiguous overload naming |
| P-05 | ⚪ Polish | BlendTree2DTrack + RukhankaAnimationTrack | Silent failure when rigDef is null |
| P-06 | ⚪ Performance | TimelineAnimationUnificationSystem | O(n×m) search in smooth blend matching |
| P-07 | ⚪ Missing | PlayerInputBridge | `EventsDirty` type not found in provided files |
