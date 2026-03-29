using System;
using Scripts.Data.Stats;
using UnityEngine;

namespace Scripts.Authoring.Stats
{
    [CreateAssetMenu(fileName = "StatCatalogSettings", menuName = "Settings/Stats/Stat Catalog Settings")]
    public sealed class StatCatalogSettings : ScriptableObject
    {
        public Definition[] Definitions = Array.Empty<Definition>();

        [Serializable]
        public struct Definition
        {
            public string Name;
            public float InitialValue;
            public StatDisplayStyle Style;
            public StatLinkType LinkType;
            public int LinkId;
        }
    }
}
