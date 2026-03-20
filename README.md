# BovineLabs Core Unity ECS Elite Standards

> **Philosophy:** Zero cyclomatic complexity systems, strict zero-allocation hot paths, strict Burst ABI compliance, and mathematical proof of stability through rigorous testing. Every snippet must compile, run in parallel, and leave zero memory leaks.

## Part 1: Advanced High-Performance Collections & Allocators

### 1. `NativeThreadStream`: Block-Based Thread-Safe Streaming
**Standard:** Never pre-allocate massive arrays "just in case." Use `NativeThreadStream` for highly parallel producers creating an unknown amount of sequential data.
**Test Standard:** Must use `[TestLeakDetection]` and strictly balance `BeginForEachIndex` with `EndForEachIndex`.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile(CompileSynchronously = true)]
public struct ProduceStreamJob : IJobFor
{
    public NativeThreadStream.Writer Writer;

    public void Execute(int index)
    {
        // Zero-allocation, block-based expansion
        this.Writer.Write(index);
        this.Writer.Write(index * 2);
    }
}

public static class NativeThreadStreamExample
{
    public static void Execute(ref SystemState state)
    {
        var stream = new NativeThreadStream(Allocator.TempJob);
        
        state.Dependency = new ProduceStreamJob 
        { 
            Writer = stream.AsWriter() 
        }.ScheduleParallel(100, 16, state.Dependency);

        state.Dependency = stream.Dispose(state.Dependency);
    }
}
```

### 2. `DynamicHashMap`: Entity Buffer Embedded Hash Map
**Standard:** Do not attach `NativeHashMap` as a system state component. Reinterpret `DynamicBuffer<byte>` as `DynamicHashMap<TKey, TValue>` for zero-overhead, entity-bound mapping.

```csharp
using System;
using BovineLabs.Core.Iterators;
using Unity.Burst;
using Unity.Entities;

// 1. Declaration
[InternalBufferCapacity(0)]
public struct InventoryMap : IDynamicHashMap<int, int>
{
    byte IDynamicHashMap<int, int>.Value { get; }
}

[BurstCompile]
public partial struct InventorySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var buffer in SystemAPI.Query<DynamicBuffer<InventoryMap>>())
        {
            // O(1) casting, fully Burst compatible
            var map = buffer.AsHashMap<InventoryMap, int, int>();
            
            // AddOrSet handles internal resizing seamlessly
            map.GetOrAddRef(101, out bool added, 1) += added ? 0 : 1;
        }
    }
}
```

### 3. `DynamicMultiHashMap`: Entity Buffer Embedded Multi-Value Hash Map
**Standard:** When an entity needs to map one key to multiple values (e.g., Spatial grid cells to occupants), use `DynamicMultiHashMap`.

```csharp
using System;
using BovineLabs.Core.Iterators;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
[InternalBufferCapacity(0)]
public struct SpatialMultiMap : IDynamicMultiHashMap<int2, Entity>
{
    byte IDynamicMultiHashMap<int2, Entity>.Value { get; }
}

[BurstCompile]
public partial struct SpatialSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var buffer in SystemAPI.Query<DynamicBuffer<SpatialMultiMap>>())
        {
            var map = buffer.AsMultiHashMap<SpatialMultiMap, int2, Entity>();
            var key = new int2(10, 15);
            
            var enumerator = map.GetValuesForKey(key);
            while (enumerator.MoveNext())
            {
                Entity occupant = enumerator.Current;
            }
        }
    }
}
```

### 4. `DynamicHashSet`: Entity Buffer Embedded Hash Set
**Standard:** Use for unique flagging (e.g., "Has Entity Interacted With X"). Lookups are instant, completely contained inside the chunk memory if small enough.

```csharp
using System;
using BovineLabs.Core.Iterators;
using Unity.Burst;
using Unity.Entities;

[InternalBufferCapacity(0)]
public struct InteractedSet : IDynamicHashSet<Entity>
{
    byte IDynamicHashSet<Entity>.Value { get; }
}

[BurstCompile]
public partial struct InteractionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var buffer in SystemAPI.Query<DynamicBuffer<InteractedSet>>())
        {
            var set = buffer.AsHashSet<InteractedSet, Entity>();
            if (set.Add(Entity.Null)) // Returns true if added
            {
                // First time interaction logic
            }
        }
    }
}
```

### 5. `DynamicUntypedBuffer`: Mixed Unmanaged Types in a Single Buffer
**Standard:** Used exclusively when serializing variable-sized packet data into entities. Eliminates creating 20 different buffers for 20 different data types.

```csharp
using BovineLabs.Core.Iterators;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
[InternalBufferCapacity(0)]
public struct PacketBuffer : IDynamicUntypedBuffer
{
    byte IDynamicUntypedBuffer.Value { get; }
}

[BurstCompile]
public partial struct NetworkDeserializeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var buffer in SystemAPI.Query<DynamicBuffer<PacketBuffer>>())
        {
            var untyped = buffer.AsUntypedBuffer();
            untyped.Add(10); // int
            untyped.Add(new float3(0, 1, 0)); // float3
            
            int readInt = untyped.ElementAtRO<int>(0);
            float3 readFloat3 = untyped.ElementAtRO<float3>(1);
        }
    }
}
```

### 6. `DynamicVariableMap`: Keys to Multiple Variable Columns
**Standard:** Used for relational databases embedded inside an entity. Useful for complex inventories (Key: ItemID, Value: Count, Column1: Durability, Column2: CustomData).

```csharp
using System;
using BovineLabs.Core.Iterators;
using BovineLabs.Core.Iterators.Columns;
using Unity.Burst;
using Unity.Entities;
[InternalBufferCapacity(0)]
public struct AdvancedInventory : IDynamicVariableMap<int, int, short, MultiHashColumn<short>>
{
    byte IDynamicVariableMap<int, int, short, MultiHashColumn<short>>.Value { get; }
}

[BurstCompile]
public partial struct AdvancedInventorySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var buffer in SystemAPI.Query<DynamicBuffer<AdvancedInventory>>())
        {
            var map = buffer.AsVariableMap<AdvancedInventory, int, int, short, MultiHashColumn<short>>();
            map.TryAdd(key: 99, item: 1 /* Count */, column1: 100 /* Durability */);
        }
    }
}
```

### 7. `BlobHashMap`: Read-Only Hash Maps in `BlobAssetReference`
**Standard:** Gameplay configurations (e.g., Item ID to Item Stats) must NEVER be stored in `NativeHashMap`s. They must be baked into a `BlobHashMap` via a Baker for instant, zero-deserialization load times.

```csharp
using System;
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct ItemStats
{
    public float Damage;
}

public struct ConfigBlob
{
    public BlobHashMap<int, ItemStats> ItemMap;
}

[BurstCompile]
public static class BlobConfigBaker
{
    public static BlobAssetReference<ConfigBlob> Bake(NativeHashMap<int, ItemStats> src)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<ConfigBlob>();
        
        builder.ConstructHashMap(ref root.ItemMap, ref src);
        
        var result = builder.CreateBlobAssetReference<ConfigBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }
}
```

### 8. `BlobPerfectHashMap`: Zero-Collision Hash Maps in Blob Assets
**Standard:** When iterating constant, read-only data where hash collisions are unacceptable (causing loop branches), calculate the perfect hash size at bake time to ensure guaranteed O(1) lookups.

```csharp
using System;
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct PerfectConfigBlob
{
    public BlobPerfectHashMap<int, int> Map;
}

[BurstCompile]
public static class PerfectBlobBaker
{
    public static BlobAssetReference<PerfectConfigBlob> Bake(NativeHashMap<int, int> src)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<PerfectConfigBlob>();
        
        // Null value (-1) is used to identify empty slots mathematically
        builder.ConstructPerfectHashMap(ref root.Map, src, -1);
        
        var result = builder.CreateBlobAssetReference<PerfectConfigBlob>(Allocator.Persistent);
        builder.Dispose();
        return result;
    }
}
```

### 9. `BlobCurve`: Unity `AnimationCurve`s Baked into BlobAssets
**Standard:** `UnityEngine.AnimationCurve` is a managed object and absolutely banned in Burst. Use `BlobCurve.Create` in the Baker to convert them to `BlobCurveSampler` for pure mathematical evaluation in jobs.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public struct CurveComponent : IComponentData
{
    public BlobAssetReference<BlobCurve> Curve;
}

[BurstCompile]
public partial struct EvaluateCurveJob : IJobEntity
{
    public float CurrentTime;

    private void Execute(in CurveComponent curveComp)
    {
        // Fast, cached, Burst-compatible evaluation
        var sampler = new BlobCurveSampler<float>(curveComp.Curve);
        float value = sampler.Evaluate(this.CurrentTime);
    }
}
```

### 10. `BlobBuilderExtensions`: Complex Collection Allocation
**Standard:** Prevent boilerplate and manual memory math when allocating nested blobs. Always use `BlobBuilderExtensions.Construct` for array and list duplications within Blob storage.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct NestedBlobData
{
    public BlobArray<int> DataList;
}

public static class BlobBuilderStandard
{
    public static void Build(NativeList<int> tempList)
    {
        var builder = new BlobBuilder(Allocator.Temp);
        ref var root = ref builder.ConstructRoot<NestedBlobData>();
        
        // Single-line construction handling memory alignment and copying
        builder.Construct(ref root.DataList, in tempList);
        builder.Dispose();
    }
}
```

### 11. `PooledNativeList`: TLS-Based Memory Pools
**Standard:** Creating a `NativeList` with `Allocator.Temp` still touches the main thread's memory allocator. In extremely hot loops, use `PooledNativeList<T>.Make()` to reuse pre-allocated lists bound to Thread Local Storage (TLS).

```csharp
using BovineLabs.Core.Utility;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct PooledListJob : IJobEntity
{
    private void Execute()
    {
        // Pulls instantly from Thread Local Storage. Zero alloc.
        using var pooledList = PooledNativeList<int>.Make();
        
        pooledList.List.Add(42);
        // Disposed automatically, returning to TLS pool
    }
}
```

### 12. `NativeCounter`: Burst-Compatible Thread-Safe Counter
**Standard:** Replaces `NativeArray<int>` of length 1. Provides `ParallelWriter` for `Interlocked.Increment` avoiding cache-line tearing and false sharing.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile]
public struct CountJob : IJobFor
{
    public NativeCounter.ParallelWriter Counter;

    public void Execute(int index)
    {
        this.Counter.Increment();
    }
}

public static class CounterStandard
{
    public static void Execute(ref SystemState state)
    {
        var counter = new NativeCounter(Allocator.TempJob);
        
        state.Dependency = new CountJob { Counter = counter.AsParallelWriter() }
            .ScheduleParallel(100, 32, state.Dependency);
            
        state.Dependency = counter.Dispose(state.Dependency);
    }
}
```

### 13. `NativeKeyedMap`: Fast Hash Map Targeting Integer Keys
**Standard:** When keys are exclusively integers (e.g., Entity Indices, Network IDs), standard hashing overhead is wasted. `NativeKeyedMap` uses absolute modulo mapping for massive performance gains.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public partial struct KeyedMapSystem : ISystem
{[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 100 capacity, max key of 500
        var map = new NativeKeyedMap<float>(100, 500, Allocator.Temp);
        
        map.Add(key: 42, item: 3.14f);
        if (map.TryGetFirstValue(42, out var val, out var iterator))
        {
            // Use val
        }
        
        map.Dispose();
    }
}
```

### 14. `NativeLinearCongruentialGenerator`: Fast Procedural Random in Burst
**Standard:** `Unity.Mathematics.Random` holds complex state. For mass procedural generation (e.g., millions of particles), use `NativeLinearCongruentialGenerator` as it guarantees purely deterministic sequence generation with mathematical minimum overhead.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct GeneratorJob : IJob
{
    public void Execute()
    {
        var lcg = new NativeLinearCongruentialGenerator(seed: 123456, Allocator.Temp);
        for (var i = 0; i < 1000; i++)
        {
            int pseudoRandomValue = lcg.Next();
        }
        lcg.Dispose();
    }
}
```

### 15. `NativeParallelMultiHashMapFallback`: Fallback Queue
**Standard:** In parallel jobs, `NativeParallelMultiHashMap` throws an exception if capacity is exceeded. Using `NativeParallelMultiHashMapFallback` automatically shunts overflows to a `NativeQueue` to prevent crashes without requiring a massive pre-allocation.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile]
public partial struct SafeMapSystem : ISystem
{[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // If we exceed 1000, it safely queues overflows.
        var fallbackMap = new NativeParallelMultiHashMapFallback<int, float>(1000, Allocator.TempJob);
        
        // Pass fallbackMap.AsWriter() into your parallel jobs here...
        
        // Apply re-integrates the queued overflows back into the map safely on the main thread
        state.Dependency = fallbackMap.Apply(state.Dependency, out var reader);
        
        state.Dependency = fallbackMap.Dispose(state.Dependency);
    }
}
```

### 16. `NativePartialKeyedMap`: Constrained Integer Map
**Standard:** Used internally for spatial hashing and mesh processing where the exact array size of keys and bucket capacities are known ahead of time.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public unsafe struct PartialMapJob : IJob
{
    public void Execute()
    {
        int* keys = stackalloc int[10];
        float* values = stackalloc float[10];
        
        var map = new NativePartialKeyedMap<float>(keys, values, length: 10, bucketCapacity: 16, Allocator.Temp);
        
        if (map.TryGetFirstValue(5, out var val, out var iterator)) { }
        
        map.Dispose();
    }
}
```

