using Unity.Entities;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridConstraintVisual : IBufferElementData
    {
        public int Agent;
        public int Cell;
        public int Time;

        public GridConstraintVisual(int agent, int cell, int time)
        {
            Agent = agent;
            Cell = cell;
            Time = time;
        }
    }
}