using UnityEngine;

namespace GameInput
{
    /// <summary>
    /// Converts a pointer position from a screen-based input device to world space.
    /// </summary>
    public interface IScreenPointProjector
    {
        bool TryProject(Vector2 screenPosition, out Vector3 worldPosition);
    }
}
