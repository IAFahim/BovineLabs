# BovineLabs Grid Algorithms - AI Optimization & Style Guide

This document outlines the strict performance, architectural, and stylistic guidelines for modifying, optimizing, or expanding the `com.bovinelabs.grid.*` packages. 

**Primary Directive:** Maximize performance using the Unity DOTS stack (`Unity.Burst`, `Unity.Collections`, `Unity.Mathematics`).

**Current Status:** 33/34 tests pass. One remaining failure: `AnyaTests.Search_WithWall` — see `Packages/todo.md` for details.

**Quick Start:** Edit code → run tests → parse with compactor. See §7 below. 
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

## 7. Testing & Iteration Loop

The project has tools for fast test iteration. Use them.

### Test Runner (headless, ~15s per targeted run)

```bash
UNITY_EDITOR="/home/i/Unity/Hub/Editor/6000.5.0b1/Editor/Unity"
PROJECT="/home/i/Documents/BovineLabs"

# Run one fixture
"$UNITY_EDITOR" -runTests -batchmode -projectPath "$PROJECT" \
  -testResults TR.xml -testPlatform EditMode \
  -testFilter "DominoTests" -logFile /dev/null 2>&1

# Run one method
  -testFilter "AnyaTests.Search_DirectLine"

# Run all
  # (omit -testFilter)
```

### Compactors (token-efficient result parsing)

Built from `Packages/com.bovinelabs.compactors/`. Build once:
```bash
cd Packages/com.bovinelabs.compactors && dotnet build -c Release
```

Run:
```bash
COMPACTOR="dotnet /home/i/Documents/BovineLabs/Packages/com.bovinelabs.compactors/bin/Release/net10.0/testresultscompact.dll"
$COMPACTOR --input TR.xml --stdout
```

Output on success:
```
status=passed total=42 passed=42 failed=0 skipped=0 inconclusive=0 duration=12.345
```

Output on failure:
```
status=failed total=42 passed=40 failed=1 skipped=1 inconclusive=0 duration=12.345

FAIL MyProject.Tests.PlayerTests.JumpsWhenGrounded
message:
Expected: True
But was: False
stack:
at MyProject.Tests.PlayerTests.cs:line 27
```

### Standard Debug Loop

```
1. Edit Runtime/XApi.cs
2. Run: UNITY_EDITOR -runTests ... -testFilter "XTests" ...
3. Parse: $COMPACTOR --input TR.xml --stdout
4. If failures → read message/stack → fix → goto 2
```

### unity-cli (live Unity, for probing)

Unity must be open. If not:
```bash
nohup /home/i/Unity/Hub/Editor/6000.5.0b1/Editor/Unity -batchmode -projectPath /home/i/Documents/BovineLabs &
```

```bash
unity-cli exec "return 42;"                    # basic probe
unity-cli editor refresh --compile              # after .cs edits
unity-cli console --type error                  # check for compile errors
cat << 'CSHARP' | unity-cli exec                # multi-line (use for anything complex)
return System.DateTime.Now;
CSHARP
```

**Limitation:** Grid sub-packages (domino, cbs, anya, etc.) are NOT loaded in the editor domain by default. `unity-cli exec` only sees assemblies with Editor platform or referenced by Editor assemblies. For algorithm debugging, use the test runner with `-testFilter`.

### Test Log Compactor

For compile errors from Unity's build log:
```bash
LOGCOMPACT="dotnet /home/i/Documents/BovineLabs/Packages/com.bovinelabs.compactors/bin/Release/net10.0/testlogcompact.dll"
$LOGCOMPACT --input TestLog.log --stdout
```

---

## 8. Algorithm Architecture: The State-API Pattern

Every algorithm follows a strict two-part structure:

1. **State Struct (`XState`)** — unmanaged, `[StructLayout(LayoutKind.Sequential)]`, contains all collections (`NativeArray`, `UnsafeList`, `MinHeap`, etc.).
2. **API Class (`XApi`)** — `public unsafe static class`, `[BurstCompile]`, contains only static methods that take `ref XState`.

### Required Lifecycle Methods

| Method | Purpose |
|---|---|
| `TryCreate(width, height, maxNodes, Allocator, out XState)` | Allocate all memory |
| `TryExecute(ref XState, ...)` or `TrySearch` / `TryStep` / `TryIterate` | Core algorithm |
| `Dispose(ref XState)` | Free all memory. Must be double-dispose safe (`if (X.IsCreated) X.Dispose()`) |

### Steppable Pattern (for visualization)

Some algorithms support frame-by-frame execution. Split into:
- `TryInitialize(ref XState, ...)` — sets up start/goal, clears heaps
- `TryStep(ref XState, ...)` — one expansion per call, returns `true` while running

Existing examples: `FastMarchingApi.TryPropagateStep`, `FieldDStarApi.TryStep`.

Algorithms that need this refactoring: `AnyaApi` (monolithic while loop → `TryInitSearch` + `TryStepSearch`), `WfcApi` (→ `TryObserveStep` + `TryPropagateStep`), `CbsApi` (nested A* in constraint tree → separate high-level/low-level steppers).

### Skeleton Template

