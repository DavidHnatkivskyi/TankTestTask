using GamePause;
using NUnit.Framework;
using UnityEngine;

namespace Tanks.Tests
{
    [TestFixture]
    public sealed class GamePauseServiceTests
    {
        private float _originalTimeScale;
        private GamePauseService _service;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            _service = new GamePauseService();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
        }

        [Test]
        public void Pause_StopsScaledTimeAndMarksTheGameAsPaused()
        {
            _service.Pause();

            Assert.That(_service.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [Test]
        public void Resume_RestoresTheTimeScaleThatWasActiveBeforePause()
        {
            Time.timeScale = 0.5f;

            _service.Pause();
            _service.Resume();

            Assert.That(_service.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void Pause_WhenAlreadyPaused_DoesNotOverwriteTheSavedTimeScale()
        {
            Time.timeScale = 0.5f;

            _service.Pause();
            _service.Pause();
            _service.Resume();

            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void Resume_WhenNotPaused_LeavesTheTimeScaleUntouched()
        {
            Time.timeScale = 2f;

            _service.Resume();

            Assert.That(_service.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(2f));
        }

        [Test]
        public void Dispose_WhilePaused_RestoresTheTimeScale()
        {
            Time.timeScale = 0.5f;
            _service.Pause();

            _service.Dispose();

            Assert.That(_service.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.5f));
        }
    }
}
