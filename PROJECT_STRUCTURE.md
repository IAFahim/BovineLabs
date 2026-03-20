# Unity ECS Examples — Full Project Structure

> Each numbered folder = one README topic = one self-contained feature.
> Every feature follows the **6-layer assembly pattern** from Section 1.
> Open the scene inside any folder to run that topic's demo.

---

## What Each Folder Teaches You At A Glance

| Folder | Demo Name | What You See Running It |
|--------|-----------|-------------------------|
| `01_AssemblyArchitecture` | Counter | All 6 asmdef layers on the simplest possible feature |
| `02_WorldBootstrap` | MultiWorld | Three isolated worlds: Game, Service, Menu |
| `03_CoreUtilities` | DamageLogger | BLLogger output, mathex SIMD speed, GlobalRandom loot |
| `04_NamingConventions` | CombatGoldStandard | Every file/type named by the book |
| `05_ComponentDesign` | InventoryData | Pure blittable structs vs the bad version |
| `06_IEnableableComponent` | UnitStates | Stun/Select/Invulnerable with zero chunk moves |
| `07_KSettings` | CharacterAnimator | Designer string IDs resolved inside Burst jobs |
| `08_SettingsSystem` | GameBalance | ScriptableObject → SettingsBase → baked ECS singleton |
| `09_SingletonBuffers` | CraftingRecipes | Recipes from 3 subscenes merged into one master buffer |
| `10_ObjectDefinition` | EnemySpawner | Spawn enemies by deterministic ObjectId, not Entity ref |
| `11_ZeroComplexity` | HealthCombat | CC=1 system, job, pure function — the canonical example |
| `12_AdvancedJobs` | SpatialPartition | IJobForThread grid + IJobParallelHashMapDefer lookup |
| `13_DynamicHashMap` | InventorySystem | Per-entity HashMap living inside a DynamicBuffer |
| `14_AdvancedIterators` | ProximityDetection | UnsafeComponentLookup + QueryEntityEnumerator |
| `15_Facets` | CombatResolver | CombatFacet composing health+defense+buffs in one chunk job |
| `16_IEntityCommands` | UnitFactory | SetupUnit() called from Baker, IJobEntity, and main thread |
| `17_LifecyclePipeline` | ProjectileSystem | Spawn → Init → Countdown → Destroy phased pipeline |
| `18_SubSceneManagement` | LevelLoader | Subscenes targeted to Client/Server/Service worlds |
| `19_PhysicsStates` | TrapSystem | Spike traps: Enter/Stay/Exit stateful collision events |
| `20_NetCodeRelevancy` | MultiplayerZone | InputBounds ghost relevancy for bandwidth control |
| `21_PauseSystem` | GamePause | Freeze simulation, UI keeps running, zero catch-up ticks |

---

## Full Tree

