using System.Collections.Generic;
using GameInput;
using GameInput.Signals;
using GamePause.Signals;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Tanks.Tests
{
    [TestFixture]
    public sealed class TankInputHandlerTests
    {
        private SignalBus _signalBus;
        private FakeTankInputSource _inputSource;
        private FakeScreenPointProjector _projector;
        private FakeGamePauseService _pauseService;
        private TankInputHandler _handler;

        private readonly List<TankMovementInputSignal> _movementSignals = new();
        private readonly List<TankAimInputSignal> _aimSignals = new();
        private readonly List<TankFireInputSignal> _fireSignals = new();
        private readonly List<CameraViewModeRequestedSignal> _viewModeSignals = new();
        private readonly List<CameraLookInputSignal> _lookSignals = new();
        private readonly List<PauseRequestedSignal> _pauseSignals = new();

        [SetUp]
        public void SetUp()
        {
            _movementSignals.Clear();
            _aimSignals.Clear();
            _fireSignals.Clear();
            _viewModeSignals.Clear();
            _lookSignals.Clear();
            _pauseSignals.Clear();

            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<TankMovementInputSignal>().OptionalSubscriber();
            container.DeclareSignal<TankAimInputSignal>().OptionalSubscriber();
            container.DeclareSignal<TankFireInputSignal>().OptionalSubscriber();
            container.DeclareSignal<CameraViewModeRequestedSignal>().OptionalSubscriber();
            container.DeclareSignal<CameraViewModeChangedSignal>().OptionalSubscriber();
            container.DeclareSignal<CameraLookInputSignal>().OptionalSubscriber();
            container.DeclareSignal<PauseRequestedSignal>().OptionalSubscriber();
            _signalBus = container.Resolve<SignalBus>();

            _inputSource = new FakeTankInputSource();
            _projector = new FakeScreenPointProjector();
            _pauseService = new FakeGamePauseService();
            _handler = CreateHandler();

            _signalBus.Subscribe<TankMovementInputSignal>(_movementSignals.Add);
            _signalBus.Subscribe<TankAimInputSignal>(_aimSignals.Add);
            _signalBus.Subscribe<TankFireInputSignal>(_fireSignals.Add);
            _signalBus.Subscribe<CameraViewModeRequestedSignal>(_viewModeSignals.Add);
            _signalBus.Subscribe<CameraLookInputSignal>(_lookSignals.Add);
            _signalBus.Subscribe<PauseRequestedSignal>(_pauseSignals.Add);
        }

        [TearDown]
        public void TearDown()
        {
            _handler.Dispose();
        }

        [Test]
        public void Tick_ValidTopDownInput_PublishesClampedMovementAimFireAndViewRequest()
        {
            _inputSource.State = new TankInputState(
                new Vector2(3f, 4f),
                new Vector2(120f, 80f),
                true,
                true);
            _inputSource.HasViewModeRequest = true;
            _inputSource.RequestedViewMode = CameraViewMode.ThirdPerson;
            _projector.CanProject = true;
            _projector.ProjectedPosition = new Vector3(4f, 0f, 7f);

            _handler.Tick();

            Assert.That(_movementSignals, Has.Count.EqualTo(1));
            Assert.That(_movementSignals[0].Movement.x, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(_movementSignals[0].Movement.y, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(_aimSignals, Has.Count.EqualTo(1));
            Assert.That(_aimSignals[0].WorldPosition, Is.EqualTo(_projector.ProjectedPosition));
            Assert.That(_fireSignals, Has.Count.EqualTo(1));
            Assert.That(_viewModeSignals, Has.Count.EqualTo(1));
            Assert.That(_viewModeSignals[0].ViewMode, Is.EqualTo(CameraViewMode.ThirdPerson));
        }

        [Test]
        public void Tick_PauseRequested_PublishesOnlyPauseRequest()
        {
            _inputSource.PauseRequested = true;
            _inputSource.State = new TankInputState(Vector2.one, Vector2.zero, true, true);

            _handler.Tick();

            Assert.That(_pauseSignals, Has.Count.EqualTo(1));
            Assert.That(_movementSignals, Is.Empty);
            Assert.That(_aimSignals, Is.Empty);
            Assert.That(_fireSignals, Is.Empty);
        }

        [Test]
        public void Tick_PauseRequestedWhileGameIsPaused_StillPublishesPauseRequest()
        {
            _pauseService.IsPaused = true;
            _inputSource.PauseRequested = true;

            _handler.Tick();

            Assert.That(_pauseSignals, Has.Count.EqualTo(1));
        }

        [Test]
        public void Tick_GameIsPaused_DoesNotPublishGameplayInput()
        {
            _pauseService.IsPaused = true;
            _inputSource.State = new TankInputState(Vector2.one, Vector2.zero, true, true);
            _inputSource.HasViewModeRequest = true;
            _inputSource.RequestedViewMode = CameraViewMode.ThirdPerson;
            _inputSource.HasLookDelta = true;
            _signalBus.Fire(new CameraViewModeChangedSignal(CameraViewMode.ThirdPerson));

            _handler.Tick();

            Assert.That(_movementSignals, Is.Empty);
            Assert.That(_aimSignals, Is.Empty);
            Assert.That(_fireSignals, Is.Empty);
            Assert.That(_viewModeSignals, Is.Empty);
            Assert.That(_lookSignals, Is.Empty);
        }

        [Test]
        public void Tick_TopDownInput_DoesNotPublishCameraLookEvenWhenSourceHasLookDelta()
        {
            _inputSource.State = new TankInputState(Vector2.zero, Vector2.zero, false, false);
            _inputSource.HasLookDelta = true;
            _inputSource.LookDelta = new Vector2(2f, -3f);

            _handler.Tick();

            Assert.That(_lookSignals, Is.Empty);
        }

        [Test]
        public void Tick_ThirdPersonInput_PublishesLookAndAllowsFireWithoutTopDownAim()
        {
            _inputSource.State = new TankInputState(Vector2.zero, Vector2.zero, false, true);
            _inputSource.HasLookDelta = true;
            _inputSource.LookDelta = new Vector2(2f, -3f);
            _signalBus.Fire(new CameraViewModeChangedSignal(CameraViewMode.ThirdPerson));

            _handler.Tick();

            Assert.That(_lookSignals, Has.Count.EqualTo(1));
            Assert.That(_lookSignals[0].Delta, Is.EqualTo(_inputSource.LookDelta));
            Assert.That(_aimSignals, Is.Empty);
            Assert.That(_fireSignals, Has.Count.EqualTo(1));
        }

        [Test]
        public void Tick_TopDownAimCannotBeProjected_PublishesMovementButNoAimOrFire()
        {
            _inputSource.State = new TankInputState(Vector2.up, new Vector2(10f, 10f), true, true);
            _projector.CanProject = false;

            _handler.Tick();

            Assert.That(_movementSignals, Has.Count.EqualTo(1));
            Assert.That(_aimSignals, Is.Empty);
            Assert.That(_fireSignals, Is.Empty);
        }

        [Test]
        public void Tick_TopDownWithoutAimScreenPosition_DoesNotFire()
        {
            _inputSource.State = new TankInputState(Vector2.zero, Vector2.zero, false, true);
            _projector.CanProject = true;

            _handler.Tick();

            Assert.That(_aimSignals, Is.Empty);
            Assert.That(_fireSignals, Is.Empty);
        }

        [Test]
        public void Tick_TopDownWithoutFireRequest_PublishesAimButDoesNotFire()
        {
            _inputSource.State = new TankInputState(Vector2.zero, new Vector2(10f, 10f), true, false);
            _projector.CanProject = true;

            _handler.Tick();

            Assert.That(_aimSignals, Has.Count.EqualTo(1));
            Assert.That(_fireSignals, Is.Empty);
        }

        [Test]
        public void Tick_AfterReturningToTopDown_FireRequiresAimAgain()
        {
            _inputSource.State = new TankInputState(Vector2.zero, Vector2.zero, false, true);
            _signalBus.Fire(new CameraViewModeChangedSignal(CameraViewMode.ThirdPerson));
            _signalBus.Fire(new CameraViewModeChangedSignal(CameraViewMode.TopDown));

            _handler.Tick();

            Assert.That(_fireSignals, Is.Empty);
        }

        [Test]
        public void Dispose_StopsTrackingTheCameraViewMode()
        {
            var disposedHandler = CreateHandler();
            disposedHandler.Dispose();
            _inputSource.State = new TankInputState(Vector2.zero, Vector2.zero, false, true);

            _signalBus.Fire(new CameraViewModeChangedSignal(CameraViewMode.ThirdPerson));
            disposedHandler.Tick();

            Assert.That(_fireSignals, Is.Empty);
        }

        private TankInputHandler CreateHandler()
        {
            return new TankInputHandler(_inputSource, _projector, _signalBus, _pauseService);
        }
    }
}