### 17. `ThreadList`: Thread-Local Storage for Temporary Lists
**Standard:** When parallel jobs need to gather variable-sized data without locks, allocate a `ThreadList`. This binds one `UnsafeList<byte>` per Unity Job Worker thread.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct GatherDataJob : IJobParallelFor
{
    public ThreadList ThreadLocalLists;

    public void Execute(int index)
    {
        // Fetches the specific list assigned to the executing thread, guaranteeing no race conditions
        ref var myThreadList = ref this.ThreadLocalLists.GetList();
        myThreadList.Add((byte)index);
    }
}
```

### 18. `ThreadRandom`: Thread-Local Random Generators
**Standard:** Multiple threads accessing `Unity.Mathematics.Random` will cause false sharing and cache-line misses. `ThreadRandom` aligns state by `JobsUtility.CacheLineSize` to ensure perfect parallel random generation.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
[BurstCompile]
public partial struct ThreadRandomSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var tr = new ThreadRandom(seed: 1337, Allocator.TempJob);
        
        // Pass `tr` into a parallel job, and inside the job call:
        // ref var random = ref tr.GetRandomRef();
        // random.NextFloat();
        
        tr.Dispose();
    }
}
```

### 19. `UnmanagedPool`: Blazing Fast Unmanaged Object Pooling
**Standard:** For objects stored on the unmanaged heap (e.g., structs, nested arrays), use `UnmanagedPool<T>` paired with a `SpinLock` to manage a pre-allocated buffer of reusable elements instead of `new` allocations.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct PoolJob : IJob
{
    public void Execute()
    {
        var pool = new UnmanagedPool<int>(capacity: 64, Allocator.Temp);
        
        pool.TryAdd(42);
        if (pool.TryGet(out int val))
        {
            // Use val
        }
        
        pool.Dispose();
    }
}
```

### 20. `UnsafeArray`: Bypassing NativeArray Structural Overhead
**Standard:** `NativeArray` contains safety checks, memory bounds, and allocator labels. In extreme micro-optimizations inside internal structures, use `UnsafeArray<T>` to act as a raw pointer wrapper with C#-like indexing.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct UnsafeArrayJob : IJob
{
    public void Execute()
    {
        // Bypasses NativeArray safety handles for raw pointer speed
        var unsafeArray = new UnsafeArray<int>(100, Allocator.Temp);
        
        unsafeArray[0] = 99;
        
        unsafeArray.Dispose();
    }
}
```

## Part 2: Advanced Memory & Low-Level ECS Extensions

### 21. `UnsafeSlabAllocator`: Block-Based Chunk Allocations
**Standard:** Avoid fragmented heap allocations when creating thousands of temporary unmanaged objects. `UnsafeSlabAllocator` grabs large continuous slabs of memory and doles out pointers sequentially, destroying them all at once upon disposal.

```csharp
using BovineLabs.Core.Memory;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public unsafe struct SlabAllocationJob : IJob
{
    public void Execute()
    {
        // Allocates blocks of 1024 ints at a time
        var allocator = new UnsafeSlabAllocator<int>(1024, Allocator.Temp);
        
        // Fast sequential pointer allocation
        int* val1 = allocator.Alloc();
        *val1 = 42;

        allocator.Dispose(); // Clears all slabs instantly
    }
}
```

### 22. `BitArray256`: SIMD-Optimized Unmanaged Bitmasks
**Standard:** Never use `bool` arrays or multiple `bool` variables to represent states. Use `BitArray8` through `BitArray256`. They map directly to `v128` SIMD registers, allowing processing of 256 boolean states in a few CPU cycles.

```csharp
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Entities;

public struct StatusEffects : IComponentData
{
    public BitArray256 ActiveEffects;
}

[BurstCompile]
public partial struct StatusEffectSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var status in SystemAPI.Query<RefRW<StatusEffects>>())
        {
            // O(1) bitwise assignment, fully Burst vectorized
            status.ValueRW.ActiveEffects[45] = true;
            
            // Check if ANY effect is active across all 256 slots instantly
            if (!status.ValueRO.ActiveEffects.AllFalse) { }
        }
    }
}
```

### 23. `FixedArray`: Struct Wrappers for Fixed-Size Arrays
**Standard:** C# `unsafe fixed` buffers restrict types to primitive scalars. `FixedArray<T, TSize>` allows unmanaged arrays of *any* unmanaged type directly inside a struct, completely bypassing heap allocation.

```csharp
using System.Runtime.InteropServices;
using BovineLabs.Core.Collections;
using Unity.Entities;
using Unity.Mathematics;
[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct Bytes64 { }

public struct PathData : IComponentData
{
    // A guaranteed 64-byte inline array of float2 (stores exactly 8 float2s)
    public FixedArray<float2, Bytes64> Waypoints;
}
```

### 24. `MemoryLabelAllocator`: Profiled Custom Allocators
**Standard:** Anonymous `Allocator.Persistent` memory leaks are impossible to track. Route persistent memory through `MemoryLabelAllocator` to explicitly tag memory blocks in the Unity Profiler.

```csharp
using BovineLabs.Core.Memory;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public unsafe class LabeledMemoryExample
{
    public static void Execute()
    {
        var allocator = new MemoryLabelAllocator();
        allocator.Initialize("BovineLabs", "PathfindingGrid");

        // Memory appears explicitly as "BovineLabs/PathfindingGrid" in the profiler
        var grid = (int*)Memory.Unmanaged.Allocate(1024, 16, allocator.Handle);
        
        Memory.Unmanaged.Free(grid, allocator.Handle);
        allocator.Dispose();
    }
}
```

### 25. `AabbExtensions`: Fast Mathematical Bounding Boxes
**Standard:** Unity's built-in `Bounds` is a managed type. Use `Unity.Physics.Aabb` combined with `AabbExtensions` for strictly mathematical, Burst-compiled bounding box manipulation.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Physics;

[BurstCompile]
public struct BoundingBoxJob : Unity.Jobs.IJob
{
    public void Execute()
    {
        Aabb aabb = new Aabb();
        
        // Zero-allocation mathematical expansion
        aabb.ExpandX(5f);
        aabb.ShrinkSafe(2f);
    }
}
```

### 26. `ArchetypeChunk.DidChange`: Bypassing False Dependencies
**Standard:** `SystemAPI.Query` change filters check if a chunk *might* have changed. `ArchetypeChunk.DidChange` safely checks exact component versions manually to avoid false positives without triggering Unity's safety system locks.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct ManualChangeCheckSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var typeIndex = TypeManager.GetTypeIndex<LocalTransform>();
        short cache = 0;
        uint version = state.LastSystemVersion;

        foreach (var chunk in SystemAPI.Query<//...>())
        {
            if (chunk.DidChange(typeIndex, ref cache, version))
            {
                // Exact chunk modification detected
            }
        }
    }
}
```

### 27. `ArchetypeChunk.GetNativeArrayReadOnly`: Bypass Safety Overhead
**Standard:** Standard `GetNativeArray` forces dependency checks. `GetNativeArrayReadOnly` forces a strictly read-only view directly from chunk memory, providing maximum performance when mathematically certain of thread safety.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public unsafe partial struct ReadOnlyChunkJob : IJobChunk
{
    public ComponentTypeHandle<LocalTransform> TransformHandle;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
    {
        // Unsafe fast path, avoids triggering write change filters
        var transforms = chunk.GetNativeArrayReadOnly(ref this.TransformHandle);
    }
}
```

### 28. `ArchetypeChunk.GetDynamicBufferAccessor`: Raw Buffer Retrieval
**Standard:** Unity's API limits how untyped buffers can be pulled from chunks. Use `GetDynamicBufferAccessor` via `DynamicComponentTypeHandle` for modular, type-agnostic buffer iterations.

```csharp
using BovineLabs.Core.Collections;
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public struct RawBufferJob : IJobChunk
{
    public DynamicComponentTypeHandle BufferHandle;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
    {
        DynamicBufferAccessor accessor = chunk.GetDynamicBufferAccessor(ref this.BufferHandle);
        UntypedDynamicBuffer untyped = accessor.GetUntypedBuffer(0);
    }
}
```

### 29. `BufferAccessor.GetUnsafe`: Safety Bypass for Buffer Reads
**Standard:** When iterating heavily nested or multi-threaded buffers where the safety system incorrectly assumes a race condition, use `GetUnsafe<T>` to strip atomic safety handles completely. *(Requires expert knowledge of your access patterns).*

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public struct UnsafeBufferJob : IJobChunk
{
    public BufferTypeHandle<MyElement> BufferHandle;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
    {
        var accessor = chunk.GetBufferAccessor(ref this.BufferHandle);
        
        // Bypasses internal Unity AtomicSafetyHandle throws for highly specialized parallel jobs
        DynamicBuffer<MyElement> unsafeBuffer = accessor.GetUnsafe(0);
    }
}
```

### 30. `BufferLookup.GetROAndChunk`: Simultaneous Retrieval
**Standard:** Calling `GetBuffer` and then trying to find the entity's chunk index requires two separate lookups into `EntityComponentStore`. `GetROAndChunk` fetches both in a single memory access.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct LookupOptimizationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var lookup = SystemAPI.GetBufferLookup<MyElement>(true);
        Entity target = SystemAPI.GetSingletonEntity<MySingleton>();

        // Fetches buffer and chunk data in one operation
        var (buffer, chunkIndex) = lookup.GetROAndChunk(target);
    }
}
```

### 31. `ComponentLookup.GetOptionalComponentDataRW`: Branchless Missing Components
**Standard:** Using `HasComponent` followed by `GetComponentRW` searches the `EntityComponentStore` twice. `GetOptionalComponentDataRW` returns a raw unmanaged pointer that is seamlessly `null` if the component doesn't exist.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public unsafe partial struct OptionalLookupSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var lookup = SystemAPI.GetComponentLookup<LocalTransform>(false);
        Entity target = SystemAPI.GetSingletonEntity<MySingleton>();

        // 1 Lookup. Zero Branches.
        LocalTransform* ptr = lookup.GetOptionalComponentDataRW(target);
        if (ptr != null)
        {
            ptr->Position.y += 1f;
        }
    }
}
```

### 32. `ComponentLookup.SetChangeFilter`: Manual Change Marking
**Standard:** Modifying data using a raw pointer bypasses Unity's change filter tracking. Use `SetChangeFilter` to manually flag the chunk as modified so downstream systems register the change.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
[BurstCompile]
public unsafe partial struct ManualFilterSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var lookup = SystemAPI.GetComponentLookup<LocalTransform>(false);
        Entity target = SystemAPI.GetSingletonEntity<MySingleton>();

        LocalTransform* ptr = lookup.GetOptionalComponentDataRW(target);
        if (ptr != null)
        {
            ptr->Position.x += 5f;
            
            // Manually inform the ECS system that this specific chunk has mutated
            lookup.SetChangeFilter(target);
        }
    }
}
```

### 33. `EntityQueryBuilder.WithAllRW`: Query Building Shorthand
**Standard:** Minimizes boilerplate. Do not manually create `FixedList32Bytes` arrays with `AccessMode.ReadWrite`. Use the `WithAllRW` extension.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial struct BuildQuerySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Fluent, clean, and highly readable query building
        var query = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW(ComponentType.ReadWrite<LocalTransform>())
            .WithAll(ComponentType.ReadOnly<LocalToWorld>())
            .Build(ref state);
    }
}
```

### 34. `EntityQuery.QueryHasSharedFilter`: Identify Shared Filters
**Standard:** When passing queries to generic utility methods, quickly determine if a specific `ISharedComponentData` is actively filtering the query without parsing the full query description array.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Entities;
using Unity.Rendering;

public static class QueryUtility
{
    public static void ValidateMaterial(EntityQuery query)
    {
        // Instantly checks internal EntityQueryImpl data structures
        if (query.QueryHasSharedFilter<MaterialMeshInfo>(out int sharedIndex))
        {
            // Filter is present, sharedIndex holds the specific filter value
        }
    }
}
```

### 35. `EntityQuery.ReplaceSharedComponentFilter`: Instant Filter Swapping
**Standard:** Rebuilding an `EntityQuery` to change a shared component filter causes GC allocation. `ReplaceSharedComponentFilter` alters the unmanaged C++ query backend directly in O(1) time.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Entities;
using Unity.Rendering;

public partial class SwapFilterSystem : SystemBase
{
    private EntityQuery query;

    protected override void OnCreate()
    {
        this.query = GetEntityQuery(typeof(MaterialMeshInfo));
        this.query.SetSharedComponentFilter(new MaterialMeshInfo { Material = 1 });
    }

    protected override void OnUpdate()
    {
        // Instantly swaps the filter on the existing query without allocations
        this.query.ReplaceSharedComponentFilter(0, new MaterialMeshInfo { Material = 2 });
    }
}
```

### 36. `EntityQuery.GetFirstEntity`: Unsafe Fast Path
**Standard:** When a query is guaranteed to have at least one entity, `GetFirstEntity` bypasses chunk iteration loops and directly accesses the first matched entity in memory.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Entities;

public partial struct FastFirstSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<MySingleton>().Build();
        
        // Immediately grabs the entity pointer.
        // Will throw in [ENABLE_UNITY_COLLECTIONS_CHECKS] if empty.
        Entity firstEntity = query.GetFirstEntity();
    }
}
```

### 37. `EntityQuery.GetSingletonBufferNoSync`: Bypass Dependency Sync
**Standard:** `GetSingletonBuffer` forces a hard thread synchronization to ensure jobs writing to the buffer have finished. If you *know* the buffer is ready (e.g. read-only config data), use `GetSingletonBufferNoSync` to prevent thread stalling.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Entities;

public partial struct ConfigReaderSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<GlobalConfigBuffer>().Build();
        
        // Zero sync overhead. Ensures the main thread never stalls on this read.
        var buffer = query.GetSingletonBufferNoSync<GlobalConfigBuffer>(isReadOnly: true);
    }
}
```

