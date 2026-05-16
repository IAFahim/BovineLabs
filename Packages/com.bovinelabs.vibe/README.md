# BovineLabs Vibe

Vibe is a DOTS-first feedback system for Unity ECS. It focuses on making your game feel good by letting you layer and trigger time-based feedbacks across gameplay systems. Vibe is powered by the BovineLabs Timeline (Entities) implementation and the BovineLabs Bridge hybrid layer, so authoring stays in Timeline while runtime stays in ECS and Burst.

## Highlights

- **Game feel focused**: Orchestrate shakes, springs, pulses, and other feedbacks without scattering logic across systems
- **Timeline-driven authoring**: Compose and blend feedbacks using tracks and clips
- **Reaction integration**: Trigger feedbacks from gameplay events without tight coupling
- **Hybrid-ready**: Drive ECS data and sync to GameObject components through Bridge when needed
- **Extensible**: Add new track types and clips to match your project

## Core Concepts

**Tracks** define a target and a type of feedback to apply over time.

**Clips** define the timed behavior (curves, durations, blends) that make up a feedback.

**Bindings** connect tracks to ECS entities or hybrid components.

## Getting Started (High Level)

1. Install required dependencies (Core, Timeline, Reaction, Bridge; Cinemachine for camera tracks).
2. Create a Timeline asset and add Vibe tracks and clips.
3. Bind targets and trigger playback from code or Reaction events.

## Track and Clip Catalog

Clip names below match the Timeline clip class names. "Initial" clips capture starting values for restore.

### Transform

- DOTS/Transform/Position Track (binds `Transform`): `PositionWorldClip`, `PositionOffsetClip`, `PositionTargetClip`, `PositionCurveClip`, `PositionShakeClip`, `PositionWiggleClip`, `PositionSpringClip`, `PositionOrbitClip`, `PositionInitialClip`
- DOTS/Transform/Rotation Track (binds `Transform`): `RotationLookAtTargetClip`, `RotationLookAtStartClip`, `RotationLookAtDirectionClip`, `RotationLookAtRotationClip`, `RotationShakeClip`, `RotationWiggleClip`, `RotationSpringClip`, `RotationInitialClip`
- DOTS/Transform/Scale Track (binds `Transform`): `ScaleConstantClip`, `ScaleOffsetClip`, `ScaleCurveClip`, `ScaleShakeClip`, `ScaleWiggleClip`, `ScaleSpringClip`, `ScaleInitialClip`
- DOTS/Transform/Non-Uniform Scale Track (binds `Transform`): `SquashStretchConstantClip`, `SquashStretchCurveClip`, `SquashStretchShakeClip`, `SquashStretchSpringClip`, `NonUniformScaleInitialClip`

### Rendering

- DOTS/Rendering/Light Track (binds `Light`): `LightConstantClip`, `LightInitialClip`, `LightFlickerClip`, `LightExtendedConstantClip`, `LightExtendedCurveClip`
- DOTS/Rendering/URP Material Property Track (binds `Renderer`): `URPMaterialPropertyClip`
- DOTS/Rendering/HDRP Material Property Track (binds `Renderer`): `HDRPMaterialPropertyClip`

### Volume (URP)

