// <copyright file="DrawSystem.Rendering.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
namespace BovineLabs.Quill
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using Unity.Rendering;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;
#if UNITY_HDRP
    using BovineLabs.Core.Extensions;
    using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_URP
    using UnityEngine.Rendering.Universal;
#endif

    public unsafe partial class DrawSystem
    {
        private UploadBackend? uploadBackend;

        private static readonly int PositionBufferID = Shader.PropertyToID("position_buffer");
        private static readonly int MeshBufferID = Shader.PropertyToID("mesh_buffer");
        private static readonly int TextBufferID = Shader.PropertyToID("text_buffer");
        private static readonly int BaseVertexID = Shader.PropertyToID("_BaseVertex");

        private MaterialBuffer[] materials = Array.Empty<MaterialBuffer>();
        private int lastFrame;
        private bool alwaysUpdate;

#if UNITY_HDRP
        private static CustomPassVolume? customPassVolume;
        private CustomPassVolume? registeredVolume;
        private DrawHDRPCustomPass? customPass;
#endif
#if UNITY_URP
        private DrawURPRenderPassFeature? renderPass;
#endif

        public void SubmitFrame(Camera camera, ICommandBuffer cmd)
        {
            if (QuillSettings.I.CamerasToIgnore.Contains(camera.name))
            {
                return;
            }

            this.BuildFrame(cmd);
        }

        private void SetupRendering()
        {
            var deviceType = SystemInfo.graphicsDeviceType;

            if (deviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            this.uploadBackend = new();

#if UNITY_EDITOR
            this.alwaysUpdate = !UnityEditor.EditorApplication.isPlaying;
#endif

            var lineMaterialPrefab = Resources.Load<Material>("BLDrawLine");
            var solidMaterialPrefab = Resources.Load<Material>("BLDrawSolid");
            var textMaterialPrefab = Resources.Load<Material>("BLDrawText");

            this.materials = new MaterialBuffer[3];
            this.materials[0] = new MaterialBuffer(Object.Instantiate(lineMaterialPrefab));
            this.materials[1] = new MaterialBuffer(Object.Instantiate(solidMaterialPrefab));
            this.materials[2] = new MaterialBuffer(Object.Instantiate(textMaterialPrefab));

#if UNITY_HDRP
            this.customPass = new DrawHDRPCustomPass(this);
            this.EnsureCustomPass();
#endif
#if UNITY_URP
            RenderPipelineManager.beginCameraRendering += this.BeginCameraRendering;
#endif
        }

#if UNITY_URP
        private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            var data = camera.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                if (this.renderPass == null)
                {
                    this.renderPass = ScriptableObject.CreateInstance<DrawURPRenderPassFeature>();
                    this.renderPass.SetSystem(this);
                }

                this.renderPass.AddRenderPasses(data.scriptableRenderer);
            }
        }
#endif

#if UNITY_HDRP
        private void EnsureCustomPass()
        {
            if (this.customPass == null)
            {
                return;
            }

            if (customPassVolume == null)
            {
                var go = new GameObject("Drawer") { hideFlags = HideFlags.HideAndDontSave };

                if (!this.World.IsEditorWorld())
                {
                    Object.DontDestroyOnLoad(go);
                }

                customPassVolume = go.AddComponent<CustomPassVolume>();
                customPassVolume.isGlobal = true;
                customPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;

                var asset = GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;
                if (asset != null)
                {
                    if (!asset.currentPlatformRenderPipelineSettings.supportCustomPass)
                    {
                        Debug.LogError("The current render pipeline has custom pass support disabled.");
                    }
                }
            }

            if (this.registeredVolume != customPassVolume)
            {
                customPassVolume.customPasses.Add(this.customPass);
                this.registeredVolume = customPassVolume;
            }
        }