```csharp
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using BovineLabs.Grid;

namespace BovineLabs.Grid.MyAlgo
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MyAlgoState
    {
        public Grid2D Grid;
        public NativeArray<float> Costs;
        public UnsafeList<int> Path;
    }

    [BurstCompile]
    public unsafe static class MyAlgoApi
    {
        public static bool TryCreate(int width, int height, Allocator a, out MyAlgoState s)
        {
            s = default;
            if (!Grid2D.TryCreate(width, height, out var g)) return false;
            s = new MyAlgoState
            {
                Grid = g,
                Costs = new NativeArray<float>(g.Length, a),
                Path = new UnsafeList<int>(g.Length, a),
            };
            return true;
        }

        [BurstCompile]
        public static bool TryExecute(ref MyAlgoState s, in NativeArray<byte> blocked)
        {
            int w = s.Grid.Width;
            int h = s.Grid.Height;
            int len = s.Grid.Length;
            float* costs = (float*)s.Costs.GetUnsafePtr();
            byte* blk = (byte*)blocked.GetUnsafeReadOnlyPtr();
            // Core logic with pointers, math.*, Hint.Likely/Unlikely
            return true;
        }

        public static void Dispose(ref MyAlgoState s)
        {
            if (s.Costs.IsCreated) s.Costs.Dispose();
            if (s.Path.IsCreated) s.Path.Dispose();
        }
    }
}
```

### Bitwise Packing (from BovineLabs.Recast)

Pack multiple values into single ints/uints to save memory:
```csharp
public uint SMin
{
    get => this.packedData & ((1u << 13) - 1);
    set => this.packedData = (this.packedData & ~((1u << 13) - 1)) | (value & ((1u << 13) - 1));
}
```

Use `ulong` as 64-bit masks (WFC states). Use `math.tzcnt()`, `math.countbits()` for fast state resolution. Avoid modulo in tight loops — use bitwise AND for power-of-2 divisors: `(dir + 1) & 0x3` instead of `(dir + 1) % 4`.

---

## 9. Test Assembly Setup (asmdef)

Test assemblies that aren't discovered by the runner always have asmdef issues. The working template:

```json
{
  "name": "com.bovinelabs.grid.X.tests",
  "references": [
    "com.bovinelabs.grid",
    "com.bovinelabs.grid.X",
    "Unity.Burst",
    "Unity.Collections",
    "Unity.Mathematics",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": true,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

If `overrideReferences != true` or `"nunit.framework.dll"` is missing or `includePlatforms` is empty → assembly compiles but test runner never discovers it. Symptom: `total=0` in compacted output despite tests existing.

---

## 9. Cross-Package Gotchas

### `NativeArray<T>.Fill()` Extension Method
Defined in `com.bovinelabs.grid/Runtime/MinHeap.cs` as `NativeArrayExtensions.Fill<T>`. Requires `using BovineLabs.Grid;` in any file that calls `.Fill()`. Missing using → `CS1061: 'NativeArray<float>' does not contain a definition for 'Fill'`.

### `GraphCutApi` API Surface
- `AddEdgeInternal(ref state, u, v, cap)` — must be `public static`, not `internal` (cross-assembly call).
- `Dispose(ref state)` — static method, NOT `state.Dispose()`.
- `BuildBinaryEnergy` adds pairwise edges in both directions (undirected). For bipartite matching (domino tiling), bypass it and build the flow network manually: source→black(cap 1), black→adjacent_white(cap 1, all 4 dirs), white→sink(cap 1).

### Domino Bipartite Matching
Must add edges in ALL 4 directions from black cells to white neighbors (not just right/down). Bottom-right corner black cells can only match left/up. Flow extraction must handle negative diff: `diff == -1` → `matchDir[v] = 1`, `diff == -w` → `matchDir[v] = 2`.

### Anya Interval Expansion (KNOWN BUG)
Each node expands in BOTH dy directions (dy=+1 and dy=-1). Despite this fix, the algorithm still fails `Search_WithWall`: start `(0,0)→(9,0)` with blocked cell at `(5,0)`. The path must go down, around, and back up. The root cause is that when expanding back up to row 0 from below, the interval projection from a root on row 0 projects to infinite width (root.y == interval.y triggers the special case), but the cellY check scans row 0 which still has the blocked cell at x=5, and the interval doesn't properly "skip over" it to rediscover x=6..9. The fix likely requires interval splitting (create two sub-intervals when a blocked cell is encountered, like `[0,5)` and `[6,10)`) rather than just `continue`-ing past blocked cells. LineOfSight shortcut handles directly visible goals, so this only affects paths requiring detours.

### CBS Constraint Limitation
Constraint format is `(agent, cell, time)` — vertex occupation only. Cannot express edge constraints ("agent cannot traverse edge at time T"). Swap conflicts get approximated by constraining the destination cell. For full edge conflict support, `CbsConstraint` would need `(agent, cellFrom, cellTo, time)` fields and `TryAStar` would need to check edge constraints in addition to vertex constraints.

### asmdef Changes
After modifying `.asmdef` files, delete old DLLs from `Library/ScriptAssemblies/` for clean rebuild. Without this, Unity may cache stale assembly references.

### MinHeap Uses `float` Keys
`HeapNode.Key0` is `float`. Anya uses `double` internally for costs but casts to `float` for the heap priority. This loses precision and can cause suboptimal node expansion order for large grids. If Anya optimality matters, consider a `double`-keyed heap variant.

---

## 10. Logging

Use BovineLabs logging, not `Debug.Log*`.
- In ECS systems: `var logger = SystemAPI.GetSingleton<BLLogger>(); logger.LogInfoString(...);`
- Outside systems: `BLGlobalLogger.LogInfoString(...);`
- See `Packages/shattered-debug-logging/SKILL.md` for full reference.

---

**End of Guidelines.** When submitting PRs or generating code modifications, ensure your output rigorously adheres to these constraints.
