using Tank;
using UnityEngine;

namespace TankControl
{
    /// <summary>
    /// Decides whether the chassis may translate for the current movement command.
    /// Rotation remains available when translation is blocked.
    /// </summary>
    public interface ITankMovementConstraint
    {
        bool CanMove(ITankChassis chassis, Vector2 movement, float deltaTime);
    }
}
