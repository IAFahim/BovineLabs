// <copyright file="CMCameraActivateClipData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    /// <summary>
    /// Parameters that control how a Cinemachine camera should be activated by a timeline clip.
    /// </summary>
    public struct CMCameraActivateClipData : IComponentData
    {
        public BlobAssetReference<CMCameraActivateClipBlob> Value;
    }

    /// <summary>
    /// Blob data for Cinemachine camera activation clips.
    /// </summary>
    public struct CMCameraActivateClipBlob
    {
        /// <summary>
        /// Enables updating the camera Enabled flag.
        /// </summary>
        public bool SetEnabled;

        /// <summary>
        /// Desired enabled state when <see cref="SetEnabled"/> is true.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Enables updating the camera priority.
        /// </summary>
        public bool SetPriority;

        /// <summary>
        /// Desired priority settings when <see cref="SetPriority"/> is true.
        /// </summary>
        public PrioritySettings Priority;

        /// <summary>
        /// Enables updating the camera output channel.
        /// </summary>
        public bool SetOutputChannel;

        /// <summary>
        /// Desired output channel when <see cref="SetOutputChannel"/> is true.
        /// </summary>
        public OutputChannels OutputChannel;

        /// <summary>
        /// Enables updating the camera blend hint.
        /// </summary>
        public bool SetBlendHint;

        /// <summary>
        /// Desired blend hint when <see cref="SetBlendHint"/> is true.
        /// </summary>
        public CinemachineCore.BlendHints BlendHint;
    }
}
#endif
