using System;
using GamePause;
using GamePause.Signals;
using GameInput.Signals;
using UnityEngine;
using Zenject;

namespace GameInput
{
    /// <summary>
    /// Converts the active input source into gameplay-neutral SignalBus messages.
    /// </summary>
    public sealed class TankInputHandler : ITickable, IDisposable
    {
        private readonly ITankInputSource _inputSource;
        private readonly IScreenPointProjector _screenPointProjector;
        private readonly SignalBus _signalBus;
        private readonly IGamePauseService _pauseService;

        private CameraViewMode _cameraViewMode = CameraViewMode.TopDown;

        public TankInputHandler(
            ITankInputSource inputSource,
            IScreenPointProjector screenPointProjector,
            SignalBus signalBus,
            IGamePauseService pauseService)
        {
            _inputSource = inputSource;
            _screenPointProjector = screenPointProjector;
            _signalBus = signalBus;
            _pauseService = pauseService;
            _signalBus.Subscribe<CameraViewModeChangedSignal>(OnCameraViewModeChanged);
        }

        public void Tick()
        {
            if (_inputSource.PauseRequested)
            {
                _signalBus.Fire(new PauseRequestedSignal());
                return;
            }

            if (_pauseService.IsPaused)
            {
                return;
            }

            var state = _inputSource.ReadState();
            var movement = Vector2.ClampMagnitude(state.Movement, 1f);

            _signalBus.Fire(new TankMovementInputSignal(movement));

            if (_inputSource.TryGetCameraViewModeRequest(out var requestedViewMode))
            {
                _signalBus.Fire(new CameraViewModeRequestedSignal(requestedViewMode));
            }

            if (_cameraViewMode == CameraViewMode.ThirdPerson &&
                _inputSource.TryGetCameraLookDelta(out var cameraLookDelta))
            {
                _signalBus.Fire(new CameraLookInputSignal(cameraLookDelta));
            }

            var canFire = _cameraViewMode == CameraViewMode.ThirdPerson || TryPublishAim(state);
            if (canFire && state.FireRequested)
            {
                _signalBus.Fire(new TankFireInputSignal());
            }
        }

        private bool TryPublishAim(TankInputState state)
        {
            if (!state.HasAimScreenPosition ||
                !_screenPointProjector.TryProject(state.AimScreenPosition, out var aimWorldPosition))
            {
                return false;
            }

            _signalBus.Fire(new TankAimInputSignal(aimWorldPosition));
            return true;
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<CameraViewModeChangedSignal>(OnCameraViewModeChanged);
        }

        private void OnCameraViewModeChanged(CameraViewModeChangedSignal signal)
        {
            _cameraViewMode = signal.ViewMode;
        }
    }
}
