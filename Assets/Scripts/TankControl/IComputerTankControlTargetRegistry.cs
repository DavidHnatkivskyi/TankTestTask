using Tank;

namespace TankControl
{
    /// <summary>
    /// Lets the spawning system assign and remove computer-controlled tanks.
    /// </summary>
    public interface IComputerTankControlTargetRegistry
    {
        void SetPlayerTarget(IPositionProvider target);

        void ClearPlayerTarget(IPositionProvider target);

        void RegisterComputer(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankBehavior behavior);

        void UnregisterComputer(ITankChassis chassis);
    }
}
