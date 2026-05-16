// <copyright file="Strings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Authoring
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Shared tooltip text used by Vibe authoring clips.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "These are the documentation")]
    internal static class Strings
    {
        public const string UseClipActivationTooltip =
            "If true, the transform will be calculated when the clip activates. If false, it'll be calculated when the track activates.";

        public const string FixedPosition =
            "If true the position of the target will be taken on activation. If false, the position will be updated every frame.";

        public const string FixedRotation =
            "If true the rotation of the target will be taken on activation. If false, the rotation will be updated every frame.";

        public const string ShakeFrequencyTooltip = "Frequency in hertz used when generating shake noise.";

        public const string ShakeDampingTooltip = "Damping factor that eases the shake back to rest over time.";

        public const string ShakeAttenuationCurveTooltip = "Curve sampled over normalized clip time (0-1) to attenuate the shake amplitude.";

        public const string ShakeRemapAttenuationCurveTooltip = "Automatically remap the attenuation curve to the playable clip length.";

        public const string NoiseAmplitudeCurveTooltip = "Curve sampled over normalized clip time (0-1) to scale noise amplitude.";

        public const string NoiseRemapAmplitudeCurveTooltip = "Automatically remap the amplitude curve to the playable clip length.";

        public const string NoiseFrequencyCurveTooltip = "Curve sampled over normalized clip time (0-1) to scale noise frequency.";

        public const string NoiseRemapFrequencyCurveTooltip = "Automatically remap the frequency curve to the playable clip length.";

        public const string NoisePerAxisFrequencyMultiplierTooltip = "Multiplier applied per axis to the noise frequency.";

        public const string RandomSeedTooltip = "If 0 will use a random seed.";

        public const string SpringInitialVelocityTooltip = "Initial velocity applied to the spring at activation.";

        public const string SpringAutoDampingTooltip = "Automatically adjust damping so the spring settles within the clip.";

        public const string SpringDampingPerAxisTooltip = "Damping factor per axis applied when not matching the clip duration.";

        public const string SpringResidualAmplitudeTooltip = "Residual amplitude the spring should reach when matching clip duration.";

        public const string WiggleFrequencyTooltip = "Frequency, in hertz, of the wiggle signal.";

        public const string WiggleSmoothingTooltip = "Smooths the generated noise towards uniform motion.";

        public const string CurveStretchTooltip = "Stretch the curve so it spans the playable clip length.";

        public const string SpringFrequencyTooltip = "Oscillation frequency, in hertz, of the spring motion.";

        public const string SpringDampingTooltip = "Damping factor applied when not matching the clip duration.";
    }
}
