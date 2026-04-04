// <copyright file="DrawSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
namespace BovineLabs.Quill
{
    using System;
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.SingletonCollection;
    using BovineLabs.Quill.Drawers;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif
#if UNITY_URP
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;
#endif

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation |
        WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(DebugSystemGroup), OrderLast = true)]
    [Configurable]
    public unsafe partial class DrawSystem : SystemBase
    {
        private const string DrawEnabled = "draw.enabled";
        private const string DrawEnabledDesc = "Sets the global drawer enabled state drawer.";
        private const string DrawSceneEnabled = "draw.scene-camera";
        private const string DrawSceneDescEnabled = "Draw using scene camera instead of game camera when required.";

        [ConfigVar(DrawSceneEnabled, false, DrawSceneDescEnabled)]
        private static readonly SharedStatic<bool> IsSceneEnabled = SharedStatic<bool>.GetOrCreate<DrawSceneEnabledTagType>();

        [ConfigVar("draw.draw-range", 500, "Custom far clip plane for collider culling to help performance.")]
        private static readonly SharedStatic<float> Range = SharedStatic<float>.GetOrCreate<RangeType>();

        private readonly Plane[] sourcePlanes = new Plane[6];

        private NativeArray<DrawBuilder> builders;
        private NativeArray<SDFCharacter> characters;

        private SingletonCollectionUtil<Singleton, Singleton.EnabledStream> drawers;

        private NativeReference<bool> enabled;
        private NativeHashSet<int> systemFilterSet;
        private NativeHashSet<FixedString32Bytes> categoryFilterSet;
        private NativeHashSet<int> knownSystemSet;
        private NativeHashSet<FixedString32Bytes> knownCategorySet;
        private NativeReference<CameraCulling> cameraCulling;

        private NativeThreadStream timedDrawer;

#if UNITY_EDITOR
        private static DrawSystem? globalOwnedBy;
        private static UnsafeThreadStream globalStream;
#endif

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.SetupRendering();
            this.CreateBuilders();
            this.CreateDrawing();
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            if (globalOwnedBy == this)
            {
                this.GetAllDependencies(this.Dependency).Complete();

                GlobalDraw.Draw.Data = default;
                globalOwnedBy = null;
            }
#endif

            foreach (var buffer in this.builders)
            {
                buffer.Dispose();
            }

            this.builders.Dispose();

            this.ClearLastFrame();
            foreach (var m in this.materials)
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(m.Material);
                }
                else
#endif
                {
                    UnityEngine.Object.Destroy(m.Material);
                }
            }

            this.enabled.Dispose();
            this.systemFilterSet.Dispose();
            this.categoryFilterSet.Dispose();
            this.knownSystemSet.Dispose();
            this.knownCategorySet.Dispose();
            this.cameraCulling.Dispose();

            this.timedDrawer.Dispose();
            this.drawers.Dispose();

            this.characters.Dispose();

            this.uploadBackend?.Dispose();

#if UNITY_HDRP
            if (customPassVolume != null)
            {
                customPassVolume.customPasses.Remove(this.customPass);
            }
#endif
#if UNITY_URP
            RenderPipelineManager.beginCameraRendering -= this.BeginCameraRendering;
            if (this.renderPass != null)
            {
                Object.DestroyImmediate(this.renderPass);
                this.renderPass = null;
            }
#endif
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
#if UNITY_EDITOR && UNITY_HDRP
            if (this.World.IsEditorWorld())
            {
                this.EnsureCustomPass();
            }
#endif

            // Return our meshes
            this.ClearLastFrame();

            var camera = this.GetCamera();
            this.cameraCulling.Value = this.GetCameraCulling(camera);

            this.Dependency = new ResetBuffers
            {
                MeshBuffers = this.builders,
                CameraRotation = camera != null ? camera.transform.rotation : quaternion.identity,
            }.ScheduleParallel(this.builders.Length, this.builders.Length, this.Dependency);

            // Playback the timed writer first
            var timedEventWriter = new NativeThreadStream(this.drawers.CurrentAllocator);
            this.Dependency = new DrawEventJob
            {
                Reader = this.timedDrawer.AsReader(),
                MeshBuffers = this.builders,
                TimedEventWriter = timedEventWriter.AsWriter(),
                DeltaTime = this.World.Time.DeltaTime,
            }.ScheduleParallel(JobsUtility.ThreadIndexCount, 1, this.Dependency);

            this.timedDrawer = timedEventWriter;

