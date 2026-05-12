# BovineLabs Grid Algorithms - AI Optimization & Style Guide

This document outlines the strict performance, architectural, and stylistic guidelines for modifying, optimizing, or expanding the `com.bovinelabs.grid.*` packages. 

**Primary Directive:** Maximize performance using the Unity DOTS stack (`Unity.Burst`, `Unity.Collections`, `Unity.Mathematics`). 
---

## 1. The "Unsafe" Paradigm (Memory & Data Structures)

Currently, the grid algorithms rely heavily on `NativeArray<T>`, `NativeList<T>`, and `NativeQueue<T>`. To squeeze out maximum performance in deep inner loops, we must bypass the overhead of safety handles where applicable.

### DO:
*   **Use `UnsafeList<T>`, `UnsafeQueue<T>`, etc., for internal state.** Inside structs like `AnyaState`, `CbsState`, `MinHeap`, use the `Unity.Collections.LowLevel.Unsafe` equivalents of Native containers.
*   **Use Raw Pointers in Hot Loops.** Extract pointers (`T* ptr = (T*)array.GetUnsafePtr()`) *before* entering heavy `while` or `for` loops. Array indexing on `NativeArray` inside a hot loop generates safety check overhead (even if Burst strips some in release, pointers guarantee optimal asm generation).
*   **Use `Allocator.Temp` carefully.** For intra-frame allocations, prefer `Allocator.Temp` but always ensure proper `Dispose()` or use `UnsafeUtility.Malloc` / `Free` if building custom unmanaged structures.

### AVOID:
*   Passing `NativeArray<T>` into deep recursive or highly iterative functions. Pass `T*` and `int length` instead to avoid struct copying and safety handle checks.
*   Allocating *any* memory during `Step`, `Search`, or `Iterate` functions. All memory must be pre-allocated in `Create` or passed in via `State` structs.

---

## 2. Burst Compiler Directives & Hints

Burst relies on the developer to prove that memory does not overlap (aliasing) and to hint at branch prediction.

### DO:
*   **Apply `[BurstCompile]`** to all static API entry points, helper methods, and inner jobs.
*   **Use `[NoAlias]` generously.** When passing multiple arrays or pointers into a method (e.g., `float* costs`, `byte* blocked`), mark them with `[NoAlias]` so Burst knows they don't point to the same memory and can vectorize operations.
*   **Use `Hint.Likely` and `Hint.Unlikely`.** In pathfinding and grid algorithms, bounds checks and goal checks are highly predictable. 
    *   *Example:* `if (Hint.Unlikely(x < 0 || x >= width))`
*   **Use `Hint.Assume`.** If you know a grid dimension is a multiple of 4, use `Hint.Assume(width % 4 == 0)` to allow Burst to unroll and vectorize loops without scalar remainders.

---

## 3. Mathematics & Explicit Vectorization

Many grid algorithms (Belief Propagation, Fast Sweeping, Cellular Automata, WFC) evaluate 4 or 8 neighbors per cell. This is a perfect target for SIMD (Single Instruction, Multiple Data) using `Unity.Mathematics`.

### DO:
*   **Vectorize Neighbor Lookups.** Instead of looping 4 times, process 4 neighbors simultaneously using `int4`, `float4`, and `bool4`.
    ```csharp
    // Instead of: for(int d=0; d<4; d++) { ... }
    int4 nx = p.x + new int4(1, 0, -1, 0);
    int4 ny = p.y + new int4(0, 1, 0, -1);
    bool4 inBounds = nx >= 0 & nx < width & ny >= 0 & ny < height;
    // Calculate indices and gather costs into a float4 using gather intrinsics or flat indexing
    ```
*   **Use Bitwise Math.** Replace iterative counting and branching with bitwise operations.
    *   *WFC Optimization:* Replace the loop that counts possible patterns with `math.countbits(possibleBits)`.
    *   *Lowest Set Bit:* Use `math.tzcnt(bits)` to find the index of the next available pattern/state without looping.
