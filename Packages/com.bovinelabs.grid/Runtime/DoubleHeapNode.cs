namespace BovineLabs.Grid
{
    public struct DoubleHeapNode
    {
        public int Id;
        public double Key0;
        public double Key1;

        public DoubleHeapNode(int id, double key0, double key1 = 0.0)
        {
            Id = id;
            Key0 = key0;
            Key1 = key1;
        }
    }
}
