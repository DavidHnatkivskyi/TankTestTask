using System;
using Config;
using DG.Tweening;
using GameInput;
using GameInput.Signals;
using GameSaving;
using GameUI.Signals;
using MapGeneration;
using Tank;
using UnityEngine;
using Zenject;

namespace CameraControl
{
    /// <summary>
    /// Follows the local player's spawned tank in either a fixed top-down view or
    /// a free-look third-person view. In third-person, the camera facing becomes
    /// the tank's world-space aim target. The top-down viewport stops at the
    /// generated map's Walls container only.
    /// </summary>
    public sealed class TankCameraController :
        IInitializable,
        IDisposable,
        ILateTickable,
        IPlayerTankCameraTargetRegistry,
        ICameraStatePersistence
    {

        private readonly Camera _camera;
        private readonly GameConfig _gameConfig;
        private readonly SignalBus _signalBus;
        private readonly GeneratedMap _map;

        private CameraTarget _target;
        private CameraViewMode _viewMode = CameraViewMode.TopDown;
        private Tween _viewTransitionTween;
        private float _viewTransitionProgress;
        private float _thirdPersonYaw;
        private float _thirdPersonPitch;
        private bool _hasThirdPersonOrientation;
        private bool _hasVisibleModalWindow;

        public TankCameraController(
            Camera camera,
            GameConfig gameConfig,
            SignalBus signalBus,
            GeneratedMap map)
        {
            _camera = camera;
            _gameConfig = gameConfig;
            _signalBus = signalBus;
            _map = map;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<CameraModeSwitchRequestedSignal>(OnCameraModeSwitchRequested);
            _signalBus.Subscribe<CameraViewModeRequestedSignal>(OnCameraViewModeRequested);
            _signalBus.Subscribe<CameraLookInputSignal>(OnCameraLookInput);
            _signalBus.Subscribe<ModalWindowVisibilityChangedSignal>(OnModalWindowVisibilityChanged);
        }

        public void Dispose()
        {
            _viewTransitionTween?.Kill();
            UnlockCursor();
            _signalBus.Unsubscribe<CameraModeSwitchRequestedSignal>(OnCameraModeSwitchRequested);
            _signalBus.Unsubscribe<CameraViewModeRequestedSignal>(OnCameraViewModeRequested);
            _signalBus.Unsubscribe<CameraLookInputSignal>(OnCameraLookInput);
            _signalBus.Unsubscribe<ModalWindowVisibilityChangedSignal>(OnModalWindowVisibilityChanged);
        }

        public void LateTick()
        {
            if (!TryGetTarget(out var chassisPosition, out var turretPosition, out var turretForward))
            {
                return;
            }

            UpdateCameraPose(chassisPosition, turretPosition, turretForward);
            if (_viewMode == CameraViewMode.ThirdPerson)
            {
                PublishThirdPersonAim(turretPosition);
            }
        }

        public void Register(ITankChassis chassis, ITurret turret)
        {
            _target = new CameraTarget(chassis, turret);
        }

        public void Unregister(ITankChassis chassis)
        {
            if (_target != null && ReferenceEquals(_target.Chassis, chassis))
            {
                _target = null;
                _viewTransitionTween?.Kill();
                _viewTransitionProgress = GetViewTransitionTarget();
                _hasThirdPersonOrientation = false;
            }
        }

        public CameraSaveData CaptureState()
        {
            return new CameraSaveData
            {
                viewMode = (int)_viewMode,
                thirdPersonYaw = _thirdPersonYaw,
                thirdPersonPitch = _thirdPersonPitch,
                hasThirdPersonOrientation = _hasThirdPersonOrientation
            };
        }

        public void RestoreState(CameraSaveData state)
        {
            if (state == null)
            {
                return;
            }

            _viewTransitionTween?.Kill();
            _viewMode = Enum.IsDefined(typeof(CameraViewMode), state.viewMode)
                ? (CameraViewMode)state.viewMode
                : CameraViewMode.TopDown;
            _thirdPersonYaw = state.thirdPersonYaw;
            _thirdPersonPitch = Mathf.Clamp(
                state.thirdPersonPitch,
                _gameConfig.ThirdPersonMinimumPitch,
                _gameConfig.ThirdPersonMaximumPitch);
            _hasThirdPersonOrientation = state.hasThirdPersonOrientation;
            _viewTransitionProgress = GetViewTransitionTarget();
            _signalBus.Fire(new CameraViewModeChangedSignal(_viewMode));
            UpdateCursorState();
        }

        private void OnCameraModeSwitchRequested(CameraModeSwitchRequestedSignal signal)
        {
            var requestedViewMode = _viewMode == CameraViewMode.TopDown
                ? CameraViewMode.ThirdPerson
                : CameraViewMode.TopDown;
            SetViewMode(requestedViewMode);
        }

        private void OnCameraViewModeRequested(CameraViewModeRequestedSignal signal)
        {
            SetViewMode(signal.ViewMode);
        }

        private void SetViewMode(CameraViewMode requestedViewMode)
        {
            if (_viewMode == requestedViewMode)
            {
                return;
            }

            _viewMode = requestedViewMode;
            _signalBus.Fire(new CameraViewModeChangedSignal(_viewMode));
            if (_viewMode == CameraViewMode.ThirdPerson &&
                TryGetTarget(out _, out _, out var turretForward))
            {
                InitializeThirdPersonOrientation(turretForward);
            }

            UpdateCursorState();
            StartViewTransition();
        }

        private void OnCameraLookInput(CameraLookInputSignal signal)
        {
            if (_viewMode != CameraViewMode.ThirdPerson)
            {
                return;
            }

            _thirdPersonYaw += signal.Delta.x * _gameConfig.ThirdPersonLookSensitivity;
            _thirdPersonPitch = Mathf.Clamp(
                _thirdPersonPitch - signal.Delta.y * _gameConfig.ThirdPersonLookSensitivity,
                _gameConfig.ThirdPersonMinimumPitch,
                _gameConfig.ThirdPersonMaximumPitch);
        }

        private void OnModalWindowVisibilityChanged(ModalWindowVisibilityChangedSignal signal)
        {
            _hasVisibleModalWindow = signal.IsVisible;
            UpdateCursorState();
        }

        private bool TryGetTarget(
            out Vector3 chassisPosition,
            out Vector3 turretPosition,
            out Vector3 turretForward)
        {
            if (_target == null || _target.Chassis == null || _target.Turret == null)
            {
                chassisPosition = default;
                turretPosition = default;
                turretForward = default;
                return false;
            }

            chassisPosition = _target.Chassis.Position;
            turretPosition = _target.Turret.Position;
            turretForward = _target.Turret.Forward;
            return true;
        }

        private void UpdateCameraPose(Vector3 chassisPosition, Vector3 turretPosition, Vector3 turretForward)
        {
            var topDownPosition = GetTopDownPosition(
                chassisPosition + _gameConfig.TopDownOffset,
                chassisPosition);
            var thirdPersonPosition = GetThirdPersonPosition(turretPosition, turretForward);
            var cameraPosition = Vector3.Lerp(
                topDownPosition,
                thirdPersonPosition,
                _viewTransitionProgress);
            var cameraRotation = _viewMode == CameraViewMode.ThirdPerson
                ? GetThirdPersonRotation()
                : Quaternion.LookRotation(Vector3.down, Vector3.forward);

            _camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }

        private Vector3 GetThirdPersonPosition(Vector3 turretPosition, Vector3 turretForward)
        {
            if (!_hasThirdPersonOrientation)
            {
                InitializeThirdPersonOrientation(turretForward);
            }

            var yawRotation = Quaternion.Euler(0f, _thirdPersonYaw, 0f);
            return turretPosition + yawRotation * _gameConfig.ThirdPersonOffset;
        }

        private Quaternion GetThirdPersonRotation()
        {
            return Quaternion.Euler(_thirdPersonPitch, _thirdPersonYaw, 0f);
        }

        private void InitializeThirdPersonOrientation(Vector3 turretForward)
        {
            _thirdPersonYaw = Quaternion.LookRotation(turretForward, Vector3.up).eulerAngles.y;
            _thirdPersonPitch = _gameConfig.ThirdPersonDefaultPitch;
            _hasThirdPersonOrientation = true;
        }

        private void PublishThirdPersonAim(Vector3 turretPosition)
        {
            var aimDirection = _camera.transform.forward;
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            aimDirection.Normalize();
            var aimPosition = turretPosition + aimDirection * _gameConfig.ThirdPersonAimDistance;
            _signalBus.Fire(new TankAimInputSignal(aimPosition));
        }

        private sealed class CameraTarget
        {
            public CameraTarget(ITankChassis chassis, ITurret turret)
            {
                Chassis = chassis;
                Turret = turret;
            }

            public ITankChassis Chassis { get; }
            public ITurret Turret { get; }
        }

        private void UpdateCursorState()
        {
            if (_viewMode == CameraViewMode.ThirdPerson && !_hasVisibleModalWindow)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                return;
            }

            UnlockCursor();
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void StartViewTransition()
        {
            var transitionTarget = GetViewTransitionTarget();
            var duration = _gameConfig.CameraMovementTime;
            if (_viewTransitionTween != null && _viewTransitionTween.IsActive())
            {
                var remainingTime = _viewTransitionTween.Duration(false) - _viewTransitionTween.Elapsed(false);
                duration = Mathf.Max(0f, _gameConfig.CameraMovementTime - remainingTime);
            }

            _viewTransitionTween?.Kill();
            if (duration <= 0f)
            {
                _viewTransitionProgress = transitionTarget;
                return;
            }

            _viewTransitionTween = DOTween.To(
                    () => _viewTransitionProgress,
                    value => _viewTransitionProgress = value,
                    transitionTarget,
                    duration)
                .SetEase(Ease.Linear);
        }

        private float GetViewTransitionTarget()
        {
            return _viewMode == CameraViewMode.ThirdPerson ? 1f : 0f;
        }

        private Vector3 GetTopDownPosition(Vector3 desiredPosition, Vector3 targetPosition)
        {
            if (!TryGetMapBounds(targetPosition, out var mapBounds))
            {
                return desiredPosition;
            }

            var cameraHeight = Mathf.Abs(desiredPosition.y - targetPosition.y);
            var halfExtents = GetViewportHalfExtents(cameraHeight);
            desiredPosition.x = ClampToRange(
                desiredPosition.x,
                mapBounds.min.x + halfExtents.x + _gameConfig.TopDownWallClearance,
                mapBounds.max.x - halfExtents.x - _gameConfig.TopDownWallClearance);
            desiredPosition.z = ClampToRange(
                desiredPosition.z,
                mapBounds.min.z + halfExtents.z + _gameConfig.TopDownWallClearance,
                mapBounds.max.z - halfExtents.z - _gameConfig.TopDownWallClearance);
            return desiredPosition;
        }

        private bool TryGetMapBounds(Vector3 targetPosition, out Bounds bounds)
        {
            if (_map != null && _map.TryGetCameraViewportBounds(out bounds) &&
                targetPosition.x >= bounds.min.x && targetPosition.x <= bounds.max.x &&
                targetPosition.z >= bounds.min.z && targetPosition.z <= bounds.max.z)
            {
                return true;
            }

            bounds = default;
            return false;
        }

        private Vector3 GetViewportHalfExtents(float cameraHeight)
        {
            var height = _camera.orthographic
                ? _camera.orthographicSize * 2f
                : cameraHeight * 2f * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var width = height * _camera.aspect;
            return new Vector3(width * 0.5f, 0f, height * 0.5f);
        }

        private static float ClampToRange(float value, float minimum, float maximum)
        {
            if (minimum <= maximum)
            {
                return Mathf.Clamp(value, minimum, maximum);
            }

            return (minimum + maximum) * 0.5f;
        }
    }
}
