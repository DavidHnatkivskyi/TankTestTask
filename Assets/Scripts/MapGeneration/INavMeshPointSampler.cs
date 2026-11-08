using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Samples valid points on the NavMesh.
    /// </summary>
    public interface INavMeshPointSampler
    {
        bool TryFind(Vector3 center, float radius, int attempts, out Vector3 position);

        bool TryProject(Vector3 point, float maximumDistance, out Vector3 position);
    }
}