### 38. `IJobParallelForDeferExtensions`: Schedule Using Buffer Length
**Standard:** Do not resolve `DynamicBuffer` lengths on the main thread and pass them into `IJobParallelFor`. Use the defer extension to let the job system dynamically evaluate the buffer length directly on the worker threads.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile]
public struct ProcessBufferJob : IJobParallelForDefer
{
    public void Execute(int index) { /* Process */ }
}

public static class DeferExample
{
    public static JobHandle Schedule(DynamicBuffer<MyElement> buffer, JobHandle dep)
    {
        var job = new ProcessBufferJob();
        
        // The job system reads the buffer length internally. 
        // Protects against the buffer resizing between schedule and execution.
        return job.Schedule(buffer, innerloopBatchCount: 64, dep);
    }
}
```

### 39. `ListExtensions.AddRangeNative`: Ultra-Fast Array Copies
**Standard:** Never loop through a `NativeArray` to add elements to a `System.Collections.Generic.List<T>`. `AddRangeNative` uses unsafe memory copying (`MemCpy`) to blast data directly into the List's internal array backing.

```csharp
using System.Collections.Generic;
using BovineLabs.Core.Extensions;
using Unity.Collections;

public static class ListCopyOptimization
{
    public static void CopyData(NativeArray<int> nativeData, List<int> managedList)
    {
        // Instantly copies unmanaged memory into the managed List<T> backing array
        managedList.AddRangeNative(nativeData);
    }
}
```

### 40. `MathematicsExtensions.Encapsulate`: Pure Math AABB Merging
**Standard:** Unity's `Bounds.Encapsulate` introduces managed overhead. `MathematicsExtensions.Encapsulate` utilizes `Unity.Mathematics.MinMaxAABB` and `math.min/max` for 100% Burst-compiled vectorized merging.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct EncapsulateJob : Unity.Jobs.IJob
{
    public void Execute()
    {
        AABB box1 = new AABB { Center = float3.zero, Extents = new float3(1) };
        AABB box2 = new AABB { Center = new float3(5), Extents = new float3(1) };

        // Fully vectorized SIMD merge
        AABB merged = box1.Encapsulate(box2);
    }
}
```

### 41. `NativeHashMapExtensions.GetOrAddRef`: Avoid Double Lookups
**Standard:** Checking `ContainsKey` followed by `Add` or `this[key]` hashes the key and searches the buckets twice. `GetOrAddRef` performs exactly one lookup and returns a mutable reference to the unmanaged memory directly.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public struct MapOptimizationJob : Unity.Jobs.IJob
{
    public NativeHashMap<int, int> Map;

    public void Execute()
    {
        // O(1) single-pass lookup. If it doesn't exist, it adds '0' and returns a ref to it.
        ref int count = ref this.Map.GetOrAddRef(key: 42, defaultValue: 0);
        
        // Mutates the value directly in the map's memory block without re-hashing
        count += 1; 
    }
}
```

### 42. `NativeHashMapExtensions.ClearAndAddBatchUnsafe`: Mass Map Population
**Standard:** When rebuilding a map from arrays where keys are mathematically guaranteed to be unique, bypassing the `Add` collision checks saves massive CPU time. `ClearAndAddBatchUnsafe` uses raw `MemCpy` to reconstruct the map instantly.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct RebuildMapJob : Unity.Jobs.IJob
{
    public NativeParallelHashMap<int, float> Map;
    [ReadOnly] public NativeArray<int> Keys;
    [ReadOnly] public NativeArray<float> Values;

    public void Execute()
    {
        // Instantly wipes the map and mem-copies the arrays into the bucket structure.
        // DANGER: Fails catastrophically if Keys array contains duplicates.
        this.Map.ClearAndAddBatchUnsafe(this.Keys, this.Values);
    }
}
```

### 43. `NativeListExtensions.ReserveNoResize`: Thread-Safe Block Allocation
**Standard:** Calling `Add` on a concurrent queue/list inside a parallel job forces thread contention on every single element. `ReserveNoResize` reserves a contiguous block of memory atomically, returning a pointer for lock-free writing.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public unsafe struct BulkWriteJob : Unity.Jobs.IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeList<int>.ParallelWriter ListWriter;

    public void Execute(int index)
    {
        // Atomically grabs space for 10 elements, returning the starting pointer
        this.ListWriter.ReserveNoResize(length: 10, out int* ptr, out int startIndex);

        for (int i = 0; i < 10; i++)
        {
            // Lock-free sequential memory write
            ptr[i] = index + i;
        }
    }
}
```

### 44. `NativeStreamExtensions.WriteLarge`: Massive Continuous Stream Writes
**Standard:** `NativeStream` normally crashes if you write a struct larger than its internal block size. `WriteLarge` mathematically divides large arrays or slices and seamlessly spans them across multiple stream blocks.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct StreamLargeDataJob : Unity.Jobs.IJob
{
    public NativeStream.Writer Writer;
    [ReadOnly] public NativeArray<float4x4> MassiveMatrixArray;

    public void Execute()
    {
        this.Writer.BeginForEachIndex(0);
        
        // Safely splices the massive array across 4KB NativeStream blocks internally
        this.Writer.WriteLarge(this.MassiveMatrixArray);
        
        this.Writer.EndForEachIndex();
    }
}
```

### 45. `SystemState.GetSingletonEntity`: Direct State Resolution
**Standard:** Creating an `EntityQuery` inside `OnUpdate` just to get a singleton entity creates boilerplate. The `GetSingletonEntity<T>` extension on `ref SystemState` handles the query building, dependency completion, and retrieval in one call.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct SingletonResolutionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Implicitly builds query, syncs dependencies, and returns the entity
        if (state.TryGetSingletonEntity<MySingletonData>(out Entity configEntity))
        {
            // Use configEntity
        }
    }
}
```

### 46. `SystemState.GetManagedSingleton`: Seamless Managed Class Access
**Standard:** ECS limits managed types (classes), but sometimes they are unavoidable (e.g., UI, third-party libraries). `GetManagedSingleton<T>` cleanly fetches managed components attached to entities directly from unmanaged system states.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Entities;
using UnityEngine;

public class CameraReference : IComponentData
{
    public Camera MainCamera;
}

public partial struct CameraResolverSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Safe access to managed IComponentData class
        if (state.TryGetManagedSingleton<CameraReference>(out var camRef))
        {
            var fov = camRef.MainCamera.fieldOfView;
        }
    }
}
```

### 47. `World.IsClientWorld`: Clean Netcode Topological Checks
**Standard:** Unity Netcode requires checking specific world flags to determine the execution environment. Never use bitwise checks manually. Use `world.IsClientWorld()`, `IsServerWorld()`, and `IsThinClientWorld()`.

```csharp
using BovineLabs.Core.Extensions;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct ServerOnlySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Explicit intent. Bypasses needing to know Netcode's exact WorldFlags layout.
        if (state.WorldUnmanaged.IsClientWorld())
        {
            return;
        }
        
        // Server-specific execution
    }
}
```

### 48. `IJobChunkWorkerBeginEnd`: Thread-Local Setup Hooks
**Standard:** When a chunk job requires temporary arrays per thread (e.g., allocating a `NativeList` for caching), `IJobChunk` provides no setup/teardown. `IJobChunkWorkerBeginEnd` injects `OnWorkerBegin` and `OnWorkerEnd` specifically per thread.

```csharp
using BovineLabs.Core.Jobs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;

[BurstCompile]
public struct CachingChunkJob : IJobChunkWorkerBeginEnd
{
    public void OnWorkerBegin()
    {
        // Executes exactly once per thread before processing any chunks
    }

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        // Process data
    }

    public void OnWorkerEnd()
    {
        // Executes exactly once per thread after all chunks assigned to it are done
    }
}
```

### 49. `IJobForThread`: Explicit Thread Distribution
**Standard:** Standard `IJobParallelFor` batches by array index, dynamically stealing work. When doing complex calculations where work *must* be evenly distributed specifically by core count, use `IJobForThread`.

```csharp
using BovineLabs.Core.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct ThreadDistributorJob : IJobForThread
{
    public void Execute(int index)
    {
        // Index is the explicit thread index slice (0 to ThreadCount-1).
        // Perform work specifically mapped to this thread slice.
    }
}

public static class JobThreadScheduler
{
    public static void Schedule()
    {
        var job = new ThreadDistributorJob();
        
        // Specifically divides the 1000 items across exactly 4 threads
        job.ScheduleParallel(arrayLength: 1000, threadCount: 4, default).Complete();
    }
}
```

### 50. `IJobHashMapDefer`: Deferred Hash Map Iteration
**Standard:** Unity provides no native way to iterate over a `NativeHashMap` in parallel *after* another job has dynamically filled it. `IJobHashMapDefer` resolves this by deferring the iteration over the map's buckets on worker threads.

```csharp
using BovineLabs.Core.Jobs;
using Unity.Burst;
using Unity.Collections;

[BurstCompile]
public struct MapProcessorJob : IJobHashMapDefer
{
    [ReadOnly] public NativeHashMap<int, float> Map;

    public void ExecuteNext(int entryIndex, int jobIndex)
    {
        // Safely reads the entry directly from the internal HashMap memory via extension
        this.Read(this.Map, entryIndex, out int key, out float value);
    }
}
```

### 51. `IJobParallelForDeferBatch`: Custom Batch Size Deferral
**Standard:** `IJobParallelForDefer` forces Unity to pick the internal batch size. If your deferred list contains highly complex calculations per element, use `IJobParallelForDeferBatch` to explicitly define the `innerloopBatchCount` at runtime.

```csharp
using BovineLabs.Core.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct DeferredComplexMathJob : IJobParallelForDeferBatch
{
    public void Execute(int startIndex, int count)
    {
        for (int i = startIndex; i < startIndex + count; i++)
        {
            // Extremely heavy math per element
        }
    }
}

public static class DeferBatchExample
{
    public static void Schedule(NativeList<int> deferredList, JobHandle dep)
    {
        var job = new DeferredComplexMathJob();
        // Forces a batch size of exactly 1 item per thread steal, regardless of list size
        job.ScheduleParallel(deferredList, innerloopBatchCount: 1, dep);
    }
}
```

### 52. `KSettingsBase`: Burst-Compatible String-to-Byte Identifiers
**Standard:** Enums are rigid; Strings allocate memory and are banned in Burst. `KSettingsBase` maps ScriptableObject string definitions into raw `byte` or `int` IDs.

```csharp
using System;
using System.Collections.Generic;
using BovineLabs.Core.Keys;
using UnityEngine;

// 1. Defined in the editor as a ScriptableObject
public class WeaponTypes : KSettingsBase<WeaponTypes, byte>
{
    [SerializeField] private NameValue<byte>[] keys = Array.Empty<NameValue<byte>>();
    public override IEnumerable<NameValue<byte>> Keys => this.keys;
}

// 2. Used in Burst
[Unity.Burst.BurstCompile]
public static class CombatResolver
{
    public static void CheckWeapon(Unity.Collections.FixedString32Bytes weaponName)
    {
        // O(1) lookup inside Burst, completely allocation free
        byte weaponId = WeaponTypes.NameToKey(weaponName);
    }
}
```

### 53. `ConfigVarAttribute`: Binding ECS to CLI / Editor Prefs
**Standard:** Debug flags and configuration settings must not be hardcoded. `[ConfigVar]` binds a `SharedStatic<T>` directly to Unity Editor Preferences and Command Line Arguments.

```csharp
using BovineLabs.Core.ConfigVars;
using Unity.Burst;

public struct DebugConfig
{
    // Bound to CLI arg `-debug.combat.logging` or Editor Prefs[ConfigVar("debug.combat.logging", false, "Enable verbose combat logging.")]
    public static readonly SharedStatic<bool> IsCombatLoggingEnabled = 
        SharedStatic<bool>.GetOrCreate<DebugConfig>();
}
```

### 54. `ConfigVarManager`: Automatic Configuration Parsing
**Standard:** The system must read all `[ConfigVar]` tags automatically at runtime. Calling `ConfigVarManager.Initialize()` parses all command line arguments and populates the `SharedStatic` variables before the ECS systems spin up.

```csharp
using BovineLabs.Core.ConfigVars;
using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Init()
    {
        // Scans all assemblies, locates [ConfigVar], and applies values globally
        ConfigVarManager.Initialize();
    }
}
```

### 55. `ConfigVarSharedStaticStringContainer`: Bridging Managed Strings
**Standard:** Since `SharedStatic` cannot hold managed `System.String` types in Burst, `ConfigVarManager` uses this internal container to safely convert external managed strings from the CLI directly into `FixedString32Bytes` representations.

```csharp
using BovineLabs.Core.ConfigVars;
using Unity.Burst;
using Unity.Collections;

public struct NetworkConfig
{
    // Binds the CLI argument '-network.ip 127.0.0.1' directly to a Burst-readable FixedString[ConfigVar("network.ip", "127.0.0.1", "Target IP Address")]
    public static readonly SharedStatic<FixedString32Bytes> TargetIP = 
        SharedStatic<FixedString32Bytes>.GetOrCreate<NetworkConfig>();
}
```

### 56. `MemoryAllocator`: Tracking Multiple Unmanaged Allocations
**Standard:** Managing multiple `UnsafeUtility.Malloc` calls leads to memory leaks. `MemoryAllocator` acts as a lifetime wrapper, tracking all unmanaged pointers and freeing them simultaneously upon disposal.

```csharp
using BovineLabs.Core.Memory;
using Unity.Collections;

public unsafe class ComplexGraphProcessor
{
    public void Execute()
    {
        // Manages lifetime of all subsequent unmanaged allocations
        using var allocator = new MemoryAllocator(Allocator.Temp);

        int* grid1 = allocator.Create<int>(1024);
        float* grid2 = allocator.Create<float>(2048);
        
        // No manual UnsafeUtility.Free required. Both pointers freed upon `using` block exit.
    }
}
```

