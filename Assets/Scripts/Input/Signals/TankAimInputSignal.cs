using UnityEngine;

namespace GameInput.Signals
{
    public sealed class TankAimInputSignal
    {
        public TankAimInputSignal(Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
        }

        public Vector3 WorldPosition { get; }
    }
}
