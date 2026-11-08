using UnityEngine;

namespace Tank
{
    /// <summary>
    /// Navigation contract used exclusively by a computer-controlled tank.
    /// </summary>
    public interface IComputerTankNavigation
    {
        bool HasReachedDestination(float arrivalDistance);

        bool TrySetDestination(Vector3 destination);

        bool TryGetDestination(out Vector3 destination);

        void SetPositionAndRotation(Vector3 position, Quaternion rotation);

        void Stop();
    }
}
