using Unity.Entities;
namespace BovineLabs.Quill.Grid.Data
{
    public struct GridPathVisual : IBufferElementData
    {
        public int From;
        public int To;
        public int Frame;

        public GridPathVisual(int from, int to, int frame = 0)
        {
            From = from;
            To = to;
            Frame = frame;
        }
    }
}
