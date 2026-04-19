# AGENTS.md — BovineLabs.Timeline.Physics Coding Conventions

Reference implementation for all BovineLabs.Timeline packages. Follow these conventions when writing or refactoring code.

## Naming

- Private instance fields: `_camelCase` (`_blendImpl`, `_activeLookup`, `_entityHandle`)
- Job struct public fields: `PascalCase` (`ActiveLookup`, `TransformLookup`, `DeltaTime`)
- No `` qualifier — use `_blendImpl` not `_blendImpl`
- No file header comments (`// BovineLabs.Timeline.Physics/...`)

## Unsafe vs Safe Lookups

### ComponentLookup

Use `UnsafeComponentLookup<T>` (from `BovineLabs.Core.Iterators`) for ALL component lookups EXCEPT:
- `ComponentLookup<Targets>` — always safe
- `ComponentLookup<TargetsCustom>` — always safe
- `ComponentLookup<LocalToWorld>` — always safe

Everything else is `UnsafeComponentLookup<T>`, regardless of read or write access.

Required usings:
```csharp
using BovineLabs.Core.Extensions;  // GetUnsafeComponentLookup<T> extension method
using BovineLabs.Core.Iterators;   // UnsafeComponentLookup<T> type
```

Getting the lookup:
```csharp
// Write access (default isReadOnly = false)
_activeLookup = state.GetUnsafeComponentLookup<ActiveVelocity>();

// Read-only
_transformLookup = state.GetUnsafeComponentLookup<LocalTransform>(true);

// Safe lookups (Targets, TargetsCustom, LocalToWorld)
_targetsLookup = state.GetComponentLookup<Targets>(true);
```

### BufferLookup

Use `UnsafeBufferLookup<T>` (from `BovineLabs.Core.Iterators`) for ALL buffer lookups EXCEPT:
- Read-only buffers in Debug systems — safe `BufferLookup<T>` is fine

```csharp
using BovineLabs.Core.Extensions;  // GetUnsafeBufferLookup<T> extension method
using BovineLabs.Core.Iterators;   // UnsafeBufferLookup<T> type

_triggerEventsLookup = state.GetUnsafeBufferLookup<StatefulTriggerEvent>(true);
```

### UnsafeComponentLookup Write Pattern

`UnsafeComponentLookup<T>` does NOT have `GetRefRW()`. Use indexer get/modify/set:

```csharp
// WRONG
StateLookup.GetRefRW(binding.Value).ValueRW.State = default;

// CORRECT
var state = StateLookup[binding.Value];
state.State = default;
StateLookup[binding.Value] = state;
```

### Job Struct Field Attributes

- Write-access Unsafe: `[NativeDisableParallelForRestriction]`
- Read-only Unsafe: `[ReadOnly]`
- Read-only Safe: `[ReadOnly]`

```csharp
[NativeDisableParallelForRestriction] public UnsafeComponentLookup<ActiveVelocity> ActiveLookup;
[ReadOnly] public UnsafeComponentLookup<LocalTransform> TransformLookup;
[ReadOnly] public ComponentLookup<Targets> TargetsLookup;
```

## Facet Pattern (IJobChunkWorkerBeginEnd)

Prefer `IJobChunkWorkerBeginEnd` with `IFacet` over `IJobEntity` for apply systems that access multiple components on the same entity.

### IFacet Definition (in Data assembly)

```csharp
public readonly partial struct PhysicsBodyFacet : IFacet
{
    public readonly RefRW<PhysicsVelocity> Velocity;
    public readonly RefRO<LocalTransform> Transform;
    [FacetOptional] public readonly RefRO<PhysicsMass> Mass;

    public partial struct Lookup { }
    public partial struct TypeHandle { }
}
```

### System Using Facet Pattern

```csharp
[Configurable]
[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial struct PhysicsPidApplySystem : ISystem
{
    private EntityQuery _linearQuery;
    private PhysicsBodyFacet.TypeHandle _facetHandle;
    private EntityTypeHandle _entityHandle;
    private ComponentTypeHandle<PhysicsLinearPIDState> _linearStateHandle;
    private ComponentTypeHandle<ActiveLinearPid> _activeLinearHandle;
    private ComponentLookup<Targets> _targetsLookup;
    private ComponentLookup<TargetsCustom> _targetsCustomLookup;
    private UnsafeComponentLookup<LocalTransform> _transformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        JobChunkWorkerBeginEndExtensions.EarlyJobInit<ApplyLinearJob>();

        _linearQuery = SystemAPI.QueryBuilder()
            .WithAllRW<PhysicsVelocity, PhysicsLinearPIDState>()
            .WithAll<ActiveLinearPid, LocalTransform>()
            .Build();

        _facetHandle.Create(ref state);
        _entityHandle = state.GetEntityTypeHandle();
        _linearStateHandle = state.GetComponentTypeHandle<PhysicsLinearPIDState>();
        _activeLinearHandle = state.GetComponentTypeHandle<ActiveLinearPid>(true);
        _targetsLookup = state.GetComponentLookup<Targets>(true);
        _targetsCustomLookup = state.GetComponentLookup<TargetsCustom>(true);
        _transformLookup = state.GetUnsafeComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        if (dt <= 0.0001f) return;

        _facetHandle.Update(ref state);
        _entityHandle.Update(ref state);
        _linearStateHandle.Update(ref state);
        _activeLinearHandle.Update(ref state);
        _targetsLookup.Update(ref state);
        _targetsCustomLookup.Update(ref state);
        _transformLookup.Update(ref state);

        state.Dependency = new ApplyLinearJob
        {
            DeltaTime = dt,
            FacetHandle = _facetHandle,
            EntityHandle = _entityHandle,
            StateHandle = _linearStateHandle,
            ActiveHandle = _activeLinearHandle,
            TargetsLookup = _targetsLookup,
            TargetsCustomLookup = _targetsCustomLookup,
            TransformLookup = _transformLookup
        }.ScheduleParallel(_linearQuery, state.Dependency);
    }
}
```

