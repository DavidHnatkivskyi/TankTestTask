using System.Collections.Generic;
using TankControl;
using UnityEngine;

namespace TankSpawning
{
    [CreateAssetMenu(fileName = "TankSpawningConfig", menuName = "Config/Tank Spawning")]
    public sealed class TankSpawningConfig : ScriptableObject, IComputerTankSettings
    {
        [SerializeField] private List<TankSpawnData> tankSpawnData = new();
        [SerializeField] private List<ComputerTankBehavior> initialComputerTankBehaviors = new();
        [SerializeField, Min(0f)] private float computerRespawnDelay = 2f;
        [SerializeField, Min(0f)] private float computerPatrolRadius = 8f;
        [SerializeField, Min(1)] private int navMeshPointSearchAttempts = 16;
        [SerializeField, Min(0f)] private float computerOrbitMinimumRadius = 4f;
        [SerializeField, Min(0f)] private float computerOrbitMaximumRadius = 8f;
        [SerializeField, Min(3)] private int computerOrbitPointCount = 12;
        [SerializeField, Min(0f)] private float computerOrbitPointArrivalDistance = 2f;
        [SerializeField, Min(0f)] private float computerFireMinimumDistance = 4f;
        [SerializeField, Min(0f)] private float computerFireMaximumDistance = 6f;
        [SerializeField, Min(0f)] private float computerFireAlignmentAngle = 5f;
        [SerializeField, Min(0f)] private float computerWaypointArrivalDistance = 0.4f;
        [SerializeField, Min(0f)] private float computerPathRetryDelay = 0.5f;
        [SerializeField, Min(0f)] private float computerPlayerPathRefreshDuration = 0.5f;
        [SerializeField, Min(0f)] private float computerOrbitPointProjectionDistance = 1f;

        public IReadOnlyList<TankSpawnData> TankSpawnData => tankSpawnData;
        public IReadOnlyList<ComputerTankBehavior> InitialComputerTankBehaviors => initialComputerTankBehaviors;
        public float ComputerRespawnDelay => computerRespawnDelay;
        public float ComputerPatrolRadius => computerPatrolRadius;
        public int NavMeshPointSearchAttempts => navMeshPointSearchAttempts;
        public float ComputerOrbitMinimumRadius => computerOrbitMinimumRadius;
        public float ComputerOrbitMaximumRadius => computerOrbitMaximumRadius;
        public int ComputerOrbitPointCount => computerOrbitPointCount;
        public float ComputerOrbitPointArrivalDistance => computerOrbitPointArrivalDistance;
        public float ComputerFireMinimumDistance => computerFireMinimumDistance;
        public float ComputerFireMaximumDistance => computerFireMaximumDistance;
        public float ComputerFireAlignmentAngle => computerFireAlignmentAngle;
        public float ComputerWaypointArrivalDistance => computerWaypointArrivalDistance;
        public float ComputerPathRetryDelay => computerPathRetryDelay;
        public float ComputerPlayerPathRefreshDuration => computerPlayerPathRefreshDuration;
        public float ComputerOrbitPointProjectionDistance => computerOrbitPointProjectionDistance;
    }
}
