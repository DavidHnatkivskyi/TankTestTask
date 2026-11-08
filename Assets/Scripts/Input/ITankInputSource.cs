using UnityEngine;

namespace GameInput
{
    /// <summary>
    /// Produces one frame of device-agnostic tank input.
    /// Implement this interface for another input scheme and bind it in <see cref="TankInputBindings"/>.
    /// </summary>
    public interface ITankInputSource
    {
        TankInputState ReadState();

        bool PauseRequested { get; }

        bool TryGetCameraViewModeRequest(out CameraViewMode viewMode);

        bool TryGetCameraLookDelta(out Vector2 lookDelta);
    }
}
