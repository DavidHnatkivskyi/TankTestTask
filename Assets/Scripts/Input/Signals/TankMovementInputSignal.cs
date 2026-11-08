using UnityEngine;

namespace GameInput.Signals
{
    public sealed class TankMovementInputSignal
    {
        public TankMovementInputSignal(Vector2 movement)
        {
            Movement = movement;
        }

        /// <summary>X is turn input; Y is forward/reverse input. Both are in [-1, 1].</summary>
        public Vector2 Movement { get; }
    }
}
