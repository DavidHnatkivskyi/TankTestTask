namespace GameSaving
{
    /// <summary>
    /// Saves and restores the current gameplay session. Call Load only after gameplay has started.
    /// </summary>
    public interface IGameSaveService
    {
        bool Save();

        bool Load();
    }
}