#if UNITY_EDITOR
            // We own the global
            if (globalOwnedBy == this && globalStream.IsCreated)
            {
                this.Dependency = this.GetAllDependencies(this.Dependency);

                this.Dependency = new DrawEvent2Job
                {
                    Reader = globalStream.AsReader(),
                    MeshBuffers = this.builders,
                    TimedEventWriter = timedEventWriter.AsWriter(),
                    DeltaTime = this.World.Time.DeltaTime,
                }.ScheduleParallel(JobsUtility.ThreadIndexCount, 1, this.Dependency);
            }
#endif

            var streams = this.drawers.Containers;

            for (var i = 0; i < streams.Length; i++)
            {
                var drawer = streams.Ptr[i];

                if (!drawer.Enabled)
                {
                    continue;
                }

                this.Dependency = new DrawEventJob
                {
                    Reader = drawer.Stream.AsReader(),
                    MeshBuffers = this.builders,
                    TimedEventWriter = timedEventWriter.AsWriter(),
                    DeltaTime = this.World.Time.DeltaTime,
                }.ScheduleParallel(JobsUtility.ThreadIndexCount, 1, this.Dependency);
            }

            this.drawers.ClearRewind(this.Dependency);

#if UNITY_EDITOR
            // If we are already the owner, or there is no owner we take ownership
            if (globalOwnedBy == this || globalOwnedBy == null)
            {
                // We make it always draw in editor world for debug tools
                if (this.World.IsEditorWorld() || (GlobalDraw.Enabled.Data && Singleton.IsEnabled.Data))
                {
                    globalStream = new UnsafeThreadStream(this.drawers.CurrentAllocator);
                    GlobalDraw.Draw.Data = new DrawerUnsafe(true, globalStream.AsWriter());
                    globalOwnedBy = this;
                }
                else
                {
                    globalStream = default;
                    GlobalDraw.Draw.Data = default;
                    globalOwnedBy = null;
                }
            }
