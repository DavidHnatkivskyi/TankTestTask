using Config;
using UnityEngine;

namespace GameInput
{
    /// <summary>
    /// Adds the SimpleInput mobile joystick to the desktop input source without
    /// exposing the plugin outside the input module.
    /// </summary>
    public sealed class SimpleInputTankInputSource : ITankInputSource, IMobilePointerInput
    {
        private const string HorizontalAxisName = "Horizontal";
        private const string VerticalAxisName = "Vertical";

        private readonly KeyboardMouseTankInputSource _keyboardMouseInputSource;
        private readonly bool _isMobileInputEnabled;
        private readonly float _joystickForwardMultiplier;
        private Vector2 _aimScreenPosition;
        private bool _hasAimScreenPosition;
        private bool _isFireHeld;

        public SimpleInputTankInputSource(
            KeyboardMouseTankInputSource keyboardMouseInputSource,
            GameConfig gameConfig)
        {
            _keyboardMouseInputSource = keyboardMouseInputSource;
            _isMobileInputEnabled = Application.isMobilePlatform || gameConfig.ForceMobileHud;
            _joystickForwardMultiplier = gameConfig.MobileJoystickForwardMultiplier;
        }

        public bool PauseRequested => _keyboardMouseInputSource.PauseRequested;

        public void SetPointerState(Vector2 screenPosition, bool isFireHeld)
        {
            _aimScreenPosition = screenPosition;
            _hasAimScreenPosition = true;
            _isFireHeld = isFireHeld;
        }

        public TankInputState ReadState()
        {
            if (!_isMobileInputEnabled)
            {
                return _keyboardMouseInputSource.ReadState();
            }

            var joystickMovement = new Vector2(
                SimpleInput.GetAxisRaw(HorizontalAxisName),
                Mathf.Clamp(
                    SimpleInput.GetAxisRaw(VerticalAxisName) * _joystickForwardMultiplier,
                    -1f,
                    1f));
            var keyboardMovement = _keyboardMouseInputSource.ReadState().Movement;
            var movement = Vector2.ClampMagnitude(joystickMovement + keyboardMovement, 1f);

            return new TankInputState(
                movement,
                _aimScreenPosition,
                _hasAimScreenPosition,
                _isFireHeld);
        }

        public bool TryGetCameraViewModeRequest(out CameraViewMode viewMode)
        {
            return _keyboardMouseInputSource.TryGetCameraViewModeRequest(out viewMode);
        }

        public bool TryGetCameraLookDelta(out Vector2 lookDelta)
        {
            return _keyboardMouseInputSource.TryGetCameraLookDelta(out lookDelta);
        }
    }
}
