# Unity ECS Examples

## What Each Branch Teaches You At A Glance

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
