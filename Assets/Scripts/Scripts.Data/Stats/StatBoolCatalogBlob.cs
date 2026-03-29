using Unity.Collections;
using Unity.Entities;

namespace Scripts.Data.Stats
{
    public struct StatBoolCatalogBlob
    {
        public BlobArray<StatBoolCatalogEntry> Entries;
    }

    public struct StatBoolCatalogEntry
    {
        public FixedString64Bytes Name;
        public FixedString32Bytes ShortName;
    }

    public struct StatBoolCatalogSingleton : IComponentData
    {
        public BlobAssetReference<StatBoolCatalogBlob> Value;
    }
}