### 57. `Ptr<T>`: Equatable Unmanaged Pointer Wrapper
**Standard:** Using `void*` or `T*` directly as keys in `NativeHashMap` is impossible because pointers don't implement `IEquatable`. `Ptr<T>` wraps the raw memory address into a strictly typed, equatable, hashable struct.

```csharp
using BovineLabs.Core.Memory;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
[BurstCompile]
public unsafe struct PointerMapJob : Unity.Jobs.IJob
{
    public void Execute()
    {
        var map = new NativeHashMap<Ptr<int>, float>(16, Allocator.Temp);
        
        int dummy = 42;
        int* rawPtr = &dummy;
        
        // Safely maps the physical memory address to a float value
        map.Add(rawPtr, 3.14f);
        
        map.Dispose();
    }
}
```

### 58. `CopyEnableable`: Syncing Component States
**Standard:** Manually querying and syncing the enableable state of one component to match another causes massive structural overhead. `CopyEnableable<TTo, TFrom>` creates a mathematically pure bit-mask copy job.

```csharp
using BovineLabs.Core.Model;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

// IEnableableComponent marker
public struct TargetVisibility : IComponentData, IEnableableComponent { }

[BurstCompile]
public partial struct SyncVisibilitySystem : ISystem
{
    private CopyEnableable<LocalTransform, TargetVisibility> syncer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        this.syncer.OnCreate(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Bitwise copy of the TargetVisibility enabled mask to the LocalTransform enabled mask.
        // Zero branching. Zero structural changes.
        this.syncer.OnUpdate(ref state);
    }
}
```

### 59. `TimerFixed`: High-Performance Fixed Timers
**Standard:** Decrementing a float in `IJobEntity` and resolving it with `if (val <= 0)` ruins Burst vectorization. `TimerFixed` operates across chunk memory, mathematically calculating remaining time and flipping a target state bool via Bitmasks.

```csharp
using BovineLabs.Core.Model;
using Unity.Burst;
using Unity.Entities;

public struct IsCasting : IComponentData { public bool Value; }
public struct CastTimeRemaining : IComponentData { public float Value; }
public struct WantsToCast : IComponentData { public bool Value; }[BurstCompile]
public partial struct CastingTimerSystem : ISystem
{
    private TimerFixed<IsCasting, CastTimeRemaining, WantsToCast> timer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Instantiates a vectorized fixed-duration timer (e.g., 2.5 seconds)
        this.timer = new TimerFixed<IsCasting, CastTimeRemaining, WantsToCast>(2.5f);
        this.timer.OnCreate(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        this.timer.OnUpdate(ref state, new TimerFixed<IsCasting, CastTimeRemaining, WantsToCast>.UpdateTimeJob());
    }
}
```

### 60. `TimerEnableable`: Vectorized IEnableableComponent Timers
**Standard:** Similar to `TimerFixed`, but instead of flipping a boolean component, it directly modifies the `v128` enabled bitmask of an `IEnableableComponent` when the timer reaches zero.

```csharp
using BovineLabs.Core.Model;
using Unity.Burst;
using Unity.Entities;

public struct Stunned : IComponentData, IEnableableComponent { }
public struct StunTimeRemaining : IComponentData { public float Value; }
public struct StunTrigger : IComponentData, IEnableableComponent { }
public struct StunDuration : IComponentData { public float Value; }

[BurstCompile]
public partial struct StunTimerSystem : ISystem
{
    private TimerEnableable<Stunned, StunTimeRemaining, StunTrigger, StunDuration> timer;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        this.timer.OnCreate(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Decrements all StunTimeRemaining floats in the chunk.
        // If zero is reached, sets the Stunned enableable bit to false at the chunk memory level.
        this.timer.OnUpdate(ref state);
    }
}
```

### 61. `TimerTriggerResetJob`: Bulk Timer Chunk Resetting
**Standard:** Resetting timers across thousands of entities individually wastes CPU cycles. `TimerTriggerResetJob` mem-compares entire chunks against a zeroed byte array and skips processing if the chunk is already reset, massively reducing write operations.

```csharp
using BovineLabs.Core.Model;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public struct CooldownTimer : IComponentData { public float Value; }

[BurstCompile]
public unsafe partial struct ResetTimersSystem : ISystem
{
    private NativeArray<float> zeros;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Allocate a zeroed array large enough for the maximum chunk capacity
        this.zeros = new NativeArray<float>(128, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) => this.zeros.Dispose();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new TimerTriggerResetJob<CooldownTimer>
        {
            RemainingHandle = SystemAPI.GetComponentTypeHandle<CooldownTimer>(),
            Zeros = this.zeros.GetUnsafeReadOnlyPtr()
        };
        
        var query = SystemAPI.QueryBuilder().WithAll<CooldownTimer>().Build();
        state.Dependency = job.ScheduleParallel(query, state.Dependency);
    }
}
```

### 62. `StateFlagModel`: Bitmask-Based State Machines
**Standard:** Avoid adding/removing dozens of tag components to manage state. `StateFlagModel` binds a highly efficient 1-byte or bitmask component to structural tag components, batching ECB structural changes effortlessly.

```csharp
using BovineLabs.Core.Collections;
using BovineLabs.Core.States;
using Unity.Burst;
using Unity.Entities;

public struct PlayerState : IComponentData { public BitArray16 Value; }
public struct PlayerStatePrevious : IComponentData { public BitArray16 Value; }
public struct IsJumping : IComponentData { } // The structural tag

[BurstCompile]
public partial struct PlayerStateSystem : ISystem
{
    private StateFlagModel model;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Registers State Key 1 to the IsJumping tag
        StateAPI.Register<PlayerState, IsJumping>(ref state, 1);
        
        this.model = new StateFlagModel(ref state, 
            ComponentType.ReadWrite<PlayerState>(), 
            ComponentType.ReadWrite<PlayerStatePrevious>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        
        // Mathematically deduces what components to add/remove based on bitmask diffs
        this.model.UpdateParallel(ref state, ecb.AsParallelWriter());
        
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
    }
}
```

### 63. `StateModelEnableable`: Zero-Allocation State Toggles
**Standard:** Structural changes stall parallel jobs. `StateModelEnableable` completely bypasses structural changes by mapping your state byte directly to `IEnableableComponent` toggles instead of adding/removing components.

```csharp
using BovineLabs.Core.States;
using Unity.Burst;
using Unity.Entities;

public struct NPCState : IComponentData { public byte Value; }
public struct NPCStatePrevious : IComponentData { public byte Value; }
public struct IsPatrolling : IComponentData, IEnableableComponent { }[BurstCompile]
public partial struct NPCStateSystem : ISystem
{
    private StateModelEnableable model;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        StateAPI.Register<NPCState, IsPatrolling>(ref state, 2);
        this.model = new StateModelEnableable(ref state, 
            ComponentType.ReadWrite<NPCState>(), 
            ComponentType.ReadWrite<NPCStatePrevious>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Zero allocations. Zero structural changes. Instantly toggles enableable bits.
        this.model.UpdateParallel(ref state);
    }
}
```

### 64. `StateModelWithHistory`: Ring Buffer Rollback States
**Standard:** For UI or complex AI that requires returning to previous states (e.g., closing a menu, reverting from a stun), `StateModelWithHistory` maintains a hardware-friendly ring buffer of past states.

```csharp
using BovineLabs.Core.States;
using Unity.Burst;
using Unity.Entities;

public struct MenuState : IComponentData { public byte Value; }
public struct MenuStatePrev : IComponentData { public byte Value; }
public struct MenuStateBack : IBufferElementData { public byte Value; }
public struct MenuStateForward : IBufferElementData { public byte Value; }

[BurstCompile]
public partial struct MenuStateSystem : ISystem
{
    private StateModelWithHistory model;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Maintains up to 10 previous states
        this.model = new StateModelWithHistory(ref state, 
            ComponentType.ReadWrite<MenuState>(), 
            ComponentType.ReadWrite<MenuStatePrev>(), 
            ComponentType.ReadWrite<MenuStateBack>(), 
            ComponentType.ReadWrite<MenuStateForward>(), 
            10);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        this.model.UpdateParallel(ref state, ecb.AsParallelWriter());
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
    }
}
```

### 65. `StatefulCollisionEvent`: Persistent Enter/Stay/Exit Collisions
**Standard:** Unity Physics triggers raw collision events with no memory of the previous frame. `StatefulCollisionEvent` runs through the BovineLabs physics extension to automatically calculate `Enter`, `Stay`, and `Exit` phases for standard jobs.

```csharp
using BovineLabs.Core.PhysicsStates;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;

[BurstCompile]
public partial struct DamageCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var collisions in SystemAPI.Query<DynamicBuffer<StatefulCollisionEvent>>())
        {
            foreach (var collision in collisions)
            {
                // Instant Burst-compiled state evaluation
                if (collision.State == StatefulEventState.Enter)
                {
                    Entity hitEntity = collision.EntityB;
                    // Apply initial damage
                }
            }
        }
    }
}
```

### 66. `StatefulTriggerEvent`: Persistent Enter/Stay/Exit Triggers
**Standard:** The trigger equivalent to collision events. Ensures that volume overlaps (like poison clouds or capture points) are evaluated reliably without writing manual HashMaps to track overlap history.

```csharp
using BovineLabs.Core.PhysicsStates;
using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct PoisonCloudSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var triggers in SystemAPI.Query<DynamicBuffer<StatefulTriggerEvent>>())
        {
            foreach (var trigger in triggers)
            {
                if (trigger.State == StatefulEventState.Stay)
                {
                    // Apply continuous poison damage
                }
                else if (trigger.State == StatefulEventState.Exit)
                {
                    // Remove poison debuff
                }
            }
        }
    }
}
```

### 67. `CalculateEventMapBucketsJob`: Dynamic Physics Hashing Optimization
**Standard:** Managing persistent physics states requires comparing the current frame's events against the previous frame. `CalculateEventMapBucketsJob` recalculates bucket indices on multi-hash maps efficiently in parallel. *(Internal architecture showcase).*

```csharp
using BovineLabs.Core.PhysicsStates;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile]
public struct OptimizePhysicsBucketsJob : IJob
{
    public NativeMultiHashMap<Entity, StatefulCollisionEventContainer> CurrentEventMap;

    public void Execute()
    {
        // Re-hashes the map based on entity counts instantly
        this.CurrentEventMap.RecalculateBuckets();
    }
}
```

### 68. `CollectEventsJob`: Deferred Physics Event Parsing
**Standard:** Physics events are written to a `NativeStream`. `CollectEventsJob` abstracts the complex pointer arithmetic needed to read these streams in parallel, mapping them directly to entity HashMaps. *(Internal architecture showcase).*

```csharp
using BovineLabs.Core.PhysicsStates;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public unsafe struct ParseEventsJob : IJob
{
    [ReadOnly] public NativeStream.Reader Reader;
    public NativeMultiHashMap<Entity, StatefulTriggerEventContainer> EventMap;

    public void Execute()
    {
        this.Reader.BeginForEachIndex(0);
        while (this.Reader.RemainingItemCount > 0)
        {
            // Highly optimized Unsafe utility extraction
            var triggerEvent = this.Reader.Read<Unity.Physics.TriggerEvent>();
            // ...
        }
        this.Reader.EndForEachIndex();
    }
}
```

### 69. `EnsureCurrentEventsCapacityJob`: Pre-Allocating Tracking Memory
**Standard:** Memory re-allocations in the middle of physics collision handling cause massive GC spikes and job stalls. `EnsureCurrentEventsCapacityJob` pre-computes the required capacity by counting the `NativeStream` ahead of time.

```csharp
using BovineLabs.Core.PhysicsStates;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile]
public struct PreallocatePhysicsMemoryJob : IJob
{
    [ReadOnly] public NativeStream.Reader Reader;
    public NativeMultiHashMap<Entity, StatefulCollisionEventContainer> Map;

    public void Execute()
    {
        var capacity = this.Reader.Count();
        this.Map.ClearLengthBuckets();

        // 2x capacity because every collision involves 2 entities
        if (this.Map.Capacity < capacity * 2)
        {
            this.Map.Capacity = capacity * 2;
        }
    }
}
```

### 70. `StatefulCollisionEventClearSystem`: Pre-Physics Buffer Cleanup
**Standard:** Persistent buffers must be cleared before the Unity Physics step overwrites them, otherwise ghost data remains. This system runs via `IJobEntity` explicitly before `PhysicsSimulationGroup`.

```csharp
using BovineLabs.Core.PhysicsStates;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
public partial struct ClearCollisionEventsSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Only touches chunks that actually had collisions last frame
        new ClearCollisionJob().ScheduleParallel();
    }

    [BurstCompile][WithChangeFilter(typeof(StatefulCollisionEvent))]
    public partial struct ClearCollisionJob : IJobEntity
    {
        public void Execute(ref DynamicBuffer<StatefulCollisionEvent> buffer)
        {
            buffer.Clear();
        }
    }
}
```

### 71. `AlwaysUpdatePhysicsWorldSystem`: Non-Fixed Physics Synchronization
**Standard:** If the game runs at 144 FPS but physics runs at 60 FPS, spatial queries (like Raycasts) will stutter. `AlwaysUpdatePhysicsWorldSystem` forces the collision world to rebuild continuously, providing smooth raycasts every frame without simulating actual physics steps.

