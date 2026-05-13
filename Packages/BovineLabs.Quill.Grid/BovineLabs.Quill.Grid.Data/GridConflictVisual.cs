using Unity.Entities;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridConflictVisual : IBufferElementData
    {
        public int AgentA;
        public int AgentB;
        public int Cell;
        public int Time;

        public GridConflictVisual(int agentA, int agentB, int cell, int time)
        {
            AgentA = agentA;
            AgentB = agentB;
            Cell = cell;
            Time = time;
        }
    }
}