using Config;
using UnityEngine;

namespace Tank
{
    [DisallowMultipleComponent]
    public sealed class TankInstance : MonoBehaviour
    {
        [SerializeField] private TankChassis chassis;
        [SerializeField] private Turret turret;
        [SerializeField] private ComputerTankNavigation computerNavigation;

        private Collider[] _colliders;
        private float _boundingRadius = -1f;

        public ITankChassis Chassis => chassis;

        public ITurret Turret => turret;

        public IComputerTankNavigation ComputerNavigation => computerNavigation;

        private void Awake()
        {
            if (chassis == null || turret == null)
            {
                throw new MissingReferenceException(
                    $"Assign {nameof(TankChassis)} and {nameof(Turret)} to '{name}'.");
            }

            _colliders = GetComponentsInChildren<Collider>();
            if (_colliders.Length == 0)
            {
                throw new MissingReferenceException(
                    $"Assign at least one {nameof(Collider)} to '{name}' or one of its children.");
            }
        }

        public void Configure(TankConfig tankConfig, ProjectileSetConfig projectileSet)
        {
            chassis.Configure(tankConfig);
            turret.Configure(projectileSet, tankConfig.FireReloadDuration);
            computerNavigation?.Configure();
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            if (computerNavigation != null)
            {
                computerNavigation.SetPositionAndRotation(position, rotation);
                return;
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public bool ContainsCollider(Collider collider)
        {
            return collider != null &&
                   (collider.transform == transform || collider.transform.IsChildOf(transform));
        }

        /// <summary>
        /// Radius of a sphere around the tank position that contains all of its colliders,
        /// whatever the tank rotation. Calculated once and reused.
        /// </summary>
        public float GetBoundingRadius()
        {
            if (_boundingRadius < 0f)
            {
                _boundingRadius = CalculateBoundingRadius();
            }

            return _boundingRadius;
        }

        public bool IsOverlapping(TankInstance other)
        {
            if (other == null)
            {
                return false;
            }

            foreach (var collider in _colliders)
            {
                if (!IsAvailable(collider))
                {
                    continue;
                }

                foreach (var otherCollider in other._colliders)
                {
                    if (!IsAvailable(otherCollider))
                    {
                        continue;
                    }

                    if (Physics.ComputePenetration(
                            collider,
                            collider.transform.position,
                            collider.transform.rotation,
                            otherCollider,
                            otherCollider.transform.position,
                            otherCollider.transform.rotation,
                            out _,
                            out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private float CalculateBoundingRadius()
        {
            Physics.SyncTransforms();
            var center = transform.position;
            var radius = 0f;
            foreach (var collider in _colliders)
            {
                if (!IsAvailable(collider))
                {
                    continue;
                }

                var bounds = collider.bounds;
                radius = Mathf.Max(radius, Vector3.Distance(center, bounds.center) + bounds.extents.magnitude);
            }

            return radius;
        }

        private static bool IsAvailable(Collider collider)
        {
            return collider != null && collider.enabled && collider.gameObject.activeInHierarchy;
        }
    }
}
