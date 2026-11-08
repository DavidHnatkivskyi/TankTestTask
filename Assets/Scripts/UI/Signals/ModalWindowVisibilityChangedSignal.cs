namespace GameUI.Signals
{
    /// <summary>
    /// Announces whether at least one modal window is currently visible.
    /// </summary>
    public sealed class ModalWindowVisibilityChangedSignal
    {
        public ModalWindowVisibilityChangedSignal(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public bool IsVisible { get; }
    }
}
