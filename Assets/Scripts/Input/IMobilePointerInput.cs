using UnityEngine;

namespace GameInput
{
    /// <summary>
    /// Receives the screen pointer that is reserved for aiming and firing outside
    /// of mobile HUD controls.
    /// </summary>
    public interface IMobilePointerInput
    {
        void SetPointerState(Vector2 screenPosition, bool isFireHeld);
    }
}
