using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "TankChassisConfig", menuName = "Config/Tank Chassis")]
    public class TankChassisConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float ReverseSpeed { get; private set; } = 2f;
        [field: SerializeField, Min(0f)] public float DecelerationDuration { get; private set; } = 0.15f;
        [field: SerializeField, Min(0.01f)] public float TurnRadius { get; private set; } = 3f;
        [field: SerializeField, Min(0f)] public float TurnSpeed { get; private set; } = 180f;
    }
}
