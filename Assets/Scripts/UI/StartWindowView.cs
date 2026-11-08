using GameUI.Signals;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GameUI
{
    public sealed class StartWindowView : ModalWindowView
    {
        [SerializeField] private Button confirmButton;
        [SerializeField] private Toggle doNotShowAgainToggle;

        private SignalBus _signalBus;

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        private void OnConfirmClicked()
        {
            _signalBus.Fire(new StartWindowDismissedSignal(doNotShowAgainToggle.isOn));
        }
    }
}
