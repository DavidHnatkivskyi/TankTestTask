using Tank;

namespace TankControl
{
    /// <summary>
    /// Lets a spawn or lifetime system assign the current user-controlled tank.
    /// The registry may have no target between destruction and respawn.
    /// </summary>
    public interface IUserTankControlTargetRegistry
    {
        void Register(ITankChassis chassis, ITurret turret);

        void Unregister(ITankChassis chassis);
    }
}
