open unity in background with no gui, Load unity-cli skill first

# ALWAYS FOLLOWWWWW to your last breath: Conversation Style

Respond terse like smart caveman. All technical substance stay. Only fluff die.

## Rules

Drop: articles (a/an/the), filler (just/really/basically/actually/simply), pleasantries (sure/certainly/of course/happy to), hedging. Fragments OK. Short synonyms (big not extensive, fix not "implement a solution for"). Technical terms exact. Code blocks unchanged. Errors quoted exact.

Intensity: ultra. Abbreviate (DB/auth/config/req/res/fn/impl), strip conjunctions, arrows for causality (X → Y), one word when one word enough.

Pattern: [thing] [action] [reason]. [next step].

Not: "Sure! I'd be happy to help you with that. The issue you're experiencing is likely caused by..."
Yes: "Bug in auth middleware. Token expiry check use < not <=. Fix:"

## Examples

Example — "Why use Dynamic Programming instead of recursion?"
- "Recursion = overlapping subproblems → O(2^n) time. DP cache = reuse → O(n) time."

Example — "Explain QuickSort vs MergeSort"
- "Merge = stable, O(n) space. Quick = in-place, worst O(n^2), good memory cache. Pick Quick for arrays."

Example — "Precision edit for bug fix"
- "Line 42 logic flaw. Array index out of bounds on loop end. Change i <= len to i < len. Fix:"

## Auto-Clarity

Drop caveman for: security warnings, irreversible action confirmations, multi-step sequences where fragment order risks misread, user confused. Resume caveman after clear part done.

Example — destructive op:
> Warning: This will permanently delete all data in the target directory and cannot be undone.
> 

bash
> rm -rf ./old_version
> 

> Caveman resume. Verify backup exist first.



This plan separates pure logic (fast, no ECS overhead) from ECS systems, Baker
logic, and Timeline integration, adhering strictly to your repository's
preference for EditMode tests over PlayMode.

Phase 0: Inspect and try to understand /home/l/Github/bovinelabs-core-internals/BovineLabs/Packages/com.bovinelabs.compactors there are 2 skills ther /home/l/Github/bovinelabs-core-internals/BovineLabs/Packages/com.bovinelabs.compactors/shattered-debug-logging
/home/l/Github/bovinelabs-core-internals/BovineLabs/Packages/com.bovinelabs.compactors/shattered-unit-tests load and understand how to use it or we might burn though token by a lot. After you unstsand how to use it proccide.

Phase 1: Baseline & Configuration Audit

Before writing new tests, establish the baseline and ensure project rules are
met.

- [ ] Run Code Coverage Baseline: Execute ./Run-UnityTests.ps1
  -EnableCodeCoverage to generate an HTML report
  (./CodeCoverage/Report/index.html). Identify the most glaring gaps in the
  BovineLabs.Timeline.* packages.
- [ ] Audit Asmdefs: Verify all *.Tests.asmdef files follow the strict matrix in
  references/unit-tests.md:
    - includePlatforms: ["Editor"]
    - autoReferenced: false, overrideReferences: true
    - Baseline references (BovineLabs.Core, BovineLabs.Testing, Unity.Entities,
      Unity.Collections, etc.) are present.
- [ ] Apply Leak Detection: Audit existing system tests and add
  [TestLeakDetection] to any test that creates NativeList, NativeHashMap, or
  tests a system with jobs (e.g., SpatialTrackCollectSystem,
  InfluencePropagateSystem).

Phase 2: Pure Unit Tests (Plain NUnit)

Rule: Do not use ECSTestsFixture for pure math, function, algorithm, or data
struct tests.

- [ ] Data Structs & Defaults: (Many already exist, fill in the gaps)
  - [ ] BovineLabs.Timeline.Animation.Data: Test RukhankaSingleClipData,
    BlendTree2DMotionData.
  - [ ] BovineLabs.Timeline.PlayerInputs.Data: Test InputState,
    ActiveBufferMask, CommandStep.
  - [ ] BovineLabs.Timeline.Spatial.Data: Test InfluenceGrid,
    InfluenceLayerConfig.
- [ ] Mixer Logic:
  - [ ] BovineLabs.Timeline.Animation: Write tests for AnimationBlendingMode
    combinations if custom mixers exist.
- [ ] Mathematical & Pure Functions:
  - [ ] SpatialMapBuildSystem.Quantized/Hash logic (extract pure logic to static
    methods and test).
  - [ ] PidMixer & PhysicsVelocityMixer (Expand existing tests to cover edge
    cases like extreme delta times).
  - [ ] InfluenceReader: Test bilinear interpolation math without ECS.

Phase 3: Baker & Authoring Tests (ECSTestsFixture)

Rule: Use ECSTestsFixture because we need an EntityManager and World to test
conversions from GameObject to ECS.

- [ ] Test Baker Output: For each Baker<T>, create a GameObject with the
  authoring component, run the baking pass (or manually invoke the Builder logic
  if extracted), and assert the correct ECS components exist.
  - [ ] HitStopAuthoring: Verify HitStopConfig and disabled HitStopState.
  - [ ] SpatialGridSettings: Verify SpatialMaskDatabase blob generation and
    SpatialFocusedMap defaults.
  - [ ] EntityLinkRootAuthoring: Verify hierarchy traversal correctly builds the
    EntityLinkEntry buffer.
  - [ ] ActionTickDistributionAuthoring: Verify BlobCurve generation from the
    AnimationCurve.
  - [ ] CommandSequenceClip (Timeline Baking): Verify CommandBlob generation
    constructs sequences correctly.
