# BovineLabs Core Unity ECS Standards

> **Audience:** Developers working on Unity projects utilizing Entities (DOTS), Burst, Jobs, and the `BovineLabs.Core`
> framework.
> **Philosophy:** Zero cyclomatic complexity systems, strict zero-allocation hot paths, pure reference-based functions
> for Burst compatibility, and mathematical proof of stability through rigorous testing.

---

## 📚 Table of Contents

**Part 1: Architecture & Data**

0. [Code Comment Policy — The Absolute Rule](#0-code-comment-policy--the-absolute-rule)
1. [Assembly Definition Architecture](#1-assembly-definition-architecture)
   2.[World & Bootstrap Architecture](#2-world--bootstrap-architecture)
   3.[Core Utilities: Logging, Math, and Assertions](#3-core-utilities-logging-math-and-assertions)
   4.[Naming Conventions & Layout](#4-naming-conventions--layout)
5. [Standard Component Design Rules](#5-standard-component-design-rules)
6. [The `IEnableableComponent` vs. Structural Changes](#6-the-ienableablecomponent-vs-structural-changes)
   7.[The `KSettings` System (Burst-Safe String IDs)](#7-the-ksettings-system-burst-safe-string-ids)
8. [Global vs. World-Specific Settings](#8-global-vs-world-specific-settings)
9. [`[Singleton]` Buffers (The Many-to-One Pattern)](#9-singleton-buffers-the-many-to-one-pattern)
   10.[Object Management: `IUID` and `ObjectDefinition`](#10-object-management-iuid-and-objectdefinition)

**Part 2: Systems, Jobs & Ecosystem**

11. [System Design — Zero Cyclomatic Complexity](#11-system-design--zero-cyclomatic-complexity)
12. [Advanced Custom Jobs (`BovineLabs.Core.Jobs`)](#12-advanced-custom-jobs-bovinelabscorejobs)
    13.[The `DynamicHashMap` Ecosystem](#13-the-dynamichashmap-ecosystem)
    14.[Advanced Iterators and Lookups](#14-advanced-iterators-and-lookups)
    15.[Facets (The `IFacet` System)](#15-facets-the-ifacet-system)
16. [Unified Entity Manipulation (`IEntityCommands`)](#16-unified-entity-manipulation-ientitycommands)
17. [The Automated Lifecycle Pipeline](#17-the-automated-lifecycle-pipeline)
18. [Advanced SubScene & World Management](#18-advanced-subscene--world-management)
19. [High-Performance Physics States](#19-high-performance-physics-states)
20. [NetCode Relevancy (Interest Management)](#20-netcode-relevancy-interest-management)
21. [Deterministic Pause System](#21-deterministic-pause-system)

**Part 3: The Testing Bible**

22. [The Philosophy of DOTS Testing](#22-the-philosophy-of-dots-testing)
23. [Test Assembly & Fixture Architecture](#23-test-assembly--fixture-architecture)
24. [Memory Leak Detection (The 24-Frame Rule)](#24-memory-leak-detection-the-24-frame-rule)
    25.[Level 1: Pure Function Testing (Ref/Out ABI)](#25-level-1-pure-function-testing-refout-abi)
26. [Level 2: Job Testing in Isolation](#26-level-2-job-testing-in-isolation)
27. [Level 3: System Orchestration Testing](#27-level-3-system-orchestration-testing)
28. [Advanced: Testing DynamicHashMaps & Collections](#28-advanced-testing-dynamichashmaps--collections)
    29.[Advanced: Testing Facets & IEnableableComponents](#29-advanced-testing-facets--ienableablecomponents)
    30.[Advanced: Testing Blob Assets](#30-advanced-testing-blob-assets)
    31.[Performance Benchmarking (The NASA Speed Standard)](#31-performance-benchmarking-the-nasa-speed-standard)
32. [Math Assertions & Helpers](#32-math-assertions--helpers)
    33.[The Elite PR Testing Checklist](#33-the-elite-pr-testing-checklist)

---

## 0. Code Comment Policy — The Absolute Rule

> **RULE: Production code contains zero comments. No exceptions.**

Self-documenting names, clear structure, and well-named systems replace every comment. If you feel the urge to write a
comment, rename the variable, extract the method, or redesign the structure until the comment is unnecessary.

**Why:** Comments lie. Code changes; comments don't. A comment that was accurate six months ago is a trap today. Naming
things correctly is permanent documentation.

```csharp
// ❌ PRODUCTION CODE VIOLATION — never ship this
private void Execute(ref Health h, in Regen r) // Apply regen to health
{
    h.Current = math.min(h.Current + r.Rate * dt, h.Max); // Clamp to max
}

// ✅ CORRECT — the code is the documentation
private void Execute(ref Health health, in Regeneration regeneration)
{
    health.Current = math.min(health.Current + regeneration.Rate * deltaTime, health.Max);
}
```

*(Note: Code examples in this document contain comments for teaching purposes only).*

---

## 1. Assembly Definition Architecture

The foundation of our ECS architecture relies on strict compilation boundaries. If code can reach where it shouldn't,
architecture degrades.

### 1.1 `autoReferenced: false` is Non-Negotiable

Every assembly must explicitly declare its dependencies. `autoReferenced: true` is banned.

- **Why?** Editor-only types will leak into player builds, build times will skyrocket, and circular dependencies will
  form.

### 1.2 The Six-Layer Assembly Pattern

Use the `BovineLabs Assembly Builder` (`BovineLabs -> Tools -> Assembly Builder`) to generate compliant layers.

| Assembly            | Purpose                                | Constraint / Attributes                          | Key Rule                               |
|---------------------|----------------------------------------|--------------------------------------------------|----------------------------------------|
| `Feature.Data`      | `IComponentData`, `IBufferElementData` | None (Ships everywhere)                          | **No logic. Pure data structs only.**  |
| `Feature`           | `ISystem`, Jobs, ViewModels            | None                                             | No MonoBehaviours. No Editor types.    |
| `Feature.Authoring` | Bakers, MonoBehaviours                 | `UNITY_EDITOR` / `[DisableAutoTypeRegistration]` | **Never** compile into player builds.  |
| `Feature.Debug`     | Debug panels, diagnostics              | `UNITY_EDITOR \|\| BL_DEBUG`                     | Ships only in dev builds.              |
| `Feature.Editor`    | Custom inspectors                      | `includePlatforms: [Editor]`                     | Tools for designers.                   |
| `Feature.Tests`     | Unit & Integration tests               | `TestAssemblies` / `[DisableAutoCreation]`       | Test systems never run in live worlds. |

### 1.3 `InternalsVisibleTo` Routing

We use `internal` heavily to hide system implementations and raw data from other domains.

```csharp
[assembly: InternalsVisibleTo("Feature")][assembly: InternalsVisibleTo("Feature.Authoring")]
[assembly: InternalsVisibleTo("Feature.Tests")]
```

> **Rule:** `Feature.Data` exposes internals to its consumers, but *never* receives `InternalsVisibleTo` back from
> runtime. No circular trust.

---

## 2. World & Bootstrap Architecture

Standard Unity DOTS pushes everything into the `DefaultWorld`. We use `BovineLabsBootstrap` to cleanly separate logic
into distinct worlds based on their lifecycle and network role.

### 2.1 The `BovineLabsBootstrap`

Inherit from `BovineLabsBootstrap` to automatically generate our core worlds:

- **GameWorld:** Main simulation world.
- **ServiceWorld:** Runs background services, UI synchronization, and persistent systems.
- **MenuWorld:** Main menus without heavy physics/gameplay systems.

```csharp
[Configurable]
public class GameBootstrap : BovineLabsBootstrap
{
    protected override void Initialize()
    {
        base.Initialize(); // Creates ServiceWorld
        CreateMenuWorld(); // Initialize specialized worlds
    }
}
```

### 2.2 System World Filtering

Always tag systems with their target world using `[WorldSystemFilter]`.

```csharp
[WorldSystemFilter(Worlds.Simulation)]
public partial struct MovementSystem : ISystem { }[WorldSystemFilter(Worlds.Service)]
public partial struct UserProfileServiceSystem : ISystem { }
```

---

## 3. Core Utilities: Logging, Math, and Assertions

### 3.1 Assertions: `Check.Assume`

Standard asserts cause branching which degrades Burst's auto-vectorization. We use `BovineLabs.Core.Assertions.Check`.

```csharp
// ❌ BAD: Debug.Assert creates branches
Debug.Assert(health > 0, "Health must be positive");

// ✅ GOOD: Check.Assume tells Burst compiler to optimize under this assumption.
Check.Assume(health > 0, "Health must be positive");
```

### 3.2 Global & World-Aware Logging

Never use `UnityEngine.Debug.Log` in ECS. Use `BLGlobalLogger` for static contexts and the `BLLogger` singleton for
world/frame-aware logging.

```csharp
[BurstCompile]
public partial struct DamageSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var logger = SystemAPI.GetSingleton<BLLogger>();
        logger.LogDebug($"Processing {count} damage events"); // Pre-allocates FixedString safely
    }
}
```

### 3.3 Thread-Safe Random: `GlobalRandom`

`Unity.Mathematics.Random` cannot be shared across threads. We use `GlobalRandom` (which maintains pre-allocated states
per worker thread).

```csharp
[BurstCompile]
partial struct RandomSpawnJob : IJobEntity
{
    private void Execute(ref LocalTransform transform)
    {
        // Zero false-sharing, zero locks, maximum performance.
        transform.Position = GlobalRandom.NextFloat3Direction() * 10f;
    }
}
```

---

## 4. Naming Conventions & Layout

### 4.1 Component Naming

Components describe WHAT an entity IS or HAS.

- **Tags:** Adjectives or States (`Dead`, `Stunned`). *Never* suffix with "Tag".
- **Data:** Nouns (`Health`, `Velocity`). *Never* suffix with "Component" or "Data".
- **Buffers:** Singular Noun (`InventoryItem`).

### 4.2 System & Job Naming

- **Systems:** `[Verb][Subject]System` (`ApplyDamageSystem`).
- **Jobs:** `[Verb][Subject]Job`. Suffix with `ChunkJob` if implementing `IJobChunk`.

---

## 5. Standard Component Design Rules

**RULE: Components are pure memory layouts.**

```csharp
// ❌ BAD: Contains methods, properties, or logic.
public struct Health : IComponentData
{
    public float Current;
    public bool IsDead => Current <= 0; // VIOLATION
}

// ✅ GOOD: Blittable, pure memory.
public struct Health : IComponentData
{
    public float Current;
    public float Max;
}
```

---

## 6. The `IEnableableComponent` vs. Structural Changes

Structural changes (adding/removing components) stall the main thread, invalidate caches, and prevent parallel
processing.

- **RULE:** Never use `EntityCommandBuffer.AddComponent<T>` for state toggles that happen frequently (`IsStunned`,
  `IsSelected`).
- **RULE:** Always use `IEnableableComponent` for transient states.

```csharp
// ✅ GOOD: Zero allocation, zero chunk moves.
public struct Stunned : IComponentData, IEnableableComponent { }
// Usage: SystemAPI.SetComponentEnabled<Stunned>(entity, true);
```

---

## 7. The `KSettings` System (Burst-Safe String IDs)

Enums are rigid. Strings are slow. `KSettings<T, TV>` converts human-readable strings from ScriptableObjects into
Burst-compatible unmanaged values (`byte`, `int`).

### 7.1 Authoring with `[K]`

```csharp
public class CharacterStates : KSettings<CharacterStates, byte> { }

public class SpawnAuthoring : MonoBehaviour
{
    [K(nameof(CharacterStates))]
    public byte InitialState; // Inspector shows strings, stores byte
}
```

### 7.2 Resolving Inside Burst

```csharp
[BurstCompile]
public void Execute(ref Character character)
{
    if (character.State == CharacterStates.NameToKey("attacking")) { }
}
```

---

## 8. Global vs. World-Specific Settings

- **`SettingsSingleton<T>`:** For data that must exist *before* ECS worlds spin up (Boot configs, Themes). Available
  instantly via `MyGlobalSettings.I`.
- **`SettingsBase`:** For ECS gameplay data. Use `[SettingsWorld("Client", "Server")]` to target specific network
  worlds.

---

## 9. `[Singleton]` Buffers (The Many-to-One Pattern)

When multiple subscenes or mods contribute to a single list of data, use `BovineLabs.Core.Settings.SingletonAttribute`
to automatically merge them.

```csharp
[Singleton]
public struct CraftableItemElement : IBufferElementData { public int ItemId; }
```

At runtime, `SingletonSystem` automatically finds all entities with this buffer, copies them into **one master singleton
entity**, and triggers a one-frame `SingletonInitialize` flag.

---

## 10. Object Management: `IUID` and `ObjectDefinition`

Traditional ECS prefab passing breaks over networks. We use `ObjectId` (a deterministic integer) backed by
`ObjectDefinition` assets.

### 10.1 Authoring

```csharp
public struct SpawnRequest : IComponentData
{
    public ObjectId PrefabId; // Deterministic cross-network integer
}

public class SpawnerAuthoring : MonoBehaviour
{
    [SearchContext("ca=enemy", ObjectDefinition.SearchProviderType)]
    public ObjectDefinition EnemyToSpawn;

    public class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring auth)
        {
            AddComponent(GetEntity(TransformUsageFlags.None), new SpawnRequest
            {
                PrefabId = auth.EnemyToSpawn // Implicit cast ObjectDefinition -> ObjectId
            });
        }
    }
}
```

### 10.2 Spawning at Runtime

```csharp
var registry = SystemAPI.GetSingleton<ObjectDefinitionRegistry>();
Entity prefabToSpawn = registry[request.ValueRO.PrefabId]; // O(1) lookup
Entity instance = ecb.Instantiate(prefabToSpawn);
```

---

## 11. System Design — Zero Cyclomatic Complexity

**Zero Cyclomatic Complexity (CC = 1) in System `OnUpdate` methods.** A System is an orchestrator. It queries data,
handles dependencies, schedules jobs, and returns. No `if`, `for`, or `foreach` logic loops.

### 11.1 The Pure Function & Burst ABI Rule

When logic must be executed inside a job, it should be extracted into **pure static extension methods**.

- **RULE:** Do not use `return` statements for structs or data (except `bool` for success conditions).
- **RULE:** Pass data via `in`, `ref`, and `out` parameters.

*Why?* Burst compiles `ref`/`out` parameters much more efficiently than struct return types, as it avoids stack copying
and enforces strict ABI compliance. It also guarantees your function has zero external state dependencies.

```csharp
// ❌ BAD: Returns a struct (causes stack copy), internal system branching
public float CalculateDamage(float dmg) { return dmg * 1.5f; }

// ✅ GOOD: Pure extension method, Burst ABI compliant, easily testable
public static class CombatMathExtensions
{
    [BurstCompile]
    public static void CalculateMitigatedDamage(this ref float incomingDamage, in float armor, out float result)
    {
        result = math.max(1f, incomingDamage * (100f / (100f + armor)));
    }
}
```

---

## 12. Advanced Custom Jobs (`BovineLabs.Core.Jobs`)

### 12.1 `IJobForThread`

Divides work across a **known number of threads** instead of Unity's dynamic batching. Each worker thread receives a
contiguous slice.

```csharp
state.Dependency = new SpatialPartitionJob { SpatialGrid = grid }
    .ScheduleParallel(grid.Length, threadCount: 4, state.Dependency);
```

### 12.2 `IJobHashMapDefer` & `IJobParallelHashMapDefer`

Iterate over `NativeHashMap` collections directly in parallel.

```csharp
[BurstCompile]
private struct ProcessSpatialMapJob : IJobParallelHashMapDefer
{
    [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;
    
    public void ExecuteNext(int entryIndex, int jobIndex)
    {
        this.Read(SpatialMap, entryIndex, out int cellHash, out Entity occupant);
    }
}
```

---

## 13. The `DynamicHashMap` Ecosystem

You cannot store `NativeHashMap`s directly on an Entity. BovineLabs solves this by reinterpreting `DynamicBuffer<byte>`
as fully functional, zero-allocation HashMaps.

```csharp
// 1. Declaration
[InternalBufferCapacity(0)]
public struct InventoryMap : IDynamicHashMap<int, int>
{
    byte IDynamicHashMap<int, int>.Value { get; }
}

// 2. Initialization in Baker
var buffer = AddBuffer<InventoryMap>(entity);
buffer.InitializeHashMap<InventoryMap, int, int>(capacity: 16);

// 3. Job Execution
var inventory = inventoryBuffer.AsMap();
inventory.Add(itemId: 45, count: 10);
if (inventory.TryGetValue(45, out var count)) { ... }
```

---

## 14. Advanced Iterators and Lookups

### 14.1 `UnsafeComponentLookup<T>`

Bypasses Unity's parallel job safety checks. Use when you can mathematically guarantee no two threads touch the same
Entity ID. Requires manual dependency injection.

### 14.2 `QueryEntityEnumerator`

For bypassing `IJobEntity` overhead on tiny datasets or when manual chunk parsing is required.

```csharp
var queryEnumerator = new QueryEntityEnumerator(myQuery);
while (queryEnumerator.MoveNextChunk(out ArchetypeChunk chunk, out ChunkEntityEnumerator chunkEnumerator))
{
    var transforms = chunk.GetChunkComponentDataPtrRW(ref transformHandle);
    while (chunkEnumerator.NextEntityIndex(out int i))
    {
        transforms[i].Position += new float3(0, 1, 0);
    }
}
```

---

## 15. Facets (The `IFacet` System)

Instead of boilerplate-heavy `IAspect`s, define a `partial struct` that implements `IFacet`. The Roslyn Source
Generator (`BovineLabs.FacetGenerator`) writes the `Lookup`, `TypeHandle`, and `ResolvedChunk` logic for you.

```csharp
public readonly partial struct CombatFacet : IFacet
{
    private readonly RefRW<Health> health;
    private readonly EnabledRefRO<Invulnerable> isInvulnerable;
    
    [FacetOptional] 
    private readonly DynamicBuffer<StatusEffect> statusEffects;
    
    [Singleton] 
    private readonly CombatSettings singletonSettings;

    public partial struct Lookup { } // Triggers generator
}
```

In jobs, use `this.FacetHandle.Resolve(chunk)` to convert the chunk instantly into Facet instances.

---

## 16. Unified Entity Manipulation (`IEntityCommands`)

Abstracts `EntityManager`, `EntityCommandBuffer`, `ParallelWriter`, and `IBaker` into a single interface so composition
logic is written exactly once.

```csharp
public static class UnitFactory
{
    // Write once, call from Bakers OR Systems!
    public static void SetupUnit<T>(ref T commands, float3 position, int team)
        where T : IEntityCommands
    {
        commands.AddComponent(LocalTransform.FromPosition(position));
        commands.AddComponent(new Team { Value = team });
    }
}
```

---

## 17. The Automated Lifecycle Pipeline

Sprinkling `EntityManager.DestroyEntity` causes unpredictable structural change stalls.

### 17.1 Initialization & Destruction

- Mark prefabs with `InitializeEntity` to catch them in the `InitializeSystemGroup`.
- Enable the `DestroyEntity` (an `IEnableableComponent`) to queue entities for the `DestroySystemGroup`.

### 17.2 Deep Hierarchy Destruction (`LinkedEntityGroup`)

When `DestroyEntity` is enabled, `DestroyOnDestroySystem` recursively traverses `LinkedEntityGroup`s, enabling
destruction on all child entities automatically, preventing orphaned memory leaks.

---

## 18. Advanced SubScene & World Management

Map SubScenes to specific `WorldFlags` (Game, Service, Client, Server) using `SubSceneSettings`.

- **Required Loading:** Pauses the game until critical geo/data is loaded.
- **Runtime Toggling:** Toggle the `LoadSubScene` enableable component to load/unload scenes dynamically at runtime.
  Never use `SceneSystem.LoadSceneAsync` directly in logic.

---

## 19. High-Performance Physics States

Unity Physics `CollisionEvent` is stateless. `BovineLabs.Core.PhysicsStates` is a Burst-compiled, fully parallelized
system that converts these into `Enter`, `Stay`, and `Exit` states automatically.

Attach `StatefulCollisionEventAuthoring` to colliders, then query `DynamicBuffer<StatefulCollisionEvent>` in your
systems.

```csharp
foreach (var collision in collisions)
{
    if (collision.State == StatefulEventState.Enter) { /* Apply Damage */ }
}
```

---

## 20. NetCode Relevancy (Interest Management)

Prevents the server from sending every ghost to every client. Attach `InputBounds` to the player connection entity. The
`RelevancySystem` spatially hashes the bounds and only serializes ghosts that fall inside the client's localized grid.

---

## 21. Deterministic Pause System

Disabling systems to "pause" causes `SystemAPI.Time.ElapsedTime` to drift, creating massive catch-up lag spikes in the
`FixedStepSimulationSystemGroup` when unpaused.

`BovineLabs.Core.Pause` intercepts the `IRateManager`. When `PauseGame` is enabled, it literally freezes ECS time
progression.

- Use `IDisableWhilePaused` to force systems off during pause.
- Use `IUpdateWhilePaused` to force UI/Service systems to run while the game is paused.

---
---

# Part 3: The Testing Bible

### Absolute Determinism, Zero-Leak Validation, and Sub-Millisecond Benchmarking

> **Philosophy:** Untested ECS code is broken code. The compiler will not save you from race conditions, memory leaks,
> or chunk fragmentation. Testing is the mathematical proof that your systems function.

---

## 22. The Philosophy of DOTS Testing

Because we enforce **Zero Cyclomatic Complexity (CC=1)** in our systems, testing becomes a pure mathematical exercise:
`Input Data -> Execute Job/System -> Output Data`.

Why test rigorously?

1. **Unmanaged Memory:** A missed `.Dispose()` will crash the game.
2. **Burst Compilation:** Managed fallbacks silently destroy performance.
3. **Race Conditions:** Parallel jobs writing to the same entity cause bugs.

---

## 23. Test Assembly & Fixture Architecture

Test assemblies must be quarantined.

1. `autoReferenced: false` in `asmdef`.
2. Must include `[assembly: DisableAutoCreation]` in `AssemblyInfo.cs` to prevent test systems from corrupting the live
   `SimulationSystemGroup`.
3. **Always** inherit tests from `BovineLabs.Testing.ECSTestsFixture`. This provides a pristine isolated `World` and
   forces `JobsUtility.JobDebuggerEnabled = true` to catch race conditions.

---

## 24. Memory Leak Detection (The 24-Frame Rule)

Creating a `NativeArray` with `Allocator.TempJob` without `.Dispose()` causes a leak. `[TestLeakDetection]`
automatically checks for leaks at the end of the test.

⚠️ **Warning on Frame Delays:** The `[TestLeakDetection]` attribute takes approximately **24 frames** of ECS lifecycle
execution to fully flush and validate internal allocators in some Unity versions.

- **DO NOT** use this attribute on tests that rely on strict, frame-by-frame time stepping or immediate `SystemAPI.Time`
  checks next frame.
- **USE IT** strategically on isolated allocation and structural tests where timing is irrelevant.

```csharp
[Test]
[TestLeakDetection] // Catches leaks, but disrupts frame-time
public void SafeAllocationTest()
{
    using var list = new NativeList<int>(10, Allocator.TempJob);
}
```

---

## 25. Level 1: Pure Function Testing (Ref/Out ABI)

The fastest, most reliable tests require no `EntityManager`. Extract logic into `public static void` extension methods.

Following our **Pure Functions & Burst ABI** rule, never use `return` for structs. Use `in`, `ref`, and `out`.

```csharp
// The Logic
public static class CombatMathExtensions
{
    [BurstCompile]
    public static void CalculateMitigatedDamage(this ref float incomingDamage, in float armor, out float result)
    {
        result = math.max(1f, incomingDamage * (100f / (100f + armor)));
    }
}

// The Test
public class CombatMathTests
{
    [TestCase(100f, 0f, ExpectedResult = 100f)]
    [TestCase(100f, 100f, ExpectedResult = 50f)]
    [TestCase(5f, 1000f, ExpectedResult = 1f)] 
    public float CalculateMitigatedDamage_ReturnsCorrectValues(float dmg, float armor)
    {
        dmg.CalculateMitigatedDamage(in armor, out float result);
        return result;
    }
}
```

---

## 26. Level 2: Job Testing in Isolation

Use `.Run()` instead of `.Schedule()` to execute jobs synchronously on the main thread for the test, bypassing the
System orchestrator overhead.

```csharp
[Test]
public void ProcessDamageJob_WhenHealthDepleted_SetsDeadToTrue()
{
    var healths = new NativeArray<Health>(1, Allocator.TempJob);
    healths[0] = new Health { Current = 10f, Max = 100f };
    
    var isDeadFlags = new NativeArray<bool>(1, Allocator.TempJob);
    isDeadFlags[0] = false;

    new ProcessDamageJob { Healths = healths, IsDead = isDeadFlags }.Run(1);

    Assert.IsTrue(isDeadFlags[0]);

    healths.Dispose();
    isDeadFlags.Dispose();
}
```

---

## 27. Level 3: System Orchestration Testing

Create entities, run the `ISystem`, and verify. **Always call `Manager.CompleteAllTrackedJobs()` before asserting.**

```csharp
[Test]
public void ApplyPoisonSystem_ReducesHealth_OverTime()
{
    var entity = Manager.CreateEntity(typeof(Health), typeof(PoisonEffect));
    Manager.SetComponentData(entity, new Health { Current = 100f });
    Manager.SetComponentData(entity, new PoisonEffect { DamagePerTick = 5f });

    var system = World.GetOrCreateSystem<ApplyPoisonSystem>();
    WorldUnmanaged.Time = new Unity.Core.TimeData(elapsedTime: 1f, deltaTime: 1f);

    system.Update(WorldUnmanaged);
    Manager.CompleteAllTrackedJobs(); // CRITICAL

    var health = Manager.GetComponentData<Health>(entity);
    Assert.AreEqual(95f, health.Current);
}
```

*Note: If the system uses an `EntityCommandBuffer`, you must manually `Update`
the `EndSimulationEntityCommandBufferSystem` to trigger playback before asserting.*

---

## 28. Advanced: Testing DynamicHashMaps & Collections

When testing `DynamicHashMap` components, you must explicitly initialize the buffer just like a Baker would.

```csharp
[Test]
public void InventorySystem_AddsItemToDynamicHashMap()
{
    var entity = Manager.CreateEntity(typeof(InventoryMap));
    
    // Initialize the DynamicHashMap
    var buffer = Manager.GetBuffer<InventoryMap>(entity);
    buffer.InitializeHashMap<InventoryMap, int, int>(capacity: 16);

    World.GetOrCreateSystem<InventorySystem>().Update(WorldUnmanaged);
    Manager.CompleteAllTrackedJobs();

    var map = Manager.GetBuffer<InventoryMap>(entity).AsMap();
    Assert.IsTrue(map.TryGetValue(itemId: 101, out var itemCount));
}
```

---

## 29. Advanced: Testing Facets & IEnableableComponents

To ensure a facet resolves properly, test it via its `Lookup`.

```csharp
[Test]
public void CombatFacet_TryGet_ResolvesCorrectly()
{
    var entity = Manager.CreateEntity(typeof(Health), typeof(Invulnerable));
    Manager.SetComponentEnabled<Invulnerable>(entity, false);

    var state = World.Unmanaged.ResolveSystemStateRef(World.GetOrCreateSystem<CombatSystem>().SystemHandle);
    
    var lookup = new CombatFacet.Lookup();
    lookup.Create(ref state);
    lookup.Update(ref state); // MUST update to refresh cache

    Assert.IsTrue(lookup.TryGet(entity, out var facet));
    Assert.IsFalse(facet.IsInvulnerable);
}
```

---

## 30. Advanced: Testing Blob Assets

Blob assets require unmanaged allocation. Ensure you use the `BlobBuilder` and `Dispose()` of the reference properly to
avoid catastrophic leaks.

```csharp
[Test]
public void PathfindingSystem_ReadsBlobAssetCorrectly()
{
    var builder = new BlobBuilder(Allocator.Temp);
    ref var root = ref builder.ConstructRoot<WaypointBlob>();
    // populate...
    var blobRef = builder.CreateBlobAssetReference<WaypointBlob>(Allocator.Persistent);
    builder.Dispose();

    var entity = Manager.CreateEntity(typeof(WaypointPath));
    Manager.SetComponentData(entity, new WaypointPath { Blob = blobRef });

    World.GetOrCreateSystem<PathfindingSystem>().Update(WorldUnmanaged);
    Manager.CompleteAllTrackedJobs();

    // Cleanup Blob (CRITICAL)
    blobRef.Dispose();
}
```

---

## 31. Performance Benchmarking (The NASA Speed Standard)

Core mathematical systems, tight loops, and custom collections MUST have performance benchmarks via
`Unity.PerformanceTesting`.

```csharp
[Test]
[Performance]
public void SIMD_Max_PerformanceTest()
{
    var input = new NativeArray<float>(100_000, Allocator.Persistent);
    
    Measure.Method(() => 
    {
        // Hot path execution
        BovineLabs.Core.Utility.mathex.max(input);
    })
    .WarmupCount(5)
    .MeasurementCount(20)
    .Run();

    input.Dispose();
}
```

---

## 32. Math Assertions & Helpers

Standard `Assert.AreEqual` fails violently with floating-point math and vectors.
**Rule:** Always use `BovineLabs.Testing.AssertMath`.

```csharp
[Test]
public void Rotation_UpdatesCorrectly()
{
    quaternion expectedRot = quaternion.Euler(0, math.PI, 0);
    quaternion actualRot = GetActualRotation();

    // ✅ GOOD: Checks XYZW within the delta
    AssertMath.AreApproximatelyEqual(expectedRot, actualRot, 0.001f);
}
```

---

## 33. The Elite PR Testing Checklist

Before opening a Pull Request, your code must pass this tribunal:

1. [ ] **Coverage:** Are all new Systems covered by at least one `[Test]` in a `.Tests` assembly?
2. [ ] **Leak Free:** Are non-time-dependent structural tests marked with `[TestLeakDetection]`?
3. [ ] **Burst ABI Compliance:** Are pure functions written as `static` extensions using `ref`/`out` instead of
   returning structs?
4. [ ] **No Managed Mocks:** Are you instantiating real `NativeArray`s and `Entity` components instead of using mocking
   frameworks?
5. [ ] **Zero Complexity Validation:** Is the system's `OnUpdate` CC=1?
6. [ ] **Completion:** Do your tests call `Manager.CompleteAllTrackedJobs()` before asserting data?
7. [ ] **ECB Syncing:** If your system uses `EntityCommandBuffer`, did you explicitly update the ECB system to force
   playback?

> **"If it's not tested, it's broken. If it allocates in the hot path, it's rejected. If it leaks memory, it's
reverted."**