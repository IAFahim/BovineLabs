// <copyright file="EntityTestBuilders.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Tests.TestDoubles
{
    using BovineLabs.Vibe.Data;
    using Unity.Entities;

    public static class EntityTestBuilders
    {
        public static Entity CreateEntityWith<T>(EntityManager manager, in T component)
            where T : unmanaged, IComponentData
        {
            var entity = manager.CreateEntity(typeof(T));
            manager.SetComponentData(entity, component);
            return entity;
        }

        public static Entity CreateCurveCacheEntity(EntityManager manager)
        {
            var entity = manager.CreateEntity(typeof(ClipBlobCurveCache));
            manager.SetComponentData(entity, ClipBlobCurveCache.Create());
            return entity;
        }
    }
}
