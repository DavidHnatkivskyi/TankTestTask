using System;
using System.Collections.Generic;
using UnityEngine;

namespace Effects
{
    [CreateAssetMenu(fileName = "EffectConfig", menuName = "Config/Effects")]
    public sealed class EffectConfig : ScriptableObject
    {
        [SerializeField] private List<EffectDefinition> effects = new();

        public GameObject GetPrefab(EffectType type)
        {
            foreach (var effect in effects)
            {
                if (effect.Type == type)
                {
                    if (effect.Prefab == null)
                    {
                        throw new InvalidOperationException($"Effect '{type}' in '{name}' requires a prefab.");
                    }

                    return effect.Prefab;
                }
            }

            throw new InvalidOperationException($"Effect config '{name}' does not define '{type}'.");
        }

        [Serializable]
        private sealed class EffectDefinition
        {
            [SerializeField] private EffectType type;
            [SerializeField] private GameObject prefab;

            public EffectType Type => type;
            public GameObject Prefab => prefab;
        }
    }
}
