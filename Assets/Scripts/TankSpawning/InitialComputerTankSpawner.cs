using MapGeneration;
using UnityEngine;

namespace TankSpawning
{
    /// <summary>
    /// Creates configured computer-controlled tanks near the map perimeter.
    /// </summary>
    internal sealed class InitialComputerTankSpawner
    {
        private readonly ITankSpawner _tankSpawner;
        private readonly TankSpawningConfig _config;
        private readonly INavMeshSpawnPointFinder _spawnPointFinder;

        public InitialComputerTankSpawner(
            ITankSpawner tankSpawner,
            TankSpawningConfig config,
            INavMeshSpawnPointFinder spawnPointFinder)
        {
            _tankSpawner = tankSpawner;
            _config = config;
            _spawnPointFinder = spawnPointFinder;
        }

        public void SpawnNearPerimeter(Vector3 playerPosition)
        {
            SpawnConfiguredTanks(playerPosition);
        }

        public void RespawnNearPerimeter(Vector3 playerPosition)
        {
            _tankSpawner.DespawnAll(TankControlType.Computer);
            SpawnConfiguredTanks(playerPosition);
        }

        private void SpawnConfiguredTanks(Vector3 playerPosition)
        {
            foreach (var behavior in _config.InitialComputerTankBehaviors)
            {
                if (!_spawnPointFinder.TryFindNearWallFarthestFrom(
                        playerPosition,
                        _config.NavMeshPointSearchAttempts,
                        out var spawnPosition))
                {
                    Debug.LogWarning("Could not find a NavMesh point for a computer-controlled tank.");
                    continue;
                }

                _tankSpawner.Spawn(
                    TankControlType.Computer,
                    behavior,
                    spawnPosition,
                    Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
            }
        }
    }
}
