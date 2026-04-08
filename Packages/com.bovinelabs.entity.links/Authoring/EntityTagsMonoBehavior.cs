using System;
using BovineLabs.Core.Keys;
using UnityEngine;

namespace BovineLabs.EntityLinks.Authoring
{
    public class EntityTagsMonoBehavior : MonoBehaviour
    {
        public EntitySelfIdBakeData[] ids;

        [Serializable]
        public struct EntitySelfIdBakeData
        {
            [K(nameof(EntityLinkKeys))] public byte key;
            public Transform linkTransformOffset;
        }
    }
}