#endif

        [SuppressMessage("ReSharper", "RedundantNameQualifier", Justification = "Required for non-fork")]
        private void BuildFrame(ICommandBuffer buffer)
        {
            if (this.materials[0].Material == null)
            {
                // Domain reload and cause timing issues
                return;
            }

            this.Dependency.Complete();

            if (this.lastFrame != UnityEngine.Time.frameCount || this.alwaysUpdate)
            {
                this.lastFrame = UnityEngine.Time.frameCount;
                this.WriteToLineBuffer(this.materials[0]);
                this.WriteToSolidBuffer(this.materials[1]);
                this.WriteToTextBuffer(this.materials[2]);
            }

            this.materials[0].Material.SetPass(0);
            buffer.DrawProcedural(this.materials[0].Material, MeshTopology.Lines, this.materials[0].Count * 2);

            this.materials[1].Material.SetPass(0);
            buffer.DrawProcedural(this.materials[1].Material, MeshTopology.Triangles, this.materials[1].Count * 3);

            this.materials[2].Material.SetPass(0);
            buffer.DrawProcedural(this.materials[2].Material, MeshTopology.Quads, this.materials[2].Count * 4);
        }

        private void WriteToLineBuffer(MaterialBuffer frameData)
        {
            var totalVertices = 0;

            foreach (var builder in this.builders)
            {
                totalVertices += builder.LineVertices.Length;
            }

            if (totalVertices == 0)
            {
                return;
            }

            var (destBuffer, baseVertex) = this.uploadBackend!.UploadLines(this.builders, totalVertices);

            frameData.Material.SetBuffer(PositionBufferID, destBuffer);
            frameData.Material.SetInt(BaseVertexID, baseVertex);
            frameData.Count = totalVertices;
        }

        private void WriteToSolidBuffer(MaterialBuffer frameData)
        {
            var totalVertices = 0;

            foreach (var builder in this.builders)
            {
                totalVertices += builder.SolidVertices.Length;
            }

            if (totalVertices == 0)
            {
                return;
            }

            var (destBuffer, baseVertex) = this.uploadBackend!.UploadSolids(this.builders, totalVertices);
            frameData.Material.SetBuffer(MeshBufferID, destBuffer);
            frameData.Material.SetInt(BaseVertexID, baseVertex);
            frameData.Count = totalVertices;
        }

        private void WriteToTextBuffer(MaterialBuffer frameData)
        {
            var totalVertices = 0;

            foreach (var builder in this.builders)
            {
                totalVertices += builder.TextVertices.Length;
            }

            if (totalVertices == 0)
            {
                return;
            }

            var (destBuffer, baseVertex) = this.uploadBackend!.UploadText(this.builders, totalVertices);
            frameData.Material.SetBuffer(TextBufferID, destBuffer);
            frameData.Material.SetInt(BaseVertexID, baseVertex);
            frameData.Count = totalVertices;
        }

        private sealed class UploadBackend
        {
            private readonly Stream<LineVertex> lines = new(0);
            private readonly Stream<SolidVertex> solids = new(0);
            private readonly Stream<TextVertex> text = new(0);

            public (GraphicsBuffer Buffer, int BaseVertex) UploadLines(NativeArray<DrawBuilder> builders, int total)
            {
                if (total <= 0)
                {
                    return (this.lines.GetOrCreateBuffer(1), 0);
                }

                var data = new NativeArray<LineVertex>(total, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var dst = (LineVertex*)data.GetUnsafePtr();
                var index = 0;

                foreach (var b in builders)
                {
                    var src = b.LineVertices.Ptr;
                    var len = b.LineVertices.Length;
                    if (len == 0)
                    {
                        continue;
                    }

                    UnsafeUtility.MemCpy(dst + index, src, (long)UnsafeUtility.SizeOf<LineVertex>() * len);
                    index += len;
                }

                var buffer = this.lines.GetOrCreateBuffer(data.Length);
                this.lines.Upload(data);
                return (buffer, 0);
            }

            public (GraphicsBuffer Buffer, int BaseVertex) UploadSolids(NativeArray<DrawBuilder> builders, int total)
            {
                if (total <= 0)
                {
                    return (this.solids.GetOrCreateBuffer(1), 0);
                }

                var data = new NativeArray<SolidVertex>(total, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var dst = (SolidVertex*)data.GetUnsafePtr();
                var index = 0;

                foreach (var b in builders)
                {
                    var src = b.SolidVertices.Ptr;
                    var len = b.SolidVertices.Length;
                    if (len == 0)
                    {
                        continue;
                    }

                    UnsafeUtility.MemCpy(dst + index, src, (long)UnsafeUtility.SizeOf<SolidVertex>() * len);
                    index += len;
                }

                var buffer = this.solids.GetOrCreateBuffer(data.Length);
                this.solids.Upload(data);
                return (buffer, 0);
            }

            public (GraphicsBuffer Buffer, int BaseVertex) UploadText(NativeArray<DrawBuilder> builders, int total)
            {
                if (total <= 0)
                {
                    return (this.text.GetOrCreateBuffer(1), 0);
                }

                var data = new NativeArray<TextVertex>(total, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var dst = (TextVertex*)data.GetUnsafePtr();
                var index = 0;

                foreach (var b in builders)
                {
                    var src = b.TextVertices.Ptr;
                    var len = b.TextVertices.Length;
                    if (len == 0)
                    {
                        continue;
                    }

                    UnsafeUtility.MemCpy(dst + index, src, (long)UnsafeUtility.SizeOf<TextVertex>() * len);
                    index += len;
                }

                var buffer = this.text.GetOrCreateBuffer(data.Length);
                this.text.Upload(data);
                return (buffer, 0);
            }

            public void Dispose()
            {
                this.lines.Dispose();
                this.solids.Dispose();
                this.text.Dispose();
            }

            private sealed class Stream<T>
                where T : unmanaged
            {
                private const int BufferChunkSize = 8 * 1024 * 1024; // match SparseUploader default // TODO make it configurable
                private const int ChunkHeadroom = 64 * 1024; // leave margin for ops/padding
                private readonly int stride = UnsafeUtility.SizeOf<T>();
                private GraphicsBuffer dest;
                private int capacity;
                private SparseUploader uploader;

                public Stream(int requiredElements)
                {
                    var newCapacity = math.max(requiredElements, this.capacity > 0 ? (int)math.ceil(this.capacity * 1.5f) : 256);
                    this.dest = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Raw, newCapacity, this.stride);
                    this.uploader = new SparseUploader(this.dest, BufferChunkSize);
                    this.capacity = newCapacity;
                }

                public GraphicsBuffer GetOrCreateBuffer(int requiredElements)
                {
                    if (this.capacity < requiredElements)
                    {
                        var newCapacity = math.max(requiredElements, this.capacity > 0 ? (int)math.ceil(this.capacity * 1.5f) : 256);
                        var oldBuffer = this.dest;
                        var newBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Raw, newCapacity, this.stride);

                        this.uploader.ReplaceBuffer(newBuffer);

                        this.dest = newBuffer;
                        this.capacity = newCapacity;
                        oldBuffer.Release();
                    }

                    return this.dest;
                }

                public void Upload(NativeArray<T> data)
                {
                    var bytes = data.Length * this.stride;
                    var maxChunk = math.max(1024, BufferChunkSize - ChunkHeadroom);
                    var chunkBytes = math.min(maxChunk, bytes);
                    var opCount = math.max(1, (bytes + chunkBytes - 1) / chunkBytes);

                    var tsu = this.uploader.Begin(bytes, chunkBytes, opCount);

                    var src = (byte*)data.GetUnsafeReadOnlyPtr();
                    var remaining = bytes;
                    var dstOffset = 0;
                    while (remaining > 0)
                    {
                        var size = math.min(remaining, chunkBytes);
                        tsu.AddUpload(src, size, dstOffset);
                        src += size;
                        dstOffset += size;
                        remaining -= size;
                    }

                    this.uploader.EndAndCommit(tsu);
                    this.uploader.FrameCleanup();
                }

                public void Dispose()
                {
                    this.uploader.Dispose();
                    this.dest.Release();
                    this.capacity = 0;
                }
            }
        }
    }
}
#endif
