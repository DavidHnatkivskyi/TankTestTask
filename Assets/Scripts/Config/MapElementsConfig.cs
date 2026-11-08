using System.Collections.Generic;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "MapElementsConfig", menuName = "Config/Map Elements")]
    public sealed class MapElementsConfig : ScriptableObject
    {
        [SerializeField] private Material groundMaterial;
        [SerializeField] private List<MapElementDefinition> elements = new()
        {
            new(MapElementType.Wall),
            new(MapElementType.Obstacle)
        };

        public Material GroundMaterial => groundMaterial;

        public IReadOnlyList<GameObject> GetPrefabs(MapElementType type)
        {
            var prefabs = new List<GameObject>();
            foreach (var element in elements)
            {
                if (element != null && element.Type == type && element.Prefab != null)
                {
                    prefabs.Add(element.Prefab);
                }
            }

            return prefabs;
        }

        public bool TryGetPrefab(MapElementType type, out GameObject prefab)
        {
            foreach (var element in GetPrefabs(type))
            {
                prefab = element;
                return true;
            }

            prefab = null;
            return false;
        }
    }
}