- DOTS/Rendering/Volume Settings Track (binds `Volume`): `VolumeSettingsClip`, `VolumeSettingsInitialClip`
- DOTS/Rendering/Volume/Bloom Track (binds `Volume`): `VolumeBloomClip`, `VolumeBloomInitialClip`
- DOTS/Rendering/Volume/Channel Mixer Track (binds `Volume`): `VolumeChannelMixerClip`, `VolumeChannelMixerInitialClip`
- DOTS/Rendering/Volume/Chromatic Aberration Track (binds `Volume`): `VolumeChromaticAberrationClip`, `VolumeChromaticAberrationInitialClip`
- DOTS/Rendering/Volume/Color Adjustments Track (binds `Volume`): `VolumeColorAdjustmentsClip`, `VolumeColorAdjustmentsInitialClip`
- DOTS/Rendering/Volume/Color Lookup Track (binds `Volume`): `VolumeColorLookupClip`, `VolumeColorLookupInitialClip`
- DOTS/Rendering/Volume/Depth Of Field Track (binds `Volume`): `VolumeDepthOfFieldClip`, `VolumeDepthOfFieldInitialClip`
- DOTS/Rendering/Volume/Film Grain Track (binds `Volume`): `VolumeFilmGrainClip`, `VolumeFilmGrainInitialClip`
- DOTS/Rendering/Volume/Lens Distortion Track (binds `Volume`): `VolumeLensDistortionClip`, `VolumeLensDistortionInitialClip`
- DOTS/Rendering/Volume/Lift Gamma Gain Track (binds `Volume`): `VolumeLiftGammaGainClip`, `VolumeLiftGammaGainInitialClip`
- DOTS/Rendering/Volume/Motion Blur Track (binds `Volume`): `VolumeMotionBlurClip`, `VolumeMotionBlurInitialClip`
- DOTS/Rendering/Volume/Panini Projection Track (binds `Volume`): `VolumePaniniProjectionClip`, `VolumePaniniProjectionInitialClip`
- DOTS/Rendering/Volume/Screen Space Lens Flare Track (binds `Volume`): `VolumeScreenSpaceLensFlareClip`, `VolumeScreenSpaceLensFlareInitialClip`
- DOTS/Rendering/Volume/Shadows Midtones Highlights Track (binds `Volume`): `VolumeShadowsMidtonesHighlightsClip`, `VolumeShadowsMidtonesHighlightsInitialClip`
- DOTS/Rendering/Volume/Split Toning Track (binds `Volume`): `VolumeSplitToningClip`, `VolumeSplitToningInitialClip`
- DOTS/Rendering/Volume/Tonemapping Track (binds `Volume`): `VolumeTonemappingClip`, `VolumeTonemappingInitialClip`
- DOTS/Rendering/Volume/Vignette Track (binds `Volume`): `VolumeVignetteClip`, `VolumeVignetteInitialClip`
- DOTS/Rendering/Volume/White Balance Track (binds `Volume`): `VolumeWhiteBalanceClip`, `VolumeWhiteBalanceInitialClip`

Color Curves is currently not supported because it uses managed TextureCurve data.

### Camera

- DOTS/Camera/Matrix Shift Track (binds `Camera`): `CameraMatrixShiftClip`, `CameraMatrixShiftInitialClip`

### Audio

- DOTS/Audio/Audio Source Clip Track (binds `AudioSource`): `AudioSourceClip`
- DOTS/Audio/Audio Source Data Track (binds `AudioSource`): `AudioSourceDataClip`, `AudioSourceDataInitialClip`, `AudioSourceVolumeSweepClip`, `AudioSourcePitchSweepClip`
- DOTS/Audio/Audio Source Pan Sweep Track (binds `AudioSource`): `AudioSourcePanSweepClip`
- DOTS/Audio/Audio Source Trigger Track (binds `AudioSource`): `AudioSourceTriggerClip`
- DOTS/Audio/Audio Mixer Snapshot Track (binds `AudioListener`): `AudioMixerSnapshotClip`
- DOTS/Audio/Chorus Filter Track (binds `AudioChorusFilter`): `AudioChorusFilterClip`, `AudioChorusFilterSweepClip`, `AudioChorusFilterInitialClip`
- DOTS/Audio/Distortion Filter Track (binds `AudioDistortionFilter`): `AudioDistortionFilterClip`, `AudioDistortionFilterSweepClip`, `AudioDistortionFilterInitialClip`
- DOTS/Audio/Echo Filter Track (binds `AudioEchoFilter`): `AudioEchoFilterClip`, `AudioEchoFilterSweepClip`, `AudioEchoFilterInitialClip`
- DOTS/Audio/High Pass Filter Track (binds `AudioHighPassFilter`): `AudioHighPassFilterClip`, `AudioHighPassFilterSweepClip`, `AudioHighPassFilterInitialClip`
- DOTS/Audio/Low Pass Filter Track (binds `AudioLowPassFilter`): `AudioLowPassFilterClip`, `AudioLowPassFilterSweepClip`, `AudioLowPassFilterInitialClip`
- DOTS/Audio/Reverb Filter Track (binds `AudioReverbFilter`): `AudioReverbFilterClip`, `AudioReverbFilterSweepClip`, `AudioReverbFilterInitialClip`
- DOTS/Audio/Music Track (binds none): `MusicSelectionClip`

### Physics (Unity Physics)

- DOTS/Physics/Impulse Track (binds `Rigidbody`): `PhysicsImpulseClip`, `PhysicsDirectionalImpulseClip`, `PhysicsTorqueImpulseClip`, `PhysicsImpulseAtPointClip`, `PhysicsRadialImpulseClip`
- DOTS/Physics/Custom Impulse Track (binds `PhysicsBodyAuthoring`): `PhysicsImpulseClip`, `PhysicsDirectionalImpulseClip`, `PhysicsTorqueImpulseClip`, `PhysicsImpulseAtPointClip`, `PhysicsRadialImpulseClip`

### Time

- DOTS/Time/Time Scale Track: `TimeScaleClip`

