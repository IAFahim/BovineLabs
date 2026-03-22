namespace Scripts.Data.SpinLockExample
{
    using Unity.Entities;

    public struct SpinLockConfig : IComponentData
    {
        public int WriterCount;
        public int ReaderCount;
        public float LockInterval;
        public Entity WriterPrefab;
        public Entity ReaderPrefab;
        public Entity ResourcePrefab;
    }

    public struct SpinLockController : IComponentData
    {
        public float Timer;
        public uint TotalLockAttempts;
        public uint SuccessfulLocks;
        public uint FailedLocks;
    }

    public struct SpinLockWriter : IComponentData
    {
        public int WriterId;
        public float LockAttemptTime;
    }

    public struct SpinLockReader : IComponentData
    {
        public int ReaderId;
        public float LockAttemptTime;
    }

    public struct SharedResource : IComponentData
    {
        public int Value;
        public uint LastWriterId;
    }
}