```
Assets/
│
├── Examples/
│   │
│   ├── _Shared/                                    ← Shared base types used across examples
│   │   ├── AssemblyInfo.cs
│   │   ├── _Shared.asmdef
│   │   └── Testing/
│   │       ├── ECSTestsFixtureBase.cs              ← Wraps BovineLabs.Testing.ECSTestsFixture
│   │       └── AssertMathHelpers.cs                ← float3/quaternion comparison helpers
│   │
│   ├── 01_AssemblyArchitecture/
│   │   ├── Scenes/
│   │   │   └── 01_AssemblyArchitecture.unity       ← Shows all 6 layers on a live counter
│   │   ├── Counter.Data/
│   │   │   ├── Components/
│   │   │   │   └── Counter.cs                      ← IComponentData { int Value; }
│   │   │   ├── AssemblyInfo.cs                     ← InternalsVisibleTo Counter, Counter.Tests
│   │   │   └── Counter.Data.asmdef                 ← autoReferenced: false, no constraints
│   │   ├── Counter/
│   │   │   ├── Systems/
│   │   │   │   └── IncrementCounterSystem.cs       ← ISystem, [BurstCompile], CC=1
│   │   │   ├── AssemblyInfo.cs                     ← InternalsVisibleTo Counter.Tests
│   │   │   └── Counter.asmdef                      ← refs Counter.Data
│   │   ├── Counter.Authoring/
│   │   │   ├── CounterAuthoring.cs                 ← MonoBehaviour + Baker in same file
│   │   │   ├── AssemblyInfo.cs                     ← [DisableAutoTypeRegistration]
│   │   │   └── Counter.Authoring.asmdef            ← defineConstraints: UNITY_EDITOR
│   │   ├── Counter.Debug/
│   │   │   ├── CounterDebugPanel.cs                ← AppUI debug view of counter value
│   │   │   └── Counter.Debug.asmdef                ← defineConstraints: UNITY_EDITOR || BL_DEBUG
│   │   ├── Counter.Editor/
│   │   │   ├── CounterEditor.cs                    ← Custom inspector for CounterAuthoring
│   │   │   └── Counter.Editor.asmdef               ← includePlatforms: [Editor]
│   │   └── Counter.Tests/
│   │       ├── AssemblyInfo.cs                     ← [DisableAutoCreation]
│   │       ├── IncrementCounterSystemTests.cs
│   │       └── Counter.Tests.asmdef                ← optionalUnityReferences: [TestAssemblies]
│   │
│   ├── 02_WorldBootstrap/
│   │   ├── Scenes/
│   │   │   ├── 02_Bootstrap_Main.unity             ← Bootstrap scene (never unloaded)
│   │   │   ├── 02_Bootstrap_GameWorld.unity        ← Additive: loaded into GameWorld
│   │   │   ├── 02_Bootstrap_ServiceWorld.unity     ← Additive: loaded into ServiceWorld
│   │   │   └── 02_Bootstrap_MenuWorld.unity        ← Additive: loaded into MenuWorld
│   │   ├── Bootstrap.Data/
│   │   │   ├── Components/
│   │   │   │   ├── WorldTag.cs                     ← Tags: GameWorldTag, ServiceWorldTag, MenuWorldTag
│   │   │   │   └── WorldStats.cs                   ← Tracks frame count per world
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Bootstrap.Data.asmdef
│   │   ├── Bootstrap/
│   │   │   ├── GameBootstrap.cs                    ← Inherits BovineLabsBootstrap
│   │   │   ├── Systems/
│   │   │   │   ├── GameWorldSystem.cs              ← [WorldSystemFilter(Worlds.Simulation)]
│   │   │   │   ├── ServiceWorldSystem.cs           ← [WorldSystemFilter(Worlds.Service)]
│   │   │   │   └── MenuWorldSystem.cs              ← [WorldSystemFilter(Worlds.Menu)]
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Bootstrap.asmdef
│   │   ├── Bootstrap.Authoring/
│   │   │   ├── WorldStatsAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Bootstrap.Authoring.asmdef
│   │   └── Bootstrap.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── WorldFilterTests.cs                 ← Verifies systems only run in their world
│   │       └── Bootstrap.Tests.asmdef
│   │
│   ├── 03_CoreUtilities/
│   │   ├── Scenes/
│   │   │   └── 03_CoreUtilities.unity
│   │   ├── Assets/
│   │   │   └── LootTable.asset                     ← ScriptableObject with loot weights
│   │   ├── CoreUtils.Data/
│   │   │   ├── Components/
│   │   │   │   ├── DamageEvent.cs                  ← IBufferElementData { float Amount; Entity Source; }
│   │   │   │   └── LootDrop.cs                     ← IComponentData { ObjectId ItemId; float3 Position; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CoreUtils.Data.asmdef
│   │   ├── CoreUtils/
│   │   │   ├── Systems/
│   │   │   │   ├── LogDamageSystem.cs              ← Uses BLLogger singleton
│   │   │   │   ├── SumDamageSystem.cs              ← Uses mathex.sum on DynamicBuffer as NativeArray
│   │   │   │   └── RollLootSystem.cs               ← Uses GlobalRandom.NextFloat inside IJobEntity
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CoreUtils.asmdef
│   │   ├── CoreUtils.Authoring/
│   │   │   ├── DamageSourceAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CoreUtils.Authoring.asmdef
│   │   └── CoreUtils.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── MathexTests.cs                      ← Pure math, no ECS
│   │       ├── GlobalRandomTests.cs                ← Verifies no false-sharing across threads
│   │       └── CoreUtils.Tests.asmdef
│   │
│   ├── 04_NamingConventions/
│   │   ├── Scenes/
│   │   │   └── 04_NamingConventions.unity          ← Fully playable combat scenario
│   │   ├── Combat.Data/
│   │   │   ├── Components/
│   │   │   │   ├── Health.cs                       ← { float Current; float Max; }
│   │   │   │   ├── Velocity.cs                     ← { float3 Value; }
│   │   │   │   └── Team.cs                         ← { int Index; }
│   │   │   ├── Tags/
│   │   │   │   ├── Dead.cs                         ← IComponentData (adjective, no "Tag" suffix)
│   │   │   │   ├── Grounded.cs
│   │   │   │   └── Invulnerable.cs                 ← IComponentData, IEnableableComponent
│   │   │   ├── Buffers/
│   │   │   │   └── DamageEvent.cs                  ← Singular noun, IBufferElementData
│   │   │   ├── Aspects/
│   │   │   │   └── HealthAspect.cs                 ← Read-only lens, no mutation logic
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Combat.Data.asmdef
│   │   ├── Combat/
│   │   │   ├── Systems/
│   │   │   │   ├── ApplyDamageSystem.cs            ← [Verb][Subject]System pattern
│   │   │   │   ├── RegenerateHealthSystem.cs
│   │   │   │   └── MarkDeadSystem.cs
│   │   │   ├── Jobs/
│   │   │   │   ├── ApplyDamageJob.cs               ← [Verb][Subject]Job pattern
│   │   │   │   └── RegenerateHealthJob.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Combat.asmdef
│   │   ├── Combat.Authoring/
│   │   │   ├── CombatUnitAuthoring.cs              ← [Concept]Authoring pattern
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Combat.Authoring.asmdef
│   │   └── Combat.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── CombatNamingConventionTests.cs      ← Roslyn analyzer tests for naming rules
│   │       └── Combat.Tests.asmdef
│   │
│   ├── 05_ComponentDesign/
│   │   ├── Scenes/
│   │   │   └── 05_ComponentDesign.unity
│   │   ├── Inventory.Data/
│   │   │   ├── Components/
│   │   │   │   ├── InventoryCapacity.cs            ← GOOD: { int Max; } — pure data
│   │   │   │   └── EquippedWeapon.cs               ← GOOD: { ObjectId WeaponId; }
│   │   │   ├── Buffers/
│   │   │   │   └── InventoryItem.cs                ← GOOD: { int ItemId; int Count; } — singular noun
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Inventory.Data.asmdef
│   │   ├── Inventory/
│   │   │   ├── Systems/
│   │   │   │   ├── ValidateInventorySystem.cs      ← Logic lives here, NOT in the component
│   │   │   │   └── SortInventorySystem.cs          ← Sorting belongs in a system
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Inventory.asmdef
│   │   ├── Inventory.Authoring/
│   │   │   ├── InventoryAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Inventory.Authoring.asmdef
│   │   └── Inventory.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── ComponentBlittabilityTests.cs       ← Verifies all components are unmanaged structs
│   │       └── Inventory.Tests.asmdef
│   │
│   ├── 06_IEnableableComponent/
│   │   ├── Scenes/
│   │   │   └── 06_IEnableableComponent.unity       ← Click to stun/select/make invulnerable units
│   │   ├── UnitStates.Data/
│   │   │   ├── Components/
│   │   │   │   ├── Stunned.cs                      ← IComponentData, IEnableableComponent
│   │   │   │   ├── Selected.cs                     ← IComponentData, IEnableableComponent
│   │   │   │   ├── Invulnerable.cs                 ← IComponentData, IEnableableComponent
│   │   │   │   └── StunRequest.cs                  ← IComponentData { Entity Target; float Duration; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── UnitStates.Data.asmdef
│   │   ├── UnitStates/
│   │   │   ├── Systems/
│   │   │   │   ├── ProcessStunRequestSystem.cs     ← Reads StunRequest, enables Stunned
│   │   │   │   ├── TickStunDurationSystem.cs       ← Decrements timer
│   │   │   │   └── RemoveExpiredStunSystem.cs      ← Disables Stunned when timer hits 0
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── UnitStates.asmdef
│   │   ├── UnitStates.Authoring/
│   │   │   ├── UnitStateAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── UnitStates.Authoring.asmdef
│   │   └── UnitStates.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── StunSystemTests.cs                  ← Verifies IsComponentEnabled, not HasComponent
│   │       ├── ChunkMoveProfilingTests.cs          ← [Performance] confirms zero chunk moves
│   │       └── UnitStates.Tests.asmdef
│   │
│   ├── 07_KSettings/
│   │   ├── Scenes/
│   │   │   └── 07_KSettings.unity                  ← Characters switch animation states via byte IDs
│   │   ├── Assets/
│   │   │   └── CharacterStates.asset               ← KSettings ScriptableObject (idle=0, run=1, atk=2)
│   │   ├── CharacterAnim.Data/
│   │   │   ├── Components/
│   │   │   │   └── CharacterState.cs               ← IComponentData { byte State; }
│   │   │   ├── Settings/
│   │   │   │   └── CharacterStates.cs              ← KSettings<CharacterStates, byte>
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CharacterAnim.Data.asmdef
│   │   ├── CharacterAnim/
│   │   │   ├── Systems/
│   │   │   │   └── TransitionStateSystem.cs        ← Uses CharacterStates.NameToKey() inside Burst
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CharacterAnim.asmdef
│   │   ├── CharacterAnim.Authoring/
│   │   │   ├── CharacterStateAuthoring.cs          ← [K(nameof(CharacterStates))] byte InitialState
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CharacterAnim.Authoring.asmdef
│   │   └── CharacterAnim.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── KSettingsResolutionTests.cs         ← Verifies NameToKey resolves correct byte
│   │       └── CharacterAnim.Tests.asmdef
│   │
│   ├── 08_SettingsSystem/
│   │   ├── Scenes/
│   │   │   └── 08_SettingsSystem.unity             ← Damage numbers change when ScriptableObject changes
│   │   ├── Assets/
│   │   │   ├── CombatSettings.asset                ← SettingsBase ScriptableObject
│   │   │   └── AppConfig.asset                     ← SettingsSingleton ScriptableObject
│   │   ├── GameBalance.Data/
│   │   │   ├── Components/
│   │   │   │   ├── CombatConfig.cs                 ← Baked ECS singleton { float DamageMultiplier; }
│   │   │   │   └── AppConfig.cs                    ← Global singleton (pre-world)
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── GameBalance.Data.asmdef
│   │   ├── GameBalance/
│   │   │   ├── Settings/
│   │   │   │   ├── CombatSettings.cs               ← [SettingsGroup("Combat")][SettingsWorld("Server")]
│   │   │   │   └── AppConfig.cs                    ← SettingsSingleton<AppConfig>
│   │   │   ├── Systems/
│   │   │   │   └── ApplyGlobalDamageMultiplierSystem.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── GameBalance.asmdef
│   │   ├── GameBalance.Authoring/
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── GameBalance.Authoring.asmdef
│   │   └── GameBalance.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── SettingsBakeTests.cs                ← Verifies settings bake into correct singleton
│   │       └── GameBalance.Tests.asmdef
│   │
│   ├── 09_SingletonBuffers/
│   │   ├── Scenes/
│   │   │   ├── 09_SingletonBuffers_Main.unity
│   │   │   ├── 09_Recipes_ModA.unity               ← SubScene: bakes 3 recipes
│   │   │   └── 09_Recipes_ModB.unity               ← SubScene: bakes 5 more recipes
│   │   ├── Crafting.Data/
│   │   │   ├── Components/
│   │   │   │   └── CraftableItem.cs                ← [Singleton] IBufferElementData { int ItemId; int Cost; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Crafting.Data.asmdef
│   │   ├── Crafting/
│   │   │   ├── Systems/
│   │   │   │   ├── BuildRecipeCacheSystem.cs       ← [UpdateInGroup(typeof(SingletonInitializeSystemGroup))]
│   │   │   │   └── ProcessCraftRequestSystem.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Crafting.asmdef
│   │   ├── Crafting.Authoring/
│   │   │   ├── RecipeAuthoring.cs                  ← Each subscene's baker contributes to [Singleton] buffer
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Crafting.Authoring.asmdef
│   │   └── Crafting.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── SingletonMergeTests.cs              ← Verifies ModA + ModB recipes all appear in master buffer
│   │       └── Crafting.Tests.asmdef
│   │
│   ├── 10_ObjectDefinition/
│   │   ├── Scenes/
│   │   │   └── 10_ObjectDefinition.unity           ← Click to spawn enemies by category filter
│   │   ├── Assets/
│   │   │   ├── ObjectManagement/
│   │   │   │   ├── Goblin.asset                    ← ObjectDefinition (category: enemy)
│   │   │   │   ├── Orc.asset                       ← ObjectDefinition (category: enemy, tier2)
│   │   │   │   └── Boss.asset                      ← ObjectDefinition (category: enemy, boss)
│   │   │   └── ObjectGroups/
│   │   │       ├── EnemyGroup.asset
│   │   │       └── EliteEnemyGroup.asset
│   │   ├── EnemySpawner.Data/
│   │   │   ├── Components/
│   │   │   │   ├── SpawnRequest.cs                 ← { ObjectId PrefabId; float3 Position; }
│   │   │   │   └── SpawnedBy.cs                    ← { ObjectId SpawnerId; } — network-safe reference
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── EnemySpawner.Data.asmdef
│   │   ├── EnemySpawner/
│   │   │   ├── Systems/
│   │   │   │   └── SpawnSystem.cs                  ← ObjectDefinitionRegistry O(1) lookup
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── EnemySpawner.asmdef
│   │   ├── EnemySpawner.Authoring/
│   │   │   ├── SpawnerAuthoring.cs                 ← [SearchContext("ca=enemy")] ObjectDefinition field
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── EnemySpawner.Authoring.asmdef
│   │   └── EnemySpawner.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── SpawnSystemTests.cs                 ← Verifies correct prefab instantiated per ObjectId
│   │       ├── ObjectGroupMatcherTests.cs          ← Verifies O(1) group membership check
│   │       └── EnemySpawner.Tests.asmdef
│   │
│   ├── 11_ZeroComplexity/
│   │   ├── Scenes/
│   │   │   └── 11_ZeroComplexity.unity             ← 10,000 units with health, damage, death — all CC=1
│   │   ├── HealthCombat.Data/
│   │   │   ├── Components/
│   │   │   │   ├── Health.cs
│   │   │   │   ├── Regeneration.cs                 ← { float Rate; }
│   │   │   │   └── DamageRequest.cs                ← { Entity Target; float Amount; }
│   │   │   ├── Tags/
│   │   │   │   ├── Dead.cs
│   │   │   │   └── Regenerating.cs                 ← IEnableableComponent
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── HealthCombat.Data.asmdef
│   │   ├── HealthCombat/
│   │   │   ├── Systems/
│   │   │   │   ├── ApplyDamageSystem.cs            ← OnUpdate CC=1: schedules one job
│   │   │   │   ├── RegenerateHealthSystem.cs       ← OnUpdate CC=1: schedules one job
│   │   │   │   └── MarkDeadSystem.cs               ← OnUpdate CC=1: schedules one job
│   │   │   ├── Jobs/
│   │   │   │   ├── ApplyDamageJob.cs               ← IJobEntity, Execute CC=2
│   │   │   │   ├── RegenerateHealthJob.cs          ← IJobEntity, Execute CC=1
│   │   │   │   └── MarkDeadJob.cs                  ← IJobEntity, Execute CC=2
│   │   │   ├── Math/
│   │   │   │   └── HealthMath.cs                   ← static float ClampedRegen(...) — pure, testable
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── HealthCombat.asmdef
│   │   ├── HealthCombat.Authoring/
│   │   │   ├── HealthAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── HealthCombat.Authoring.asmdef
│   │   └── HealthCombat.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── HealthMathTests.cs                  ← Pure function, no ECS, [TestCase] with 6 permutations
│   │       ├── RegenerateHealthSystemTests.cs      ← [TestLeakDetection], ECSTestsFixture
│   │       ├── ApplyDamageSystemTests.cs
│   │       └── HealthCombat.Tests.asmdef
│   │
│   ├── 12_AdvancedJobs/
│   │   ├── Scenes/
│   │   │   └── 12_AdvancedJobs.unity               ← Spatial grid rebuilt every frame, hash map queried
│   │   ├── SpatialPartition.Data/
│   │   │   ├── Components/
│   │   │   │   ├── GridCell.cs                     ← IComponentData { int CellIndex; }
│   │   │   │   └── SpatialQuery.cs                 ← IComponentData { int CellHash; Entity Result; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── SpatialPartition.Data.asmdef
│   │   ├── SpatialPartition/
│   │   │   ├── Systems/
│   │   │   │   ├── BuildSpatialGridSystem.cs       ← Schedules IJobForThread across 4 threads
│   │   │   │   └── QuerySpatialMapSystem.cs        ← Schedules IJobParallelHashMapDefer
│   │   │   ├── Jobs/
│   │   │   │   ├── BuildGridJob.cs                 ← IJobForThread: each thread owns a grid slice
│   │   │   │   └── QueryMapJob.cs                  ← IJobParallelHashMapDefer: parallel HashMap iteration
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── SpatialPartition.asmdef
│   │   ├── SpatialPartition.Authoring/
│   │   │   ├── SpatialGridAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── SpatialPartition.Authoring.asmdef
│   │   └── SpatialPartition.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── BuildGridJobTests.cs                ← [Performance] benchmark vs IJobParallelFor baseline
│   │       ├── QueryMapJobTests.cs
│   │       └── SpatialPartition.Tests.asmdef
│   │
│   ├── 13_DynamicHashMap/
│   │   ├── Scenes/
│   │   │   └── 13_DynamicHashMap.unity             ← Units pick up/drop items, map updates per entity
│   │   ├── InventoryMap.Data/
│   │   │   ├── Buffers/
│   │   │   │   ├── InventoryMap.cs                 ← IDynamicHashMap<int, int> — maps ItemId -> Count
│   │   │   │   └── Blackboard.cs                   ← IDynamicUntypedHashMap<FixedString32Bytes>
│   │   │   ├── Components/
│   │   │   │   ├── PickupRequest.cs                ← { int ItemId; int Count; }
│   │   │   │   └── DropRequest.cs                  ← { int ItemId; int Count; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── InventoryMap.Data.asmdef
│   │   ├── InventoryMap/
│   │   │   ├── Systems/
│   │   │   │   ├── ProcessPickupSystem.cs          ← inventory.AsMap().Add(itemId, count)
│   │   │   │   └── ProcessDropSystem.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── InventoryMap.asmdef
│   │   ├── InventoryMap.Authoring/
│   │   │   ├── InventoryMapAuthoring.cs            ← Baker calls buffer.InitializeHashMap(capacity: 16)
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── InventoryMap.Authoring.asmdef
│   │   └── InventoryMap.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── DynamicHashMapTests.cs              ← [TestLeakDetection], tests Add/TryGetValue/Enumerate
│   │       └── InventoryMap.Tests.asmdef
│   │
│   ├── 14_AdvancedIterators/
│   │   ├── Scenes/
│   │   │   └── 14_AdvancedIterators.unity          ← Proximity detection: find nearest enemy per frame
│   │   ├── Proximity.Data/
│   │   │   ├── Components/
│   │   │   │   ├── NearestEnemy.cs                 ← { Entity Value; float DistanceSq; }
│   │   │   │   └── DetectionRadius.cs              ← { float Value; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Proximity.Data.asmdef
│   │   ├── Proximity/
│   │   │   ├── Systems/
│   │   │   │   ├── ProximityDetectionSystem.cs     ← Uses UnsafeComponentLookup + manual chunk iteration
│   │   │   │   └── ApplyFallbackMapSystem.cs       ← NativeParallelMultiHashMapFallback.Apply()
│   │   │   ├── Iterators/
│   │   │   │   └── ProximityIterator.cs            ← QueryEntityEnumerator + ChunkEntityEnumerator
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Proximity.asmdef
│   │   ├── Proximity.Authoring/
│   │   │   ├── ProximityAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Proximity.Authoring.asmdef
│   │   └── Proximity.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── ProximityDetectionTests.cs          ← [TestLeakDetection], verifies correct nearest entity
│   │       ├── FallbackMapTests.cs                 ← Intentionally overflows map to test fallback path
│   │       └── Proximity.Tests.asmdef
│   │
│   ├── 15_Facets/
│   │   ├── Scenes/
│   │   │   └── 15_Facets.unity                     ← Combat resolution: attack, defense, buffs all in one chunk job
│   │   ├── CombatFacet.Data/
│   │   │   ├── Components/
│   │   │   │   ├── Health.cs
│   │   │   │   ├── DefenseStats.cs                 ← { float Armor; float MagicResist; }
│   │   │   │   └── AttackPower.cs                  ← { float Physical; float Magical; }
│   │   │   ├── Tags/
│   │   │   │   └── Invulnerable.cs                 ← IEnableableComponent
│   │   │   ├── Buffers/
│   │   │   │   └── StatusEffect.cs                 ← IBufferElementData { byte EffectId; float Strength; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CombatFacet.Data.asmdef
│   │   ├── CombatFacet/
│   │   │   ├── Facets/
│   │   │   │   └── CombatFacet.cs                  ← partial struct CombatFacet : IFacet
│   │   │   ├── Systems/
│   │   │   │   └── CombatResolutionSystem.cs       ← Uses CombatFacet.TypeHandle in IJobChunk
│   │   │   ├── Jobs/
│   │   │   │   └── ResolveCombatChunkJob.cs        ← IJobChunk, FacetHandle.Resolve(chunk)[i]
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CombatFacet.asmdef
│   │   ├── CombatFacet.Authoring/
│   │   │   ├── CombatUnitAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── CombatFacet.Authoring.asmdef
│   │   └── CombatFacet.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── CombatFacetLookupTests.cs           ← Verifies TryGet resolves EnabledRefRO correctly
│   │       └── CombatFacet.Tests.asmdef
│   │
│   ├── 16_IEntityCommands/
│   │   ├── Scenes/
│   │   │   └── 16_IEntityCommands.unity            ← Units spawned from Baker AND from runtime job — identical setup
│   │   ├── UnitFactory.Data/
│   │   │   ├── Components/
│   │   │   │   ├── Team.cs
│   │   │   │   ├── Health.cs
│   │   │   │   └── UnitClass.cs                    ← { byte ClassId; }
│   │   │   ├── Tags/
│   │   │   │   └── Stunned.cs                      ← IEnableableComponent
│   │   │   ├── Buffers/
│   │   │   │   └── DamageEvent.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── UnitFactory.Data.asmdef
│   │   ├── UnitFactory/
│   │   │   ├── Factory/
│   │   │   │   └── UnitFactory.cs                  ← static SetupUnit<T>(ref T commands, ...) where T : IEntityCommands
│   │   │   ├── Systems/
│   │   │   │   └── SpawnFromRequestSystem.cs       ← Uses CommandBufferParallelCommands → calls UnitFactory.SetupUnit
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── UnitFactory.asmdef
│   │   ├── UnitFactory.Authoring/
│   │   │   ├── UnitAuthoring.cs                    ← Baker uses BakerCommands → calls UnitFactory.SetupUnit
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── UnitFactory.Authoring.asmdef
│   │   └── UnitFactory.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── UnitFactoryTests.cs                 ← Same SetupUnit called in baking test vs runtime test
│   │       └── UnitFactory.Tests.asmdef
│   │
│   ├── 17_LifecyclePipeline/
│   │   ├── Scenes/
│   │   │   └── 17_LifecyclePipeline.unity          ← Projectiles: fire → init → countdown → auto-destroy
│   │   ├── Projectile.Data/
│   │   │   ├── Components/
│   │   │   │   ├── ProjectileSpeed.cs              ← { float Value; }
│   │   │   │   ├── LifetimeTimer.cs                ← { float Value; } — used with DestroyTimer<T>
│   │   │   │   └── ProjectileOwner.cs              ← { Entity Value; }
│   │   │   ├── Tags/
│   │   │   │   └── Projectile.cs                   ← Tag identifying projectile entities
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Projectile.Data.asmdef
│   │   ├── Projectile/
│   │   │   ├── Systems/
│   │   │   │   ├── FireProjectileSystem.cs         ← Creates SpawnRequest; LifeCycleAuthoring provides InitializeEntity
│   │   │   │   ├── InitializeProjectileSystem.cs  ← [UpdateInGroup(typeof(InitializeSystemGroup))]
│   │   │   │   ├── MoveProjectileSystem.cs
│   │   │   │   └── LifetimeSystem.cs               ← DestroyTimer<LifetimeTimer> auto-enables DestroyEntity
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Projectile.asmdef
│   │   ├── Projectile.Authoring/
│   │   │   ├── ProjectileAuthoring.cs              ← Includes LifeCycleAuthoring bake
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Projectile.Authoring.asmdef
│   │   └── Projectile.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── ProjectileLifecycleTests.cs         ← Verifies init runs once, destroy runs on timer expiry
│   │       ├── LifetimeTimerTests.cs               ← Pure: DestroyTimer decrements correctly
│   │       └── Projectile.Tests.asmdef
│   │
│   ├── 18_SubSceneManagement/
│   │   ├── Scenes/
│   │   │   ├── 18_SubSceneManagement_Main.unity    ← Bootstrap, drives SubSceneSettings
│   │   │   ├── 18_Level_SharedGeometry.unity       ← SubScene: loads into all worlds
│   │   │   ├── 18_Level_ServerLogic.unity          ← SubScene: [SettingsWorld("Server")] only
│   │   │   ├── 18_Level_ClientVisuals.unity        ← SubScene: [SettingsWorld("Client")] only
│   │   │   └── 18_Level_ServiceUI.unity            ← SubScene: loads into ServiceWorld
│   │   ├── Assets/
│   │   │   ├── SubSceneSets/
│   │   │   │   ├── SharedSubSceneSet.asset
│   │   │   │   ├── ServerSubSceneSet.asset
│   │   │   │   ├── ClientSubSceneSet.asset
│   │   │   │   └── ServiceSubSceneSet.asset
│   │   │   └── SubSceneSettings.asset              ← Maps each set to its WorldFlags
│   │   ├── LevelLoader.Data/
│   │   │   ├── Components/
│   │   │   │   └── NextLevelTag.cs                 ← Tag on the portal entity
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── LevelLoader.Data.asmdef
│   │   ├── LevelLoader/
│   │   │   ├── Systems/
│   │   │   │   └── PortalSystem.cs                 ← Reads SubSceneLoaded, enables LoadSubScene
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── LevelLoader.asmdef
│   │   ├── LevelLoader.Authoring/
│   │   │   ├── PortalAuthoring.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── LevelLoader.Authoring.asmdef
│   │   └── LevelLoader.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── SubSceneWorldTargetingTests.cs      ← Verifies Server scenes don't load in Client world
│   │       └── LevelLoader.Tests.asmdef
│   │
│   ├── 19_PhysicsStates/
│   │   ├── Scenes/
│   │   │   └── 19_PhysicsStates.unity              ← Spike traps deal damage only on Enter, drain health on Stay
│   │   ├── Traps.Data/
│   │   │   ├── Components/
│   │   │   │   ├── SpikeTrap.cs                    ← IComponentData { float EnterDamage; float StayDamage; }
│   │   │   │   └── Health.cs
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Traps.Data.asmdef
│   │   ├── Traps/
│   │   │   ├── Systems/
│   │   │   │   └── ProcessSpikesSystem.cs          ← [UpdateAfter(StatefulCollisionEventSystem)]
│   │   │   ├── Jobs/
│   │   │   │   └── ProcessSpikeDamageJob.cs        ← Reads DynamicBuffer<StatefulCollisionEvent>
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Traps.asmdef
│   │   ├── Traps.Authoring/
│   │   │   ├── SpikeTrapAuthoring.cs               ← Adds StatefulCollisionEventAuthoring + SpikeTrap
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Traps.Authoring.asmdef
│   │   └── Traps.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── SpikeTrapTests.cs                   ← Verifies Enter fires once, Stay fires each frame, Exit=0 damage
│   │       └── Traps.Tests.asmdef
│   │
│   ├── 20_NetCodeRelevancy/
│   │   ├── Scenes/
│   │   │   ├── 20_NetCode_Server.unity
│   │   │   └── 20_NetCode_Client.unity
│   │   ├── Assets/
│   │   │   └── RelevanceConfig.asset               ← BovineLabs relevancy settings
│   │   ├── Relevancy.Data/
│   │   │   ├── Components/
│   │   │   │   ├── PlayerBounds.cs                 ← Wraps InputBounds; follows camera AABB
│   │   │   │   └── GhostPriority.cs                ← { bool AlwaysRelevant; }
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Relevancy.Data.asmdef
│   │   ├── Relevancy/
│   │   │   ├── Systems/
│   │   │   │   └── UpdatePlayerBoundsSystem.cs     ← Syncs camera AABB to InputBounds component
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Relevancy.asmdef
│   │   ├── Relevancy.Authoring/
│   │   │   ├── RelevancyAuthoring.cs               ← Bakes RelevanceAlways or RelevanceManual
│   │   │   ├── AssemblyInfo.cs
│   │   │   └── Relevancy.Authoring.asmdef
│   │   └── Relevancy.Tests/
│   │       ├── AssemblyInfo.cs
│   │       ├── RelevancyTests.cs                   ← Verifies out-of-bounds ghosts are not serialized
│   │       └── Relevancy.Tests.asmdef
│   │
│   └── 21_PauseSystem/
│       ├── Scenes/
│       │   └── 21_PauseSystem.unity                ← Press Escape: gameplay freezes, UI panel stays live
│       ├── Pause.Data/
│       │   ├── Components/
│       │   │   └── PauseMenuVisible.cs             ← IComponentData — drives UI state
│       │   ├── AssemblyInfo.cs
│       │   └── Pause.Data.asmdef
│       ├── Pause/
│       │   ├── Systems/
│       │   │   ├── PauseInputSystem.cs             ← Reads Escape key, calls PauseGame.Pause/Unpause
│       │   │   ├── GameplaySystem.cs               ← Normal ISystem — stops when paused (no marker)
│       │   │   └── PauseMenuRenderSystem.cs        ← ISystem, IUpdateWhilePaused — keeps running
│       │   ├── AssemblyInfo.cs
│       │   └── Pause.asmdef
│       ├── Pause.Authoring/
│       │   ├── PauseAuthoring.cs
│       │   ├── AssemblyInfo.cs
│       │   └── Pause.Authoring.asmdef
│       └── Pause.Tests/
│           ├── AssemblyInfo.cs
│           ├── PauseSystemTests.cs                 ← Verifies GameplaySystem.OnUpdate NOT called when paused
│           ├── PauseMenuRenderSystemTests.cs       ← Verifies PauseMenuRenderSystem IS called when paused
│           ├── NoCatchUpTicksTests.cs              ← Verifies ElapsedTime frozen — zero catch-up frames
│           └── Pause.Tests.asmdef
│
├── Scenes/
│   └── SampleScene.unity                          ← Original — left untouched
│
├── Settings/                                      ← Original URP settings — untouched
│   ├── DefaultVolumeProfile.asset
│   ├── Mobile_Renderer.asset
│   ├── Mobile_RPAsset.asset
│   ├── PC_Renderer.asset
│   ├── PC_RPAsset.asset
│   ├── SampleSceneProfile.asset
│   └── UniversalRenderPipelineGlobalSettings.asset
│
└── InputSystem_Actions.inputactions
```