```csharp
using BovineLabs.Core.PhysicsUpdate;
using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(TransformSystemGroup), OrderLast = true)]
public partial class SmoothPhysicsQuerySystem : SystemBase
{
    private BuildPhysicsWorld buildPhysicsWorld;

    protected override void OnCreate()
    {
        this.buildPhysicsWorld = this.World.GetExistingSystemManaged<BuildPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        var physicsUpdated = SystemAPI.GetSingletonRW<AlwaysUpdatePhysicsWorld>();

        // Skip if physics naturally updated this frame
        if (physicsUpdated.ValueRO.FixedStepUpdatedThisFrame)
        {
            physicsUpdated.ValueRW.FixedStepUpdatedThisFrame = false;
            return;
        }

        // Force a spatial map rebuild for smooth raycasting
        this.buildPhysicsWorld.Update(this.World.Unmanaged);
    }
}
```

### 72. `FixedStepUpdatedSystem`: Fixed-Step Execution Tracking
**Standard:** Safely flags whether the `FixedStepSimulationSystemGroup` executed on the current frame. This flag allows variable-rate systems to know whether physics arrays need manual updating.

```csharp
using BovineLabs.Core.PhysicsUpdate;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(PhysicsInitializeGroup))]
public partial struct TrackPhysicsStepSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.AddComponent<AlwaysUpdatePhysicsWorld>(state.SystemHandle);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Signifies that the fixed step successfully fired
        SystemAPI.SetSingleton(new AlwaysUpdatePhysicsWorld { FixedStepUpdatedThisFrame = true });
    }
}
```

### 73. `InputBounds` & `RelevancySystem`: Netcode Spatial Hashing
**Standard:** Sending all ghosts to all clients destroys network bandwidth. Define an `InputBounds` on the client connection. `RelevancySystem` spatially hashes these bounds and only serializes ghosts that fall into those specific cells.

```csharp
using BovineLabs.Core.Relevancy;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct InputBounds : IInputComponentData
{
    [GhostField(Quantization = 100)]
    public float3 Min;

    [GhostField(Quantization = 100)]
    public float3 Max;
}[BurstCompile]
public partial struct ProcessRelevancyJob : Unity.Jobs.IJobEntity
{
    public void Execute(in InputBounds bounds, ref GhostConnectionPosition position)
    {
        // Syncs the client's localized view to the server for ghost culling
        position = new GhostConnectionPosition { Position = (bounds.Max + bounds.Min) / 2f };
    }
}
```

### 74. `RelevanceAlways` & `RelevanceManual`: Ghost Culling Overrides
**Standard:** Global elements (like the sun, objectives, or managers) must never be culled. Attach `RelevanceAlways` to bypass spatial checks. Attach `RelevanceManual` to handle complex quest-based visibility manually.

```csharp
using BovineLabs.Core.Relevancy;
using Unity.Entities;
using Unity.NetCode;

public static class RelevancyConfig
{
    public static void Setup(EntityManager em, Entity ghostEntity, bool isGlobal)
    {
        if (isGlobal)
        {
            // Forces Netcode to always send this entity regardless of distance
            em.AddComponent<RelevanceAlways>(ghostEntity);
        }
        else
        {
            // Disables default spatial checks, delegating it to custom logic
            em.AddComponent<RelevanceManual>(ghostEntity);
        }
    }
}
```

### 75. `SingletonAttribute` & `SingletonSystem`: Global Buffer Merging
**Standard:** Rather than trying to combine config buffers manually across subscenes, tag the buffer element with `[Singleton]`. `SingletonSystem` automatically finds all instances, merges them into a global entity, and destroys the source buffers.

```csharp
using BovineLabs.Core.Settings;
using Unity.Entities;

// 1. Tag the buffer data
[Singleton]
public struct GlobalCraftingRecipe : IBufferElementData
{
    public int RecipeId;
    public int OutputId;
}

// 2. Query it globally anywhere in the game safely
public partial struct CraftingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // SingletonSystem automatically combined all subscene recipes here
        var recipes = SystemAPI.GetSingletonBuffer<GlobalCraftingRecipe>(isReadOnly: true);
    }
}
```

### 76. `SingletonInitializeSystemGroup`: One-Frame Initialization Signals
**Standard:** When a singleton buffer is updated by `SingletonSystem`, it enables the `SingletonInitialize` component for exactly one frame. Systems that need to cache this buffer should live in `SingletonInitializeSystemGroup` to run strictly when changes occur.

```csharp
using BovineLabs.Core.Settings;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SingletonInitializeSystemGroup))]
public partial struct BuildRecipeCacheSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // This system ONLY executes on the exact frame the Singleton buffer was modified/merged.
        var recipes = SystemAPI.GetSingletonBuffer<GlobalCraftingRecipe>(true);
        
        // Rebuild internal fast-access dictionaries...
    }
}
```

### 77. `StripLocalAttribute` & `StripLocalSystem`: World-Specific Data Purging
**Standard:** When simulating `Client` and `Server` locally, duplicate visual or server-only processing components cause conflicts. Mark them with `[StripLocal]` and `StripLocalSystem` will automatically wipe them from the `LocalSimulation` world.

```csharp
using BovineLabs.Core.Stripping;
using Unity.Entities;

// Automatically stripped from the Local world upon startup
[StripLocal]
public struct ServerAuthoritativeData : IComponentData
{
    public int SecureHash;
}

public partial struct LocalProcessingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // ServerAuthoritativeData is guaranteed not to exist here
    }
}
```

### 78. `AssetLoadingSystem`: Deferred Hybrid Instantiation
**Standard:** Spawning heavy Unity `GameObject`s (like UI or complex VFX) stalls ECS. Write a reference to `AssetLoad` buffers. The `AssetLoadingSystem` will process these on the `ServiceWorld` asynchronously without breaking the simulation loop.

```csharp
using BovineLabs.Core.SubScenes;
using Unity.Entities;
using UnityEngine;

public partial struct TriggerVFXSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var buffer = SystemAPI.GetSingletonBuffer<AssetLoad>();
        
        // Requests the ServiceWorld to instantiate the GameObject gracefully
        buffer.Add(new AssetLoad
        {
            TargetWorld = WorldFlags.GameClient,
            Asset = new UnityObjectRef<GameObject>() // Fetched from authoring
        });
    }
}
```

### 79. `SubSceneLoadData` & `SubSceneBuffer`: Granular Streaming Control
**Standard:** Grouping scenes logically is essential. `SubSceneLoadData` manages the required state, while `SubSceneBuffer` stores the explicit list of Unity `SceneAsset` GUIDs to be streamed.

```csharp
using BovineLabs.Core.SubScenes;
using Unity.Entities;
using Unity.Entities.Serialization;

public static class SubSceneOrchestrator
{
    public static void RequestLevelLoad(EntityManager em, Entity sceneManagerEntity)
    {
        em.SetComponentData(sceneManagerEntity, new SubSceneLoadData
        {
            TargetWorld = WorldFlags.Game,
            IsRequired = true,
            WaitForLoad = true
        });

        // Enables the streaming processor
        em.SetComponentEnabled<LoadSubScene>(sceneManagerEntity, true);
    }
}
```

### 80. `SubSceneLoadingSystem`: Unmanaged SubScene Orchestration
**Standard:** Bypasses Unity's traditional `SceneManager`. `SubSceneLoadingSystem` watches for `LoadSubScene` toggles and feeds the GUIDs directly into `SceneSystem.LoadSceneAsync`, pausing the game loop mathematically until critical assets arrive.

```csharp
using BovineLabs.Core.Pause;
using BovineLabs.Core.SubScenes;
using Unity.Burst;
using Unity.Entities;
using Unity.Scenes;

[BurstCompile]
public partial struct LevelStreamSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (loadData, entity) in SystemAPI.Query<RefRO<SubSceneLoadData>>().WithAll<SubSceneLoaded>().WithEntityAccess())
        {
            // Level is fully streamed in memory. Unpause the game loop.
            PauseGame.Unpause(ref state);
            
            // Clean up the load request
            state.EntityManager.SetComponentEnabled<LoadSubScene>(entity, false);
        }
    }
}
```

### 81. `IFacet` & `FacetAttribute`: Source-Generated ECS Wrappers
**Standard:** Manual chunk iteration with `ComponentTypeHandle` causes massive boilerplate. Implement `IFacet` on a `partial struct` and let the Roslyn Source Generator instantly write the `Lookup`, `TypeHandle`, and `ResolvedChunk` logic for you.

```csharp
using BovineLabs.Core;
using Unity.Burst;
using Unity.Entities;

// 1. The Source Generator handles the rest automatically
public readonly partial struct CombatFacet : IFacet
{
    private readonly RefRW<Health> health;
    private readonly RefRO<Damage> damage;
    
    public partial struct Lookup { } // Triggers generation
}

[BurstCompile]
public partial struct CombatSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 2. Clean, allocation-free, mathematically perfect chunk resolution
        var job = new CombatJob();
        state.Dependency = job.ScheduleParallel(CombatFacet.CreateQueryBuilder().Build(ref state), state.Dependency);
    }
}
```

### 82. `FacetOptionalAttribute`: Graceful Missing Component Handling
**Standard:** Not all entities in a query have identical archetypes. Mark fields with `[FacetOptional]` to instruct the generator to use `TryGet` safely, preventing Unity from throwing missing component exceptions.

```csharp
using BovineLabs.Core;
using Unity.Burst;
using Unity.Entities;

public readonly partial struct MovementFacet : IFacet
{
    private readonly RefRW<LocalTransform> transform;
    
    // Safely resolves to default if the chunk lacks this component
    [FacetOptional] 
    private readonly RefRO<PhysicsVelocity> velocity;
}
```

### 83. `DynamicGenerator`: Auto-Generated Collection Extensions
**Standard:** Creating dynamic buffers for complex unmanaged collections manually requires unsafe casting. The `DynamicGenerator` Roslyn analyzer detects `IDynamicHashMap` implementations and auto-generates `AsHashMap()` extension methods at compile time.

```csharp
using BovineLabs.Core.Iterators;
using Unity.Burst;
using Unity.Entities;

[InternalBufferCapacity(0)]
public struct GridOccupants : IDynamicMultiHashMap<int, Entity>
{
    byte IDynamicMultiHashMap<int, Entity>.Value { get; }
}

[BurstCompile]
public static class GridExtensions
{
    public static void ReadGrid(DynamicBuffer<GridOccupants> buffer)
    {
        // 'AsMultiHashMap' is automatically generated by BovineLabs.DynamicGenerator
        DynamicMultiHashMap<int, Entity> map = buffer.AsMultiHashMap<GridOccupants, int, Entity>();
    }
}
```

### 84. `FunctionsBuilder`: Burst-Compiled Function Pointers
**Standard:** Standard delegates allocate garbage and cannot be used in Burst. `FunctionsBuilder` compiles unmanaged `IFunction<T>` instances into `FunctionPointer`s, allowing polymorphism and modular logic execution inside parallel jobs.

```csharp
using BovineLabs.Core.Functions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

public struct ProcessContext { public float DeltaTime; }
public struct ProcessResult { public float Damage; }

[BurstCompile]
public unsafe struct CriticalStrikeFunction : IFunction<ProcessContext>
{
    public UpdateFunction UpdateFunction => null;
    public DestroyFunction DestroyFunction => null;
    public ExecuteFunction ExecuteFunction => Execute;

    private ProcessResult Execute(ref ProcessContext context)
    {
        return new ProcessResult { Damage = context.DeltaTime * 100f };
    }[BurstCompile]
    [Unity.Burst.CompilerServices.MonoPInvokeCallback(typeof(ExecuteFunction))]
    private static void Execute(void* target, void* data, void* result)
    {
        *(ProcessResult*)result = ((CriticalStrikeFunction*)target)->Execute(ref Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AsRef<ProcessContext>(data));
    }
}
```

### 85. `FunctionsHash`: O(1) Executable Burst Delegates
**Standard:** For massive modding support or event systems, `FunctionsBuilder.BuildHash()` creates a NativeHashMap of Burst function pointers keyed by a `long` hash. This allows resolving and executing dynamic behaviors in O(1) time strictly inside Burst.

```csharp
using BovineLabs.Core.Functions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public partial struct EventExecutionSystem : ISystem
{
    private FunctionsHash<ProcessContext, ProcessResult> eventFunctions;

    public void OnCreate(ref SystemState state)
    {
        // ReflectAll finds every IFunction<ProcessContext> in the assembly automatically
        this.eventFunctions = new FunctionsBuilder<ProcessContext, ProcessResult>(Allocator.Temp)
            .ReflectAll(ref state)
            .BuildHash();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ProcessContext ctx = new ProcessContext { DeltaTime = 1f };
        long eventHash = 123456789L; // Sourced from data

        // Instant Burst-compiled polymorphic execution
        if (this.eventFunctions.TryExecute(eventHash, ref ctx, out ProcessResult result))
        {
            // Apply result
        }
    }
}
```

### 86. `UpdateWorldTimeSystem`: Burst-Compiled Time Management
**Standard:** Unity's default `UpdateWorldTimeSystem` is heavily managed and restricts logic in Service worlds. BovineLabs overrides this with a purely unmanaged, Burst-compiled variant ensuring zero GC overhead on the absolute base level of the game loop.

```csharp
using Unity.Burst;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)][UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.All)]
public partial struct UpdateWorldTimeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var currentElapsedTime = SystemAPI.Time.ElapsedTime;
        var deltaTime = math.min(UnityEngine.Time.deltaTime, state.WorldUnmanaged.MaximumDeltaTime);
        
        // Zero allocation time push
        state.WorldUnmanaged.Time = new TimeData(currentElapsedTime + deltaTime, deltaTime);
    }
}
```

### 87. `SingletonCollectionUtil`: Rewindable Double-Buffered Allocators
**Standard:** Maintaining temporary native collections across frames usually results in leaks or read/write race conditions. `SingletonCollectionUtil` uses `DoubleRewindableAllocators` to safely ping-pong memory across the frame boundary.

