using Unity.Entities;

namespace BovineLabs.Quill.Grid.Data
{
    public struct GridAgentPathVisual : IBufferElementData
    {
        public int AgentIndex;
        public int TimeStep;
        public int Cell;

        public GridAgentPathVisual(int agentIndex, int timeStep, int cell)
        {
            AgentIndex = agentIndex;
            TimeStep = timeStep;
            Cell = cell;
        }
    }
}