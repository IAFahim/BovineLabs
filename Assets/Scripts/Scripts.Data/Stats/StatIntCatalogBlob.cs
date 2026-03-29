using Unity.Collections;
using Unity.Entities;

namespace Scripts.Data.Stats
{
    public struct StatIntCatalogBlob
    {
        public BlobArray<StatIntCatalogEntry> Entries;
    }

    public struct StatIntCatalogEntry
    {
        public FixedString64Bytes Name;
        public FixedString32Bytes ShortName;
    }

    public struct StatIntCatalogSingleton : IComponentData
    {
        public BlobAssetReference<StatIntCatalogBlob> Value;
    }
}
