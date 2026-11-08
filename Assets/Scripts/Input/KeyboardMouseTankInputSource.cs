using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GameInput
{
    /// <summary>
    /// Desktop controls: WASD or arrow keys for movement; in top-down view the
    /// pointer selects the aim position, while in third-person its delta looks around.
    /// </summary>
    public sealed class KeyboardMouseTankInputSource : ITankInputSource
    {
        public bool PauseRequested => Keyboard.current?.escapeKey.wasPressedThisFrame ?? false;

        public TankInputState ReadState()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var movement = ReadMovement(keyboard);
            var hasMouse = mouse != null;
            var aimPosition = hasMouse ? mouse.position.ReadValue() : Vector2.zero;
            var fireRequested = hasMouse && mouse.leftButton.isPressed;

            return new TankInputState(movement, aimPosition, hasMouse, fireRequested);
        }

        public bool TryGetCameraViewModeRequest(out CameraViewMode viewMode)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                viewMode = default;
                return false;
            }

            var scrollDelta = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scrollDelta, 0f))
            {
                viewMode = default;
                return false;
            }

            viewMode = scrollDelta < 0f
                ? CameraViewMode.ThirdPerson
                : CameraViewMode.TopDown;
            return true;
        }

        public bool TryGetCameraLookDelta(out Vector2 lookDelta)
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                lookDelta = default;
                return false;
            }

            lookDelta = mouse.delta.ReadValue();
            return lookDelta.sqrMagnitude > 0f;
        }

        private static Vector2 ReadMovement(Keyboard keyboard)
        {
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var forward = ReadAxis(keyboard.wKey, keyboard.sKey, keyboard.upArrowKey, keyboard.downArrowKey);
            var turn = ReadAxis(keyboard.dKey, keyboard.aKey, keyboard.rightArrowKey, keyboard.leftArrowKey);
            return Vector2.ClampMagnitude(new Vector2(turn, forward), 1f);
        }

        private static float ReadAxis(
            KeyControl positivePrimary,
            KeyControl negativePrimary,
            KeyControl positiveSecondary,
            KeyControl negativeSecondary)
        {
            var positive = positivePrimary.isPressed || positiveSecondary.isPressed;
            var negative = negativePrimary.isPressed || negativeSecondary.isPressed;
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
