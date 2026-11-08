using Tank;
using UnityEngine;

namespace TankControl
{
    /// <summary>
    /// Restores a saved movement destination for a registered computer tank.
    /// </summary>
    public interface IComputerTankMovementRestorer
    {
        bool TryRestoreMovementTarget(ITankChassis chassis, Vector3 destination);
    }
}
