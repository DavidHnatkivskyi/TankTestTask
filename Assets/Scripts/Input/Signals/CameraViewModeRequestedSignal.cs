namespace GameInput.Signals
{
    /// <summary>
    /// Requests a specific camera view mode from a directional input.
    /// </summary>
    public sealed class CameraViewModeRequestedSignal
    {
        public CameraViewModeRequestedSignal(CameraViewMode viewMode)
        {
            ViewMode = viewMode;
        }

        public CameraViewMode ViewMode { get; }
    }
}
