using Unity.Collections;
using Unity.Entities;

namespace Scripts.Data.Stats
{
    public struct StatCatalogBlob
    {
        public BlobArray<StatCatalogName> Names;
    }

    public struct StatCatalogName
    {
        public FixedString64Bytes Value;
    }

    public struct StatCatalogSingleton : IComponentData
    {
        public BlobAssetReference<StatCatalogBlob> Value;
    }
}
