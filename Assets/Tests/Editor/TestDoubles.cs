using System.Collections.Generic;
using CameraControl;
using GameInput;
using GamePause;
using GameSaving;
using MapGeneration;
using Projectile;
using Tank;
using TankControl;
using UnityEngine;

namespace Tanks.Tests
{
    internal sealed class FakeTankInputSource : ITankInputSource
    {
        public TankInputState State { get; set; }
        public bool PauseRequested { get; set; }
        public bool HasViewModeRequest { get; set; }
        public CameraViewMode RequestedViewMode { get; set; }
        public bool HasLookDelta { get; set; }
        public Vector2 LookDelta { get; set; }

        public TankInputState ReadState()
        {
            return State;
        }

        public bool TryGetCameraViewModeRequest(out CameraViewMode viewMode)
        {
            viewMode = RequestedViewMode;
            return HasViewModeRequest;
        }

        public bool TryGetCameraLookDelta(out Vector2 lookDelta)
        {
            lookDelta = LookDelta;
            return HasLookDelta;
        }
    }

    internal sealed class FakeScreenPointProjector : IScreenPointProjector
    {
        public bool CanProject { get; set; }
        public Vector3 ProjectedPosition { get; set; }

        public bool TryProject(Vector2 screenPosition, out Vector3 worldPosition)
        {
            worldPosition = ProjectedPosition;
            return CanProject;
        }
    }

