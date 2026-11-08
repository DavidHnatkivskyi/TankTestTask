using GameInput;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace GameUI
{
    /// <summary>
    /// Forwards pointer input from the free mobile HUD area. It remains behind
    /// interactive HUD controls, so their touches are not interpreted as firing.
    /// </summary>
    public sealed class MobilePointerInputView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private IMobilePointerInput _mobilePointerInput;

        [Inject]
        private void Construct(IMobilePointerInput mobilePointerInput)
        {
            _mobilePointerInput = mobilePointerInput;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetPointerState(eventData, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            SetPointerState(eventData, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPointerState(eventData, false);
        }

        private void SetPointerState(PointerEventData eventData, bool isFireHeld)
        {
            _mobilePointerInput.SetPointerState(eventData.position, isFireHeld);
        }
    }
}
