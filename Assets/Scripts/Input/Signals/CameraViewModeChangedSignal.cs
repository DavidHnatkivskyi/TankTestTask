namespace GameInput.Signals
{
    /// <summary>
    /// Announces the camera mode selected after a switch request.
    /// </summary>
    public sealed class CameraViewModeChangedSignal
    {
        public CameraViewModeChangedSignal(CameraViewMode viewMode)
        {
            ViewMode = viewMode;
        }

        public CameraViewMode ViewMode { get; }
    }
}
