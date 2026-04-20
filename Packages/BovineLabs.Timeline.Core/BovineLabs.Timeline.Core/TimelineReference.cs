using Unity.Entities;

namespace BovineLabs.Timeline.Core
{
    /// <summary>
    ///     Tag component used to identify entities driven by a Timeline Director.
    ///     Used by StartUI and other systems to locate timeline-bound entities.
    /// </summary>
    public struct TimelineReference : IComponentData
    {
    }
}