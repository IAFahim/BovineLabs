// <copyright file="PhysicsDebugDraw.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Quill.Debug.Physics
{
    using Unity.Entities;

    public struct PhysicsDebugDraw : IComponentData
    {
        public bool DrawColliderEdges;
        public bool DrawColliderAabbs;
        public bool DrawCollisionEvents;
        public bool DrawTriggerEvents;

        public bool DrawMeshColliderEdges;
        public bool DrawTerrainColliderEdges;
    }
}
#endif
