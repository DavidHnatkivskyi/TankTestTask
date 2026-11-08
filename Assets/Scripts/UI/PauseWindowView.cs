using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public sealed class PauseWindowView : ModalWindowView
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button exitButton;

        public event Action ContinueRequested;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action ExitRequested;

        private void Awake()
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            saveButton.onClick.AddListener(OnSaveClicked);
            loadButton.onClick.AddListener(OnLoadClicked);
            exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDestroy()
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
            saveButton.onClick.RemoveListener(OnSaveClicked);
            loadButton.onClick.RemoveListener(OnLoadClicked);
            exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void OnContinueClicked()
        {
            ContinueRequested?.Invoke();
        }

        private void OnSaveClicked()
        {
            SaveRequested?.Invoke();
        }

        private void OnLoadClicked()
        {
            LoadRequested?.Invoke();
        }

        private void OnExitClicked()
        {
            ExitRequested?.Invoke();
        }
    }
}
