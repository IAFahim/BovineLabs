#if BL_REACTION
namespace BovineLabs.Vibe
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Timeline.Data;
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Provides utilities for retrieving target transforms defined on a director.
    /// </summary>
    internal static class TargetResolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResolveLocalTransform(
            in DirectorRoot director, in Target target, ref ComponentLookup<Targets> targetsLookup, ref ComponentLookup<TargetsCustom> targetsCustomLookup,
            ref ComponentLookup<LocalTransform> localTransforms, out LocalTransform targetTransform)
        {
            targetTransform = default;

            if (target == Target.None)
            {
                return false;
            }

            if (!targetsLookup.TryGetComponent(director.Director, out var targets))
            {
                return false;
            }

            var targetEntity = targets.Get(target, director.Director, targetsCustomLookup);
            if (targetEntity == Entity.Null)
            {
                return false;
            }

            return localTransforms.TryGetComponent(targetEntity, out targetTransform);
        }
    }
}
#endif
