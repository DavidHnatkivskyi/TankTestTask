using Config;
using Projectile;
using UnityEngine;
using Zenject;

namespace Tank
{
    public class Turret : MonoBehaviour, ITurret, IProjectileHitFilter
    {
        [SerializeField] private TurretConfig config;
        [SerializeField] private Transform projectileSpawnPoint;
        private IProjectileEmitter _projectileEmitter;
        private ProjectileSetConfig _projectileSet;
        private float _fireReloadDuration;
        private float _nextFireTime;

        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;

        [Inject]
        private void Construct(IProjectileEmitter projectileEmitter)
        {
            _projectileEmitter = projectileEmitter;
        }

        private void Awake()
        {
            if (config == null)
            {
                throw new MissingReferenceException($"Assign {nameof(TurretConfig)} to '{name}'.");
            }

            if (projectileSpawnPoint == null)
            {
                throw new MissingReferenceException($"Assign a projectile spawn point to '{name}'.");
            }
        }

        public void RotateTowards(Vector3 worldPosition, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                config.RotationSpeed * deltaTime);
        }

        public void Configure(ProjectileSetConfig projectileSet, float fireReloadDuration)
        {
            _projectileSet = projectileSet ?? throw new MissingReferenceException(
                $"Assign a {nameof(ProjectileSetConfig)} to '{name}'.");
            _fireReloadDuration = fireReloadDuration;
            _nextFireTime = 0f;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public bool TryFire()
        {
            if (_projectileSet == null || _projectileEmitter == null || Time.time < _nextFireTime)
            {
                return false;
            }

            var direction = projectileSpawnPoint.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            direction.Normalize();
            _projectileEmitter.Emit(new ProjectileShot(
                _projectileSet,
                projectileSpawnPoint.position,
                direction,
                this));
            _nextFireTime = Time.time + _fireReloadDuration;
            return true;
        }

        public bool ShouldIgnore(Collider collider)
        {
            return collider != null && collider.transform.IsChildOf(transform.root);
        }
    }
}
