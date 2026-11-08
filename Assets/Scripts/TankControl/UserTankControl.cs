using System;
using GameInput.Signals;
using Tank;
using UnityEngine;
using Zenject;

namespace TankControl
{
    /// <summary>
    /// Player-input control module. It is safe to run without a registered tank and can
    /// be reused after a tank is respawned by registering the new chassis and turret.
    /// </summary>
    public sealed class UserTankControl : IInitializable, IDisposable, ITickable, IUserTankControlTargetRegistry
    {
        private readonly SignalBus _signalBus;
        private readonly ITankMovementConstraint _movementConstraint;

        private ITankChassis _chassis;
        private ITurret _turret;
        private Vector2 _movement;
        private Vector3 _aimWorldPosition;
        private bool _hasAimWorldPosition;

        public UserTankControl(
            SignalBus signalBus,
            ITankMovementConstraint movementConstraint)
        {
            _signalBus = signalBus;
            _movementConstraint = movementConstraint;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<TankMovementInputSignal>(OnMovementInput);
            _signalBus.Subscribe<TankAimInputSignal>(OnAimInput);
            _signalBus.Subscribe<TankFireInputSignal>(OnFireInput);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<TankMovementInputSignal>(OnMovementInput);
            _signalBus.Unsubscribe<TankAimInputSignal>(OnAimInput);
            _signalBus.Unsubscribe<TankFireInputSignal>(OnFireInput);
        }

        public void Register(ITankChassis chassis, ITurret turret)
        {
            _chassis = chassis;
            _turret = turret;
            ClearInputState();
        }

        public void Unregister(ITankChassis chassis)
        {
            if (ReferenceEquals(_chassis, chassis))
            {
                ClearTarget();
            }
        }

        public void Tick()
        {
            if (!TryGetActiveTarget(out var chassis, out var turret))
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_movementConstraint.CanMove(chassis, _movement, deltaTime))
            {
                chassis.Move(_movement.y, _movement.x, deltaTime);
            }
            else
            {
                chassis.Stop();
                chassis.Move(0f, _movement.x, deltaTime);
            }

            RotateTurret(turret, deltaTime);
        }

        private void OnMovementInput(TankMovementInputSignal signal)
        {
            _movement = Vector2.ClampMagnitude(signal.Movement, 1f);
        }

        private void OnAimInput(TankAimInputSignal signal)
        {
            _aimWorldPosition = signal.WorldPosition;
            _hasAimWorldPosition = true;
        }

        private void OnFireInput(TankFireInputSignal signal)
        {
            if (IsAlive(_turret))
            {
                _turret.TryFire();
            }
        }

        private void RotateTurret(ITurret turret, float deltaTime)
        {
            if (!_hasAimWorldPosition)
            {
                return;
            }

            turret.RotateTowards(_aimWorldPosition, deltaTime);
        }

        private bool TryGetActiveTarget(out ITankChassis chassis, out ITurret turret)
        {
            if (!IsAlive(_chassis) || !IsAlive(_turret))
            {
                ClearTarget();
                chassis = null;
                turret = null;
                return false;
            }

            chassis = _chassis;
            turret = _turret;
            return true;
        }

        private void ClearTarget()
        {
            _chassis = null;
            _turret = null;
            ClearInputState();
        }

        private void ClearInputState()
        {
            _movement = Vector2.zero;
            _hasAimWorldPosition = false;
        }

        private static bool IsAlive(object value)
        {
            return value is UnityEngine.Object unityObject
                ? unityObject != null
                : value != null;
        }
    }
}
