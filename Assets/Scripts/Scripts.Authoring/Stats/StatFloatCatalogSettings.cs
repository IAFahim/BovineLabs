using System;
using Scripts.Data.Stats;
using UnityEngine;

namespace Scripts.Authoring.Stats
{
    [CreateAssetMenu(fileName = "StatFloatCatalogSettings", menuName = "Settings/Stats/Float Catalog Settings")]
    public sealed class StatFloatCatalogSettings : ScriptableObject
    {
        public Definition[] Definitions = Array.Empty<Definition>();

        [Serializable]
        public struct Definition
        {
            public string Name;
            public string ShortName;
            public float InitialValue;
            public StatFloatView View;
            public int LinkId;
        }
    }
}
