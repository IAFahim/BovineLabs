// <copyright file="LightTrackSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe
{
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Jobs;
    using BovineLabs.Timeline;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Vibe.Data;
    using BovineLabs.Vibe.Data.Light;
    using BovineLabs.Vibe.Jobs;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Evaluates light clips and applies their blended results to ECS light data.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    public unsafe partial struct LightTrackSystem : ISystem
    {
        private const float UIntToFloat = 1f / uint.MaxValue;
        private const uint HashScramble = 0x9E3779B9u;

        private TrackLifeImpl<LightData, LightInitial> lifeImpl;
        private TrackBlendImpl<LightBlend, LightAnimated> blendImpl;
        private TrackLifeImpl<LightDataExtended, LightExtendedInitial> extendedLifeImpl;
        private TrackBlendImpl<LightExtendedBlend, LightExtendedAnimated> extendedBlendImpl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LightClipData>();
            this.lifeImpl.OnCreate(ref state);
            this.blendImpl.OnCreate(ref state);
            this.extendedLifeImpl.OnCreate(ref state);
            this.extendedBlendImpl.OnCreate(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            this.blendImpl.OnDestroy(ref state);
            this.extendedBlendImpl.OnDestroy(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.lifeImpl.OnUpdate(ref state);
            this.extendedLifeImpl.OnUpdate(ref state);

            var clipActivateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<LightAnimated>()
                .WithAll<LightClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<LightAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<LightClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                LightInitials = SystemAPI.GetComponentLookup<LightInitial>(true),
                Lights = SystemAPI.GetComponentLookup<LightData>(true),
            }.Schedule(clipActivateQuery, state.Dependency);

            var extendedClipActivateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<LightExtendedAnimated>()
                .WithAll<LightClipData, Clip, ClipActive, TrackBinding>()
                .WithDisabled<ClipActivePrevious>()
                .Build();

            state.Dependency = new ExtendedClipActivateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<LightExtendedAnimated>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<LightClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                TrackBindingHandle = SystemAPI.GetComponentTypeHandle<TrackBinding>(true),
                LightInitials = SystemAPI.GetComponentLookup<LightExtendedInitial>(true),
                Lights = SystemAPI.GetComponentLookup<LightDataExtended>(),
            }.Schedule(extendedClipActivateQuery, state.Dependency);

            var clipUpdateQuery = SystemAPI.QueryBuilder().WithAllRW<LightAnimated>().WithAll<LightClipData, Clip, ClipActive, LocalTime>().Build();

            state.Dependency = new ClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<LightAnimated>(),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<LightClipData>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
            }.ScheduleParallel(clipUpdateQuery, state.Dependency);

            var extendedClipUpdateQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<LightExtendedAnimated>()
                .WithAll<LightClipData, Clip, ClipActive, LocalTime, ClipBlobCurveCache>()
                .Build();

            state.Dependency = new ExtendedClipUpdateJob
            {
                AnimatedHandle = SystemAPI.GetComponentTypeHandle<LightExtendedAnimated>(),
                ClipBlobCacheHandle = SystemAPI.GetComponentTypeHandle<ClipBlobCurveCache>(),
                ClipDataHandle = SystemAPI.GetComponentTypeHandle<LightClipData>(true),
                ClipHandle = SystemAPI.GetComponentTypeHandle<Clip>(true),
                LocalTimeHandle = SystemAPI.GetComponentTypeHandle<LocalTime>(true),
                TrackInitials = SystemAPI.GetComponentLookup<LightExtendedInitial>(true),
            }.ScheduleParallel(extendedClipUpdateQuery, state.Dependency);

            var blendData = this.blendImpl.Update(ref state);

            state.Dependency = new WriteJob
            {
                BlendData = blendData,
                Lights = SystemAPI.GetComponentLookup<LightData>(),
            }.ScheduleParallel(blendData, 64, state.Dependency);

            var extendedBlendData = this.extendedBlendImpl.Update(ref state);

            state.Dependency = new ExtendedWriteJob
            {
                BlendData = extendedBlendData,
                Lights = SystemAPI.GetComponentLookup<LightDataExtended>(),
            }.ScheduleParallel(extendedBlendData, 64, state.Dependency);
        }

        private static LightBlend CreateBlendFromLight(in LightData data, bool overrideAll)
        {
            return new LightBlend
            {
                Color = new float3(data.Color.r, data.Color.g, data.Color.b),
                Intensity = data.Intensity,
                ColorTemperature = data.ColorTemperature,
                OverrideColor = overrideAll,
                OverrideIntensity = overrideAll,
                OverrideColorTemperature = overrideAll,
            };
        }

        private static LightExtendedBlend CreateBlendFromExtended(in LightDataExtended data, bool overrideAll)
        {
            return new LightExtendedBlend
            {
                Range = data.Range,
                SpotAngle = data.SpotAngle,
                InnerSpotAngle = data.InnerSpotAngle,
                ShadowStrength = data.ShadowStrength,
                ShadowBias = data.ShadowBias,
                ShadowNormalBias = data.ShadowNormalBias,
                ShadowNearPlane = data.ShadowNearPlane,
                CookieSize = data.CookieSize,
                OverrideRange = overrideAll,
                OverrideSpotAngle = overrideAll,
                OverrideInnerSpotAngle = overrideAll,
                OverrideShadowStrength = overrideAll,
                OverrideShadowBias = overrideAll,
                OverrideShadowNormalBias = overrideAll,
                OverrideShadowNearPlane = overrideAll,
                OverrideCookieSize = overrideAll,
            };
        }

        [BurstCompile]
        private struct ClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<LightAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<LightClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<LightInitial> LightInitials;

            [ReadOnly]
            public ComponentLookup<LightData> Lights;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (LightAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (LightClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var bindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipBlob = ref clipDatas[entityIndexInChunk].Value.Value;
                    var clip = clips[entityIndexInChunk];
                    var binding = bindings[entityIndexInChunk];

                    var baseData = this.LightInitials.TryGetComponent(clip.Track, out var initial) ? initial.Value : default;

                    var reference = baseData;
                    if (this.Lights.TryGetComponent(binding.Value, out var current))
                    {
                        reference = current;
                    }

                    animated.ReferenceIntensity = reference.Intensity;
                    animated.ReferenceColor = new float3(reference.Color.r, reference.Color.g, reference.Color.b);

                    LightBlend blend;

                    switch (clipBlob.Type)
                    {
                        case LightClipType.Initial:
                            blend = CreateBlendFromLight(baseData, true);
                            animated.ReferenceIntensity = baseData.Intensity;
                            animated.ReferenceColor = new float3(baseData.Color.r, baseData.Color.g, baseData.Color.b);
                            break;
                        case LightClipType.Constant:
                            blend = CreateConstantBlend(clipBlob.Constant, reference);
                            break;
                        case LightClipType.Flicker:
                            blend = CreateBlendFromLight(reference, false);
                            break;
                        default:
                            blend = CreateBlendFromLight(reference, false);
                            break;
                    }

                    animated.Value = blend;
                }
            }

            private static LightBlend CreateConstantBlend(in LightConstantData data, in LightData reference)
            {
                var blend = CreateBlendFromLight(reference, false);

                if (data.OverrideColor)
                {
                    blend.OverrideColor = true;
                    blend.Color = data.Color;
                }

                if (data.OverrideIntensity)
                {
                    blend.OverrideIntensity = true;
                    blend.Intensity = data.Intensity;
                }

                if (data.OverrideColorTemperature)
                {
                    blend.OverrideColorTemperature = true;
                    blend.ColorTemperature = data.ColorTemperature;
                }

                return blend;
            }
        }

        [BurstCompile]
        private struct ExtendedClipActivateJob : IJobChunk
        {
            public ComponentTypeHandle<LightExtendedAnimated> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<LightClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentLookup<LightExtendedInitial> LightInitials;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LightDataExtended> Lights;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (LightExtendedAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (LightClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var bindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipData = ref clipDatas[entityIndexInChunk];
                    ref var clipBlob = ref clipData.Value.Value;
                    var clip = clips[entityIndexInChunk];
                    var binding = bindings[entityIndexInChunk];

                    var initialData = this.LightInitials.TryGetComponent(clip.Track, out var initial) ? initial.Value : default;

                    var referenceData = initialData;
                    if (this.Lights.TryGetComponent(binding.Value, out var current))
                    {
                        referenceData = current;
                    }

                    animated.ReferenceRange = referenceData.Range;
                    animated.ReferenceSpotAngle = referenceData.SpotAngle;
                    animated.ReferenceInnerSpotAngle = referenceData.InnerSpotAngle;
                    animated.ReferenceShadowStrength = referenceData.ShadowStrength;
                    animated.ReferenceShadowBias = referenceData.ShadowBias;
                    animated.ReferenceShadowNormalBias = referenceData.ShadowNormalBias;
                    animated.ReferenceShadowNearPlane = referenceData.ShadowNearPlane;
                    animated.ReferenceCookieSize = referenceData.CookieSize;

                    LightExtendedBlend blend;

                    switch (clipBlob.Type)
                    {
                        case LightClipType.Initial:
                            blend = CreateBlendFromExtended(initialData, true);
                            this.TryApplyInitialOverrides(binding.Value, initialData);
                            break;
                        case LightClipType.ExtendedConstant:
                            blend = CreateExtendedConstantBlend(clipBlob.ExtendedConstant, referenceData);
                            this.TryApplyExtendedOverrides(binding.Value, clipBlob.ExtendedConstant, clipData.Cookie);
                            break;
                        case LightClipType.ExtendedCurve:
                            blend = CreateExtendedCurveBaseBlend(ref clipBlob.ExtendedCurve, referenceData, initialData);

                            break;
                        default:
                            blend = CreateBlendFromExtended(referenceData, false);
                            break;
                    }

                    animated.Value = blend;
                }
            }

            private void TryApplyExtendedOverrides(Entity entity, in LightExtendedData data, UnityObjectRef<Texture> cookie)
            {
                if (!this.Lights.TryGetRefRW(entity, out var light))
                {
                    return;
                }

                ApplyExtendedOverrides(ref light.ValueRW, in data, cookie);
            }

            private void TryApplyInitialOverrides(Entity entity, in LightDataExtended initialData)
            {
                if (!this.Lights.TryGetRefRW(entity, out var light))
                {
                    return;
                }

                ApplyInitialOverrides(ref light.ValueRW, in initialData);
            }

            private static LightExtendedBlend CreateExtendedConstantBlend(in LightExtendedData data, in LightDataExtended reference)
            {
                var blend = CreateBlendFromExtended(reference, false);

                if (data.OverrideRange)
                {
                    blend.OverrideRange = true;
                    blend.Range = data.Range;
                }

                if (data.OverrideSpotAngle)
                {
                    blend.OverrideSpotAngle = true;
                    blend.SpotAngle = data.SpotAngle;
                }

                if (data.OverrideInnerSpotAngle)
                {
                    blend.OverrideInnerSpotAngle = true;
                    blend.InnerSpotAngle = data.InnerSpotAngle;
                }

                if (data.OverrideShadowStrength)
                {
                    blend.OverrideShadowStrength = true;
                    blend.ShadowStrength = data.ShadowStrength;
                }

                if (data.OverrideShadowBias)
                {
                    blend.OverrideShadowBias = true;
                    blend.ShadowBias = data.ShadowBias;
                }

                if (data.OverrideShadowNormalBias)
                {
                    blend.OverrideShadowNormalBias = true;
                    blend.ShadowNormalBias = data.ShadowNormalBias;
                }

                if (data.OverrideShadowNearPlane)
                {
                    blend.OverrideShadowNearPlane = true;
                    blend.ShadowNearPlane = data.ShadowNearPlane;
                }

                if (data.OverrideCookieSize)
                {
                    blend.OverrideCookieSize = true;
                    blend.CookieSize = data.CookieSize;
                }

                return blend;
            }

            private static LightExtendedBlend CreateExtendedCurveBaseBlend(
                ref LightExtendedCurveData data, in LightDataExtended reference, in LightDataExtended initial)
            {
                var blend = CreateBlendFromExtended(reference, false);

                ApplyCurveBase(out blend.OverrideRange, ref blend.Range, ref data.Range, reference.Range, initial.Range);
                ApplyCurveBase(out blend.OverrideSpotAngle, ref blend.SpotAngle, ref data.SpotAngle, reference.SpotAngle, initial.SpotAngle);
                ApplyCurveBase(out blend.OverrideInnerSpotAngle, ref blend.InnerSpotAngle, ref data.InnerSpotAngle, reference.InnerSpotAngle,
                    initial.InnerSpotAngle);

                ApplyCurveBase(out blend.OverrideShadowStrength, ref blend.ShadowStrength, ref data.ShadowStrength, reference.ShadowStrength,
                    initial.ShadowStrength);

                return blend;
            }

            private static void ApplyCurveBase(out bool overrideFlag, ref float value, ref LightExtendedCurve curve, float reference, float initial)
            {
                if (!curve.OverrideValue)
                {
                    overrideFlag = false;
                    return;
                }

                overrideFlag = true;
                value = curve.UseInitial ? initial : reference;
            }

            private static void ApplyExtendedOverrides(ref LightDataExtended light, in LightExtendedData data, UnityObjectRef<Texture> cookie)
            {
                if (data.OverrideCookie)
                {
                    light.Cookie = cookie;
                }

                if (data.OverrideRenderMode)
                {
                    light.RenderMode = data.RenderMode;
                }

                if (data.OverrideShadows)
                {
                    light.Shadows = data.Shadows;
                }

                if (data.OverrideType)
                {
                    light.Type = data.Type;
                }

                if (data.OverrideCullingMask)
                {
                    light.CullingMask = data.CullingMask;
                }

                if (data.OverrideRenderingLayerMask)
                {
                    light.RenderingLayerMask = data.RenderingLayerMask;
                }
            }

            private static void ApplyInitialOverrides(ref LightDataExtended light, in LightDataExtended initial)
            {
                light.Type = initial.Type;
                light.Shadows = initial.Shadows;
                light.RenderMode = initial.RenderMode;
                light.Cookie = initial.Cookie;
                light.CullingMask = initial.CullingMask;
                light.RenderingLayerMask = initial.RenderingLayerMask;
            }
        }

        [BurstCompile]
        private struct ClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<LightAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<LightClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (LightAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipDatas = (LightClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);
                var hasCurveCache = chunk.Has(ref this.ClipBlobCacheHandle);
                var clipCurveCaches = hasCurveCache ? chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle) : null;

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipBlob = ref clipDatas[entityIndexInChunk].Value.Value;

                    if (clipBlob.Type != LightClipType.Flicker)
                    {
                        continue;
                    }

                    var time = (float)localTimes[entityIndexInChunk].Value;
                    if (hasCurveCache)
                    {
                        ref var clipCurveCache = ref clipCurveCaches[entityIndexInChunk];
                        animated.Value = FlickerLight(ref clipBlob.Flicker, in animated, time, true, ref clipCurveCache);
                    }
                    else
                    {
                        var clipCurveCache = default(ClipBlobCurveCache);
                        animated.Value = FlickerLight(ref clipBlob.Flicker, in animated, time, false, ref clipCurveCache);
                    }
                }
            }

            private static LightBlend FlickerLight(
                ref LightFlickerData flickerData, in LightAnimated animated, float time, bool hasCurveCache, ref ClipBlobCurveCache clipCurveCache)
            {
                var normalized = SampleFlicker(ref flickerData, time, flickerData.Seed, hasCurveCache, ref clipCurveCache);
                var minMultiplier = math.min(flickerData.MinIntensityMultiplier, flickerData.MaxIntensityMultiplier);
                var maxMultiplier = math.max(flickerData.MinIntensityMultiplier, flickerData.MaxIntensityMultiplier);
                var multiplier = math.lerp(minMultiplier, maxMultiplier, normalized);
                multiplier = math.max(multiplier, 0f);

                var blend = animated.Value;
                blend.OverrideIntensity = true;
                blend.Intensity = math.max(multiplier * animated.ReferenceIntensity, 0f);

                if (flickerData.OverrideColor)
                {
                    blend.OverrideColor = true;
                    blend.Color = math.lerp(flickerData.ColorA, flickerData.ColorB, normalized);
                }
                else
                {
                    blend.OverrideColor = false;
                    blend.Color = animated.ReferenceColor;
                }

                if (flickerData.OverrideColorTemperature)
                {
                    blend.OverrideColorTemperature = true;
                    blend.ColorTemperature = math.max(math.lerp(flickerData.TemperatureA, flickerData.TemperatureB, normalized), 0f);
                }
                else
                {
                    blend.OverrideColorTemperature = false;
                }

                return blend;
            }

            private static float SampleFlicker(ref LightFlickerData data, float localTime, uint seed, bool hasCurveCache, ref ClipBlobCurveCache clipCurveCache)
            {
                var time = math.max(localTime * data.Speed, 0f);

                if (data.UseCustomCurve && data.Curve.IsCreated)
                {
                    var value = hasCurveCache ? data.Curve.Evaluate(time, ref clipCurveCache.Cache0) : data.Curve.Evaluate(time);

                    return math.saturate(value);
                }

                return data.Preset switch
                {
                    LightFlickerPreset.Strobe => SampleStrobe(time, data.DutyCycle),
                    LightFlickerPreset.Buzz => SampleBuzz(time, seed),
                    _ => SampleOrganic(time, seed),
                };
            }

            private static float SampleStrobe(float time, float dutyCycle)
            {
                var duty = math.saturate(dutyCycle);

                if (duty <= math.FLT_MIN_NORMAL)
                {
                    return 0f;
                }

                if (duty >= 1f - math.FLT_MIN_NORMAL)
                {
                    return 1f;
                }

                var phase = math.frac(time);
                return phase < duty ? 1f : 0f;
            }

            private static float SampleBuzz(float time, uint seed)
            {
                var baseSeed = seed != 0 ? seed : 1u;
                var t = math.max(time * 17f, 0f);
                var step = (uint)math.floor(t);
                var frac = math.frac(t);

                var a = Hash01(step, baseSeed);
                var b = Hash01(step + 1u, baseSeed ^ HashScramble);
                var smooth = math.smoothstep(0f, 1f, frac);
                return math.lerp(a, b, smooth);
            }

            private static float SampleOrganic(float time, uint seed)
            {
                var baseSeed = seed != 0 ? seed : 1u;
                var offset = baseSeed * 0.001f;
                var n1 = noise.snoise(new float2(time * 0.75f, offset));
                var n2 = noise.snoise(new float2((time * 0.27f) + (offset * 7f), offset * 3.1f));
                var combined = (0.65f * n1) + (0.35f * n2);
                return math.saturate((combined * 0.5f) + 0.5f);
            }

            private static float Hash01(uint value, uint seed)
            {
                var hashed = math.hash(new uint2(value, seed));
                return hashed * UIntToFloat;
            }
        }

        [BurstCompile]
        private struct ExtendedClipUpdateJob : IJobChunk
        {
            public ComponentTypeHandle<LightExtendedAnimated> AnimatedHandle;

            public ComponentTypeHandle<ClipBlobCurveCache> ClipBlobCacheHandle;

            [ReadOnly]
            public ComponentTypeHandle<LightClipData> ClipDataHandle;

            [ReadOnly]
            public ComponentTypeHandle<Clip> ClipHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentLookup<LightExtendedInitial> TrackInitials;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (LightExtendedAnimated*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var clipBlobCaches = chunk.GetComponentDataPtrRW(ref this.ClipBlobCacheHandle);
                var clipDatas = (LightClipData*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipDataHandle);
                var clips = (Clip*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref var clipBlob = ref clipDatas[entityIndexInChunk].Value.Value;

                    if (clipBlob.Type != LightClipType.ExtendedCurve)
                    {
                        continue;
                    }

                    ref var curveData = ref clipBlob.ExtendedCurve;
                    ref readonly var clip = ref clips[entityIndexInChunk];
                    var time = (float)localTimes[entityIndexInChunk].Value;
                    ref var clipCurveCache = ref clipBlobCaches[entityIndexInChunk];
                    var initialData = this.TrackInitials.TryGetComponent(clip.Track, out var initial) ? initial.Value : default;

                    animated.Value = EvaluateExtendedCurves(ref curveData, in animated, in initialData, time, ref clipCurveCache);
                }
            }

            private static LightExtendedBlend EvaluateExtendedCurves(
                ref LightExtendedCurveData data, in LightExtendedAnimated animated, in LightDataExtended initial, float time,
                ref ClipBlobCurveCache clipCurveCache)
            {
                var blend = CreateBlendFromExtendedReference(in animated);

                if (data.Range.OverrideValue)
                {
                    var baseValue = data.Range.UseInitial ? initial.Range : animated.ReferenceRange;
                    blend.OverrideRange = true;
                    blend.Range = EvaluateCurve(ref data.Range, baseValue, time, ref clipCurveCache.Cache0);
                }

                if (data.SpotAngle.OverrideValue)
                {
                    var baseValue = data.SpotAngle.UseInitial ? initial.SpotAngle : animated.ReferenceSpotAngle;
                    blend.OverrideSpotAngle = true;
                    blend.SpotAngle = EvaluateCurve(ref data.SpotAngle, baseValue, time, ref clipCurveCache.Cache1);
                }

                if (data.InnerSpotAngle.OverrideValue)
                {
                    var baseValue = data.InnerSpotAngle.UseInitial ? initial.InnerSpotAngle : animated.ReferenceInnerSpotAngle;
                    blend.OverrideInnerSpotAngle = true;
                    blend.InnerSpotAngle = EvaluateCurveNoCache(ref data.InnerSpotAngle, baseValue, time);
                }

                if (data.ShadowStrength.OverrideValue)
                {
                    var baseValue = data.ShadowStrength.UseInitial ? initial.ShadowStrength : animated.ReferenceShadowStrength;
                    blend.OverrideShadowStrength = true;
                    blend.ShadowStrength = EvaluateCurve(ref data.ShadowStrength, baseValue, time, ref clipCurveCache.Cache2);
                }

                return blend;
            }

            private static LightExtendedBlend CreateBlendFromExtendedReference(in LightExtendedAnimated animated)
            {
                return new LightExtendedBlend
                {
                    Range = animated.ReferenceRange,
                    SpotAngle = animated.ReferenceSpotAngle,
                    InnerSpotAngle = animated.ReferenceInnerSpotAngle,
                    ShadowStrength = animated.ReferenceShadowStrength,
                    ShadowBias = animated.ReferenceShadowBias,
                    ShadowNormalBias = animated.ReferenceShadowNormalBias,
                    ShadowNearPlane = animated.ReferenceShadowNearPlane,
                    CookieSize = animated.ReferenceCookieSize,
                };
            }

            private static float EvaluateCurve(ref LightExtendedCurve curve, float baseValue, float time, ref BlobCurveCache cache)
            {
                if (!curve.OverrideValue || !curve.Curve.IsCreated)
                {
                    return baseValue;
                }

                var minValue = math.min(curve.Min, curve.Max);
                var maxValue = math.max(curve.Min, curve.Max);
                var normalized = math.saturate(curve.Curve.Evaluate(time, ref cache));
                var value = math.lerp(minValue, maxValue, normalized);
                return curve.Relative ? baseValue + value : value;
            }

            private static float EvaluateCurveNoCache(ref LightExtendedCurve curve, float baseValue, float time)
            {
                if (!curve.OverrideValue || !curve.Curve.IsCreated)
                {
                    return baseValue;
                }

                var minValue = math.min(curve.Min, curve.Max);
                var maxValue = math.max(curve.Min, curve.Max);
                var normalized = math.saturate(curve.Curve.Evaluate(time));
                var value = math.lerp(minValue, maxValue, normalized);
                return curve.Relative ? baseValue + value : value;
            }
        }

        [BurstCompile]
        private struct WriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<LightBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LightData> Lights;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Lights.TryGetRefRW(entity, out var light))
                {
                    return;
                }

                var current = CreateBlendFromLight(light.ValueRO, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(LightMixer));
                ApplyBlend(ref light.ValueRW, in blended);
            }

            private static void ApplyBlend(ref LightData light, in LightBlend blend)
            {
                if (blend.OverrideColor)
                {
                    var colour = light.Color;
                    light.Color = new Color(blend.Color.x, blend.Color.y, blend.Color.z, colour.a);
                }

                if (blend.OverrideIntensity)
                {
                    light.Intensity = math.max(blend.Intensity, 0f);
                }

                if (blend.OverrideColorTemperature)
                {
                    light.ColorTemperature = blend.ColorTemperature;
                }
            }
        }

        [BurstCompile]
        private struct ExtendedWriteJob : IJobParallelHashMapDefer
        {
            [ReadOnly]
            public NativeParallelHashMap<Entity, MixData<LightExtendedBlend>>.ReadOnly BlendData;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LightDataExtended> Lights;

            public void ExecuteNext(int entryIndex, int jobIndex)
            {
                this.Read(this.BlendData, entryIndex, out var entity, out var mixData);

                if (!this.Lights.TryGetRefRW(entity, out var light))
                {
                    return;
                }

                var current = CreateBlendFromExtended(light.ValueRO, false);
                var blended = JobHelpers.Blend(ref mixData, current, default(LightExtendedMixer));
                ApplyExtendedBlend(ref light.ValueRW, in blended);
            }

            private static void ApplyExtendedBlend(ref LightDataExtended light, in LightExtendedBlend blend)
            {
                if (blend.OverrideRange)
                {
                    light.Range = math.max(blend.Range, 0f);
                }

                if (blend.OverrideSpotAngle)
                {
                    light.SpotAngle = math.clamp(blend.SpotAngle, 0f, 179f);
                }

                if (blend.OverrideInnerSpotAngle)
                {
                    var limit = blend.OverrideSpotAngle ? light.SpotAngle : math.clamp(light.SpotAngle, 0f, 179f);
                    light.InnerSpotAngle = math.clamp(blend.InnerSpotAngle, 0f, limit);
                }

                if (blend.OverrideShadowStrength)
                {
                    light.ShadowStrength = math.saturate(blend.ShadowStrength);
                }

                if (blend.OverrideShadowBias)
                {
                    light.ShadowBias = math.max(blend.ShadowBias, 0f);
                }

                if (blend.OverrideShadowNormalBias)
                {
                    light.ShadowNormalBias = math.max(blend.ShadowNormalBias, 0f);
                }

                if (blend.OverrideShadowNearPlane)
                {
                    light.ShadowNearPlane = math.max(blend.ShadowNearPlane, 0f);
                }

                if (blend.OverrideCookieSize)
                {
                    light.CookieSize = math.max(blend.CookieSize, float2.zero);
                }
            }
        }

        private struct LightMixer : IMixer<LightBlend>
        {
            public LightBlend Lerp(in LightBlend a, in LightBlend b, in float s)
            {
                return new LightBlend
                {
                    OverrideColor = a.OverrideColor || b.OverrideColor,
                    Color = MixUtil.LerpFloat3(a.Color, b.Color, s, a.OverrideColor, b.OverrideColor),
                    OverrideIntensity = a.OverrideIntensity || b.OverrideIntensity,
                    Intensity = MixUtil.LerpFloat(a.Intensity, b.Intensity, s, a.OverrideIntensity, b.OverrideIntensity),
                    OverrideColorTemperature = a.OverrideColorTemperature || b.OverrideColorTemperature,
                    ColorTemperature = MixUtil.LerpFloat(a.ColorTemperature, b.ColorTemperature, s, a.OverrideColorTemperature, b.OverrideColorTemperature),
                };
            }

            public LightBlend Add(in LightBlend a, in LightBlend b)
            {
                return new LightBlend
                {
                    OverrideColor = a.OverrideColor || b.OverrideColor,
                    Color = MixUtil.AddFloat3(a.ColorTemperature, b.ColorTemperature, a.OverrideColor, b.OverrideColor),
                    OverrideIntensity = a.OverrideIntensity || b.OverrideIntensity,
                    Intensity = MixUtil.AddFloat(a.Intensity, b.Intensity, a.OverrideIntensity, b.OverrideIntensity),
                    OverrideColorTemperature = a.OverrideColorTemperature || b.OverrideColorTemperature,
                    ColorTemperature = MixUtil.AddFloat(a.ColorTemperature, b.ColorTemperature, a.OverrideColorTemperature, b.OverrideColorTemperature),
                };
            }
        }

        private struct LightExtendedMixer : IMixer<LightExtendedBlend>
        {
            public LightExtendedBlend Lerp(in LightExtendedBlend a, in LightExtendedBlend b, in float s)
            {
                return new LightExtendedBlend
                {
                    OverrideRange = a.OverrideRange || b.OverrideRange,
                    Range = MixUtil.LerpFloat(a.Range, b.Range, s, a.OverrideRange, b.OverrideRange),
                    OverrideSpotAngle = a.OverrideSpotAngle || b.OverrideSpotAngle,
                    SpotAngle = MixUtil.LerpFloat(a.SpotAngle, b.SpotAngle, s, a.OverrideSpotAngle, b.OverrideSpotAngle),
                    OverrideInnerSpotAngle = a.OverrideInnerSpotAngle || b.OverrideInnerSpotAngle,
                    InnerSpotAngle = MixUtil.LerpFloat(a.InnerSpotAngle, b.InnerSpotAngle, s, a.OverrideInnerSpotAngle, b.OverrideInnerSpotAngle),
                    OverrideShadowStrength = a.OverrideShadowStrength || b.OverrideShadowStrength,
                    ShadowStrength = MixUtil.LerpFloat(a.ShadowStrength, b.ShadowStrength, s, a.OverrideShadowStrength, b.OverrideShadowStrength),
                    OverrideShadowBias = a.OverrideShadowBias || b.OverrideShadowBias,
                    ShadowBias = MixUtil.LerpFloat(a.ShadowBias, b.ShadowBias, s, a.OverrideShadowBias, b.OverrideShadowBias),
                    OverrideShadowNormalBias = a.OverrideShadowNormalBias || b.OverrideShadowNormalBias,
                    ShadowNormalBias = MixUtil.LerpFloat(a.ShadowNormalBias, b.ShadowNormalBias, s, a.OverrideShadowNormalBias, b.OverrideShadowNormalBias),
                    OverrideShadowNearPlane = a.OverrideShadowNearPlane || b.OverrideShadowNearPlane,
                    ShadowNearPlane = MixUtil.LerpFloat(a.ShadowNearPlane, b.ShadowNearPlane, s, a.OverrideShadowNearPlane, b.OverrideShadowNearPlane),
                    OverrideCookieSize = a.OverrideCookieSize || b.OverrideCookieSize,
                    CookieSize = MixUtil.LerpFloat2(a.CookieSize, b.CookieSize, s, a.OverrideCookieSize, b.OverrideCookieSize),
                };
            }

            public LightExtendedBlend Add(in LightExtendedBlend a, in LightExtendedBlend b)
            {
                return new LightExtendedBlend
                {
                    OverrideRange = a.OverrideRange || b.OverrideRange,
                    Range = MixUtil.AddFloat(a.Range, b.Range, a.OverrideRange, b.OverrideRange),
                    OverrideSpotAngle = a.OverrideSpotAngle || b.OverrideSpotAngle,
                    SpotAngle = MixUtil.AddFloat(a.SpotAngle, b.SpotAngle, a.OverrideSpotAngle, b.OverrideSpotAngle),
                    OverrideInnerSpotAngle = a.OverrideInnerSpotAngle || b.OverrideInnerSpotAngle,
                    InnerSpotAngle = MixUtil.AddFloat(a.InnerSpotAngle, b.InnerSpotAngle, a.OverrideInnerSpotAngle, b.OverrideInnerSpotAngle),
                    OverrideShadowStrength = a.OverrideShadowStrength || b.OverrideShadowStrength,
                    ShadowStrength = MixUtil.AddFloat(a.ShadowStrength, b.ShadowStrength, a.OverrideShadowStrength, b.OverrideShadowStrength),
                    OverrideShadowBias = a.OverrideShadowBias || b.OverrideShadowBias,
                    ShadowBias = MixUtil.AddFloat(a.ShadowBias, b.ShadowBias, a.OverrideShadowBias, b.OverrideShadowBias),
                    OverrideShadowNormalBias = a.OverrideShadowNormalBias || b.OverrideShadowNormalBias,
                    ShadowNormalBias = MixUtil.AddFloat(a.ShadowNormalBias, b.ShadowNormalBias, a.OverrideShadowNormalBias, b.OverrideShadowNormalBias),
                    OverrideShadowNearPlane = a.OverrideShadowNearPlane || b.OverrideShadowNearPlane,
                    ShadowNearPlane = MixUtil.AddFloat(a.ShadowNearPlane, b.ShadowNearPlane, a.OverrideShadowNearPlane, b.OverrideShadowNearPlane),
                    OverrideCookieSize = a.OverrideCookieSize || b.OverrideCookieSize,
                    CookieSize = MixUtil.AddFloat2(a.CookieSize, b.CookieSize, a.OverrideCookieSize, b.OverrideCookieSize),
                };
            }
        }
    }
}
#endif
