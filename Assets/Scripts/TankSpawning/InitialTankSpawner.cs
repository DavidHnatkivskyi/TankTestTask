using System;
using GameFlow.Signals;
using MapGeneration;
using Zenject;

namespace TankSpawning
{
    /// <summary>
    /// Spawns the local player's tank at the NavMesh point closest to the map center.
    /// </summary>
    internal sealed class InitialTankSpawner : IInitializable, IDisposable
    {
        private readonly ITankSpawner _tankSpawner;
        private readonly InitialComputerTankSpawner _initialComputerTankSpawner;
        private readonly INavMeshSpawnPointFinder _spawnPointFinder;
        private readonly SignalBus _signalBus;

        public InitialTankSpawner(
            ITankSpawner tankSpawner,
            InitialComputerTankSpawner initialComputerTankSpawner,
            INavMeshSpawnPointFinder spawnPointFinder,
            SignalBus signalBus)
        {
            _tankSpawner = tankSpawner;
            _initialComputerTankSpawner = initialComputerTankSpawner;
            _spawnPointFinder = spawnPointFinder;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<GameplayStartedSignal>(OnGameplayStarted);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<GameplayStartedSignal>(OnGameplayStarted);
        }

        private void OnGameplayStarted()
        {
            if (!_spawnPointFinder.TryFindClosestToMapCenter(out var spawnPosition))
            {
                throw new System.InvalidOperationException("Cannot find a NavMesh point near the map center for the local player.");
            }

            _tankSpawner.Spawn(
                TankControlType.LocalPlayer,
                spawnPosition,
                UnityEngine.Quaternion.identity);
            _initialComputerTankSpawner.SpawnNearPerimeter(spawnPosition);
        }
    }
}