---

## Assembly Cross-Reference

Every `Feature.Data.asmdef` is completely identical in structure. For your reference:

```
Feature.Data.asmdef
  autoReferenced: false
  defineConstraints: []          ← ships in ALL builds

Feature.asmdef
  autoReferenced: false
  defineConstraints: []          ← ships in ALL builds
  refs: Feature.Data

Feature.Authoring.asmdef
  autoReferenced: false
  defineConstraints: [UNITY_EDITOR]   ← NEVER in player build
  refs: Feature.Data

Feature.Debug.asmdef
  autoReferenced: false
  defineConstraints: [UNITY_EDITOR || BL_DEBUG]
  refs: Feature, Feature.Data

Feature.Editor.asmdef
  autoReferenced: false
  includePlatforms: [Editor]
  refs: Feature, Feature.Authoring, Feature.Data

Feature.Tests.asmdef
  autoReferenced: false
  includePlatforms: [Editor]
  optionalUnityReferences: [TestAssemblies]
  refs: Feature, Feature.Data, BovineLabs.Testing
  AssemblyInfo.cs: [assembly: DisableAutoCreation]
```

---

## How To Use This Structure

```
1. Open any numbered scene to run that topic's demo in Play Mode.

2. Read the scripts in the matching folder — they are the
   practical implementation of exactly what the README describes.

3. Run the tests for that topic via the Unity Test Runner
   (Window -> General -> Test Runner -> EditMode).

4. When adding a new feature to the real project, copy the folder
   of the closest matching topic and rename it.
   The asmdef wiring is already correct.
```

---

## File Count Summary

| Layer | Files Per Topic | Notes |
|-------|----------------|-------|
| `.Data` components | 2–4 `.cs` + 1 `.asmdef` + 1 `AssemblyInfo.cs` | Pure data only |
| Runtime systems | 2–4 `.cs` + 1 `.asmdef` + 1 `AssemblyInfo.cs` | ISystem + Jobs |
| `.Authoring` bakers | 1–3 `.cs` + 1 `.asmdef` + 1 `AssemblyInfo.cs` | Editor-only |
| `.Tests` | 2–4 `.cs` + 1 `.asmdef` + 1 `AssemblyInfo.cs` | [DisableAutoCreation] |
| Scenes | 1–4 `.unity` per topic | |
| Assets | 0–5 `.asset` per topic | SOs, prefabs, configs |
| **Total per topic** | **~20–30 files** | |
| **Total project** | **~500–600 files** | All 21 topics |
