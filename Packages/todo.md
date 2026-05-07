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



Critical bugs discovered: SpatialTrackDebugSystem schedules both DrawGridJob and DrawHeatmapJob twice per frame. DecayAccumulateJob tests never set DeltaTime, meaning math.pow(rate, 0) = 1 regardless of decay rate — every decay test is silently wrong. InfluenceLayerConfig.Normalization and .Bidirectional are baked but never applied in InfluencePropagateSystem. And SpatialInfluenceSystemGroup has no [UpdateAfter(SpatialMapBuildSystem)], creating a race condition.
Core architectural problem: Two parallel stamp pipelines. Both GatherHeatmapJob and InfluenceMaskStampJob iterate ClipActive + SpatialActiveClipData and stamp masks independently. The SpatialHeatmapSingleton (sparse int) is a degraded duplicate of InfluenceGrid.Smoothed (dense float)

VvisualizeVvisualize show_widgetNow the full written plan. Click each system row on the "Execution pipeline" tab to see per-system details, and the other tabs for bugs and data flow.

The core insight driving everything: these two systems are not competing — they're two layers of the same abstraction. The BovineLabs.Spatial side is the discrete event layer (who entered whose zone, fire a Reaction condition). The BovineLabs.Timeline.Spatial side is the continuous field layer (what is the danger pressure at this position, which direction should this AI steer). The perfect machine is when discrete events are gated by continuous field state, and the continuous field is stamped by the same masks that drive the discrete events — with no duplicated work.
The unified execution contract per frame:
SpatialMapBuildSystem          → SpatialMapSingleton (camera pos, entity hash map)
↓
SpatialInfluenceSystemGroup
InfluenceClearSystem          → wipes Accumulated
InfluenceMaskStampSystem      → stamps ClipActive masks → Accumulated
InfluenceTrailSystem          → stamps trail emitters → Accumulated
InfluencePropagateSystem      → decay → [split if bidirectional] → blur
→ normalize → recombine → gradient
↓
TimelineComponentAnimationGroup
InfluenceHeatmapBridgeJob     → Smoothed[layer0] → SpatialHeatmapSingleton (bridge only)
SpatialTrackCollectSystem     → spatial hash enter/exit → Reaction events
[optionally gated by InfluenceGrid threshold]
The eight phases in priority order: Fix the ordering race (1), fix the double debug scheduling (2), fix DecayAccumulateJob so tests are meaningful (3), eliminate the duplicate stamp pipeline and replace with the bridge (4), implement Normalization (5), implement Bidirectional (6), add influence-gated events to SpatialTrackCollectSystem (7), fix multi-bit FlagToLayerIndex registration (8).
Phases 1–4 are correctness fixes. Phases 5–8 are the features that were designed and baked but never wired. The result is a system where AI agents sample InfluenceReader.SampleGradientMask for steering, SpatialTrackCollectSystem fires enter/exit reactions only when the influence field confirms meaningful overlap, and the heatmap debug view shows the temporally-smoothed field instead of raw integer sums — a genuine improvement in visual quality at zero additional cost.