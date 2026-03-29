using Unity.Collections;
using Unity.Entities;

namespace Scripts.Data.Stats
{
    public struct StatFloatCatalogBlob
    {
        public BlobArray<StatFloatCatalogEntry> Entries;
    }

    public struct StatFloatCatalogEntry
    {
        public FixedString64Bytes Name;
        public FixedString32Bytes ShortName;
    }

    public struct StatFloatCatalogSingleton : IComponentData
    {
        public BlobAssetReference<StatFloatCatalogBlob> Value;
    }
}
