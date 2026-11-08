using Tank;

namespace CameraControl
{
    /// <summary>
    /// Lets the tank lifetime owner assign the local player's camera target.
    /// </summary>
    public interface IPlayerTankCameraTargetRegistry
    {
        void Register(ITankChassis chassis, ITurret turret);

        void Unregister(ITankChassis chassis);
    }
}
