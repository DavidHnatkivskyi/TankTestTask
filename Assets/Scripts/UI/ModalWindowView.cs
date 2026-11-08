using UnityEngine;

namespace GameUI
{
    /// <summary>
    /// Base type for modal UI prefabs managed by the UI stack.
    /// </summary>
    public abstract class ModalWindowView : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