### Job Struct (IJobChunkWorkerBeginEnd)

```csharp
[BurstCompile]
private struct ApplyLinearJob : IJobChunkWorkerBeginEnd
{
    public float DeltaTime;
    public PhysicsBodyFacet.TypeHandle FacetHandle;
    [ReadOnly] public EntityTypeHandle EntityHandle;
    public ComponentTypeHandle<PhysicsLinearPIDState> StateHandle;
    [ReadOnly] public ComponentTypeHandle<ActiveLinearPid> ActiveHandle;

    [ReadOnly] public ComponentLookup<Targets> TargetsLookup;
    [ReadOnly] public ComponentLookup<TargetsCustom> TargetsCustomLookup;
    [ReadOnly] public UnsafeComponentLookup<LocalTransform> TransformLookup;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var resolved = FacetHandle.Resolve(chunk);
        var entities = chunk.GetNativeArray(EntityHandle);
        var states = chunk.GetNativeArray(ref StateHandle);
        var actives = chunk.GetNativeArray(ref ActiveHandle);

        for (var i = 0; i < chunk.Count; i++)
        {
            var facet = resolved[i];

            if (!PhysicsMath.TryResolveLinearPidTarget(
                facet.Transform.ValueRO, actives[i].Config, entities[i],
                in TargetsLookup, in TargetsCustomLookup, in TransformLookup,
                out var targetPos))
            {
                continue;
            }

            // ... compute and apply ...

            facet.Velocity.ValueRW = nextVelocity;

            var s = states[i];
            s.State = nextState;
            states[i] = s;
        }
    }
}
```

## Helper Method Signatures (PhysicsMath pattern)

When passing lookups to static helper methods, use `in` parameter modifier and match the actual type:

```csharp
// Method signature
public static bool TryResolveLinearPidTarget(
    in LocalTransform transform,
    in PhysicsLinearPIDData config,
    Entity entity,
    in ComponentLookup<Targets> targetsLookup,
    in ComponentLookup<TargetsCustom> targetsCustomLookup,
    in UnsafeComponentLookup<LocalTransform> transformLookup,
    out float3 targetPos)
```

Always pass with `in` at call site:
```csharp
PhysicsMath.TryResolveLinearPidTarget(transform, config, entity, in TargetsLookup, in TargetsCustomLookup, in TransformLookup, out var pos)
```

## Singleton Access

Prefer `TryGetSingleton` over `GetSingleton` to avoid exceptions when singleton doesn't exist:

```csharp
// WRONG
var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();

// CORRECT
if (!SystemAPI.TryGetSingleton<DrawSystem.Singleton>(out var drawSystem))
{
    return;
}
var drawer = drawSystem.CreateDrawer();
```

## Track System Pattern (IJobEntity still OK for track systems)

Track systems that use `TrackBlendImpl` can stay as `IJobEntity` — the Facet pattern is for apply systems:

```csharp
public partial struct PhysicsVelocityTrackSystem : ISystem
{
    private TrackBlendImpl<PhysicsVelocityData, PhysicsVelocityAnimated> _blendImpl;
    private UnsafeComponentLookup<ActiveVelocity> _activeLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _blendImpl.OnCreate(ref state);
        _activeLookup = state.GetUnsafeComponentLookup<ActiveVelocity>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) => _blendImpl.OnDestroy(ref state);
}
```

## When to Use Which Job Type

| System Type | Job Type | Why |
|---|---|---|
| Track blend/activate systems | `IJobEntity` | Simple entity iteration, `TrackBlendImpl` integration |
| Apply systems (modify velocity etc.) | `IJobChunkWorkerBeginEnd` + Facet | Multiple RW components, chunk-level access |
| Trigger event systems | `IJobEntity` or chunk | Depends on complexity |
| Debug drawing systems | `IJobEntity` | Read-only, simple iteration |

## Style

- Remove `` qualifiers
- Remove file-path header comments
- Add newline at end of file
- Early return with `if (!condition) { return; }` (brace on return)
- `in` keyword when passing lookups/structs to methods
- No `ref` on `SystemState` parameter in lambdas/one-liners — use expression body only for trivial delegates like `OnDestroy`
