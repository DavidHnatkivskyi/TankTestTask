namespace GameSaving
{
    /// <summary>
    /// Narrow persistence contract implemented by the module that owns spawned tanks.
    /// </summary>
    public interface ITankStatePersistence
    {
        bool TryCaptureState(out TankRuntimeSaveState state);

        bool TryRestoreState(TankRuntimeSaveState state);
    }
}
