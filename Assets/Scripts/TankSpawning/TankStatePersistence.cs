using System.Collections.Generic;
using GameSaving;
using TankControl;
using UnityEngine;

namespace TankSpawning
{
    internal sealed class TankStatePersistence : ITankStatePersistence
    {
        private readonly TankRuntimeRegistry _runtimeRegistry;
        private readonly ITankRuntimeCommands _runtimeCommands;
        private readonly IComputerTankMovementRestorer _movementRestorer;
        private readonly TankSpawningConfig _config;

        public TankStatePersistence(
            TankRuntimeRegistry runtimeRegistry,
            ITankRuntimeCommands runtimeCommands,
            IComputerTankMovementRestorer movementRestorer,
            TankSpawningConfig config)
        {
            _runtimeRegistry = runtimeRegistry;
            _runtimeCommands = runtimeCommands;
            _movementRestorer = movementRestorer;
            _config = config;
        }

        public bool TryCaptureState(out TankRuntimeSaveState state)
        {
            state = new TankRuntimeSaveState();

            foreach (var handle in _runtimeRegistry.ActiveHandles)
            {
                if (string.IsNullOrWhiteSpace(handle.SpawnData.SaveId))
                {
                    Debug.LogError($"Tank spawn data for '{handle.SpawnData.Prefab.name}' has no save ID.");
                    state = null;
                    return false;
                }

                switch (handle.ControlType)
                {
                    case TankControlType.LocalPlayer:
                        if (state.playerTank != null)
                        {
                            Debug.LogError("Only one local player tank can be saved.");
                            state = null;
                            return false;
                        }

                        state.playerTank = CreateTankSaveData(handle);
                        break;
                    case TankControlType.Computer:
                        state.computerTanks.Add(CreateComputerTankSaveData(handle));
                        break;
                    default:
                        Debug.LogError($"Cannot save tank control type '{handle.ControlType}'.");
                        state = null;
                        return false;
                }
            }

            return true;
        }

        public bool TryRestoreState(TankRuntimeSaveState state)
        {
            if (!TryValidateState(state, out var playerSpawnData, out var computerTanks))
            {
                return false;
            }

            _runtimeCommands.DespawnAll(TankControlType.LocalPlayer);
            _runtimeCommands.DespawnAll(TankControlType.Computer);

            if (state.playerTank != null)
            {
                var playerHandle = _runtimeCommands.SpawnExact(
                    playerSpawnData,
                    state.playerTank.position,
                    state.playerTank.chassisRotation);
                playerHandle.Turret.SetRotation(state.playerTank.turretRotation);
            }

            foreach (var computerTank in computerTanks)
            {
                var computerHandle = _runtimeCommands.SpawnExact(
                    computerTank.SpawnData,
                    computerTank.State.position,
                    computerTank.State.chassisRotation);
                computerHandle.Turret.SetRotation(computerTank.State.turretRotation);

                if (computerTank.State.hasMovementTarget &&
                    !_movementRestorer.TryRestoreMovementTarget(
                        computerHandle.Chassis,
                        computerTank.State.movementTarget))
                {
                    Debug.LogWarning(
                        $"Could not restore movement target for tank '{computerTank.SpawnData.SaveId}'.");
                }
            }

            return true;
        }

        private bool TryValidateState(
            TankRuntimeSaveState state,
            out TankSpawnData playerSpawnData,
            out List<RestoredComputerTank> computerTanks)
        {
            playerSpawnData = null;
            computerTanks = new List<RestoredComputerTank>();

            if (state == null)
            {
                Debug.LogError("Cannot restore an empty tank save state.");
                return false;
            }

            if (state.playerTank != null &&
                !TryGetSpawnData(state.playerTank.tankSaveId, TankControlType.LocalPlayer, out playerSpawnData))
            {
                return false;
            }

            if (state.computerTanks == null)
            {
                return true;
            }

            foreach (var savedComputerTank in state.computerTanks)
            {
                if (savedComputerTank == null ||
                    !TryGetSpawnData(
                        savedComputerTank.tankSaveId,
                        TankControlType.Computer,
                        out var spawnData))
                {
                    return false;
                }

                computerTanks.Add(new RestoredComputerTank(spawnData, savedComputerTank));
            }

            return true;
        }

        private bool TryGetSpawnData(
            string saveId,
            TankControlType expectedControlType,
            out TankSpawnData spawnData)
        {
            spawnData = null;
            if (string.IsNullOrWhiteSpace(saveId))
            {
                Debug.LogError("A saved tank has no save ID.");
                return false;
            }

            foreach (var candidate in _config.TankSpawnData)
            {
                if (candidate == null || candidate.SaveId != saveId)
                {
                    continue;
                }

                if (candidate.ControlType != expectedControlType)
                {
                    Debug.LogError(
                        $"Tank save ID '{saveId}' does not belong to '{expectedControlType}'.");
                    return false;
                }

                if (spawnData != null)
                {
                    Debug.LogError($"Tank save ID '{saveId}' is configured more than once.");
                    return false;
                }

                spawnData = candidate;
            }

            if (spawnData != null)
            {
                return true;
            }

            Debug.LogError($"No configured tank matches save ID '{saveId}'.");
            return false;
        }

        private static TankSaveData CreateTankSaveData(TankSpawnHandle handle)
        {
            return new TankSaveData
            {
                tankSaveId = handle.SpawnData.SaveId,
                position = handle.Chassis.Position,
                chassisRotation = GetHorizontalRotation(handle.Chassis.Forward),
                turretRotation = GetHorizontalRotation(handle.Turret.Forward)
            };
        }

        private static ComputerTankSaveData CreateComputerTankSaveData(TankSpawnHandle handle)
        {
            var tankState = CreateTankSaveData(handle);
            var hasMovementTarget = handle.ComputerNavigation.TryGetDestination(out var movementTarget);
            return new ComputerTankSaveData
            {
                tankSaveId = tankState.tankSaveId,
                position = tankState.position,
                chassisRotation = tankState.chassisRotation,
                turretRotation = tankState.turretRotation,
                hasMovementTarget = hasMovementTarget,
                movementTarget = movementTarget
            };
        }

        private static Quaternion GetHorizontalRotation(Vector3 forward)
        {
            forward.y = 0f;
            return forward.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(forward, Vector3.up)
                : Quaternion.identity;
        }

        private sealed class RestoredComputerTank
        {
            public RestoredComputerTank(TankSpawnData spawnData, ComputerTankSaveData state)
            {
                SpawnData = spawnData;
                State = state;
            }

            public TankSpawnData SpawnData { get; }

            public ComputerTankSaveData State { get; }
        }
    }
}
