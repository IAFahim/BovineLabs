namespace BovineLabs.Vibe.Data.NonUniformScale
{
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Stores the post-transform matrix captured when a track first activates so we can restore it when deactivated.
    /// </summary>
    public struct PostTransformMatrixInitial : IComponentData
    {
        public PostTransformMatrix Value;
    }
}
