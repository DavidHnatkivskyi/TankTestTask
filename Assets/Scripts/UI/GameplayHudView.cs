using Config;
using GameInput.Signals;
using GamePause.Signals;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace GameUI
{
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField] private Button cameraModeButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject desktopCameraHint;
        [SerializeField] private GameObject mobileMovementJoystick;
        [SerializeField] private GameObject mobilePointerInput;

        private SignalBus _signalBus;
        private bool _isMobileHudEnabled;

        [Inject]
        private void Construct(SignalBus signalBus, GameConfig gameConfig)
        {
            _signalBus = signalBus;
            _isMobileHudEnabled = Application.isMobilePlatform || gameConfig.ForceMobileHud;
        }

        private void Awake()
        {
            cameraModeButton.gameObject.SetActive(_isMobileHudEnabled);
            pauseButton.gameObject.SetActive(_isMobileHudEnabled);
            desktopCameraHint.SetActive(!_isMobileHudEnabled);
            mobileMovementJoystick.gameObject.SetActive(_isMobileHudEnabled);
            mobilePointerInput.gameObject.SetActive(_isMobileHudEnabled);

            if (_isMobileHudEnabled)
            {
                cameraModeButton.onClick.AddListener(OnCameraModeClicked);
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        private void OnDestroy()
        {
            if (_isMobileHudEnabled)
            {
                cameraModeButton.onClick.RemoveListener(OnCameraModeClicked);
                pauseButton.onClick.RemoveListener(OnPauseClicked);
            }
        }

        private void OnCameraModeClicked()
        {
            _signalBus.Fire(new CameraModeSwitchRequestedSignal());
        }

        private void OnPauseClicked()
        {
            _signalBus.Fire(new PauseRequestedSignal());
        }
    }
}
