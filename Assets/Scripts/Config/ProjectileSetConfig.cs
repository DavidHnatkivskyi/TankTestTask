using System;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "ProjectileSetConfig", menuName = "Config/Projectile Set")]
    public sealed class ProjectileSetConfig : ScriptableObject
    {
        [Header("Projectile")]
        [SerializeField] private GameObject projectileVisual;

        [Header("Simulation")]
        [SerializeField, Min(0f)] private float speed = 35f;
        [SerializeField, Min(0f)] private float maximumDistance = 60f;
        [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;

        public GameObject ProjectileVisual => projectileVisual;
        public float Speed => speed;
        public float MaximumDistance => maximumDistance;
        public LayerMask CollisionMask => collisionMask;

        public void Validate()
        {
            if (projectileVisual == null)
            {
                throw new InvalidOperationException($"Projectile set '{name}' requires a projectile visual.");
            }

            if (speed <= 0f)
            {
                throw new InvalidOperationException($"Projectile set '{name}' must have a positive speed.");
            }

            if (maximumDistance <= 0f)
            {
                throw new InvalidOperationException($"Projectile set '{name}' must have a positive maximum distance.");
            }

        }
    }
}
