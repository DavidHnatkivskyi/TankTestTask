using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Effects
{
    public sealed class EffectSpawner : IInitializable, IDisposable, ITickable
    {
        private readonly DiContainer _container;
        private readonly SignalBus _signalBus;
        private readonly EffectConfig _effectConfig;
        private readonly Dictionary<EffectType, EffectPool> _pools = new();
        private readonly List<ActiveEffect> _activeEffects = new();

        private Transform _poolRoot;

        public EffectSpawner(DiContainer container, SignalBus signalBus, EffectConfig effectConfig)
        {
            _container = container;
            _signalBus = signalBus;
            _effectConfig = effectConfig;
        }

        public void Initialize()
        {
            _poolRoot = new GameObject("[Effect Pool]").transform;
            _signalBus.Subscribe<EffectSpawnRequested>(Spawn);
        }

        public void Dispose()
        {
            _signalBus.TryUnsubscribe<EffectSpawnRequested>(Spawn);

            if (_poolRoot == null)
            {
                _activeEffects.Clear();
                _pools.Clear();
                return;
            }

            DespawnAllEffects();
            UnityEngine.Object.Destroy(_poolRoot.gameObject);
            _pools.Clear();
        }

        public void Tick()
        {
            for (var index = _activeEffects.Count - 1; index >= 0; index--)
            {
                var effect = _activeEffects[index];
                if (effect.Instance.GameObject.activeSelf && Time.time < effect.DespawnTime)
                {
                    continue;
                }

                _activeEffects.RemoveAt(index);
                GetPool(effect.Type).Return(effect.Instance);
            }
        }

        private void Spawn(EffectSpawnRequested request)
        {
            var effect = GetPool(request.Type).Rent(request.Position, request.Rotation);
            _activeEffects.Add(new ActiveEffect(
                request.Type,
                effect,
                Time.time + effect.Duration));
        }

        private EffectPool GetPool(EffectType type)
        {
            if (_pools.TryGetValue(type, out var pool))
            {
                return pool;
            }

            pool = new EffectPool(_container, _effectConfig.GetPrefab(type), _poolRoot);
            _pools.Add(type, pool);
            return pool;
        }

        private void DespawnAllEffects()
        {
            for (var index = _activeEffects.Count - 1; index >= 0; index--)
            {
                var effect = _activeEffects[index];
                GetPool(effect.Type).Return(effect.Instance);
            }

            _activeEffects.Clear();
        }

        private sealed class ActiveEffect
        {
            public ActiveEffect(EffectType type, EffectInstance instance, float despawnTime)
            {
                Type = type;
                Instance = instance;
                DespawnTime = despawnTime;
            }

            public EffectType Type { get; }
            public EffectInstance Instance { get; }
            public float DespawnTime { get; }
        }

        private sealed class EffectInstance
        {
            public EffectInstance(GameObject gameObject)
            {
                GameObject = gameObject;
                ParticleSystems = gameObject.GetComponentsInChildren<ParticleSystem>(true);

                foreach (var particleSystem in ParticleSystems)
                {
                    var main = particleSystem.main;
                    Duration = Mathf.Max(
                        Duration,
                        main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
                }
            }

            public GameObject GameObject { get; }

            public ParticleSystem[] ParticleSystems { get; }

            public float Duration { get; }
        }

        private sealed class EffectPool
        {
            private readonly DiContainer _container;
            private readonly GameObject _prefab;
            private readonly Transform _root;
            private readonly Stack<EffectInstance> _inactiveInstances = new();

            public EffectPool(DiContainer container, GameObject prefab, Transform root)
            {
                _container = container;
                _prefab = prefab;
                _root = root;
            }

            public EffectInstance Rent(Vector3 position, Quaternion rotation)
            {
                var instance = _inactiveInstances.Count > 0
                    ? _inactiveInstances.Pop()
                    : new EffectInstance(_container.InstantiatePrefab(_prefab, position, rotation, _root));

                instance.GameObject.transform.SetPositionAndRotation(position, rotation);
                instance.GameObject.SetActive(true);
                RestartParticles(instance);
                return instance;
            }

            public void Return(EffectInstance instance)
            {
                foreach (var particleSystem in instance.ParticleSystems)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                instance.GameObject.SetActive(false);
                _inactiveInstances.Push(instance);
            }

            private static void RestartParticles(EffectInstance instance)
            {
                foreach (var particleSystem in instance.ParticleSystems)
                {
                    particleSystem.Clear(true);
                    particleSystem.Play(true);
                }
            }
        }
    }
}