*   **Use `math.select`.** Eliminate branching (`if / else`) in inner loops by calculating both sides and using `math.select(falseValue, trueValue, condition)`.
    *   *Example:* `float cost = math.select(float.PositiveInfinity, standardCost, inBounds);`

---

## 4. Specific Algorithm Refactoring Targets

When optimizing specific packages within this repository, address these known bottlenecks:

### A. Priority Queues (`MinHeap.cs`)
*   Change the backing array from `NativeList<HeapNode>` to `UnsafeList<HeapNode>` or a raw `HeapNode*` block allocated via `UnsafeUtility.Malloc`.
*   Ensure `HeapNode` is tightly packed. Consider struct layouts and alignments (`[StructLayout(LayoutKind.Sequential, Pack = ...)]`).

### B. Wave Function Collapse (`wfc`)
*   Replace loops over `64` bits with `math.countbits()`, `math.tzcnt()`, and `math.lzcnt()`. 
*   `Compatibility` checks should be purely bitwise `&` and `|` operations. Remove all `for (int i=0; i<64; i++)` iterations when checking entropy.

### C. Belief Propagation & Flow Fields (`belief`, `continuum`)
*   **Gauss-Seidel / Sweeping:** Flatten 2D loops into 1D pointer increments where possible. 
*   Instead of calling `s.Grid.ToIndex(x, y)` repeatedly in loops, maintain a running index (`int idx = y * width + x; idx++`).
*   In `BeliefApi.Iterate`, you read from `Messages` and write to `MessagesNext`. Ensure these are passed to the inner logic as `[NoAlias] float* current, [NoAlias] float* next` to guarantee vectorization.

### D. Distance Transforms (`edt`, `fastmarching`)
*   Divide independent rows/columns into `IJobParallelFor` jobs. EDT (Felzenszwalb-Huttenlocher) is perfectly separable by row and then by column. Create Job structs for these phases rather than doing it on the main thread.

---

## 5. Structuring State Data (AoS vs. SoA)

Grid data is currently often passed as multiple parallel `NativeArray`s or interleaved structs.

### DO:
*   **Evaluate SoA (Struct of Arrays).** If an algorithm iterates over millions of cells but only needs `Cost` and not `ParentIndex`, keep `Cost` and `ParentIndex` in separate arrays (which you are mostly doing, e.g., `s.G`, `s.Parent`). Keep this pattern!
*   **Cache essential grid data locally.** Inside an algorithm's heavy loop, cache `width`, `height`, and array pointers to local stack variables. 
    ```csharp
    int w = s.Grid.Width;
    int h = s.Grid.Height;
    float* gPtr = (float*)s.G.GetUnsafePtr();
    // Do heavy work
    ```

---

## 6. Code Style & Safety Checks

### DO:
*   **Wrap Unsafe code cleanly.** 
*   **Use Conditional Compilation for Safety.** Write bounds checks inside `[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]` methods. Do not let safety checks pollute release builds.
    ```csharp
    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private static void CheckBounds(int idx, int capacity) {
        if ((uint)idx >= (uint)capacity) throw new IndexOutOfRangeException();
    }
    ```
*   **Use `ref` returns.** For lookup tables or grid cells, use `ref T` returns to avoid struct copying.

### AVOID:
*   Avoid `float.PositiveInfinity` comparisons in hot loops if integer maximums (`int.MaxValue`) can be used, as float comparisons can sometimes be slower or result in `NaN` propagation issues if not careful. If using floats, ensure `math.isinf()` or direct equality is used properly.
*   Never use Managed Types (`class`, `IEnumerable`, `Action`, `Func`) anywhere in the grid processing pipelines.

---
**End of Guidelines.** When submitting PRs or generating code modifications, ensure your output rigorously adheres to these constraints.
