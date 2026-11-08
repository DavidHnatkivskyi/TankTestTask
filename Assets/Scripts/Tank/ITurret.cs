using UnityEngine;

namespace Tank
{
    public interface ITurret
        : IPositionProvider
    {
        Vector3 Forward { get; }

        void SetRotation(Quaternion rotation);

        void RotateTowards(Vector3 worldPosition, float deltaTime);
        bool TryFire();
    }
}
