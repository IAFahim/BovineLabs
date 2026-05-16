// <copyright file="RukhankaAuthoringUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Authoring.Animation
{
    using BovineLabs.Timeline.Authoring;
    using Rukhanka.Toolbox;
    using UnityEngine;

    internal static class RukhankaAuthoringUtility
    {
        public static uint HashName(string value)
        {
            return string.IsNullOrEmpty(value) ? 0u : value.CalculateHash32();
        }

        public static int ResolveLayerIndex(Animator animator, string layerName, int fallbackIndex)
        {
            if (animator == null || string.IsNullOrEmpty(layerName))
            {
                return fallbackIndex;
            }

            var resolved = animator.GetLayerIndex(layerName);
            return resolved >= 0 ? resolved : fallbackIndex;
        }

        public static Animator ResolveBindingAnimator(in BakingContext context)
        {
            if (context.Director == null || context.Track == null)
            {
                return null;
            }

            return context.Director.GetGenericBinding(context.Track) as Animator;
        }
    }
}

#endif