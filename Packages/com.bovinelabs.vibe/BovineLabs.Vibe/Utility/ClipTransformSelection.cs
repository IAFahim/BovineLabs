namespace BovineLabs.Vibe
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data.LocalTransform;
    using BovineLabs.Vibe.Data.NonUniformScale;
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Helper methods for resolving whether a clip should use its captured transform or the track baseline.
    /// </summary>
    internal static class ClipTransformSelection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LocalTransform SelectLocalTransform(
            bool useClipInitial, in LocalTransform clipInitial, ref ComponentLookup<LocalTransformInitial> trackInitials, in Clip clip)
        {
            return useClipInitial ? clipInitial : GetTrackLocalTransform(ref trackInitials, clip);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LocalTransform GetTrackLocalTransform(ref ComponentLookup<LocalTransformInitial> trackInitials, in Clip clip)
        {
            return trackInitials[clip.Track].Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PostTransformMatrix SelectPostTransformMatrix(
            bool useClipInitial, in PostTransformMatrix clipInitial, ref ComponentLookup<PostTransformMatrixInitial> trackInitials, in Clip clip)
        {
            return useClipInitial ? clipInitial : GetTrackPostTransformMatrix(ref trackInitials, clip);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PostTransformMatrix GetTrackPostTransformMatrix(ref ComponentLookup<PostTransformMatrixInitial> trackInitials, in Clip clip)
        {
            return trackInitials[clip.Track].Value;
        }
    }
}
