using System;
using System.Threading;
using Config;
using Cysharp.Threading.Tasks;
using GameFlow;
using GameFlow.Signals;
using GamePause;
using GamePause.Signals;
using GameSaving;
using GameUI.Signals;
using UnityEngine;
using Zenject;

namespace GameUI
{
    /// <summary>
    /// Owns persistent gameplay HUD and a last-in-first-out modal window stack.
    /// </summary>
    public sealed class UIManager : IInitializable, IDisposable
    {
        private const string StartWindowDismissedPreferenceKey = "ui.start-window.dismissed";

        private readonly DiContainer _container;
        private readonly SignalBus _signalBus;
        private readonly UISceneRoot _sceneRoot;
        private readonly IGamePauseService _pauseService;
        private readonly ModalWindowStack _modalWindows;
        private readonly CancellationTokenSource _disposeCancellation = new();

        private StartWindowView _startWindow;
        private PlayerRespawnWindowView _playerRespawnWindow;
        private PauseWindowView _pauseWindow;
        private IGameSaveService _gameSaveService;
        private IGameExitService _gameExitService;
        private GameConfig _gameConfig;
        private bool _hasGameplayStarted;
        private bool _isPlayerRespawnWindowPending;

        public UIManager(
            DiContainer container,
            SignalBus signalBus,
            UISceneRoot sceneRoot,
            IGamePauseService pauseService)
        {
            _container = container;
            _signalBus = signalBus;
            _sceneRoot = sceneRoot;
            _pauseService = pauseService;
            _modalWindows = new ModalWindowStack(signalBus);
        }

        [Inject]
        private void Construct(
            IGameSaveService gameSaveService,
            IGameExitService gameExitService,
            GameConfig gameConfig)
        {
            _gameSaveService = gameSaveService;
            _gameExitService = gameExitService;
            _gameConfig = gameConfig;
        }

        public void Initialize()
        {
            ValidateSceneRoot();
            _container.InstantiatePrefabForComponent<GameplayHudView>(
                _sceneRoot.GameplayHudPrefab,
                _sceneRoot.HudRoot);
            _startWindow = InstantiateModal(_sceneRoot.StartWindowPrefab);
            _playerRespawnWindow = InstantiateModal(_sceneRoot.PlayerRespawnWindowPrefab);
            _pauseWindow = InstantiateModal(_sceneRoot.PauseWindowPrefab);

            _signalBus.Subscribe<ShowStartWindowSignal>(OnShowStartWindow);
            _signalBus.Subscribe<PlayerTankDestroyedSignal>(OnPlayerTankDestroyed);
            _signalBus.Subscribe<PlayerTankRespawnedSignal>(OnPlayerTankRespawned);
            _signalBus.Subscribe<StartWindowDismissedSignal>(OnStartWindowDismissed);
            _signalBus.Subscribe<PauseRequestedSignal>(OnPauseRequested);
            _pauseWindow.ContinueRequested += OnContinueRequested;
            _pauseWindow.SaveRequested += OnSaveRequested;
            _pauseWindow.LoadRequested += OnLoadRequested;
            _pauseWindow.ExitRequested += OnExitRequested;

            ShowStartWindow();
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<ShowStartWindowSignal>(OnShowStartWindow);
            _signalBus.Unsubscribe<PlayerTankDestroyedSignal>(OnPlayerTankDestroyed);
            _signalBus.Unsubscribe<PlayerTankRespawnedSignal>(OnPlayerTankRespawned);
            _signalBus.Unsubscribe<StartWindowDismissedSignal>(OnStartWindowDismissed);
            _signalBus.Unsubscribe<PauseRequestedSignal>(OnPauseRequested);
            _pauseWindow.ContinueRequested -= OnContinueRequested;
            _pauseWindow.SaveRequested -= OnSaveRequested;
            _pauseWindow.LoadRequested -= OnLoadRequested;
            _pauseWindow.ExitRequested -= OnExitRequested;
            _disposeCancellation.Cancel();
            _disposeCancellation.Dispose();
        }

        private void OnShowStartWindow()
        {
            ShowStartWindow();
        }

        private void OnPlayerTankDestroyed()
        {
            ShowPlayerRespawnWindowAfterDelayAsync().Forget();
        }

        private async UniTaskVoid ShowPlayerRespawnWindowAfterDelayAsync()
        {
            _isPlayerRespawnWindowPending = true;
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_gameConfig.PlayerRespawnWindowDelay),
                    cancellationToken: _disposeCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _isPlayerRespawnWindowPending = false;
            }

            _modalWindows.Push(_playerRespawnWindow);
        }

        private void OnPlayerTankRespawned()
        {
            if (ReferenceEquals(_modalWindows.Top, _playerRespawnWindow))
            {
                _modalWindows.CloseTop();
            }
        }

        private void OnStartWindowDismissed(StartWindowDismissedSignal signal)
        {
            if (signal.DoNotShowAgain)
            {
                PlayerPrefs.SetInt(StartWindowDismissedPreferenceKey, 1);
                PlayerPrefs.Save();
            }

            if (ReferenceEquals(_modalWindows.Top, _startWindow))
            {
                _modalWindows.CloseTop();
            }

            StartGameplay();
        }

        private void OnPauseRequested()
        {
            if (!_hasGameplayStarted || _isPlayerRespawnWindowPending)
            {
                return;
            }

            if (ReferenceEquals(_modalWindows.Top, _pauseWindow))
            {
                ClosePauseWindow();
                return;
            }

            if (_modalWindows.Count > 0)
            {
                return;
            }

            _pauseService.Pause();
            _modalWindows.Push(_pauseWindow);
        }

        private void OnContinueRequested()
        {
            if (ReferenceEquals(_modalWindows.Top, _pauseWindow))
            {
                ClosePauseWindow();
            }
        }

        private void OnSaveRequested()
        {
            _gameSaveService.Save();
        }

        private void OnLoadRequested()
        {
            _gameSaveService.Load();
        }

        private void OnExitRequested()
        {
            _gameExitService.Exit();
        }

        private void ShowStartWindow()
        {
            if (PlayerPrefs.GetInt(StartWindowDismissedPreferenceKey, 0) == 0)
            {
                _modalWindows.Push(_startWindow);
                return;
            }

            StartGameplay();
        }

        private void StartGameplay()
        {
            if (_hasGameplayStarted)
            {
                return;
            }

            _hasGameplayStarted = true;
            _signalBus.Fire(new GameplayStartedSignal());
        }

        private T InstantiateModal<T>(T prefab)
            where T : ModalWindowView
        {
            var window = _container.InstantiatePrefabForComponent<T>(prefab, _sceneRoot.ModalRoot);
            window.Hide();
            return window;
        }

        private void ClosePauseWindow()
        {
            _modalWindows.CloseTop();
            _pauseService.Resume();
        }

        private void ValidateSceneRoot()
        {
            if (_sceneRoot == null ||
                _sceneRoot.HudRoot == null ||
                _sceneRoot.ModalRoot == null ||
                _sceneRoot.StartWindowPrefab == null ||
                _sceneRoot.PlayerRespawnWindowPrefab == null ||
                _sceneRoot.PauseWindowPrefab == null ||
                _sceneRoot.GameplayHudPrefab == null)
            {
                throw new InvalidOperationException("Assign all UI references on UISceneRoot.");
            }
        }
    }
}
