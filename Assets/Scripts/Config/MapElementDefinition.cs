using System;
using UnityEngine;

namespace Config
{
    [Serializable]
    public sealed class MapElementDefinition
    {
        [SerializeField] private MapElementType type;
        [SerializeField] private GameObject prefab;

        public MapElementDefinition(MapElementType type)
        {
            this.type = type;
        }

        public MapElementType Type => type;
        public GameObject Prefab => prefab;
    }
}
