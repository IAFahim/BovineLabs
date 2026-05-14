using Unity.Collections;

namespace BovineLabs.Grid.Belief
{
    public unsafe struct BeliefState
    {
        public Grid2D Grid;
        public int LabelCount;
        public float* Unary;
        public float* Messages;
        public float* MessagesNext;
        public float* Belief;
        public float* Scratch;
        public Unity.Collections.AllocatorManager.AllocatorHandle Allocator;
    }
}