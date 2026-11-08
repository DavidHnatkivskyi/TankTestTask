using System;
using GameFlow.Signals;
using MapGeneration;
using UnityEngine;
using Zenject;

namespace TankSpawning
{
    /// <summary>
    /// Restores the local player's tank only after its respawn-window request.
    /// </summary>
    internal sealed class PlayerTankRespawnHandler : IInitializable, IDisposable
    {
        private readonly ITankSpawner _tankSpawner;
        private readonly InitialComputerTankSpawner _initialComputerTankSpawner;
        private readonly INavMeshSpawnPointFinder _spawnPointFinder;
        private readonly SignalBus _signalBus;

        private bool _isAwaitingRespawn;

        public PlayerTankRespawnHandler(
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
            _signalBus.Subscribe<PlayerTankDestroyedSignal>(OnPlayerTankDestroyed);
            _signalBus.Subscribe<PlayerRespawnRequestedSignal>(OnPlayerRespawnRequested);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<PlayerTankDestroyedSignal>(OnPlayerTankDestroyed);
            _signalBus.Unsubscribe<PlayerRespawnRequestedSignal>(OnPlayerRespawnRequested);
        }

        private void OnPlayerTankDestroyed()
        {
            _isAwaitingRespawn = true;
        }

        private void OnPlayerRespawnRequested()
        {
            if (!_isAwaitingRespawn ||
                !_spawnPointFinder.TryFindClosestToMapCenter(out var spawnPosition))
            {
                return;
            }

            _tankSpawner.Spawn(TankControlType.LocalPlayer, spawnPosition, Quaternion.identity);
            _initialComputerTankSpawner.RespawnNearPerimeter(spawnPosition);
            _isAwaitingRespawn = false;
            _signalBus.Fire(new PlayerTankRespawnedSignal());
        }
    }
}