```csharp
using BovineLabs.Core.SingletonCollection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public struct MyGlobalCache : ISingletonCollection<NativeThreadStream>
{
    public unsafe Unity.Collections.LowLevel.Unsafe.UnsafeList<NativeThreadStream>* Collections { get; set; }
    public Allocator Allocator { get; set; }
}

public partial struct CacheSystem : ISystem
{
    private SingletonCollectionUtil<MyGlobalCache, NativeThreadStream> cacheUtil;

    public void OnCreate(ref SystemState state)
    {
        this.cacheUtil = new SingletonCollectionUtil<MyGlobalCache, NativeThreadStream>(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        // Automatically clears the previous frame's allocator safely
        this.cacheUtil.ClearRewind(state.Dependency);
    }
}
```

### 88. `TypeManagerOverrides`: Modifying Internal Unity Capacities
**Standard:** `InternalBufferCapacity` is hardcoded at compile time. `TypeManagerOverrides` safely hacks Unity's internal unmanaged `TypeManager` registry to alter buffer capacities and enableable flags at runtime based on `ScriptableObject` configurations.

```csharp
using BovineLabs.Core.Utility;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

public static unsafe class TypeOverrideUtility
{
    public static void SetBufferCapacity(int typeIndex, int bufferCapacity)
    {
        // Unsafe pointer manipulation to rewrite Unity's immutable TypeManager state
        var typeInfoPointer = TypeManager.GetTypeInfoPointer() + typeIndex;
        
        *&typeInfoPointer->BufferCapacity = bufferCapacity;
        *&typeInfoPointer->SizeInChunk = sizeof(BufferHeader) + (bufferCapacity * typeInfoPointer->ElementSize);
    }
}
```

### 89. `ChangeFilterTrackingSystem`: Global DidChange Profiling
**Standard:** Unoptimized `DidChange` filters evaluate too broadly, ruining caching. Add `[ChangeFilterTracking]` to a component to instruct `ChangeFilterTrackingSystem` to automatically profile how often its chunk filter is triggered versus actual data utilization.

```csharp
using BovineLabs.Core;
using Unity.Entities;

// System profiles this component mathematically to ensure changes aren't falsely flagging chunks
[ChangeFilterTracking]
public struct DynamicHealth : IComponentData
{
    public float Value;
}
```

### 90. `ChangeFilterTrackingWindow`: Visualizing Chunk Change Inefficiencies
**Standard:** Data collected by the `ChangeFilterTrackingSystem` is automatically displayed in the `ChangeFilterTrackingWindow`. Any component triggering changes on >85% of chunks persistently requires immediate refactoring to a different access pattern.

```csharp
using BovineLabs.Core.Editor.ChangeFilterTracking;
using UnityEditor;

public static class ChangeFilterTools
{
    [MenuItem("Tools/BovineLabs/Change Filter")]
    public static void OpenWindow()
    {
        // Renders profiling data directly from ECS Memory into a UI Toolkit window
        EditorWindow.GetWindow<ChangeFilterTrackingWindow>().Show();
    }
}
```

### 91. `AssemblyBuilderWindow`: Enforcing Strict Compilation Boundaries
**Standard:** Circular dependencies and Editor-leaking code ruin build times. The `AssemblyBuilderWindow` automates the creation of `.asmdef` files enforcing `autoReferenced: false`, `[DisableAutoTypeRegistration]`, and strict `InternalsVisibleTo` mapping.

```csharp
using BovineLabs.Core.Editor.AssemblyBuilder;
using UnityEditor;

// Creates a 6-layer architecture instantly:
// .Data (Structs) -> .Main (Systems) -> .Authoring (Bakers) -> .Editor -> .Tests -> .Debug
public static class AssemblyStandard
{
    [MenuItem("Tools/BovineLabs/Assembly Builder")]
    public static void Open()
    {
        EditorWindow.GetWindow<AssemblyBuilderWindow>().Show();
    }
}
```

### 92. `ComponentDependencyWindow`: Tracking System Read/Writers
**Standard:** Guessing which systems touch a component leads to race conditions. `ComponentDependencyWindow` deeply inspects the unmanaged `SystemState.m_JobDependencyForReadingSystems` pointer arrays to map every single reader and writer.

```csharp
using BovineLabs.Core.Editor.Dependency;
using UnityEditor;

public static class ComponentDependencyTools
{[MenuItem("Tools/BovineLabs/Component Dependencies")]
    public static void Open()
    {
        // Mathematically maps the exact execution pipeline of all components
        EditorWindow.GetWindow<ComponentDependencyWindow>().Show();
    }
}
```

### 93. `SystemDependencyWindow`: Mapping Execution Order & Dependencies
**Standard:** Resolving JobHandle stalls requires knowing exactly which system is waiting on another. `SystemDependencyWindow` cross-references read/write intersections across `SystemHandle` indices to map out the exact Sync Point locations.

```csharp
using BovineLabs.Core.Editor.Dependency;
using UnityEditor;

public static class SystemDependencyTools
{[MenuItem("Tools/BovineLabs/System Dependencies")]
    public static void Open()
    {
        // Visualizes graph: System A -> writes [Position] -> read by System B
        EditorWindow.GetWindow<SystemDependencyWindow>().Show();
    }
}
```

### 94. `AssemblyGraphWindow`: Detecting Circular ECS Dependencies
**Standard:** If the `.Authoring` assembly references `.Main`, Unity will serialize game objects incorrectly. `AssemblyGraphWindow` parses JSON definitions of `.asmdef` files project-wide to visually map and detect illegal reference chains.

```csharp
using BovineLabs.Core.Editor.Dependency;
using UnityEditor;

public static class AssemblyGraphTools
{[MenuItem("Tools/BovineLabs/Assembly Graph")]
    public static void Open()
    {
        // Evaluates project structure to enforce the BovineLabs Assembly Architecture
        EditorWindow.GetWindow<AssemblyGraphWindow>().Show();
    }
}
```

### 95. `ConfigVarsWindow`: UI Toolkit Auto-Binding for SharedStatics
**Standard:** Hardcoded system parameters are banned. `ConfigVarsWindow` utilizes `ReflectionUtility` to find all `[ConfigVar]` tags and automatically binds them to UI Toolkit text fields, sliders, and color pickers for instant runtime tweaking.

```csharp
using BovineLabs.Core.Editor.ConfigVars;
using UnityEditor;

public static class ConfigTools
{[MenuItem("BovineLabs/ConfigVars")]
    public static void Open()
    {
        // Automatically fetches all `SharedStatic<T>` values and syncs them to EditorPrefs
        EditorWindow.GetWindow<ConfigVarsWindow>().Show();
    }
}
```

### 96. `WelcomeWindow` & `FeatureToggle`: Compiler Define Orchestration
**Standard:** Manual `#define` editing is prone to typos. `FeatureToggle` reads `PlayerSettings.GetScriptingDefineSymbols` and binds them to UI Toolkit buttons, allowing safe enabling/disabling of Core extensions like `BL_DISABLE_PHYSICS_STATES`.

```csharp
using BovineLabs.Core.Editor.Welcome;
using UnityEditor;
using UnityEditor.Build;

public static class FeatureToggleStandard
{
    public static void TogglePhysics(bool state)
    {
        // Safely manipulates the global C# compiler flags
        ScriptingDefineSymbolsEditor.ApplyDefinesToAll(
            addDefines: state ? new[] { "BL_DISABLE_PHYSICS_STATES" } : new string[0], 
            removeDefines: state ? new string[0] : new[] { "BL_DISABLE_PHYSICS_STATES" });
    }
}
```

### 97. `ViewModelToolbar`: Active UI Toolkit ViewModel Debugging
**Standard:** UI Toolkit ViewModels exist purely in managed memory. `ViewModelToolbar` uses `Object.FindObjectsByType<UIDocument>` to extract active ViewModels and bridges them into the Unity Inspector natively using `ObjectSelectionProxy`.

```csharp
using BovineLabs.Core.Editor.UI;
using UnityEditor;
using UnityEngine;

public class ObjectSelectionProxy : ScriptableObject
{
    public object Obj { get; set; }

    public static ObjectSelectionProxy CreateInstance(object obj)
    {
        var proxy = CreateInstance<ObjectSelectionProxy>();
        proxy.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable;
        proxy.Obj = obj;
        return proxy;
    }
}
```

### 98. `MainToolbarPresetPostProcessor`: IL Weaving Unity Toolbars
**Standard:** Unity restricts adding elements to the main top toolbar. The `MainToolbarPresetPostProcessor` uses `Mono.Cecil` to inject the internal `UnityOnlyMainToolbarPresetAttribute` into assemblies after compilation, forcefully expanding Editor toolsets.

```csharp
using System;
using BovineLabs.Core.Editor;
using JetBrains.Annotations;
using UnityEditor.Toolbars;

public static class CustomToolbar
{
    // The IL Weaver finds this attribute and injects the Unity internal requirement automatically
    [MainToolbarPreset]
    [MainToolbarElement("BovineLabs/Reload", defaultDockPosition = MainToolbarDockPosition.Middle)]
    [UsedImplicitly]
    private static MainToolbarElement Reload()
    {
        return new MainToolbarDropdown(new MainToolbarContent("Reload"), null);
    }
}
```

### 99. `WorldSafeShutdown`: Guaranteed Memory Cleanup on Exit
**Standard:** Exiting Play Mode while ECS parallel jobs are executing will hard crash Unity due to unmanaged memory pointer invalidation. `WorldSafeShutdown` intercepts `Application.quitting` to explicitly force `CompleteAllTrackedJobs()` and cleanly disable groups.

```csharp
using Unity.Entities;
using UnityEngine;

public static class WorldSafeShutdown
{[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        Application.quitting += OnQuit;
    }

    private static void OnQuit()
    {
        foreach (var world in World.All)
        {
            if (world.IsCreated && (world.Flags & WorldFlags.Live) != 0)
            {
                // Absolute guarantee no unmanaged pointer access violation occurs during teardown
                world.EntityManager.CompleteAllTrackedJobs();
            }
        }
    }
}
```

### 100. `TestLeakDetectionAttribute`: Automated ECS Memory Leak Tracking
**Standard:** "If it leaks memory, it's reverted." Every Unit Test operating on unmanaged data MUST use `[TestLeakDetection]`. It hooks into `UnsafeUtility` to track memory before the test, and asserts zero delta upon completion.

```csharp
using BovineLabs.Testing;
using NUnit.Framework;
using Unity.Collections;

public class MathTests : ECSTestsFixture
{
    [Test]
    [TestLeakDetection] // Fails the test automatically if Allocator.TempJob isn't Disposed
    public void NativeArrayAllocation_MustNotLeak()
    {
        var array = new NativeArray<int>(10, Allocator.TempJob);
        
        array[0] = 5;
        
        array.Dispose(); // Without this, TestLeakDetection throws AssertionException
    }
}
```

### 101. `GameObjectHelper.AddAuthoringComponent`: Programmatic Authoring Injection
**Standard:** Do not manually drag-and-drop generated authoring components onto hundreds of prefabs. `GameObjectHelper` uses reflection to invoke internal Unity editor menus, attaching components programmatically during automated pipeline steps.

```csharp
using System;
using BovineLabs.Core.Editor.Helpers;
using UnityEditor;
using UnityEngine;

public static class AutomatedAuthoringPipeline
{[MenuItem("Tools/BovineLabs/Inject Authoring")]
    public static void Inject()
    {
        foreach (var go in Selection.gameObjects)
        {
            // Bypasses manual inspector assignment by locating the generated authoring script
            GameObjectHelper.AddAuthoringComponent(go, typeof(HealthAuthoring));
        }
    }
}
```

### 102. `SerializedHelper.IterateAllChildren`: Clean UI Toolkit Iteration
**Standard:** When dynamically generating UI Toolkit elements for custom inspectors, iterating through `SerializedObject` typically yields the dreaded `m_Script` property. `IterateAllChildren` flattens the hierarchy and filters it safely.

```csharp
using BovineLabs.Core.Editor.Helpers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class CleanInspector : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        
        // Iterates properties cleanly, ignoring internal Unity script metadata
        foreach (var prop in SerializedHelper.IterateAllChildren(this.serializedObject, includeScript: false))
        {
            root.Add(new PropertyField(prop));
        }
        
        return root;
    }
}
```

### 103. `TextAssetHelper`: NativeArray to Binary Serialization
**Standard:** When baking complex paths or navigation meshes, you must write unmanaged `NativeArray` data directly to disk as binary `.bytes` files. `TextAssetHelper` handles the IO and `AssetDatabase` refreshes seamlessly.

```csharp
using BovineLabs.Core.Editor.Helpers;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

public static class BinaryDataBaker
{
    public static void SaveData(SerializedProperty prop, NativeArray<byte> unmanagedData)
    {
        // Instantly writes the NativeArray to disk and binds it to the serialized property
        TextAssetHelper.CreateForProperty(prop, "BakedNavigationData", unmanagedData);
    }
}
```

### 104. `BitFieldAttributeEditor` / `[BitField]`: Visualizing Unmanaged Masks
**Standard:** Storing boolean arrays in ECS is a memory disaster. Store data as a `uint` or `int` bitmask, and use the `[BitField]` attribute to render it as a human-readable multi-select dropdown in the Inspector.

```csharp
using BovineLabs.Core.Inspectors;
using UnityEngine;

public class FactionConfig : ScriptableObject
{
    // The designer sees a checklist of factions. The ECS runtime sees a single 32-bit integer.
    [BitField(Flags = true)]
    public int HostileFactionsMask;
}
```

### 105. `BlobAssetOwnerInspector`: Visualizing Raw Blob Memory
**Standard:** `BlobAssetReference` hides its memory footprint in the Inspector. `BlobAssetOwnerInspector` resolves the unmanaged pointer (`Target.BlobAssetBatchPtr`) to display the exact byte size, header count, and reference count of live blobs.

