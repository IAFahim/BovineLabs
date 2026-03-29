using System;
using Scripts.Data.Stats;
using UnityEngine;

namespace Scripts.Authoring.Stats
{
    [CreateAssetMenu(fileName = "StatBoolCatalogSettings", menuName = "Settings/Stats/Bool Catalog Settings")]
    public sealed class StatBoolCatalogSettings : ScriptableObject
    {
        public Definition[] Definitions = Array.Empty<Definition>();

        [Serializable]
        public struct Definition
        {
            public string Name;
            public string ShortName;
            public bool InitialValue;
            public StatBoolView View;
        }
    }
}
