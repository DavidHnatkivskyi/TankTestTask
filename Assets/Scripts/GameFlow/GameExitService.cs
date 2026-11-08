using UnityEngine;

namespace GameFlow
{
    /// <summary>
    /// Provides the platform-specific application exit behavior.
    /// </summary>
    internal sealed class GameExitService : IGameExitService
    {
        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
