using TankControl;
using UnityEngine;

namespace TankSpawning
{
    public interface ITankSpawner
    {
        TankSpawnHandle Spawn(TankControlType controlType, Vector3 position, Quaternion rotation);

        TankSpawnHandle Spawn(
            TankControlType controlType,
            ComputerTankBehavior computerBehavior,
            Vector3 position,
            Quaternion rotation);

        bool Despawn(TankSpawnHandle handle);

        void DespawnAll(TankControlType controlType);
    }
}
