using UnityEngine;

namespace GameInput
{
    /// <summary>
    /// Input collected during one frame. Aim is a screen-space point, while firing is a
    /// mode-independent button command.
    /// </summary>
    public readonly struct TankInputState
    {
        public TankInputState(
            Vector2 movement,
            Vector2 aimScreenPosition,
            bool hasAimScreenPosition,
            bool fireRequested)
        {
            Movement = movement;
            AimScreenPosition = aimScreenPosition;
            HasAimScreenPosition = hasAimScreenPosition;
            FireRequested = fireRequested;
        }

        public Vector2 Movement { get; }

        public Vector2 AimScreenPosition { get; }

        public bool HasAimScreenPosition { get; }

        public bool FireRequested { get; }
    }
}
