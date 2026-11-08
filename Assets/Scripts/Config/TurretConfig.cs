using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "TurretConfig", menuName = "Config/Turret")]
    public class TurretConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float RotationSpeed { get; private set; } = 180f;
    }
}
