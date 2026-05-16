// <copyright file="TimeScaleClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Data.Time
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Serialized configuration baked for time scale clips.
    /// </summary>
    public struct TimeScaleClipData : IComponentData
    {
        public BlobAssetReference<TimeScaleClipBlob> Value;
    }

    /// <summary>
    /// Blob holding time scale clip parameters.
    /// </summary>
    public struct TimeScaleClipBlob
    {
        public float TargetScale;
        public float ClampMin;
        public float ClampMax;
        public bool UseCurve;
        public bool RestoreOnDeactivate;
        public BlobCurve Curve;
    }
}