```csharp
using System;
using BovineLabs.Core.Editor.Inspectors;
using Unity.Entities;
using UnityEngine.UIElements;

// Internal Editor tooling. 
// Automatically intercepts BlobAssetOwner properties to expose UnsafeUtility.AsRef<BlobAssetBatchWrapper> data.
internal unsafe class CustomBlobInspector : PropertyInspector<BlobAssetOwner>
{
    public override VisualElement Build()
    {
        var parent = new VisualElement();
        if (this.Target.IsCreated)
        {
            // Parses unmanaged memory to show designers exact memory allocations
            parent.Add(new Label($"Total Blob Bytes: {this.Target.BlobAssetBatchPtr}"));
        }
        return parent;
    }
}
```

### 106. `HalfDrawer` & `half` Math: Custom Half-Precision GUI
**Standard:** Using 32-bit floats for colors, UVs, or network data wastes bandwidth. Use Unity's `half` (16-bit float), and `HalfDrawer` will seamlessly intercept it, displaying a normal `FloatField` to the designer while packing it into a `ushort` behind the scenes.

```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct NetworkedPosition : IComponentData
{
    // Evaluates to 8 bytes instead of 16. 
    // The inspector automatically shows a Vector4 field via Half4Drawer.
    public half4 CompressedPosition;
}
```

### 107. `InlineObjectProperty` / `[InlineObject]`: Nested ScriptableObjects
**Standard:** Forcing designers to click through multiple `ScriptableObject` assets breaks workflow focus. `[InlineObject]` unfolds the target asset's properties directly inside the parent's inspector view.

```csharp
using BovineLabs.Core.PropertyDrawers;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    // Instead of a simple object reference field, the entire EnemyStats UI is injected here
    [InlineObject]
    public EnemyStats StatsConfig;
}
```

### 108. `MinMaxAttributeDrawer` / `[MinMax]`: Unmanaged Vector2 Ranges
**Standard:** Designing random ranges using two separate float fields is error-prone. Use `[MinMax(min, max)]` on an ECS `float2` (represented as `Vector2` in authoring) to draw a dual-slider UI Toolkit element.

```csharp
using BovineLabs.Core.PropertyDrawers;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    [MinMax(1f, 10f)]
    public Vector2 SpawnDelayRange; // Bakes directly into a float2 MinMax bounds
}
```

### 109. `PrefabElementEditor` / `[PrefabElement]`: Locking Scene Overrides
**Standard:** Designers modifying Prefab instances in the scene hierarchy cause severe baking inconsistencies. `[PrefabElement]` intercepts the inspector, locking the instance and forcing edits directly onto the source `.prefab` asset.

```csharp
using BovineLabs.Core.PropertyDrawers;
using UnityEngine;

public class CoreConfigAuthoring : MonoBehaviour
{
    // Any change made to this field in the scene is instantly written to the source prefab
    [PrefabElement]
    public float GlobalDamageMultiplier;
}
```

### 110. `StableTypeHashAttributeDrawer`: Type-Safe ECS Component Selection
**Standard:** Storing component types as strings is brittle and slow. Use `[StableTypeHash]` to populate a dropdown with valid `IComponentData` types, storing their deterministic `ulong` Burst hash directly.

```csharp
using BovineLabs.Core.PropertyDrawers;
using UnityEngine;

public class SystemConfig : ScriptableObject
{
    // Filters the dropdown to ONLY show unmanaged, zero-sized tag components[StableTypeHash(StableTypeHashAttribute.TypeCategory.ComponentData, OnlyZeroSize = true)]
    public ulong TargetComponentHash;
}
```

### 111. `ToggleOption`: UI Toolkit Conditional Rendering
**Standard:** Custom Editor scripts with heavy `if` branches are obsolete. `ToggleOption` binds a UI Toolkit `Toggle` directly to a serialized boolean field, automatically hiding or revealing the dependent value field without manual redraws.

```csharp
using BovineLabs.Core.Editor.Inspectors;
using UnityEditor;
using UnityEngine.UIElements;

public class ConfigEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        // Automatically links 'UseOverride' to 'OverrideValue'
        root.Add(new ToggleOption(this.serializedObject, "UseOverride", "OverrideValue"));
        return root;
    }
}
```

### 112. `UnityObjectRefInspector`: Resolving Managed References
**Standard:** Hybrid ECS often requires `UnityObjectRef<T>` to hold references to GameObjects or Materials. The `UnityObjectRefInspector` catches this unmanaged struct and draws a standard Unity `ObjectField`, seamlessly resolving the internal `EntityId`.

```csharp
using Unity.Entities;
using UnityEngine;

public struct HybridRenderer : IComponentData
{
    // The BovineLabs inspector intercepts this and draws a normal Material slot
    public UnityObjectRef<Material> CustomMaterial;
}
```

### 113. `WeakObjectReferenceInspector`: Rendering Weak References
**Standard:** `WeakObjectReference<T>` is used for deferred or optional asset loading. The inspector correctly resolves `UntypedWeakReferenceId.GetEditorObject()` to ping the target asset in the project window without forcing it to load into memory.

```csharp
using Unity.Entities.Content;
using Unity.Entities;
using UnityEngine;

public struct AudioData : IComponentData
{
    // Inspector resolves the GUID safely
    public WeakObjectReference<AudioClip> ImpactSound;
}
```

### 114. `EntitySelection.GetAllSelectionsInWorld`: Editor-to-ECS Mapping
**Standard:** Selecting a GameObject in the hierarchy during Play Mode does not automatically select its baked ECS Entity. `EntitySelection.GetAllSelectionsInWorld` bridges the gap, translating GameObject Instance IDs to DOTS Entities.

```csharp
using BovineLabs.Core.Editor.Internal;
using Unity.Collections;
using Unity.Entities;

public static class EditorSelectionTools
{
    public static void LogSelectedEntities(World world)
    {
        var entities = new NativeList<Entity>(Allocator.Temp);
        var ids = new NativeList<int>(Allocator.Temp);
        
        // Safely maps Hierarchy selections to active ECS memory
        EntitySelection.GetAllSelectionsInWorld(world, entities, ids);
        
        entities.Dispose();
        ids.Dispose();
    }
}
```

### 115. `LoadPrefabsAsEntities`: Direct Scene Injection
**Standard:** Creating a SubScene just to test a single prefab is a massive waste of time. `LoadPrefabsAsEntities` intercepts the Prefab importer window, providing a "Load into World" button that injects the prefab via `SceneSystem.LoadSceneAsync` instantly.

```csharp
using BovineLabs.Core.Editor.Utility;
using UnityEditor;

public static class QuickTestUtility
{
    [MenuItem("Tools/BovineLabs/Toggle Prefab Loading")]
    public static void Toggle()
    {
        // Enables the Editor UI injection allowing direct Prefab-to-Entity spawning
        LoadPrefabsAsEntities.Enabled = !LoadPrefabsAsEntities.Enabled;
    }
}
```

### 116. `ReloadToolbarButton`: Injected Main Toolbar Actions
**Standard:** Recompiling scripts or updating global SubScene dependencies usually requires menu diving. The `MainToolbarPresetPostProcessor` weaves a reload button directly into the Unity Play/Pause toolbar.

```csharp
using BovineLabs.Core.Editor;
using BovineLabs.Core.Editor.Utility;
using UnityEditor.Toolbars;

public static class CustomToolbarActions
{
    // Automatically injected into the Unity Top Toolbar via Mono.Cecil IL weaving
    [MainToolbarPreset][MainToolbarElement("BovineLabs/Reload", defaultDockPosition = MainToolbarDockPosition.Middle)]
    private static MainToolbarElement ReloadButton()
    {
        return new MainToolbarDropdown(new MainToolbarContent("Reload"), null);
    }
}
```

### 117. `WelcomeWindow`: Feature Configuration Dashboard
**Standard:** Project-wide compiler `#define`s (e.g., `BL_DISABLE_PHYSICS_STATES`) scatter configurations. The `WelcomeWindow` acts as a centralized dashboard parsing the `asmdef` states and offering UI toggles for all BovineLabs Core features.

```csharp
using BovineLabs.Core.Editor.Welcome;
using UnityEditor;

public static class DashboardTools
{[MenuItem("BovineLabs/Manager")]
    public static void OpenDashboard()
    {
        // UI Toolkit window managing package dependencies and compiler flags
        EditorWindow.GetWindow<WelcomeWindow>().Show();
    }
}
```

### 118. `BaseObjectWindow`: Robust ECS Debugging Lists
**Standard:** Writing custom Editor Windows for ECS debugging from scratch results in UI lag. `BaseObjectWindow<TItem, TService, TPreferences>` provides a heavily optimized ListView foundation with caching, asynchronous sorting, and persistent memory across assembly reloads.

```csharp
using BovineLabs.Core.Editor.Windows.Base;
using UnityEditor;

// Extending this class instantly provides a high-performance searchable, sortable data grid
public sealed class SelectionHistoryWindow : BaseObjectWindow<SelectionHistoryItem, SelectionHistoryService, SelectionHistoryPreferences>
{
    protected override string StylesheetPath => "Packages/com.bovinelabs.core/Windows/Style.uss";
    protected override string RootClassName => "selection-history";
    protected override GUIContent WindowTitle => new GUIContent("History");
    
    // Abstract overrides handle job-based data fetching and UI binding
}
```

### 119. `FeatureToggle`: UXML Compiler Define Binds
**Standard:** Modifying `PlayerSettings.SetScriptingDefineSymbols` triggers domain reloads. The `FeatureToggle` UXML element groups related `#define`s together, visually graying out dependent features to prevent broken compilation states.

```csharp
using BovineLabs.Core.Editor.Welcome;
using UnityEngine.UIElements;

// Used in UI Toolkit (.uxml) files to create compiler-aware toggles:
// <BovineLabs.Core.Editor.Welcome.FeatureToggle Define="BL_DISABLE_TIME" Description="Overrides Unity's internal time system." />
```

### 120. `PrefabInstance` / `BakingOnlyEntity`: SubScene Bypass
**Standard:** Not every static mesh requires a full SubScene. The `PrefabInstance` authoring script uses a `BakingOnlyEntity` to bake matrix transforms into pure DOTS `LocalToWorld` components, destroying the GameObject immediately.

```csharp
using BovineLabs.Core.Authoring.BakeFast;
using Unity.Entities;
using Unity.Transforms;

public class PrefabInstanceBaker : Baker<PrefabInstance>
{
    public override void Bake(PrefabInstance authoring)
    {
        var entity = GetEntity(TransformUsageFlags.WorldSpace);
        
        // This component ensures the entity exists only during the baking phase
        AddComponent<BakingOnlyEntity>(entity);

        AddComponent(entity, new PrefabInstanceBake
        {
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.None),
            Transform = authoring.transform.localToWorldMatrix,
            IsStatic = authoring.gameObject.isStatic
        });
    }
}
```

### 121. `EntityBlobBakedData`: Deferred Blob Registration
**Standard:** Creating Blob Assets during arbitrary baking phases can cause duplication and memory leaks. `EntityBlobBakedData` safely holds a baked `BlobAssetReference` as a temporary component so that a dedicated baking system can merge them efficiently.

```csharp
using BovineLabs.Core.Collections;
using Unity.Entities;

// Attached during normal Baker passes
[BakingType]
public struct EntityBlobBakedData : IComponentData
{
    public Entity Target;
    public int Key;
    
    // Temporarily holds the reference before final map construction
    public BlobAssetReference<byte> Blob;
}
```

### 122. `EntityBlobBakingSystem`: Centralized Blob Map Construction
**Standard:** Scattered blob data ruins CPU cache locality. The `EntityBlobBakingSystem` runs exclusively in the `BakingSystem` world, grouping all temporary blobs into a mathematically perfect, zero-collision `BlobPerfectHashMap`.

```csharp
using BovineLabs.Core.Authoring.Blobs;
using BovineLabs.Core.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial struct EntityBlobBakingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<EntityBlobBakedData>().Build();
        
        // Constructs a contiguous BlobPerfectHashMap guaranteeing O(1) lookups
        // at runtime with zero hash collisions for the collected data.
        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var root = ref blobBuilder.ConstructRoot<BlobPerfectHashMap<int, int>>();
        
        // ... (Map population logic omitted for brevity) ...
        
        blobBuilder.Dispose();
    }
}
```

### 123. `CloneTransformAuthoring`: Automated Transform Mirroring
**Standard:** Hardcoding entity references across GameObjects in the editor is fragile. `CloneTransformAuthoring` explicitly links two GameObjects in the baker, translating their hierarchy dependency into a direct, unmanaged ECS `Entity` reference.

```csharp
using BovineLabs.Core.Clone;
using Unity.Entities;
using UnityEngine;

public class CloneTransformAuthoring : MonoBehaviour
{
    public GameObject Target;

    private class CloneTransformBaker : Baker<CloneTransformAuthoring>
    {
        public override void Bake(CloneTransformAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var targetEntity = GetEntity(authoring.Target, TransformUsageFlags.Dynamic);
            
            // Maps the unmanaged relationship for the runtime job
            AddComponent(entity, new CloneTransform { Value = targetEntity });
        }
    }
}
```

### 124. `LifeCycleAuthoring`: Standardizing Init/Destroy Tags
**Standard:** Structural changes at runtime (like `AddComponent<DestroyEntity>`) cause massive job stalls. `LifeCycleAuthoring` pre-bakes all lifecycle components onto the entity as `IEnableableComponent`s. At runtime, you simply toggle a bitmask.

