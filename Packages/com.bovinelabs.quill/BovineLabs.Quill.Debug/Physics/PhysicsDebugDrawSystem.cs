// <copyright file="PhysicsDebugDrawSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Quill.Debug.Physics
{
    using BovineLabs.Core;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Internal;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Transforms;
    using Color = UnityEngine.Color;

    [Configurable]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(DebugSystemGroup))]
    public partial struct PhysicsDebugDrawSystem : ISystem
    {
        [ConfigVar("draw.physics-cull", true, "Should physics colliders be culled by the game camera to help performance.")]
        private static readonly SharedStatic<bool> Cull = SharedStatic<bool>.GetOrCreate<CullType>();

        [ConfigVar("draw.physics-terrain_range", 100, "Custom far distance for terrain colliders culling to help performance.")]
        private static readonly SharedStatic<float> TerrainRange = SharedStatic<float>.GetOrCreate<TerrainRangeType>();

        [ConfigVar("draw.physics-static_color", 0, 1, 0, 1, "Custom far distance for collider culling to help performance.")]
        private static readonly SharedStatic<Color> StaticColor = SharedStatic<Color>.GetOrCreate<StaticColorType>();

        [ConfigVar("draw.physics-dynamics_color", 1, 0, 0, 1, "Custom far distance for collider culling to help performance.")]
        private static readonly SharedStatic<Color> DynamicColor = SharedStatic<Color>.GetOrCreate<DynamicColorType>();

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out PhysicsDebugDraw debug))
            {
                return;
            }

            if (debug is { DrawColliderEdges: false, DrawColliderAabbs: false, DrawMeshColliderEdges: false, DrawTerrainColliderEdges: false })
            {
                return;
            }

            var singleton = SystemAPI.GetSingleton<DrawSystem.Singleton>();
            var drawer = singleton.CreateDrawer();

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var bodies = physicsWorld.Bodies;

            if (debug.DrawColliderEdges || debug.DrawMeshColliderEdges || debug.DrawTerrainColliderEdges)
            {
                this.GetFrustumPlanes(singleton.CameraCulling, out var cameraFrustum, out var terrainFrustum);

                state.Dependency = new DrawCollidersJob
                {
                    Bodies = bodies,
                    NumDynamicBodies = physicsWorld.NumDynamicBodies,
                    Drawer = drawer,
                    DrawColliderEdges = debug.DrawColliderEdges,
                    DrawMeshColliderEdges = debug.DrawMeshColliderEdges,
                    DrawTerrainColliderEdges = debug.DrawTerrainColliderEdges,
                    CameraFrustum = cameraFrustum,
                    TerrainCameraFrustum = terrainFrustum,
                }.ScheduleParallel(bodies.Length, 1, state.Dependency);
            }

            if (debug.DrawColliderAabbs)
            {
                state.Dependency = new DrawAabbsJob
                {
                    Bodies = bodies,
                    Drawer = drawer,
                }.ScheduleParallel(bodies.Length, 1, state.Dependency);
            }
        }

        private void GetFrustumPlanes(CameraCulling culling, out CameraCulling cameraFrustum, out CameraCulling terrainFrustum)
        {
            cameraFrustum = Cull.Data ? culling : default;
            terrainFrustum = cameraFrustum;

            if (!cameraFrustum.Equals(default))
            {
                terrainFrustum = terrainFrustum.GetWithFarClipDistance(TerrainRange.Data);
            }
        }

        [BurstCompile(DisableSafetyChecks = true)] // We need this to run fast all the time, let's hope I haven't made a mistake
        private unsafe struct DrawCollidersJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            public Drawer Drawer;
            public int NumDynamicBodies;

            public bool DrawColliderEdges;
            public bool DrawMeshColliderEdges;
            public bool DrawTerrainColliderEdges;

            public CameraCulling CameraFrustum;
            public CameraCulling TerrainCameraFrustum;

            public void Execute(int index)
            {
                var body = this.Bodies[index];
                if (!body.Collider.IsCreated)
                {
                    return;
                }

                var color = index < this.NumDynamicBodies ? DynamicColor.Data : StaticColor.Data;

                var collider = (Collider*)body.Collider.GetUnsafePtr();

                var bounds = collider->CalculateAabb(body.WorldFromBody, body.Scale);
                var aabb = new AABB
                {
                    Center = bounds.Center,
                    Extents = bounds.Extents / 2f, // physics extents is size
                };

                if (!this.CameraFrustum.Equals(default) && !this.CameraFrustum.AnyIntersect(aabb))
                {
                    return;
                }

                var transform = float4x4.TRS(body.WorldFromBody.pos, body.WorldFromBody.rot, body.Scale);

                this.DrawCollider(collider, transform, color);
            }

            private void DrawCollider(Collider* collider, float4x4 transform, Color color)
            {
                switch (collider->CollisionType)
                {
                    case CollisionType.Convex:
                        if (!this.DrawColliderEdges)
                        {
                            return;
                        }

                        break;

                    case CollisionType.Composite:
                        if (collider->Type == ColliderType.Mesh)
                        {
                            if (!this.DrawMeshColliderEdges)
                            {
                                return;
                            }
                        }
                        else if (collider->Type == ColliderType.Terrain)
                        {
                            if (!this.DrawTerrainColliderEdges)
                            {
                                return;
                            }
                        }

                        break;

                    case CollisionType.Terrain:
                        if (!this.DrawTerrainColliderEdges)
                        {
                            return;
                        }

                        break;
                }

                switch (collider->Type)
                {
                    case ColliderType.Box:
                        this.DrawBox((BoxCollider*)collider, transform, color);
                        break;
                    case ColliderType.Triangle:
                        this.DrawTris((PolygonCollider*)collider, transform, color);
                        break;
                    case ColliderType.Quad:
                        this.DrawQuad((PolygonCollider*)collider, transform, color);
                        break;
                    case ColliderType.Cylinder:
                        this.DrawCylinder((CylinderCollider*)collider, transform, color);
                        break;
                    case ColliderType.Convex:
                        this.DrawConvex((ConvexCollider*)collider, transform, color);
                        break;
                    case ColliderType.Sphere:
                        this.DrawSphere((SphereCollider*)collider, transform, color);
                        break;
                    case ColliderType.Capsule:
                        this.DrawCapsule((CapsuleCollider*)collider, transform, color);
                        break;
                    case ColliderType.Compound:
                        this.DrawCompound((CompoundCollider*)collider, transform, color);
                        break;
                    case ColliderType.Mesh:
                        this.DrawMesh((MeshCollider*)collider, transform, color);
                        break;
                    case ColliderType.Terrain:
                        this.DrawTerrain((TerrainCollider*)collider, transform, color);
                        break;
                }
            }

            private void DrawBox(BoxCollider* collider, float4x4 transform, Color color)
            {
                var position = math.transform(transform, collider->Center);
                var orientation = math.mul(transform.Rotation(), collider->Orientation);
                var size = collider->Size * transform.Scale();
                this.Drawer.Cuboid(position, orientation, size, color);
            }

            private void DrawTris(PolygonCollider* collider, float4x4 transform, Color color)
            {
                this.Drawer.Triangle(math.transform(transform, collider->Vertices[0]), math.transform(transform, collider->Vertices[1]),
                    math.transform(transform, collider->Vertices[2]), color);
            }

            private void DrawQuad(PolygonCollider* collider, float4x4 transform, Color color)
            {
                this.Drawer.Quad(math.transform(transform, collider->Vertices[0]), math.transform(transform, collider->Vertices[1]),
                    math.transform(transform, collider->Vertices[2]), math.transform(transform, collider->Vertices[3]), color);
            }

            private void DrawCylinder(CylinderCollider* collider, float4x4 transform, Color color)
            {
                var position = math.transform(transform, collider->Center);
                var orientation = math.mul(transform.Rotation(), collider->Orientation);
                var scale = math.cmax(transform.Scale());
                var height = scale * collider->Height;
                var radius = scale * collider->Radius;

                this.Drawer.Cylinder(position, orientation, height, radius, collider->SideCount, color);
            }

            private void DrawConvex(ConvexCollider* collider, float4x4 transform, Color color)
            {
                var faces = collider->GetNumFaces();
                for (var faceIndex = 0; faceIndex < faces; faceIndex++)
                {
                    var edges = collider->GetNumVerticesFromFace(faceIndex);

                    for (var edgeIndex = 0; edgeIndex < edges; edgeIndex++)
                    {
                        collider->GetEdgeFromFace(faceIndex, edgeIndex, out var from, out var to);

                        var p0 = math.transform(transform, from);
                        var p1 = math.transform(transform, to);
                        this.Drawer.Line(p0, p1, color);
                    }
                }
            }

            private void DrawSphere(SphereCollider* collider, float4x4 transform, Color color)
            {
                const int defaultColliderSideCount = 16;
                var center = math.transform(transform, collider->Center);
                var radius = collider->Radius * math.cmax(transform.Scale());

                this.Drawer.Sphere(center, radius, defaultColliderSideCount, color);
            }

            private void DrawCapsule(CapsuleCollider* collider, float4x4 transform, Color color)
            {
                var height = math.distance(collider->Vertex0, collider->Vertex1) + (2 * collider->Radius);
                var center = (collider->Vertex1 + collider->Vertex0) / 2f;

                var position = math.transform(transform, center);
                var radius = math.cmax(transform.Scale()) * collider->Radius;

                var axis = collider->Vertex1 - collider->Vertex0; // axis in wfc-space
                var colliderOrientation = mathex.FromToRotation(math.up(), -axis);

                var rotation = math.mul(transform.Rotation(), colliderOrientation);

                this.Drawer.Capsule(position, rotation, height, radius, 20, color);
            }

            private void DrawCompound(CompoundCollider* collider, float4x4 transform, Color color)
            {
                for (var i = 0; i < collider->NumChildren; i++)
                {
                    ref var child = ref collider->Children[i];
                    var childCollider = child.Collider;
                    var worldFromChild = math.mul(transform, float4x4.TRS(child.CompoundFromChild.pos, child.CompoundFromChild.rot, new float3(1)));
                    this.DrawCollider(childCollider, worldFromChild, color);
                }
            }

            private void DrawMesh(MeshCollider* collider, float4x4 transform, Color color)
            {
                const int maxVertsPerPrimitive = 10; // isTrianglePair

                ref var mesh = ref collider->Mesh;

                using var lineVerticesPool = PooledNativeList<float3>.Make();
                var lineVertices = lineVerticesPool.List;

                for (var sectionIndex = 0; sectionIndex < mesh.Sections.Length; sectionIndex++)
                {
                    ref var section = ref mesh.Sections[sectionIndex];

                    var maxCapacity = lineVertices.Length + (section.PrimitiveVertexIndices.Length * maxVertsPerPrimitive);
                    if (lineVertices.Capacity < maxCapacity)
                    {
                        lineVertices.Capacity = maxCapacity;
                    }

                    for (var primitiveIndex = 0; primitiveIndex < section.PrimitiveVertexIndices.Length; primitiveIndex++)
                    {
                        var vertexIndices = section.PrimitiveVertexIndices[primitiveIndex];
                        var flags = section.PrimitiveFlags[primitiveIndex];
                        var isTrianglePair = (flags & Mesh.PrimitiveFlags.IsTrianglePair) != 0;
                        var isQuad = (flags & Mesh.PrimitiveFlags.IsQuad) != 0;

                        var v0 = math.transform(transform, section.Vertices[vertexIndices.A]);
                        var v1 = math.transform(transform, section.Vertices[vertexIndices.B]);
                        var v2 = math.transform(transform, section.Vertices[vertexIndices.C]);
                        var v3 = math.transform(transform, section.Vertices[vertexIndices.D]);

                        if (isQuad)
                        {
                            lineVertices.AddNoResize(v0);
                            lineVertices.AddNoResize(v1);

                            lineVertices.AddNoResize(v1);
                            lineVertices.AddNoResize(v2);

                            lineVertices.AddNoResize(v2);
                            lineVertices.AddNoResize(v3);

                            lineVertices.AddNoResize(v3);
                            lineVertices.AddNoResize(v0);
                        }
                        else if (isTrianglePair)
                        {
                            lineVertices.AddNoResize(v0);
                            lineVertices.AddNoResize(v1);

                            lineVertices.AddNoResize(v1);
                            lineVertices.AddNoResize(v2);

                            lineVertices.AddNoResize(v2);
                            lineVertices.AddNoResize(v3);

                            lineVertices.AddNoResize(v3);
                            lineVertices.AddNoResize(v0);

                            lineVertices.AddNoResize(v0);
                            lineVertices.AddNoResize(v2);
                        }
                        else
                        {
                            lineVertices.AddNoResize(v0);
                            lineVertices.AddNoResize(v1);

                            lineVertices.AddNoResize(v1);
                            lineVertices.AddNoResize(v2);

                            lineVertices.AddNoResize(v2);
                            lineVertices.AddNoResize(v0);
                        }
                    }
                }

                this.Drawer.Lines(lineVertices.AsArray(), color);
            }

            private void DrawTerrain(TerrainCollider* collider, float4x4 transform, Color color)
            {
                const int linesPerSection = 10;
                const int size = 16;

                ref var terrain = ref collider->Terrain;

                var chunks = (terrain.Size - 1) / size;

                var numCellsX = terrain.Size.x - 1;
                var numCellsY = terrain.Size.y - 1;

                using var lineVerticesPool = PooledNativeList<float3>.Make();
                var lineVertices = lineVerticesPool.List;

                for (var cx = 0; cx < chunks.x; cx++)
                {
                    var xMin = cx * size;
                    var xMax = (cx + 1) * size;

                    for (var cy = 0; cy < chunks.y; cy++)
                    {
                        var yMin = cy * size;
                        var yMax = (cy + 1) * size;

                        var min = math.transform(transform, new float3(xMin, terrain.Heights[xMin + (terrain.Size.x * yMin)], yMin) * terrain.Scale);
                        var max = math.transform(transform, new float3(xMax, terrain.Heights[xMax + (terrain.Size.x * yMax)], yMax) * terrain.Scale);

                        var extents = (max - min) / 2f;
                        var center = (max + min) / 2f;

                        var aabb = new AABB
                        {
                            Center = center,
                            Extents = extents,
                        };

                        if (!this.TerrainCameraFrustum.Equals(default) && !this.TerrainCameraFrustum.AnyIntersect(aabb))
                        {
                            continue;
                        }

                        var estCapacity = lineVertices.Length + ((size + 1) * (size + 1) * linesPerSection); // +1 for the final edge
                        if (lineVertices.Capacity < estCapacity)
                        {
                            lineVertices.Capacity = estCapacity;
                        }

                        for (var i = cx * size; i < (cx + 1) * size; i++)
                        {
                            var maxX = i == numCellsX - 1;

                            for (var j = cy * size; j < (cy + 1) * size; j++)
                            {
                                var i0 = i;
                                var i1 = i + 1;
                                var j0 = j;
                                var j1 = j + 1;
                                var v0 = math.transform(transform, new float3(i0, terrain.Heights[i0 + (terrain.Size.x * j0)], j0) * terrain.Scale);
                                var v1 = math.transform(transform, new float3(i1, terrain.Heights[i1 + (terrain.Size.x * j0)], j0) * terrain.Scale);
                                var v2 = math.transform(transform, new float3(i0, terrain.Heights[i0 + (terrain.Size.x * j1)], j1) * terrain.Scale);

                                lineVertices.AddNoResize(v0);
                                lineVertices.AddNoResize(v2);

                                lineVertices.AddNoResize(v2);
                                lineVertices.AddNoResize(v1);

                                lineVertices.AddNoResize(v1);
                                lineVertices.AddNoResize(v0);

                                var maxY = j == numCellsY - 1;
                                if (maxX || maxY)
                                {
                                    var v3 = math.transform(transform, new float3(i1, terrain.Heights[i1 + (terrain.Size.x * j1)], j1) * terrain.Scale);

                                    if (maxY)
                                    {
                                        // edge along x, at y border
                                        lineVertices.AddNoResize(v2);
                                        lineVertices.AddNoResize(v3);
                                    }

                                    if (maxX)
                                    {
                                        // edge along y, at x border
                                        lineVertices.AddNoResize(v3);
                                        lineVertices.AddNoResize(v1);
                                    }
                                }
                            }
                        }
                    }
                }

                this.Drawer.Lines(lineVertices.AsArray(), color);
            }
        }

        [BurstCompile]
        private struct DrawAabbsJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            public Drawer Drawer;

            public void Execute(int index)
            {
                var body = this.Bodies[index];
                if (!body.Collider.IsCreated)
                {
                    return;
                }

                var aabb = this.Bodies[index].Collider.Value.CalculateAabb(this.Bodies[index].WorldFromBody, this.Bodies[index].Scale);
                this.Drawer.Cuboid(aabb.Center, quaternion.identity, aabb.Extents, Color.red);
            }
        }

        private struct CullType
        {
        }

        private struct TerrainRangeType
        {
        }

        private struct StaticColorType
        {
        }

        private struct DynamicColorType
        {
        }
    }
}
#endif
