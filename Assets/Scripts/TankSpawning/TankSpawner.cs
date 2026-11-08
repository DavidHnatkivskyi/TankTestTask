using System;
using System.Collections.Generic;
using Tank;
using TankControl;
using UnityEngine;
using Zenject;

namespace TankSpawning
{
    /// <summary>
    /// Selects configured tanks, rents their instances and returns despawned tanks to their pools.
    /// </summary>
    internal sealed class TankSpawner : ITankSpawner, ITankRuntimeCommands
    {
        private readonly DiContainer _container;
        private readonly TankSpawningConfig _config;
        private readonly TankControlRegistrationService _controlRegistration;
        private readonly TankRuntimeRegistry _runtimeRegistry;
        private readonly Dictionary<TankSpawnData, Stack<TankInstance>> _pools = new();

        public TankSpawner(
            DiContainer container,
            TankSpawningConfig config,
            TankControlRegistrationService controlRegistration,
            TankRuntimeRegistry runtimeRegistry)
        {
            _container = container;
            _config = config;
            _controlRegistration = controlRegistration;
            _runtimeRegistry = runtimeRegistry;
        }

        public TankSpawnHandle Spawn(TankControlType controlType, Vector3 position, Quaternion rotation)
        {
            return SpawnInternal(SelectSpawnData(controlType), position, rotation);
        }

        public TankSpawnHandle Spawn(
            TankControlType controlType,
            ComputerTankBehavior computerBehavior,
            Vector3 position,
            Quaternion rotation)
        {
            return SpawnInternal(SelectSpawnData(controlType, computerBehavior), position, rotation);
        }

        public bool Despawn(TankSpawnHandle handle)
        {
            if (handle == null || !_runtimeRegistry.Remove(handle))
            {
                return false;
            }

            _controlRegistration.Unregister(handle);
            handle.Instance.SetActive(false);
            GetPool(handle.SpawnData).Push(handle.Instance);
            return true;
        }

        public void DespawnAll(TankControlType controlType)
        {
            if (controlType == TankControlType.Computer)
            {
                _runtimeRegistry.InvalidateComputerPopulation();
            }

            var handlesToDespawn = new List<TankSpawnHandle>();
            foreach (var handle in _runtimeRegistry.ActiveHandles)
            {
                if (handle.ControlType == controlType)
                {
                    handlesToDespawn.Add(handle);
                }
            }

            foreach (var handle in handlesToDespawn)
            {
                Despawn(handle);
            }
        }

        TankSpawnHandle ITankRuntimeCommands.SpawnExact(
            TankSpawnData spawnData,
            Vector3 position,
            Quaternion rotation)
        {
            return SpawnInternal(spawnData, position, rotation);
        }

        private TankSpawnHandle SpawnInternal(
            TankSpawnData spawnData,
            Vector3 position,
            Quaternion rotation)
        {
            var tankInstance = GetTankInstance(spawnData);
            var handle = CreateHandle(spawnData, tankInstance);
            handle.Instance.SetPositionAndRotation(position, rotation);
            handle.Instance.SetActive(true);
            _runtimeRegistry.Add(handle);
            _controlRegistration.Register(handle);
            return handle;
        }

        private TankSpawnData SelectSpawnData(TankControlType controlType)
        {
            return SelectSpawnData(controlType, null);
        }

        private TankSpawnData SelectSpawnData(
            TankControlType controlType,
            ComputerTankBehavior? computerBehavior)
        {
            if (_config == null)
            {
                throw new InvalidOperationException("TankSpawningConfig is not assigned to GameInstaller.");
            }

            var totalChance = 0f;
            foreach (var data in _config.TankSpawnData)
            {
                if (IsEligible(data, controlType, computerBehavior))
                {
                    totalChance += data.SpawnChance;
                }
            }

            if (totalChance <= 0f)
            {
                throw new InvalidOperationException($"No tank configured for {controlType}.");
            }

            var choice = UnityEngine.Random.value * totalChance;
            foreach (var data in _config.TankSpawnData)
            {
                if (!IsEligible(data, controlType, computerBehavior))
                {
                    continue;
                }

                choice -= data.SpawnChance;
                if (choice <= 0f)
                {
                    return data;
                }
            }

            throw new InvalidOperationException("Could not choose a configured tank.");
        }

        private TankInstance GetTankInstance(TankSpawnData spawnData)
        {
            var pool = GetPool(spawnData);
            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            return _container.InstantiatePrefabForComponent<TankInstance>(spawnData.Prefab);
        }

        private static TankSpawnHandle CreateHandle(
            TankSpawnData spawnData,
            TankInstance tankInstance)
        {
            tankInstance.Configure(spawnData.TankConfig, spawnData.ProjectileSet);
            return new TankSpawnHandle(spawnData, tankInstance);
        }

        private Stack<TankInstance> GetPool(TankSpawnData spawnData)
        {
            if (_pools.TryGetValue(spawnData, out var pool))
            {
                return pool;
            }

            pool = new Stack<TankInstance>();
            _pools.Add(spawnData, pool);
            return pool;
        }

        private static bool IsEligible(
            TankSpawnData data,
            TankControlType controlType,
            ComputerTankBehavior? computerBehavior)
        {
            return data != null &&
                   data.Prefab != null &&
                   data.TankConfig != null &&
                   data.ControlType == controlType &&
                   (!computerBehavior.HasValue || data.ComputerBehavior == computerBehavior.Value) &&
                   data.SpawnChance > 0f;
        }
    }
}
