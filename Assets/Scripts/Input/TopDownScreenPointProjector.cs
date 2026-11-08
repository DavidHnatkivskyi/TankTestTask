using UnityEngine;

namespace GameInput
{
    /// <summary>
    /// Projects mouse input to world-space points for the top-down camera.
    /// This is an input adapter; gameplay receives only world-space commands.
    /// </summary>
    public sealed class TopDownScreenPointProjector : IScreenPointProjector
    {
        private readonly Camera _camera;

        public TopDownScreenPointProjector(Camera camera)
        {
            _camera = camera;
        }

        public bool TryProject(Vector2 screenPosition, out Vector3 worldPosition)
        {
            if (_camera == null)
            {
                worldPosition = default;
                return false;
            }

            var ray = _camera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                worldPosition = hit.point;
                return true;
            }

            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out var distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }

            worldPosition = default;
            return false;
        }
    }
}
