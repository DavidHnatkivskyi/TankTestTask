using UnityEngine;
using UnityEngine.AI;

namespace MapGeneration
{
    /// <summary>
    /// Finds NavMesh positions for local and computer tank spawning.
    /// </summary>
    public sealed class NavMeshSpawnPointFinder : INavMeshSpawnPointFinder
    {
        private const float ProjectionDistance = 1f;
        private readonly GeneratedMap _map;

        public NavMeshSpawnPointFinder(GeneratedMap map)
        {
            _map = map;
        }

        public bool TryFind(Vector3 center, float radius, int attempts, out Vector3 position)
        {
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var offset = Random.insideUnitCircle * radius;
                var candidate = center + new Vector3(offset.x, 0f, offset.y);
                if (!TryProject(candidate, ProjectionDistance, out var projectedPosition))
                {
                    continue;
                }

                var horizontalOffset = projectedPosition - center;
                horizontalOffset.y = 0f;
                if (horizontalOffset.sqrMagnitude <= radius * radius)
                {
                    position = projectedPosition;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public bool TryFindClosestToMapCenter(out Vector3 position)
        {
            if (!_map.TryGetCameraViewportBounds(out var bounds))
            {
                position = default;
                return false;
            }

            return TryProject(bounds.center, GetMapProjectionDistance(bounds), out position);
        }

        public bool TryFindNearWallFarthestFrom(
            Vector3 playerPosition,
            int attempts,
            out Vector3 spawnPosition)
        {
            spawnPosition = default;
            if (!_map.TryGetCameraViewportBounds(out var bounds) || attempts <= 0)
            {
                return false;
            }

            var hasCandidate = false;
            var greatestDistanceSquared = 0f;
            var projectionDistance = GetMapProjectionDistance(bounds);

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                if (!TryProject(GetRandomWallPoint(bounds), projectionDistance, out var candidate))
                {
                    continue;
                }

                var offset = candidate - playerPosition;
                offset.y = 0f;
                var distanceSquared = offset.sqrMagnitude;
                if (!hasCandidate || distanceSquared > greatestDistanceSquared)
                {
                    spawnPosition = candidate;
                    greatestDistanceSquared = distanceSquared;
                    hasCandidate = true;
                }
            }

            if (hasCandidate)
            {
                return true;
            }

            return false;
        }

        public bool TryProject(Vector3 point, float maximumDistance, out Vector3 position)
        {
            if (NavMesh.SamplePosition(point, out var hit, maximumDistance, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }

            position = default;
            return false;
        }

        private static float GetMapProjectionDistance(Bounds bounds)
        {
            return Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        private static Vector3 GetRandomWallPoint(Bounds bounds)
        {
            return Random.Range(0, 4) switch
            {
                0 => new Vector3(bounds.min.x, bounds.center.y, Random.Range(bounds.min.z, bounds.max.z)),
                1 => new Vector3(bounds.max.x, bounds.center.y, Random.Range(bounds.min.z, bounds.max.z)),
                2 => new Vector3(Random.Range(bounds.min.x, bounds.max.x), bounds.center.y, bounds.min.z),
                _ => new Vector3(Random.Range(bounds.min.x, bounds.max.x), bounds.center.y, bounds.max.z)
            };
        }
    }
}
