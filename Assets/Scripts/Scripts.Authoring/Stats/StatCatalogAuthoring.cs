using System;
using Scripts.Data.Stats;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Scripts.Authoring.Stats
{
    public sealed class StatCatalogAuthoring : MonoBehaviour
    {
        public StatFloatCatalogSettings FloatSettings;
        public StatIntCatalogSettings IntSettings;
        public StatBoolCatalogSettings BoolSettings;

        public sealed class Baker : Baker<StatCatalogAuthoring>
        {
            public override void Bake(StatCatalogAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.BakeFloats(entity, authoring.FloatSettings);
                this.BakeInts(entity, authoring.IntSettings);
                this.BakeBools(entity, authoring.BoolSettings);
            }

            private void BakeFloats(Entity entity, StatFloatCatalogSettings settings)
            {
                var definitions = Validate(settings);
                var buffer = this.AddBuffer<StatFloatElement>(entity);
                buffer.ResizeUninitialized(definitions.Length);

                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<StatFloatCatalogBlob>();
                var entries = blobBuilder.Allocate(ref root.Entries, definitions.Length);

                for (var i = 0; i < definitions.Length; i++)
                {
                    var definition = definitions[i];
                    entries[i] = new StatFloatCatalogEntry
                    {
                        Name = new FixedString64Bytes(definition.Name ?? string.Empty),
                        ShortName = new FixedString32Bytes(string.IsNullOrWhiteSpace(definition.ShortName) ? definition.Name ?? string.Empty : definition.ShortName),
                    };
                    buffer[i] = new StatFloatElement
                    {
                        Id = (ushort)i,
                        LinkId = ResolveLinkId(definition.LinkId, definitions.Length, i, definition.Name),
                        View = definition.View,
                        Value = definition.InitialValue,
                    };
                }

                var blobReference = blobBuilder.CreateBlobAssetReference<StatFloatCatalogBlob>(Allocator.Persistent);
                this.AddBlobAsset(ref blobReference, out _);
                this.AddComponent(entity, new StatFloatCatalogSingleton { Value = blobReference });
                blobBuilder.Dispose();
            }

            private void BakeInts(Entity entity, StatIntCatalogSettings settings)
            {
                var definitions = Validate(settings);
                var buffer = this.AddBuffer<StatIntElement>(entity);
                buffer.ResizeUninitialized(definitions.Length);

                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<StatIntCatalogBlob>();
                var entries = blobBuilder.Allocate(ref root.Entries, definitions.Length);

                for (var i = 0; i < definitions.Length; i++)
                {
                    var definition = definitions[i];
                    entries[i] = new StatIntCatalogEntry
                    {
                        Name = new FixedString64Bytes(definition.Name ?? string.Empty),
                        ShortName = new FixedString32Bytes(string.IsNullOrWhiteSpace(definition.ShortName) ? definition.Name ?? string.Empty : definition.ShortName),
                    };
                    buffer[i] = new StatIntElement
                    {
                        Id = (ushort)i,
                        LinkId = ResolveLinkId(definition.LinkId, definitions.Length, i, definition.Name),
                        View = definition.View,
                        Value = definition.InitialValue,
                    };
                }

                var blobReference = blobBuilder.CreateBlobAssetReference<StatIntCatalogBlob>(Allocator.Persistent);
                this.AddBlobAsset(ref blobReference, out _);
                this.AddComponent(entity, new StatIntCatalogSingleton { Value = blobReference });
                blobBuilder.Dispose();
            }

            private void BakeBools(Entity entity, StatBoolCatalogSettings settings)
            {
                var definitions = Validate(settings);
                var buffer = this.AddBuffer<StatBoolElement>(entity);
                buffer.ResizeUninitialized(definitions.Length);

                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var root = ref blobBuilder.ConstructRoot<StatBoolCatalogBlob>();
                var entries = blobBuilder.Allocate(ref root.Entries, definitions.Length);

                for (var i = 0; i < definitions.Length; i++)
                {
                    var definition = definitions[i];
                    entries[i] = new StatBoolCatalogEntry
                    {
                        Name = new FixedString64Bytes(definition.Name ?? string.Empty),
                        ShortName = new FixedString32Bytes(string.IsNullOrWhiteSpace(definition.ShortName) ? definition.Name ?? string.Empty : definition.ShortName),
                    };
                    buffer[i] = new StatBoolElement
                    {
                        Id = (ushort)i,
                        View = definition.View,
                        Value = definition.InitialValue ? (byte)1 : (byte)0,
                    };
                }

                var blobReference = blobBuilder.CreateBlobAssetReference<StatBoolCatalogBlob>(Allocator.Persistent);
                this.AddBlobAsset(ref blobReference, out _);
                this.AddComponent(entity, new StatBoolCatalogSingleton { Value = blobReference });
                blobBuilder.Dispose();
            }

            private static StatFloatCatalogSettings.Definition[] Validate(StatFloatCatalogSettings settings)
            {
                if (settings == null)
                {
                    throw new InvalidOperationException($"{nameof(StatCatalogAuthoring)} requires {nameof(StatFloatCatalogSettings)}.");
                }

                if (settings.Definitions == null || settings.Definitions.Length == 0)
                {
                    throw new InvalidOperationException($"{nameof(StatFloatCatalogSettings)} must contain at least one definition.");
                }

                if (settings.Definitions.Length >= StatConstants.MaxStatCount)
                {
                    throw new InvalidOperationException($"{nameof(StatFloatCatalogSettings)} supports at most {StatConstants.MaxStatCount - 1} stats.");
                }

                return settings.Definitions;
            }

            private static StatIntCatalogSettings.Definition[] Validate(StatIntCatalogSettings settings)
            {
                if (settings == null)
                {
                    throw new InvalidOperationException($"{nameof(StatCatalogAuthoring)} requires {nameof(StatIntCatalogSettings)}.");
                }

                if (settings.Definitions == null || settings.Definitions.Length == 0)
                {
                    throw new InvalidOperationException($"{nameof(StatIntCatalogSettings)} must contain at least one definition.");
                }

                if (settings.Definitions.Length >= StatConstants.MaxStatCount)
                {
                    throw new InvalidOperationException($"{nameof(StatIntCatalogSettings)} supports at most {StatConstants.MaxStatCount - 1} stats.");
                }

                return settings.Definitions;
            }

            private static StatBoolCatalogSettings.Definition[] Validate(StatBoolCatalogSettings settings)
            {
                if (settings == null)
                {
                    throw new InvalidOperationException($"{nameof(StatCatalogAuthoring)} requires {nameof(StatBoolCatalogSettings)}.");
                }

                if (settings.Definitions == null || settings.Definitions.Length == 0)
                {
                    throw new InvalidOperationException($"{nameof(StatBoolCatalogSettings)} must contain at least one definition.");
                }

                if (settings.Definitions.Length >= StatConstants.MaxStatCount)
                {
                    throw new InvalidOperationException($"{nameof(StatBoolCatalogSettings)} supports at most {StatConstants.MaxStatCount - 1} stats.");
                }

                return settings.Definitions;
            }

            private static ushort ResolveLinkId(int linkId, int count, int index, string name)
            {
                if (linkId < 0)
                {
                    return StatConstants.NoLink;
                }

                if (linkId >= count)
                {
                    throw new InvalidOperationException($"Entry {index} ({name}) has invalid LinkId {linkId} for count {count}.");
                }

                return (ushort)linkId;
            }
        }
    }
}
