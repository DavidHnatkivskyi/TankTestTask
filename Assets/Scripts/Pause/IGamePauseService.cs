namespace GamePause
{
    /// <summary>
    /// Controls whether gameplay time is currently paused.
    /// </summary>
    public interface IGamePauseService
    {
        bool IsPaused { get; }

        void Pause();

        void Resume();
    }
}
