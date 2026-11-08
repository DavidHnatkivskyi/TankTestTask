using System.Collections.Generic;
using GameUI.Signals;
using Zenject;

namespace GameUI
{
    /// <summary>
    /// Last-in-first-out stack of modal windows. Only the top window is visible and
    /// the stack announces when the first window opens or the last one closes.
    /// </summary>
    internal sealed class ModalWindowStack
    {
        private readonly SignalBus _signalBus;
        private readonly Stack<ModalWindowView> _windows = new();
        private bool _hasVisibleWindow;

        public ModalWindowStack(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public int Count => _windows.Count;

        public ModalWindowView Top => _windows.Count > 0 ? _windows.Peek() : null;

        public void Push(ModalWindowView window)
        {
            if (ReferenceEquals(Top, window))
            {
                return;
            }

            Top?.Hide();
            _windows.Push(window);
            window.Show();
            UpdateVisibility();
        }

        public void CloseTop()
        {
            _windows.Pop().Hide();
            Top?.Show();
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var hasVisibleWindow = _windows.Count > 0;
            if (_hasVisibleWindow == hasVisibleWindow)
            {
                return;
            }

            _hasVisibleWindow = hasVisibleWindow;
            _signalBus.Fire(new ModalWindowVisibilityChangedSignal(_hasVisibleWindow));
        }
    }
}