### Timeline

- DOTS/Timeline/Cancel Track (binds none): `TimelineCancelClip`

### UI (Anchor)

Requires the BovineLabs Anchor package and `BL_ANCHOR` define.

- DOTS/UI/Anchor Nav Track (binds none): `NavigateClip`, `ClearNavigateClip`, `ClearBackStackClip`, `PopBackStackClip`, `PopBackStackToPanelClip`, `CloseAllPopupsClip`, `ClosePopupClip`

### Cinemachine

Requires the Unity Cinemachine package.

- DOTS/Cinemachine/Activate Track (binds `CinemachineCamera`): `CMActivateClip`
- DOTS/Cinemachine/Camera Track (binds `CinemachineCamera`): `CMCameraClip`, `CMCameraInitialClip`
- DOTS/Cinemachine/Camera Offset Track (binds `CinemachineCameraOffset`): `CMCameraOffsetClip`, `CMCameraOffsetInitialClip`
- DOTS/Cinemachine/Brain Track (binds `CinemachineBrain`): `CMBrainClip`, `CMBrainInitialClip`
- DOTS/Cinemachine/Follow Track (binds `CinemachineFollow`): `CMFollowClip`, `CMFollowInitialClip`
- DOTS/Cinemachine/Follow Zoom Track (binds `CinemachineFollowZoom`): `CMFollowZoomClip`, `CMFollowZoomInitialClip`
- DOTS/Cinemachine/Orbit Follow Track (binds `CinemachineOrbitalFollow`): `CMOrbitFollowClip`, `CMOrbitFollowInitialClip`
- DOTS/Cinemachine/Third Person Follow Track (binds `CinemachineThirdPersonFollowDots`, requires `UNITY_PHYSICS`): `CMThirdPersonFollowClip`, `CMThirdPersonFollowInitialClip`
- DOTS/Cinemachine/Group Framing Track (binds `CinemachineGroupFraming`): `CMGroupFramingClip`, `CMGroupFramingInitialClip`
- DOTS/Cinemachine/Hard Lock To Target Track (binds `CinemachineHardLockToTarget`): `CMHardLockToTargetClip`, `CMHardLockToTargetInitialClip`
- DOTS/Cinemachine/Hard Look At Track (binds `CinemachineHardLookAt`): `CMHardLookAtClip`, `CMHardLookAtInitialClip`
- DOTS/Cinemachine/Pan Tilt Track (binds `CinemachinePanTilt`): `CMPanTiltClip`, `CMPanTiltInitialClip`
- DOTS/Cinemachine/Position Composer Track (binds `CinemachinePositionComposer`): `CMPositionComposerClip`, `CMPositionComposerInitialClip`
- DOTS/Cinemachine/Recomposer Track (binds `CinemachineRecomposer`): `CMRecomposerClip`, `CMRecomposerInitialClip`
- DOTS/Cinemachine/Rotate With Follow Target Track (binds `CinemachineRotateWithFollowTarget`): `CMRotateWithFollowTargetClip`, `CMRotateWithFollowTargetInitialClip`
- DOTS/Cinemachine/Rotation Composer Track (binds `CinemachineRotationComposer`): `CMRotationComposerClip`, `CMRotationComposerInitialClip`
- DOTS/Cinemachine/Spline Dolly Track (binds `CinemachineSplineDolly`): `CMSplineDollyClip`, `CMSplineDollyInitialClip`
- DOTS/Cinemachine/Spline Dolly Target Track (binds `CinemachineSplineDolly`): `CMSplineDollyTargetClip`
- DOTS/Cinemachine/Volume Settings Track (binds `CinemachineVolumeSettings`): `CMVolumeSettingsClip`, `CMVolumeSettingsInitialClip`
- DOTS/Cinemachine/FreeLook Modifier Track (binds `CinemachineFreeLookModifier`): `CMFreeLookModifierClip`, `CMFreeLookModifierInitialClip`
- DOTS/Cinemachine/Basic Multi Channel Perlin Track (binds `CinemachineBasicMultiChannelPerlin`): `CMBasicMultiChannelPerlinClip`, `CMBasicMultiChannelPerlinInitialClip`

### Animation (Rukhanka)

Requires the Rukhanka package.

- DOTS/Rukhanka/Animator Parameter Track (binds `Animator`): `RukhankaAnimatorParameterClip`
- DOTS/Rukhanka/Animator Speed Track (binds `Animator`): `RukhankaAnimatorSpeedClip`
- DOTS/Rukhanka/Animator State Track (binds `Animator`): `RukhankaAnimatorCrossfadeClip`, `RukhankaAnimatorPlayStateClip`
