namespace BovineLabs.Grid
{
    public struct HeapNode
    {
        public int Id;
        public float Key0;
        public float Key1;

        public HeapNode(int id, float key0, float key1 = 0f)
        {
            Id = id;
            Key0 = key0;
            Key1 = key1;
        }
    }
}