- [ ] Test Blob Asset Generation Safety: Ensure all blob builders
  (DistributionCurveBlob, CommandBlob, SpatialMaskDatabaseBlob) dispose of their
  allocators correctly and don't leak memory.

Phase 4: ECS System & Job Tests (ECSTestsFixture)

Create entities, run System.Update(WorldUnmanaged), and assert state changes.
Use [TestLeakDetection].

- [ ] EntityLinks Systems:
  - [ ] EntityLinkMutateSystem: Test Assign, Swap, and Remove operations. Verify
    the EntityLinkEntry buffer is modified correctly inside the EntityLock.
  - [ ] EntityLinkParentSystem: Test TransformUtility.SetupParent integration.
    Ensure RestoreOnEnd correctly reverts to PreviousParent when
    ClipActivePrevious fires.
  - [ ] EntityLinkTargetPatchSystem: Test routing of Target, Source, Custom0,
    Custom1.
- [ ] Spatial & Influence Systems:
  - [ ] SpatialHeatmapSystem: Create mock clips with masks, run the system, and
    verify the NativeParallelHashMap heatmap generates correct weights.
  - [ ] InfluenceMaskStampJob: Test atomic float additions. (You have basic
    atomics tests; now test the actual job).
  - [ ] InfluencePropagateSystem: Test the integration of DecayAccumulateJob ->
    BoxBlur -> ComputeGradientJob running in sequence on the InfluenceGrid.
- [ ] Physics Timeline Systems:
  - [ ] PhysicsKinematicsApplySystem: Apply an ActiveForce (Impulse vs
    Continuous) and verify PhysicsVelocity is altered correctly on the target
    entity over a simulated DeltaTime.
  - [ ] PhysicsDragApplySystem: Test exponential decay application on
    PhysicsVelocity.
  - [ ] PhysicsPidApplySystem: Setup a mock target position, run the system, and
    verify the PID outputs the correct directional force/torque into
    PhysicsVelocity.
- [ ] Player Inputs Systems:
  - [ ] ConsumerBufferMaskSystem: Accumulate masks from multiple
    BufferWindowConfig clips and verify the BitOr logic works.
  - [ ] ConsumerHistorySystem: Mock an InputState.Pressed change, run update,
    and verify InputHistory correctly populates with timestamps.
  - [ ] CommandSequenceSystem: The most complex logic. Write scenarios for
    Contains, Consume, OrderedContains, NotFirst, etc. Mock an InputHistory
    buffer, run the job, and verify the ConditionEventWriter triggers.

Phase 5: Timeline Integration & Track Blending (ECSTestsFixture)

Testing Timeline visually is hard, but testing DOTS Timeline data is easy. Mock
the state the PlayableGraph would normally push to ECS.

- [ ] Simulate Timeline Clip Execution:
  - [ ] Write a helper method in your tests: InjectTimelineClip<T>(Entity
    target, T clipData, float weight, float localTime) that adds ClipActive,
    TrackBinding, LocalTime, and T to a mock entity.
- [ ] Track Systems:
  - [ ] TimelineTimeScaleTrackSystem: Inject a TimelineTimeScaleAnimated clip.
    Run the system. Assert the target clock gets a TimelineTimeScaleMultiplier.
  - [ ] PhysicsVelocityTrackSystem: Inject multiple clips targeting the same
    entity. Verify PhysicsVelocityMixer successfully lerps/adds them into a
    single ActiveVelocity component.
  - [ ] EntityLinkParentSystem: Test timeline enter (ClipActive) vs timeline
    exit (ClipActivePrevious).

Phase 6: Edge Cases & Exceptional PlayMode Tests

Rule: Only use PlayMode for things fundamentally tied to the Unity Player Loop
(rendering, input system hardware).

- [ ] Input System Bridge (PlayerInputBridge):
  - [ ] Requires PlayMode: Simulate actual Unity InputSystem button presses and
    verify PlayerInputBridge translates them to InputState components.
- [ ] Rendering/Presentation Systems:
  - [ ] UpdateSmearVelocitySystem: (Optional EditMode vs PlayMode) Runs in
    PresentationSystemGroup. Ensure SmearVelocity matches PhysicsVelocity.
  - [ ] WorldTimeScaleApplySystem: Verify that modifying WorldTimeScale actually
    alters UnityEngine.Time.timeScale and fixedDeltaTime via the Burst
    Trampoline.

Execution Strategy for the Developer

1.  Start with the Core: Finish testing Timeline.Core and EntityLinks. Nearly
    everything else relies on routing through links (Target.Custom0, RouteTo,
    etc.).
2.  Move to Essence & Physics: These are highly mathematical and deterministic.
    Test the PID controllers and Stat mutations.
3.  Tackle Player Inputs: CommandSequenceSystem is highly prone to off-by-one
    errors (Ordered Searches, Consume logic). Write exhaustive data-driven tests
    for Evaluate().
4.  Finish with Spatial/Influence: These use large NativeArrays and Blur passes.
    Focus on [TestLeakDetection] to ensure you aren't leaving grid memory
    hanging in Editor memory during map teardowns.


When you are done with a package push to github. Keep fixing and testing.