    internal sealed class FakeGamePauseService : IGamePauseService
    {
        public bool IsPaused { get; set; }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }
    }

    internal sealed class FakeTankChassis : ITankChassis
    {
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; } = Vector3.forward;
        public int MoveCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        public float LastForwardInput { get; private set; }
        public float LastTurnInput { get; private set; }

        public void Move(float forwardInput, float turnInput, float deltaTime)
        {
            MoveCallCount++;
            LastForwardInput = forwardInput;
            LastTurnInput = turnInput;
        }

        public void Stop()
        {
            StopCallCount++;
        }

        public Vector3 GetMovementDelta(float forwardInput, float turnInput, float deltaTime)
        {
            return Vector3.zero;
        }
    }

    internal sealed class FakeTurret : ITurret
    {
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; set; } = Vector3.forward;
        public bool CanFire { get; set; } = true;
        public int FireAttemptCount { get; private set; }
        public int RotateCallCount { get; private set; }
        public Vector3 LastAimPosition { get; private set; }

        public void SetRotation(Quaternion rotation)
        {
            Forward = rotation * Vector3.forward;
        }

        public void RotateTowards(Vector3 worldPosition, float deltaTime)
        {
            RotateCallCount++;
            LastAimPosition = worldPosition;
        }

        public bool TryFire()
        {
            FireAttemptCount++;
            return CanFire;
        }
    }

    internal sealed class FakeMovementConstraint : ITankMovementConstraint
    {
        public bool AllowsMovement { get; set; } = true;

        public bool CanMove(ITankChassis chassis, Vector2 movement, float deltaTime)
        {
            return AllowsMovement;
        }
    }

    internal sealed class RecordingUserTankRegistry : IUserTankControlTargetRegistry
    {
        public List<ITankChassis> Registered { get; } = new();
        public List<ITankChassis> Unregistered { get; } = new();

        public void Register(ITankChassis chassis, ITurret turret)
        {
            Registered.Add(chassis);
        }

        public void Unregister(ITankChassis chassis)
        {
            Unregistered.Add(chassis);
        }
    }

    internal sealed class RecordingComputerTankRegistry : IComputerTankControlTargetRegistry
    {
        public List<IPositionProvider> PlayerTargets { get; } = new();
        public List<IPositionProvider> ClearedPlayerTargets { get; } = new();
        public List<ComputerRegistration> Registered { get; } = new();
        public List<ITankChassis> Unregistered { get; } = new();

        public void SetPlayerTarget(IPositionProvider target)
        {
            PlayerTargets.Add(target);
        }

        public void ClearPlayerTarget(IPositionProvider target)
        {
            ClearedPlayerTargets.Add(target);
        }

        public void RegisterComputer(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankBehavior behavior)
        {
            Registered.Add(new ComputerRegistration(chassis, turret, navigation, behavior));
        }

        public void UnregisterComputer(ITankChassis chassis)
        {
            Unregistered.Add(chassis);
        }
    }

    internal readonly struct ComputerRegistration
    {
        public ComputerRegistration(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankBehavior behavior)
        {
            Chassis = chassis;
            Turret = turret;
            Navigation = navigation;
            Behavior = behavior;
        }

        public ITankChassis Chassis { get; }
        public ITurret Turret { get; }
        public IComputerTankNavigation Navigation { get; }
        public ComputerTankBehavior Behavior { get; }
    }

    internal sealed class RecordingCameraTargetRegistry : IPlayerTankCameraTargetRegistry
    {
        public List<ITankChassis> Registered { get; } = new();
        public List<ITankChassis> Unregistered { get; } = new();

        public void Register(ITankChassis chassis, ITurret turret)
        {
            Registered.Add(chassis);
        }

        public void Unregister(ITankChassis chassis)
        {
            Unregistered.Add(chassis);
        }
    }

    internal sealed class FakeSpawnPointFinder : INavMeshSpawnPointFinder
    {
        public bool TryFind(Vector3 center, float radius, int attempts, out Vector3 position)
        {
            position = center;
            return true;
        }

        public bool TryFindClosestToMapCenter(out Vector3 position)
        {
            position = Vector3.zero;
            return true;
        }

        public bool TryFindNearWallFarthestFrom(Vector3 position, int attempts, out Vector3 spawnPosition)
        {
            spawnPosition = position + Vector3.right;
            return true;
        }

        public bool TryProject(Vector3 point, float maximumDistance, out Vector3 position)
        {
            position = point;
            return true;
        }
    }

    internal sealed class RecordingMovementRestorer : IComputerTankMovementRestorer
    {
        public List<MovementRestoreRequest> Requests { get; } = new();

        public bool TryRestoreMovementTarget(ITankChassis chassis, Vector3 destination)
        {
            Requests.Add(new MovementRestoreRequest(chassis, destination));
            return true;
        }
    }

    internal readonly struct MovementRestoreRequest
    {
        public MovementRestoreRequest(ITankChassis chassis, Vector3 destination)
        {
            Chassis = chassis;
            Destination = destination;
        }

        public ITankChassis Chassis { get; }
        public Vector3 Destination { get; }
    }

    internal sealed class FakeProjectileEmitter : IProjectileEmitter
    {
        public List<ProjectileShot> Shots { get; } = new();

        public void Emit(ProjectileShot shot)
        {
            Shots.Add(shot);
        }
    }

    internal sealed class FakeTankStatePersistence : ITankStatePersistence
    {
        public TankRuntimeSaveState CapturedState { get; set; }
        public bool CanCapture { get; set; } = true;
        public bool CanRestore { get; set; } = true;
        public List<TankRuntimeSaveState> RestoredStates { get; } = new();

        public bool TryCaptureState(out TankRuntimeSaveState state)
        {
            state = CapturedState;
            return CanCapture;
        }

        public bool TryRestoreState(TankRuntimeSaveState state)
        {
            RestoredStates.Add(state);
            return CanRestore;
        }
    }

    internal sealed class FakeCameraStatePersistence : ICameraStatePersistence
    {
        public CameraSaveData CapturedState { get; set; }
        public List<CameraSaveData> RestoredStates { get; } = new();

        public CameraSaveData CaptureState()
        {
            return CapturedState;
        }

        public void RestoreState(CameraSaveData state)
        {
            RestoredStates.Add(state);
        }
    }
}
