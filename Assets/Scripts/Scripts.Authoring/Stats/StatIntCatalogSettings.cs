using System;
using Scripts.Data.Stats;
using UnityEngine;

namespace Scripts.Authoring.Stats
{
    [CreateAssetMenu(fileName = "StatIntCatalogSettings", menuName = "Settings/Stats/Int Catalog Settings")]
    public sealed class StatIntCatalogSettings : ScriptableObject
    {
        public Definition[] Definitions = Array.Empty<Definition>();

        [Serializable]
        public struct Definition
        {
            public string Name;
            public string ShortName;
            public int InitialValue;
            public StatIntView View;
            public int LinkId;
        }
    }
}
