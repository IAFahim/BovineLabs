using BovineLabs.Core.EntityCommands;

namespace BovineLabs.Timeline.Core.Data.Builders
{
    public struct TimelineReferenceBuilder
    {
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            builder.AddComponent<TimelineReference>();
        }
    }
}