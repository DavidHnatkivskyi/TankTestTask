using System;
using UnityEngine;

namespace GamePause
{
    /// <summary>
    /// Pauses scaled Unity time while preserving the time scale that was active before pausing.
    /// </summary>
    public sealed class GamePauseService : IGamePauseService, IDisposable
    {
        private float _timeScaleBeforePause;

        public bool IsPaused { get; private set; }

        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            Time.timeScale = _timeScaleBeforePause;
            IsPaused = false;
        }

        public void Dispose()
        {
            Resume();
        }
    }
}
