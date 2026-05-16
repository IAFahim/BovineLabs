// <copyright file="SpaceVector3.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring.LocalTransform
{
    using System;
    using BovineLabs.Vibe.Data.LocalTransform;
    using UnityEngine;

    /// <summary>
    /// Serializable representation of a <see cref="Vector3"/> expressed relative to a specific <see cref="TransformSpace"/>.
    /// </summary>
    [Serializable]
    public class SpaceVector3
    {
        [Tooltip("Vector value expressed in the selected transform space.")]
        public Vector3 Value;

        [Tooltip("Transform space used to interpret the value.")]
        public TransformSpace Space;

        [Tooltip(Strings.UseClipActivationTooltip)]
        public bool UseClipActivation = true;

        public static SpaceVector3 World(Vector3 value)
        {
            return new SpaceVector3 { Space = TransformSpace.World, Value = value };
        }

        public static SpaceVector3 Local(Vector3 value)
        {
            return new SpaceVector3 { Space = TransformSpace.Local, Value = value };
        }
    }
}