```csharp
using BovineLabs.Core.LifeCycle;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class LifeCycleAuthoring : MonoBehaviour
{
    private class Baker : Baker<LifeCycleAuthoring>
    {
        public override void Bake(LifeCycleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            
            // Baked onto the entity but disabled by default. 
            // Toggling these at runtime incurs ZERO structural changes.
            AddComponent<DestroyEntity>(entity);
            SetComponentEnabled<DestroyEntity>(entity, false);
            
            AddComponent<InitializeEntity>(entity);
            SetComponentEnabled<InitializeEntity>(entity, true);
        }
    }
}
```

### 125. `LookupAuthoring`: Mapping Definitions to DynamicHashMaps
**Standard:** Complex configuration mappings shouldn't rely on string lookups. `LookupAuthoring` binds `ObjectDefinition` ScriptableObjects directly into an `IDynamicHashMap` buffer on a manager entity.

```csharp
using System;
using System.Collections.Generic;
using BovineLabs.Core.Authoring.ObjectManagement;
using BovineLabs.Core.Iterators;
using BovineLabs.Core.ObjectManagement;
using Unity.Entities;
using UnityEngine;
[RequireComponent(typeof(LifeCycleAuthoring))]
public abstract class LookupAuthoring<TMap, TValue> : MonoBehaviour
    where TMap : unmanaged, IDynamicHashMap<ObjectId, TValue>
    where TValue : unmanaged
{
    // Evaluated during the bake process to populate the unmanaged memory map
    public abstract bool TryGetInitialization(out TValue value);
}
```

### 126. `ObjectDefinitionAuthoring`: Biting Categories into Bitmasks
**Standard:** Tagging entities with multiple categorization components wastes chunk space. `ObjectDefinitionAuthoring` reads `ScriptableObject` categories and condenses them into a single fast-evaluating `uint` bitmask.

```csharp
using BovineLabs.Core.Authoring.ObjectManagement;
using BovineLabs.Core.ObjectManagement;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ObjectDefinitionAuthoring : MonoBehaviour
{
    public ObjectDefinition Definition;

    private class Baker : Baker<ObjectDefinitionAuthoring>
    {
        public override void Bake(ObjectDefinitionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            
            // Assigns the deterministic cross-network ObjectId
            AddComponent(entity, (ObjectId)authoring.Definition);

            // Condenses category layers into hardware-friendly bitwise operations
            uint categories = authoring.Definition.Categories.Value;
            while (categories != 0)
            {
                var categoryIndex = (byte)math.tzcnt(categories);
                categories ^= 1U << categoryIndex;
                // Apply categorized logic...
            }
        }
    }
}
```

### 127. `SubSceneEditorSet`: Editor-Only Streaming Mocks
**Standard:** Loading the entire game world in the editor slows down iteration. `SubSceneEditorSet` provides a lightweight `ScriptableObject` mapping to load fragmented, debug-only subsets of the world automatically.

```csharp
using BovineLabs.Core.ObjectManagement;
using BovineLabs.Core.Authoring.SubScenes;
using UnityEngine;

// Injects directly into SubSceneSettings via AutoRef[AutoRef(nameof(SubSceneSettings), nameof(SubSceneSettings.EditorSceneSets), nameof(SubSceneEditorSet), "Scenes/Editor")]
public class SubSceneEditorSet : SubSceneSetBase
{
    // Used exclusively to mock SubScene streaming environments inside the Editor
}
```

### 128. `MainToolbarPresetPostProcessor`: IL Weaving Unity Interfaces
**Standard:** Unity strictly prohibits custom scripts from adding elements to the main Editor toolbar. This `ILPostProcessor` intercepts compilation and manually weaves `[UnityEditor.Toolbars.UnityOnlyMainToolbarPresetAttribute]` into your custom toolbar methods.

```csharp
using System.Linq;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

// Post-compilation step that modifies the raw Intermediate Language (IL)
public sealed class MainToolbarPresetPostProcessor : ILPostProcessor
{
    public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
    {
        // 1. Locate BovineLabs.Core.Editor assembly
        // 2. Identify methods tagged with [MainToolbarPreset]
        // 3. Inject Unity's internal attribute to bypass the security check
        // 4. Return the modified PE stream
        return new ILPostProcessResult(null, new System.Collections.Generic.List<Unity.CompilationPipeline.Common.Diagnostics.DiagnosticMessage>());
    }
}
```

### 129. `AnalyzersProjectFileGeneration`: Auto-Injecting Roslyn Analyzers
**Standard:** Unity often drops custom Roslyn Analyzers during `.csproj` regeneration. This `AssetPostprocessor` catches the project file generation event and manually re-injects the `<Analyzer>` and `<AdditionalFiles>` XML tags.

```csharp
using System.IO;
using System.Xml.Linq;
using UnityEditor;

public class AnalyzersProjectFileGeneration : AssetPostprocessor
{
    private static string OnGeneratedCSProject(string path, string contents)
    {
        var xml = XDocument.Parse(contents);
        
        // Forces Unity's solution to acknowledge BovineLabs.FacetGenerator and DynamicGenerator
        // ensuring zero-downtime IDE syntax highlighting and error checking.
        
        using var str = new StringWriter();
        xml.Save(str);
        return str.ToString();
    }
}
```

### 130. `InitializeAllOnLoadExt`: Centralizing Editor Domain Reloads
**Standard:** Having 50 different classes tagged with `[InitializeOnLoad]` causes chaotic domain reload timings. Centralize editor initialization into a single deterministic orchestrator.

```csharp
using BovineLabs.Core.Editor;
using UnityEditor;

[InitializeOnLoad]
public static class InitializeAllOnLoadExt
{
    static InitializeAllOnLoadExt()
    {
        // Absolute control over Editor initialization sequence
        SetWorldToEditorWindows.Initialize();
        WorldSafeShutdown.Initialize();
        StartupSceneSwap.Initialize();
        ObjectInstantiate.Initialize();
    }
}
```

### 131. `ComponentInspectorWindow`: ScriptableObject-to-ECS Validation
**Standard:** Designers create `ScriptableObject` data, but ECS translates it to `IComponentData`. The `ComponentInspectorWindow` bridges this gap using `MultiColumnListView` to show designers the exact flat unmanaged data generated from their assets.

```csharp
using BovineLabs.Core.Authoring.ObjectManagement;
using UnityEditor;
using UnityEngine.UIElements;

public class ComponentInspectorWindow : EditorWindow
{
    private MultiColumnListView listView;

    private void CreateGUI()
    {
        this.listView = new MultiColumnListView();
        // Dynamically creates columns based on the unmanaged memory layout 
        // mapped from the selected ObjectDefinition.
        this.rootVisualElement.Add(this.listView);
    }
}
```

### 132. `ObjectInstantiate.Editor`: Dynamic Hierarchy Swapping
**Standard:** Placing ECS prefabs in a traditional Unity hierarchy destroys spatial parity during baking. `ObjectInstantiate` watches `ObjectChangeEvents` to instantly swap dropped prefabs with runtime-instantiation markers.

```csharp
using BovineLabs.Core.Authoring.ObjectManagement;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public partial class ObjectInstantiate : MonoBehaviour
{
    public static void TryReplace(GameObject newGameObject)
    {
        // Intercepts dragging a prefab into the scene and replaces it with a fast-spawning marker
        var go = new GameObject(newGameObject.name);
        var instance = go.AddComponent<ObjectInstantiate>();
        
        DestroyImmediate(newGameObject);
        Selection.SetActiveObjectWithContext(instance, go);
    }
}
```

### 133. `StartupSceneSwap`: Bypassing the Active Scene on Play
**Standard:** Hitting "Play" in a random subscene breaks the global ECS initialization logic. `StartupSceneSwap` intercepts `ExitingEditMode`, saves the currently open scene, and forcibly redirects Unity to the master `StartupScene`.

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class StartupSceneSwap
{
    private static void OnPlayModeStateChanged(PlayModeStateChange obj)
    {
        if (obj == PlayModeStateChange.EnteredPlayMode)
        {
            // Forces the pure ECS bootstrap scene to load regardless of what the designer was editing
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Bootstrap.unity", 
                new LoadSceneParameters { loadSceneMode = LoadSceneMode.Single });
        }
    }
}
```

### 134. `SubSceneEditorSystem`: Runtime Streaming Overrides
**Standard:** During debugging, you only want specific subscenes to load. `SubSceneEditorSystem` checks the `SharedStatic` override toggle set by the Editor Toolbar and aggressively manipulates `SubSceneLoadData` before streaming begins.

```csharp
using BovineLabs.Core.ConfigVars;
using BovineLabs.Core.SubScenes;
using Unity.Burst;
using Unity.Entities;

[UpdateBefore(typeof(SubSceneLoadingSystem))]
public partial class SubSceneEditorSystem : SystemBase
{
    public static readonly SharedStatic<int> Override = SharedStatic<int>.GetOrCreate<SubSceneEditorSystem>();

    protected override void OnUpdate()
    {
        // Suppresses standard loading and injects the developer's requested debug subset
        foreach (var (loadData, enableable) in SystemAPI.Query<RefRO<SubSceneLoadData>, EnabledRefRW<LoadSubScene>>())
        {
            if (!loadData.ValueRO.IsRequired)
            {
                enableable.ValueRW = false; // Hard disable
            }
        }
    }
}
```

### 135. `SubSceneEditorToolbar`: Fast SubScene Injector
**Standard:** Developers should not navigate folders to find test scenes. `SubSceneEditorToolbar` reads the global config and places a dropdown on the Unity Toolbar to toggle streaming datasets live.

```csharp
using BovineLabs.Core.Editor.SubScenes;
using UnityEditor;
using UnityEditor.Toolbars;

public static class SubSceneToolbar
{
    [MainToolbarPreset][MainToolbarElement("BovineLabs/SceneSet", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement SceneSelection()
    {
        // Renders a fast, generic menu bridging the managed UI to the unmanaged SharedStatic override
        return new MainToolbarDropdown(new MainToolbarContent("Scenes"), null);
    }
}
```

### 136. `SubScenePrebakeSystem`: Asynchronous Editor Pre-caching
**Standard:** Entering Play Mode incurs a massive stall if subscenes aren't baked. `SubScenePrebakeSystem` runs in the `Editor` world, silently forcing `SceneSystem` to cache baked representations of critical scenes in the background.

```csharp
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;

[WorldSystemFilter(WorldSystemFilterFlags.Editor)][UpdateInGroup(typeof(SceneSystemGroup))]
public partial class SubScenePrebakeSystem : SystemBase
{
    protected override void OnCreate()
    {
        // Asynchronously builds DOTS binary files while the designer is still working in Edit Mode
        var guid = new Unity.Entities.Hash128("...");
        SceneSystem.LoadSceneAsync(this.World.Unmanaged, guid);
    }
}
```

### 137. `ViewModelToolbar`: UI Toolkit Live Debugging
**Standard:** Identifying why a UI Toolkit element isn't updating is tedious. `ViewModelToolbar` uses reflection to scrape active `UIDocument` contexts and allows inspecting their managed ViewModels directly inside Unity's property viewer.

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class ViewModelDebugger
{
    public static void InspectActiveUI()
    {
        // Grabs the live, managed UI data context bridging the gap to the ECS logic
        var doc = Object.FindAnyObjectByType<UIDocument>();
        var viewModel = doc.rootVisualElement.dataSource;
        
        Selection.activeObject = BovineLabs.Core.Editor.UI.ObjectSelectionProxy.CreateInstance(viewModel);
    }
}
```

### 138. `CloneTransformSystem`: Unmanaged Parent-Child Bypassing
**Standard:** Unity's `Parent` / `Child` system has massive synchronization overhead. `CloneTransformSystem` utilizes an unmanaged `ComponentLookup` to directly overwrite a follower's `LocalTransform` with the target's data, strictly inside the `TransformSystemGroup`.

```csharp
using BovineLabs.Core.Clone;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(TransformSystemGroup), OrderFirst = true)]
public partial struct CloneTransformSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new CloneTransformJob { LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true) }.Schedule();
    }

    [BurstCompile]
    private partial struct CloneTransformJob : IJobEntity
    {
        [ReadOnly] [NativeDisableContainerSafetyRestriction] 
        public ComponentLookup<LocalTransform> LocalTransforms;

        private void Execute(ref LocalTransform transform, in CloneTransform clone)
        {
            // Zero hierarchy overhead, instant direct memory copy of the matrix
            if (this.LocalTransforms.TryGetComponent(clone.Value, out var copy))
            {
                transform = copy;
            }
        }
    }
}
```

### 139. `AfterSceneSystemGroup`: Post-Load Synchronization
**Standard:** ECS systems initializing components must know *when* a subscene has finished streaming. `AfterSceneSystemGroup` explicitly executes immediately following Unity's `SceneSystemGroup` to catch freshly streamed entities before normal simulations begin.

```csharp
using BovineLabs.Core.Groups;
using Unity.Entities;
using Unity.Scenes;
[WorldSystemFilter(Worlds.Simulation)]
[UpdateAfter(typeof(SceneSystemGroup))][UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class AfterSceneSystemGroup : ComponentSystemGroup
{
    // Houses any logic requiring immediate execution upon a new chunk entering the world
}
```

### 140. `AfterTransformSystemGroup` / `BeforeTransformSystemGroup`
**Standard:** Intermingling transform modifications with logic creates frame-behind glitches. Write systems modifying vectors in `BeforeTransformSystemGroup`. Read systems calculating physics or rendering data go in `AfterTransformSystemGroup`.

```csharp
using Unity.Entities;
using Unity.Transforms;

[WorldSystemFilter(Worlds.Simulation)][UpdateBefore(typeof(TransformSystemGroup))]
public partial class BeforeTransformSystemGroup : ComponentSystemGroup
{
    // Strict phase for altering LocalTransform components
}

[WorldSystemFilter(Worlds.Simulation)]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial class AfterTransformSystemGroup : ComponentSystemGroup
{
    // Strict phase for calculating distances or Line-of-Sight based on up-to-date matrices
}
```