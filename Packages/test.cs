var a = Unity.Collections.Allocator.Temp;
unsafe {
    float* ptr = (float*)Unity.Collections.AllocatorManager.Allocate(a, sizeof(float), Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AlignOf<float>(), 10);
    Unity.Collections.AllocatorManager.Free(a, ptr);
}
return "Passed";