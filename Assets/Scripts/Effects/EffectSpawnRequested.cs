using UnityEngine;

namespace Effects
{
    public readonly struct EffectSpawnRequested
    {
        public EffectSpawnRequested(EffectType type, Vector3 position, Quaternion rotation)
        {
            Type = type;
            Position = position;
            Rotation = rotation;
        }

        public EffectType Type { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}
