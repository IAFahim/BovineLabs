namespace BovineLabs.Vibe.Data.NonUniformScale
{
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Stores the post-transform matrix captured when a clip activates.
    /// </summary>
    public struct PostTransformMatrixClipInitial : IComponentData
    {
        public PostTransformMatrix Value;
    }
}
