namespace GameSaving
{
    /// <summary>
    /// Narrow persistence contract implemented by the gameplay camera owner.
    /// </summary>
    public interface ICameraStatePersistence
    {
        CameraSaveData CaptureState();

        void RestoreState(CameraSaveData state);
    }
}
