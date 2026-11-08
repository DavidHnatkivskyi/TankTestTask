using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// Identifies a map hierarchy created by the map generator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedMap : MonoBehaviour
    {
        private const string WallsContainerName = "Walls";

        private Transform _wallsRoot;
        private bool _hasFoundWallsRoot;
        private Bounds _cachedViewportBounds;
        private bool _hasCachedViewportBounds;

        /// <summary>
        /// Returns the world-space area enclosed by the outer faces of the generated
        /// perimeter walls. Other generated elements, including obstacles, are
        /// deliberately excluded.
        /// </summary>
        public bool TryGetCameraViewportBounds(out Bounds bounds)
        {
            if (!_hasCachedViewportBounds)
            {
                if (!TryCalculateCameraViewportBounds(out _cachedViewportBounds))
                {
                    bounds = default;
                    return false;
                }

                _hasCachedViewportBounds = true;
            }

            bounds = _cachedViewportBounds;
            return true;
        }

        private bool TryCalculateCameraViewportBounds(out Bounds bounds)
        {
            var wallsRoot = GetWallsRoot();
            if (wallsRoot == null)
            {
                bounds = default;
                return false;
            }

            var renderers = wallsRoot.GetComponentsInChildren<Renderer>();
            var mapCenter = transform.position;
            var hasMinimumX = false;
            var hasMaximumX = false;
            var hasMinimumZ = false;
            var hasMaximumZ = false;
            var minimumX = 0f;
            var maximumX = 0f;
            var minimumZ = 0f;
            var maximumZ = 0f;

            foreach (var renderer in renderers)
            {
                var wallBounds = renderer.bounds;
                if (wallBounds.size.x < wallBounds.size.z)
                {
                    if (wallBounds.center.x < mapCenter.x)
                    {
                        minimumX = GetLesserLimit(minimumX, wallBounds.min.x, hasMinimumX);
                        hasMinimumX = true;
                    }
                    else
                    {
                        maximumX = GetGreaterLimit(maximumX, wallBounds.max.x, hasMaximumX);
                        hasMaximumX = true;
                    }

                    continue;
                }

                if (wallBounds.size.z < wallBounds.size.x)
                {
                    if (wallBounds.center.z < mapCenter.z)
                    {
                        minimumZ = GetLesserLimit(minimumZ, wallBounds.min.z, hasMinimumZ);
                        hasMinimumZ = true;
                    }
                    else
                    {
                        maximumZ = GetGreaterLimit(maximumZ, wallBounds.max.z, hasMaximumZ);
                        hasMaximumZ = true;
                    }
                }
            }

            if (!hasMinimumX || !hasMaximumX || !hasMinimumZ || !hasMaximumZ ||
                minimumX >= maximumX || minimumZ >= maximumZ)
            {
                bounds = default;
                return false;
            }

            bounds = new Bounds();
            bounds.SetMinMax(
                new Vector3(minimumX, 0f, minimumZ),
                new Vector3(maximumX, 0f, maximumZ));
            return true;
        }

        private Transform GetWallsRoot()
        {
            if (!_hasFoundWallsRoot)
            {
                _wallsRoot = transform.Find(WallsContainerName);
                _hasFoundWallsRoot = true;
            }

            return _wallsRoot;
        }

        private static float GetGreaterLimit(float current, float candidate, bool hasCurrent)
        {
            return hasCurrent ? Mathf.Max(current, candidate) : candidate;
        }

        private static float GetLesserLimit(float current, float candidate, bool hasCurrent)
        {
            return hasCurrent ? Mathf.Min(current, candidate) : candidate;
        }
    }
}
