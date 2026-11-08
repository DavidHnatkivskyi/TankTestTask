using System;
using System.Collections.Generic;
using Config;
using Effects;
using UnityEngine;
using Zenject;

namespace Projectile
{
    /// <summary>
    /// Owns visual projectile simulation and delegates collider impacts to <see cref="IProjectileImpactHandler"/>.
    /// </summary>
    public sealed class ProjectileSimulation : IInitializable, IDisposable, ITickable, IProjectileEmitter
    {
        private const float MinimumDistance = 0.001f;
        private const int RaycastBufferSize = 16;

        private readonly DiContainer _container;
        private readonly ProjectileConfig _projectileCatalog;
        private readonly SignalBus _signalBus;
        private readonly IProjectileImpactHandler _impactHandler;
        private readonly RaycastHit[] _raycastHits = new RaycastHit[RaycastBufferSize];
        private readonly List<ActiveProjectile> _activeProjectiles = new();
        private readonly Dictionary<ProjectileSetConfig, ProjectileVisualPool> _pools = new();

        private Transform _poolRoot;

        public ProjectileSimulation(
            DiContainer container,
            ProjectileConfig projectileCatalog,
            SignalBus signalBus,
            IProjectileImpactHandler impactHandler)
        {
            _container = container;
            _projectileCatalog = projectileCatalog;
            _signalBus = signalBus;
            _impactHandler = impactHandler;
        }

        public void Initialize()
        {
            _poolRoot = new GameObject("[Projectile VFX Pool]").transform;
        }

        public void Dispose()
        {
            if (_poolRoot == null)
            {
                _activeProjectiles.Clear();
                _pools.Clear();
                return;
            }

            DespawnAllProjectiles();
            UnityEngine.Object.Destroy(_poolRoot.gameObject);
            _pools.Clear();
        }

        public void Tick()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            for (var index = _activeProjectiles.Count - 1; index >= 0; index--)
            {
                SimulateProjectile(index, deltaTime);
            }
        }

        public void Emit(ProjectileShot shot)
        {
            ValidateProjectileShot(shot);
            var rotation = Quaternion.LookRotation(shot.Direction);
            var visual = GetPool(shot.ProjectileSet).Rent(shot.Origin, rotation);
            _activeProjectiles.Add(new ActiveProjectile(visual, shot.Origin, shot.Direction, shot.HitFilter)
            {
                RemainingDistance = shot.ProjectileSet.MaximumDistance,
                Config = shot.ProjectileSet
            });
            _signalBus.Fire(new EffectSpawnRequested(EffectType.MuzzleFlash, shot.Origin, rotation));
        }

        private void SimulateProjectile(int index, float deltaTime)
        {
            var projectile = _activeProjectiles[index];
            var stepDistance = Mathf.Min(projectile.Config.Speed * deltaTime, projectile.RemainingDistance);
            if (TryFindCollision(projectile, stepDistance, out var hit))
            {
                _impactHandler.TryHandleImpact(hit.collider);
                DespawnProjectile(index, true, hit.point, Quaternion.LookRotation(hit.normal));
                return;
            }

            projectile.Position += projectile.Direction * stepDistance;
            projectile.RemainingDistance -= stepDistance;
            projectile.Visual.transform.SetPositionAndRotation(projectile.Position, Quaternion.LookRotation(projectile.Direction));

            if (projectile.RemainingDistance <= MinimumDistance)
            {
                DespawnProjectile(index, true, projectile.Position, Quaternion.LookRotation(-projectile.Direction));
            }
        }

        private bool TryFindCollision(ActiveProjectile projectile, float distance, out RaycastHit closestHit)
        {
            closestHit = default;
            var hasHit = false;
            var hitCount = Physics.RaycastNonAlloc(
                projectile.Position,
                projectile.Direction,
                _raycastHits,
                distance,
                projectile.Config.CollisionMask,
                QueryTriggerInteraction.Ignore);

            for (var index = 0; index < hitCount; index++)
            {
                var hit = _raycastHits[index];
                if (projectile.HitFilter.ShouldIgnore(hit.collider) ||
                    hasHit && hit.distance >= closestHit.distance)
                {
                    continue;
                }

                closestHit = hit;
                hasHit = true;
            }

            return hasHit;
        }

        private void DespawnProjectile(int index, bool spawnImpact, Vector3 position, Quaternion rotation)
        {
            var projectile = _activeProjectiles[index];
            _activeProjectiles.RemoveAt(index);
            GetPool(projectile.Config).Return(projectile.Visual);

            if (spawnImpact)
            {
                _signalBus.Fire(new EffectSpawnRequested(EffectType.ProjectileImpact, position, rotation));
            }
        }

        private void DespawnAllProjectiles()
        {
            for (var index = _activeProjectiles.Count - 1; index >= 0; index--)
            {
                DespawnProjectile(index, false, Vector3.zero, Quaternion.identity);
            }
        }

        private ProjectileVisualPool GetPool(ProjectileSetConfig config)
        {
            if (_pools.TryGetValue(config, out var pool))
            {
                return pool;
            }

            pool = new ProjectileVisualPool(_container, config.ProjectileVisual, _poolRoot);
            _pools.Add(config, pool);
            return pool;
        }

        private void ValidateProjectileShot(ProjectileShot shot)
        {
            if (!_projectileCatalog.Contains(shot.ProjectileSet))
            {
                throw new InvalidOperationException(
                    $"Projectile set must be included in '{_projectileCatalog.name}'.");
            }

            shot.ProjectileSet.Validate();
            if (shot.HitFilter == null)
            {
                throw new InvalidOperationException(
                    "Projectile firing requires a hit filter.");
            }

            if (shot.Direction.sqrMagnitude <= MinimumDistance * MinimumDistance)
            {
                throw new InvalidOperationException("Projectile firing requires a direction.");
            }
        }

        private sealed class ActiveProjectile
        {
            public ActiveProjectile(
                GameObject visual,
                Vector3 position,
                Vector3 direction,
                IProjectileHitFilter hitFilter)
            {
                Visual = visual;
                Position = position;
                Direction = direction;
                HitFilter = hitFilter;
            }

            public GameObject Visual { get; }
            public Vector3 Position { get; set; }
            public Vector3 Direction { get; }
            public IProjectileHitFilter HitFilter { get; }
            public float RemainingDistance { get; set; }
            public ProjectileSetConfig Config { get; set; }
        }

        private sealed class ProjectileVisualPool
        {
            private readonly DiContainer _container;
            private readonly GameObject _prefab;
            private readonly Transform _root;
            private readonly Stack<GameObject> _inactiveInstances = new();

            public ProjectileVisualPool(DiContainer container, GameObject prefab, Transform root)
            {
                _container = container;
                _prefab = prefab;
                _root = root;
            }

            public GameObject Rent(Vector3 position, Quaternion rotation)
            {
                var instance = _inactiveInstances.Count > 0
                    ? _inactiveInstances.Pop()
                    : _container.InstantiatePrefab(_prefab, position, rotation, _root);

                instance.transform.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
                return instance;
            }

            public void Return(GameObject instance)
            {
                instance.SetActive(false);
                _inactiveInstances.Push(instance);
            }
        }
    }
}
