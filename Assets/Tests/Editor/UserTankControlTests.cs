using System.Collections;
using GameInput.Signals;
using NUnit.Framework;
using TankControl;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace Tanks.Tests
{
    [TestFixture]
    public sealed class UserTankControlTests
    {
        private const int MaximumFramesToWaitForDeltaTime = 10;

        private SignalBus _signalBus;
        private UserTankControl _control;
        private FakeTankChassis _chassis;
        private FakeTurret _turret;
        private FakeMovementConstraint _movementConstraint;

        [SetUp]
        public void SetUp()
        {
            var container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<TankMovementInputSignal>().OptionalSubscriber();
            container.DeclareSignal<TankAimInputSignal>().OptionalSubscriber();
            container.DeclareSignal<TankFireInputSignal>().OptionalSubscriber();
            _signalBus = container.Resolve<SignalBus>();

            _movementConstraint = new FakeMovementConstraint();
            _control = new UserTankControl(_signalBus, _movementConstraint);
            _control.Initialize();
            _chassis = new FakeTankChassis();
            _turret = new FakeTurret();
        }

        [TearDown]
        public void TearDown()
        {
            _control.Dispose();
        }

        [Test]
        public void FireInput_IsForwardedOnlyToTheCurrentlyRegisteredTank()
        {
            _control.Register(_chassis, _turret);

            _signalBus.Fire(new TankFireInputSignal());

            Assert.That(_turret.FireAttemptCount, Is.EqualTo(1));

            _control.Unregister(_chassis);
            _signalBus.Fire(new TankFireInputSignal());

            Assert.That(_turret.FireAttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void Unregister_DifferentChassis_KeepsTheCurrentTank()
        {
            _control.Register(_chassis, _turret);

            _control.Unregister(new FakeTankChassis());
            _signalBus.Fire(new TankFireInputSignal());

            Assert.That(_turret.FireAttemptCount, Is.EqualTo(1));
        }

        [Test]
        public void Tick_WithoutRegisteredTank_DoesNothing()
        {
            _signalBus.Fire(new TankMovementInputSignal(Vector2.one));

            Assert.DoesNotThrow(() => _control.Tick());
        }

        [UnityTest]
        public IEnumerator Tick_AfterUnregister_DoesNotMoveTheOldChassis()
        {
            _control.Register(_chassis, _turret);
            _signalBus.Fire(new TankMovementInputSignal(Vector2.one));
            _control.Unregister(_chassis);

            yield return WaitForPositiveDeltaTime();
            _control.Tick();

            Assert.That(_chassis.MoveCallCount, Is.EqualTo(0));
            Assert.That(_chassis.StopCallCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Tick_WhenMovementIsAllowed_MovesChassisWithoutStopping()
        {
            _control.Register(_chassis, _turret);
            _signalBus.Fire(new TankMovementInputSignal(new Vector2(0.25f, 0.8f)));

            yield return WaitForPositiveDeltaTime();
            _control.Tick();

            Assert.That(_chassis.MoveCallCount, Is.EqualTo(1));
            Assert.That(_chassis.StopCallCount, Is.EqualTo(0));
            Assert.That(_chassis.LastForwardInput, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(_chassis.LastTurnInput, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator Tick_WhenMovementIsBlocked_StopsTranslationButKeepsTurning()
        {
            _movementConstraint.AllowsMovement = false;
            _control.Register(_chassis, _turret);
            _signalBus.Fire(new TankMovementInputSignal(new Vector2(0.25f, 0.8f)));

            yield return WaitForPositiveDeltaTime();
            _control.Tick();

            Assert.That(_chassis.StopCallCount, Is.EqualTo(1));
            Assert.That(_chassis.MoveCallCount, Is.EqualTo(1));
            Assert.That(_chassis.LastForwardInput, Is.EqualTo(0f));
            Assert.That(_chassis.LastTurnInput, Is.EqualTo(0.25f));
        }

        [UnityTest]
        public IEnumerator Tick_OversizedMovementInput_IsClampedToUnitLength()
        {
            _control.Register(_chassis, _turret);
            _signalBus.Fire(new TankMovementInputSignal(new Vector2(3f, 4f)));

            yield return WaitForPositiveDeltaTime();
            _control.Tick();

            Assert.That(_chassis.LastTurnInput, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(_chassis.LastForwardInput, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator Tick_WithAimInput_RotatesTurretTowardsTheAimPosition()
        {
            var aimPosition = new Vector3(5f, 0f, 9f);
            _control.Register(_chassis, _turret);
            _signalBus.Fire(new TankAimInputSignal(aimPosition));

            yield return WaitForPositiveDeltaTime();
            _control.Tick();

            Assert.That(_turret.RotateCallCount, Is.EqualTo(1));
            Assert.That(_turret.LastAimPosition, Is.EqualTo(aimPosition));
        }

        [UnityTest]
        public IEnumerator Tick_WithoutAimInput_DoesNotRotateTurret()
        {
            _control.Register(_chassis, _turret);

            yield return WaitForPositiveDeltaTime();
            _control.Tick();

            Assert.That(_turret.RotateCallCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Register_NewTank_DiscardsInputReceivedForThePreviousTank()
        {
            _control.Register(_chassis, _turret);
            _signalBus.Fire(new TankMovementInputSignal(new Vector2(0.5f, 1f)));
            _signalBus.Fire(new TankAimInputSignal(new Vector3(5f, 0f, 9f)));

            var respawnedChassis = new FakeTankChassis();
            var respawnedTurret = new FakeTurret();
            _control.Register(respawnedChassis, respawnedTurret);

            yield return WaitForPositiveDeltaTime();
            _control.Tick();

            Assert.That(respawnedChassis.LastForwardInput, Is.EqualTo(0f));
            Assert.That(respawnedChassis.LastTurnInput, Is.EqualTo(0f));
            Assert.That(respawnedTurret.RotateCallCount, Is.EqualTo(0));
        }

        // UserTankControl reads the global Time.deltaTime and ignores a Tick with zero delta,
        // so the test waits for a real frame. If the editor never produces one, the result is
        // inconclusive instead of a misleading failure.
        private static IEnumerator WaitForPositiveDeltaTime()
        {
            for (var frame = 0; frame < MaximumFramesToWaitForDeltaTime && Time.deltaTime <= 0f; frame++)
            {
                yield return null;
            }

            Assume.That(Time.deltaTime, Is.GreaterThan(0f), "The editor did not produce a frame with a positive delta time.");
        }
    }
}
