using System;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "TankConfig", menuName = "Config/Tank")]
    public sealed class TankConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float movementSpeed = 5f;
        [SerializeField, Min(0f)] private float fireReloadDuration = 0.25f;

        public float MovementSpeed => movementSpeed;
        public float FireReloadDuration => fireReloadDuration;

        public void Validate()
        {
            if (movementSpeed < 0f)
            {
                throw new InvalidOperationException($"Tank config '{name}' cannot have a negative movement speed.");
            }

            if (fireReloadDuration < 0f)
            {
                throw new InvalidOperationException($"Tank config '{name}' cannot have a negative fire reload duration.");
            }
        }
    }
}
