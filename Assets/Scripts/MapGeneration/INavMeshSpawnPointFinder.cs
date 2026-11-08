using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Finds a valid point on the NavMesh for tank spawning.
    /// </summary>
    public interface INavMeshSpawnPointFinder : INavMeshPointSampler
    {
        bool TryFindClosestToMapCenter(out Vector3 position);

        bool TryFindNearWallFarthestFrom(
            Vector3 position,
            int attempts,
            out Vector3 spawnPosition);
    }
}
