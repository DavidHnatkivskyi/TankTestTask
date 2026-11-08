using UnityEngine;

namespace TankSpawning
{
    internal interface ITankRuntimeCommands
    {
        TankSpawnHandle SpawnExact(
            TankSpawnData spawnData,
            Vector3 position,
            Quaternion rotation);

        void DespawnAll(TankControlType controlType);
    }
}
