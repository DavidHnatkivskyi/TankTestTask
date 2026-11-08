using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MapGeneration;
using UnityEngine;

namespace TankSpawning
{
    internal sealed class ComputerTankRespawner : IDisposable
    {
        private readonly ITankRuntimeCommands _runtimeCommands;
        private readonly TankRuntimeRegistry _runtimeRegistry;
        private readonly TankSpawningConfig _config;
        private readonly INavMeshSpawnPointFinder _spawnPointFinder;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public ComputerTankRespawner(
            ITankRuntimeCommands runtimeCommands,
            TankRuntimeRegistry runtimeRegistry,
            TankSpawningConfig config,
            INavMeshSpawnPointFinder spawnPointFinder)
        {
            _runtimeCommands = runtimeCommands;
            _runtimeRegistry = runtimeRegistry;
            _config = config;
            _spawnPointFinder = spawnPointFinder;
        }

        public void Schedule(TankSpawnData spawnData)
        {
            RespawnAsync(spawnData, _runtimeRegistry.ComputerPopulationVersion).Forget();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private async UniTask RespawnAsync(TankSpawnData spawnData, int populationVersion)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_config.ComputerRespawnDelay),
                    cancellationToken: _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (populationVersion != _runtimeRegistry.ComputerPopulationVersion)
            {
                return;
            }

            if (!TryGetLocalPlayerPosition(out var playerPosition) ||
                !_spawnPointFinder.TryFindNearWallFarthestFrom(
                    playerPosition,
                    _config.NavMeshPointSearchAttempts,
                    out var spawnPosition))
            {
                Debug.LogWarning("Could not find a NavMesh point to respawn a computer-controlled tank.");
                return;
            }

            _runtimeCommands.SpawnExact(
                spawnData,
                spawnPosition,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
        }

        private bool TryGetLocalPlayerPosition(out Vector3 position)
        {
            foreach (var handle in _runtimeRegistry.ActiveHandles)
            {
                if (handle.ControlType == TankControlType.LocalPlayer)
                {
                    position = handle.Chassis.Position;
                    return true;
                }
            }

            position = default;
            return false;
        }
    }
}
