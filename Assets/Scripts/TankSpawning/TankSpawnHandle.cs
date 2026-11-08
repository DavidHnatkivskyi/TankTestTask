using Tank;

namespace TankSpawning
{
    /// <summary>
    /// Describes one tank currently checked out from <see cref="TankSpawner"/>.
    /// Keep this handle to return the tank to the pool.
    /// </summary>
    public sealed class TankSpawnHandle
    {
        internal TankSpawnHandle(
            TankSpawnData spawnData,
            TankInstance instance)
        {
            SpawnData = spawnData;
            Instance = instance;
        }

        internal TankSpawnData SpawnData { get; }
        internal TankInstance Instance { get; }
        internal ITankChassis Chassis => Instance.Chassis;
        internal ITurret Turret => Instance.Turret;
        internal IComputerTankNavigation ComputerNavigation => Instance.ComputerNavigation;
        internal TankControlType ControlType => SpawnData.ControlType;
    }
}
