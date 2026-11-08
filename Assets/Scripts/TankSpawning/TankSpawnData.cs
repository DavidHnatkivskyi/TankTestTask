using System;
using Config;
using TankControl;
using UnityEngine;

namespace TankSpawning
{
    [Serializable]
    public sealed class TankSpawnData
    {
        [SerializeField] private string saveId;
        [SerializeField] private GameObject prefab;
        [SerializeField] private TankControlType controlType;
        [SerializeField, Min(0f)] private float spawnChance = 1f;
        [SerializeField] private TankConfig tankConfig;
        [SerializeField] private ProjectileSetConfig projectileSet;
        [SerializeField] private ComputerTankBehavior computerBehavior;

        public string SaveId => saveId;
        public GameObject Prefab => prefab;
        public TankControlType ControlType => controlType;
        public float SpawnChance => spawnChance;
        public TankConfig TankConfig => tankConfig;
        public ProjectileSetConfig ProjectileSet => projectileSet;
        public ComputerTankBehavior ComputerBehavior => computerBehavior;
    }
}
