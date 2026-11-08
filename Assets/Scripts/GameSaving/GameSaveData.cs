using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSaving
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version;
        public CameraSaveData camera;
        public TankRuntimeSaveState tanks;
    }

    [Serializable]
    public sealed class CameraSaveData
    {
        public int viewMode;
        public float thirdPersonYaw;
        public float thirdPersonPitch;
        public bool hasThirdPersonOrientation;
    }

    [Serializable]
    public sealed class TankRuntimeSaveState
    {
        public TankSaveData playerTank;
        public List<ComputerTankSaveData> computerTanks = new();
    }

    [Serializable]
    public class TankSaveData
    {
        public string tankSaveId;
        public Vector3 position;
        public Quaternion chassisRotation;
        public Quaternion turretRotation;
    }

    [Serializable]
    public sealed class ComputerTankSaveData : TankSaveData
    {
        public bool hasMovementTarget;
        public Vector3 movementTarget;
    }
}
