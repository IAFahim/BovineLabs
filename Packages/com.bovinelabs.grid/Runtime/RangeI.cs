namespace BovineLabs.Grid
{
    public struct RangeI
    {
        public int Offset;
        public int Count;

        public RangeI(int offset, int count)
        {
            Offset = offset;
            Count = count;
        }

        public int End => Offset + Count;

        public bool Contains(int index)
        {
            return (uint)(index - Offset) < (uint)Count;
        }
    }
}
