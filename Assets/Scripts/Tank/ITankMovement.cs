using UnityEngine;

namespace Tank
{
    public interface ITankMovement : IPositionProvider
    {
        Vector3 Forward { get; }

        void Move(float forwardInput, float turnInput, float deltaTime);
        void Stop();
        Vector3 GetMovementDelta(float forwardInput, float turnInput, float deltaTime);
    }
}
