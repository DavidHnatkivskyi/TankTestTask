using GameFlow.Signals;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GameUI
{
    public sealed class PlayerRespawnWindowView : ModalWindowView
    {
        [SerializeField] private Button respawnButton;

        private SignalBus _signalBus;

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void Awake()
        {
            respawnButton.onClick.AddListener(OnRespawnClicked);
        }

        private void OnDestroy()
        {
            respawnButton.onClick.RemoveListener(OnRespawnClicked);
        }

        private void OnRespawnClicked()
        {
            _signalBus.Fire(new PlayerRespawnRequestedSignal());
        }
    }
}