#endif
        }

        private JobHandle GetAllDependencies(JobHandle handle)
        {
            foreach (var w in World.All)
            {
                if ((w.Flags & WorldFlags.Live) != 0)
                {
                    JobHandle.CombineDependencies(handle, w.Unmanaged.GetTrackedJobHandle());
                }
            }

            return handle;
        }

        private void ClearLastFrame()
        {
            foreach (var buffer in this.materials)
            {
                buffer.Count = 0;
            }
        }

        private void CreateBuilders()
        {
            this.builders = new NativeArray<DrawBuilder>(JobsUtility.ThreadIndexCount, Allocator.Persistent);

            this.characters = SDFFont.CreateCharacterArray(Allocator.Persistent);

            for (var i = 0; i < this.builders.Length; i++)
            {
                this.builders[i] = new DrawBuilder(this.characters, Allocator.Persistent);
            }
        }

        private void CreateDrawing()
        {
            this.drawers = new SingletonCollectionUtil<Singleton, Singleton.EnabledStream>(ref this.CheckedStateRef);

            this.enabled = new NativeReference<bool>(true, Allocator.Persistent);
            this.systemFilterSet = new NativeHashSet<int>(0, Allocator.Persistent);
            this.categoryFilterSet = new NativeHashSet<FixedString32Bytes>(0, Allocator.Persistent);
            this.knownSystemSet = new NativeHashSet<int>(0, Allocator.Persistent);
            this.knownCategorySet = new NativeHashSet<FixedString32Bytes>(0, Allocator.Persistent);
            this.cameraCulling = new NativeReference<CameraCulling>(Allocator.Persistent);

            SystemAPI
                .GetSingletonRW<Singleton>()
                .ValueRW
                .Init(this.enabled, this.systemFilterSet, this.categoryFilterSet, this.knownSystemSet, this.knownCategorySet, this.cameraCulling);

            this.timedDrawer = new NativeThreadStream(this.drawers.CurrentAllocator);
        }

        private Camera? GetCamera()
        {
#if UNITY_EDITOR
            if (IsSceneEnabled.Data && SceneView.lastActiveSceneView != null)
            {
                return SceneView.lastActiveSceneView.camera;
            }
#endif

            if (this.EntityManager.TryGetManagedSingleton<Camera>(out var camera))
            {
                return camera;
            }

            camera = Camera.main;
            if (camera != null)
            {
                return camera;
            }

            return Camera.allCamerasCount != 0 ? Camera.allCameras[0] : null;
        }

        private CameraCulling GetCameraCulling(Camera? camera)
        {
            if (camera == null)
            {
                return default;
            }

            return CameraCulling.Create(camera, this.sourcePlanes, Range.Data);
        }

        [Configurable]
        public struct Singleton : ISingletonCollection<Singleton.EnabledStream>
        {
            internal NativeReference<bool> Enabled;
            internal NativeHashSet<int> SystemFilterSet;
            internal NativeHashSet<FixedString32Bytes> CategoryFilterSet;
            internal NativeHashSet<int> KnownSystemSet;
            internal NativeHashSet<FixedString32Bytes> KnownCategorySet;

            [ConfigVar(DrawEnabled, true, DrawEnabledDesc)]
            internal static readonly SharedStatic<bool> IsEnabled = SharedStatic<bool>.GetOrCreate<DrawEnabledTagType>();

            private NativeReference<CameraCulling> cameraCulling;

            public CameraCulling CameraCulling => this.cameraCulling.Value;

            /// <inheritdoc />
            UnsafeList<EnabledStream>* ISingletonCollection<EnabledStream>.Collections { get; set; }

            /// <inheritdoc />
            Allocator ISingletonCollection<EnabledStream>.Allocator { get; set; }

            public Drawer CreateDrawer()
            {
                var enabled = IsEnabled.Data && this.Enabled.Value;
                var writer = this.CreateEnabledThreadStream(enabled);
                return new Drawer(enabled, writer);
            }

            public Drawer CreateDrawer<T>(FixedString32Bytes category = default)
            {
                var systemTypeIndex = TypeManager.GetSystemTypeIndex<T>();

                var enabled = IsEnabled.Data && this.Enabled.Value &&
                    (this.SystemFilterSet.Contains(systemTypeIndex) || (category != default && this.CategoryFilterSet.Contains(category)));

                this.KnownSystemSet.Add(systemTypeIndex);

                if (category != default)
                {
                    this.KnownCategorySet.Add(category);
                }

                var writer = this.CreateEnabledThreadStream(enabled);
                return new Drawer(enabled, writer);
            }

            internal void Init(
                NativeReference<bool> enabled, NativeHashSet<int> systemFilterSet, NativeHashSet<FixedString32Bytes> categoryFilterSet,
                NativeHashSet<int> knownSystemSet, NativeHashSet<FixedString32Bytes> knownCategorySet, NativeReference<CameraCulling> cameraCullingRef)
            {
                this.Enabled = enabled;
                this.SystemFilterSet = systemFilterSet;
                this.CategoryFilterSet = categoryFilterSet;
                this.KnownSystemSet = knownSystemSet;
                this.KnownCategorySet = knownCategorySet;
                this.cameraCulling = cameraCullingRef;
            }

            internal struct EnabledStream
            {
                public NativeThreadStream Stream;
                public bool Enabled;
            }
        }

        public struct DrawSceneEnabledTagType
        {
        }

        private struct DrawEnabledTagType
        {
        }

        private struct RangeType
        {
        }

        [BurstCompile]
        private struct ResetBuffers : IJobFor
        {
            public NativeArray<DrawBuilder> MeshBuffers;
            public quaternion CameraRotation;

            public void Execute(int index)
            {
                var buffer = this.MeshBuffers[index];
                buffer.Reset();
                buffer.CameraRotation = this.CameraRotation;
                this.MeshBuffers[index] = buffer;
            }
        }

        [BurstCompile]
        private struct DrawEventJob : IJobFor
        {
            [ReadOnly]
            public NativeThreadStream.Reader Reader;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<DrawBuilder> MeshBuffers;

            public NativeThreadStream.Writer TimedEventWriter;

            public float DeltaTime;

            [NativeDisableContainerSafetyRestriction]
            private NativeList<byte> readLargeBuffer;

            public void Execute(int foreachIndex)
            {
                if (!this.readLargeBuffer.IsCreated)
                {
                    this.readLargeBuffer = new NativeList<byte>(Allocator.Temp);
                }

                var buffer = this.MeshBuffers[JobsUtility.ThreadIndex];

                this.Reader.BeginForEachIndex(foreachIndex);

                while (this.Reader.RemainingItemCount != 0)
                {
                    var header = this.Reader.Read<DrawHeader>();

                    switch (header.DrawType)
                    {
                        case DrawType.Point:
                            this.ExecuteDrawer<PointDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Line:
                            this.ExecuteDrawer<LineDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Arrow:
                            this.ExecuteDrawer<ArrowDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Plane:
                            this.ExecuteDrawer<PlaneDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Circle:
                            this.ExecuteDrawer<CircleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Arc:
                            this.ExecuteDrawer<ArcDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Cone:
                            this.ExecuteDrawer<ConeDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Sector:
                            this.ExecuteDrawer<SectorDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Cuboid:
                            this.ExecuteDrawer<CuboidDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Triangle:
                            this.ExecuteDrawer<TriangleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Quad:
                            this.ExecuteDrawer<QuadDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Cylinder:
                            this.ExecuteDrawer<CylinderDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Capsule:
                            this.ExecuteDrawer<CapsuleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Sphere:
                            this.ExecuteDrawer<SphereDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D32:
                            this.ExecuteDrawer<TextDrawer2D<FixedString32Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D64:
                            this.ExecuteDrawer<TextDrawer2D<FixedString64Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D128:
                            this.ExecuteDrawer<TextDrawer2D<FixedString128Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D512:
                            this.ExecuteDrawer<TextDrawer2D<FixedString512Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D4096:
                            this.ExecuteDrawer<TextDrawer2D<FixedString4096Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D32:
                            this.ExecuteDrawer<TextDrawer3D<FixedString32Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D64:
                            this.ExecuteDrawer<TextDrawer3D<FixedString64Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D128:
                            this.ExecuteDrawer<TextDrawer3D<FixedString128Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D512:
                            this.ExecuteDrawer<TextDrawer3D<FixedString512Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D4096:
                            this.ExecuteDrawer<TextDrawer3D<FixedString4096Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.SolidTriangle:
                            this.ExecuteDrawer<SolidTriangleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.SolidQuad:
                            this.ExecuteDrawer<SolidQuadDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Lines:
                            this.ExecuteLinesDrawer(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.SolidTriangles:
                            this.ExecuteTrianglesDrawer(header, ref this.Reader, ref buffer);
                            break;
                        default:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                            throw new InvalidOperationException($"Unsupported DrawType {(int)header.DrawType}");
#else
                            break;
#endif
                    }
                }

                this.Reader.EndForEachIndex();

                // Needs to be written back as the buffers are instanced
                this.MeshBuffers[JobsUtility.ThreadIndex] = buffer;
            }

            private void ExecuteDrawer<T>(DrawHeader header, ref NativeThreadStream.Reader reader, ref DrawBuilder buffer)
                where T : unmanaged, IDrawer
            {
                var duration = header.Duration - this.DeltaTime;

                var drawer = reader.Read<T>();
                drawer.Draw(ref buffer);

                if (duration > 0)
                {
                    this.TimedEventWriter.Write(new DrawHeader(header.DrawType, duration));
                    this.TimedEventWriter.Write(drawer);
                }
            }

            private void ExecuteLinesDrawer(DrawHeader header, ref NativeThreadStream.Reader reader, ref DrawBuilder buffer)
            {
                var duration = header.Duration - this.DeltaTime;

                var drawer = reader.Read<LinesDrawer>();

                this.readLargeBuffer.ResizeUninitialized(drawer.Count * UnsafeUtility.SizeOf<float3>());
                var lines = this.readLargeBuffer.AsArray().Reinterpret<float3>(UnsafeUtility.SizeOf<byte>());
                reader.ReadLarge<float3>((byte*)lines.GetUnsafePtr(), lines.Length);
                buffer.DrawLines(lines, drawer.Color);

                if (duration > 0)
                {
                    this.TimedEventWriter.Write(new DrawHeader(header.DrawType, duration));
                    this.TimedEventWriter.Write(drawer);
                    this.TimedEventWriter.WriteLarge(lines);
                }
            }

            private void ExecuteTrianglesDrawer(DrawHeader header, ref NativeThreadStream.Reader reader, ref DrawBuilder buffer)
            {
                var duration = header.Duration - this.DeltaTime;

                var drawer = reader.Read<SolidTrianglesDrawer>();

                this.readLargeBuffer.ResizeUninitialized(drawer.Count * UnsafeUtility.SizeOf<float3x3>());
                var triangles = this.readLargeBuffer.AsArray().Reinterpret<float3x3>(UnsafeUtility.SizeOf<byte>());
                reader.ReadLarge<float3x3>((byte*)triangles.GetUnsafePtr(), triangles.Length);

                for (var i = 0; i < triangles.Length; i++)
                {
                    buffer.DrawTriangle(triangles[i].c0, triangles[i].c1, triangles[i].c2, drawer.Color);
                }

                if (duration > 0)
                {
                    this.TimedEventWriter.Write(new DrawHeader(header.DrawType, duration));
                    this.TimedEventWriter.Write(drawer);
                    this.TimedEventWriter.WriteLarge(triangles);
                }
            }
        }

        // TODO Currently this job is duplicated as generic variation is causing burst to crash
        [BurstCompile]
        private struct DrawEvent2Job : IJobFor
        {
            [ReadOnly]
            public UnsafeThreadStream.Reader Reader;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<DrawBuilder> MeshBuffers;

            public NativeThreadStream.Writer TimedEventWriter;

            public float DeltaTime;

            [NativeDisableContainerSafetyRestriction]
            private NativeList<byte> readLargeBuffer;

            public void Execute(int foreachIndex)
            {
                if (!this.readLargeBuffer.IsCreated)
                {
                    this.readLargeBuffer = new NativeList<byte>(Allocator.Temp);
                }

                var buffer = this.MeshBuffers[JobsUtility.ThreadIndex];

                this.Reader.BeginForEachIndex(foreachIndex);

                while (this.Reader.RemainingItemCount != 0)
                {
                    var header = this.Reader.Read<DrawHeader>();

                    switch (header.DrawType)
                    {
                        case DrawType.Point:
                            this.ExecuteDrawer<PointDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Line:
                            this.ExecuteDrawer<LineDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Arrow:
                            this.ExecuteDrawer<ArrowDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Plane:
                            this.ExecuteDrawer<PlaneDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Circle:
                            this.ExecuteDrawer<CircleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Arc:
                            this.ExecuteDrawer<ArcDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Cone:
                            this.ExecuteDrawer<ConeDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Sector:
                            this.ExecuteDrawer<SectorDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Cuboid:
                            this.ExecuteDrawer<CuboidDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Triangle:
                            this.ExecuteDrawer<TriangleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Quad:
                            this.ExecuteDrawer<QuadDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Cylinder:
                            this.ExecuteDrawer<CylinderDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Capsule:
                            this.ExecuteDrawer<CapsuleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Sphere:
                            this.ExecuteDrawer<SphereDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D32:
                            this.ExecuteDrawer<TextDrawer2D<FixedString32Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D64:
                            this.ExecuteDrawer<TextDrawer2D<FixedString64Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D128:
                            this.ExecuteDrawer<TextDrawer2D<FixedString128Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D512:
                            this.ExecuteDrawer<TextDrawer2D<FixedString512Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text2D4096:
                            this.ExecuteDrawer<TextDrawer2D<FixedString4096Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D32:
                            this.ExecuteDrawer<TextDrawer3D<FixedString32Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D64:
                            this.ExecuteDrawer<TextDrawer3D<FixedString64Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D128:
                            this.ExecuteDrawer<TextDrawer3D<FixedString128Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D512:
                            this.ExecuteDrawer<TextDrawer3D<FixedString512Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Text3D4096:
                            this.ExecuteDrawer<TextDrawer3D<FixedString4096Bytes>>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.SolidTriangle:
                            this.ExecuteDrawer<SolidTriangleDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.SolidQuad:
                            this.ExecuteDrawer<SolidQuadDrawer>(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.Lines:
                            this.ExecuteLinesDrawer(header, ref this.Reader, ref buffer);
                            break;
                        case DrawType.SolidTriangles:
                            this.ExecuteTrianglesDrawer(header, ref this.Reader, ref buffer);
                            break;
                        default:
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                            throw new InvalidOperationException($"Unsupported DrawType {(int)header.DrawType}");
#else
                            break;
#endif
                    }
                }

                this.Reader.EndForEachIndex();

                // Needs to be written back as the buffers are instanced
                this.MeshBuffers[JobsUtility.ThreadIndex] = buffer;
            }

            private void ExecuteDrawer<T>(DrawHeader header, ref UnsafeThreadStream.Reader reader, ref DrawBuilder buffer)
                where T : unmanaged, IDrawer
            {
                var duration = header.Duration - this.DeltaTime;

                var drawer = reader.Read<T>();
                drawer.Draw(ref buffer);

                if (duration > 0)
                {
                    this.TimedEventWriter.Write(new DrawHeader(header.DrawType, duration));
                    this.TimedEventWriter.Write(drawer);
                }
            }

            private void ExecuteLinesDrawer(DrawHeader header, ref UnsafeThreadStream.Reader reader, ref DrawBuilder buffer)
            {
                var duration = header.Duration - this.DeltaTime;

                var drawer = reader.Read<LinesDrawer>();

                this.readLargeBuffer.ResizeUninitialized(drawer.Count * UnsafeUtility.SizeOf<float3>());
                var lines = this.readLargeBuffer.AsArray().Reinterpret<float3>(UnsafeUtility.SizeOf<byte>());
                reader.ReadLarge<float3>((byte*)lines.GetUnsafePtr(), lines.Length);

                for (var i = 0; i < lines.Length; i += 2)
                {
                    buffer.DrawLine(lines[i], lines[i + 1], drawer.Color);
                }

                if (duration > 0)
                {
                    this.TimedEventWriter.Write(new DrawHeader(header.DrawType, duration));
                    this.TimedEventWriter.Write(drawer);
                    this.TimedEventWriter.WriteLarge(lines);
                }
            }

            private void ExecuteTrianglesDrawer(DrawHeader header, ref UnsafeThreadStream.Reader reader, ref DrawBuilder buffer)
            {
                var duration = header.Duration - this.DeltaTime;

                var drawer = reader.Read<SolidTrianglesDrawer>();

                this.readLargeBuffer.ResizeUninitialized(drawer.Count * UnsafeUtility.SizeOf<float3x3>());
                var triangles = this.readLargeBuffer.AsArray().Reinterpret<float3x3>(UnsafeUtility.SizeOf<byte>());
                reader.ReadLarge<float3x3>((byte*)triangles.GetUnsafePtr(), triangles.Length);

                for (var i = 0; i < triangles.Length; i++)
                {
                    buffer.DrawTriangle(triangles[i].c0, triangles[i].c1, triangles[i].c2, drawer.Color);
                }

                if (duration > 0)
                {
                    this.TimedEventWriter.Write(new DrawHeader(header.DrawType, duration));
                    this.TimedEventWriter.Write(drawer);
                    this.TimedEventWriter.WriteLarge(triangles);
                }
            }
        }

        [Configurable]
        private class MaterialBuffer
        {
            public readonly Material Material;

            public MaterialBuffer(Material material)
            {
                this.Material = material;
            }

            // number of lines, verts, tris etc
            public int Count { get; set; }
        }
    }
}
#else
namespace BovineLabs.Quill
{
    using System.Diagnostics.CodeAnalysis;
    using JetBrains.Annotations;
    using Unity.Collections;
    using Unity.Entities;

    public partial class DrawSystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.EntityManager.AddComponent<Singleton>(this.SystemHandle);
            this.Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            // NO-OP
        }

        public struct Singleton : IComponentData
        {
            [UsedImplicitly] // just don't want to be an empty struct
            public byte Empty;

            public Drawer CreateDrawer()
            {
                return default;
            }

            [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Used in other conditions")]
            [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used in other conditions")]
            public Drawer CreateDrawer<T>(FixedString32Bytes category = default)
            {
                return default;
            }
        }
    }
}
#endif
