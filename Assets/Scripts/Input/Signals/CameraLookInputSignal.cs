using UnityEngine;

namespace GameInput.Signals
{
    public sealed class CameraLookInputSignal
    {
        public CameraLookInputSignal(Vector2 delta)
        {
            Delta = delta;
        }

        public Vector2 Delta { get; }
    }
}
