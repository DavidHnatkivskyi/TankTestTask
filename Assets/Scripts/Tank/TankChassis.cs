using Config;
using UnityEngine;

namespace Tank
{
    public class TankChassis : MonoBehaviour, ITankChassis
    {
        [SerializeField]private TankChassisConfig config;

        private TankConfig _tankConfig;
        private float _currentSpeed;
        private float _decelerationRate;
        private bool _isDecelerating;

        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public float ForwardSpeed => _tankConfig.MovementSpeed;
        public float DecelerationDuration => config.DecelerationDuration;
        public float TurnSpeed => config.TurnSpeed;

        private void Awake()
        {
            if (config == null)
            {
                throw new MissingReferenceException($"Assign {nameof(TankChassisConfig)} to '{name}'.");
            }
        }

        public void Move(float forwardInput, float turnInput, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            forwardInput = Mathf.Clamp(forwardInput, -1f, 1f);
            turnInput = Mathf.Clamp(turnInput, -1f, 1f);

            UpdateSpeed(forwardInput, deltaTime);
            var turnSpeed = GetTurnSpeed(_currentSpeed);
            var adjustedTurnInput = GetAdjustedTurnInput(_currentSpeed, turnInput);

            transform.Rotate(Vector3.up, adjustedTurnInput * turnSpeed * deltaTime, Space.Self);
            transform.position += transform.forward * _currentSpeed * deltaTime;
        }

        public void Stop()
        {
            _currentSpeed = 0f;
            _decelerationRate = 0f;
            _isDecelerating = false;
        }

        public void Configure(TankConfig tankConfig)
        {
            _tankConfig = tankConfig ?? throw new MissingReferenceException(
                $"Assign a {nameof(TankConfig)} to '{name}'.");
            _tankConfig.Validate();
        }

        public Vector3 GetMovementDelta(float forwardInput, float turnInput, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return Vector3.zero;
            }

            forwardInput = Mathf.Clamp(forwardInput, -1f, 1f);
            turnInput = Mathf.Clamp(turnInput, -1f, 1f);

            var speed = GetNextSpeed(forwardInput, deltaTime);
            var turnSpeed = GetTurnSpeed(speed);
            var adjustedTurnInput = GetAdjustedTurnInput(speed, turnInput);
            var movementDirection = Quaternion.AngleAxis(adjustedTurnInput * turnSpeed * deltaTime, Vector3.up) * transform.forward;
            return movementDirection * speed * deltaTime;
        }

        private float GetSpeed(float forwardInput)
        {
            return forwardInput >= 0f
                ? forwardInput * ForwardSpeed
                : forwardInput * config.ReverseSpeed;
        }

        private float GetTurnSpeed(float speed)
        {
            if (Mathf.Approximately(speed, 0f))
            {
                return config.TurnSpeed;
            }

            var radiusLimitedTurnSpeed = Mathf.Rad2Deg * Mathf.Abs(speed) / config.TurnRadius;
            return Mathf.Min(config.TurnSpeed, radiusLimitedTurnSpeed);
        }

        private void UpdateSpeed(float forwardInput, float deltaTime)
        {
            if (!Mathf.Approximately(forwardInput, 0f))
            {
                _currentSpeed = GetSpeed(forwardInput);
                _isDecelerating = false;
                return;
            }

            if (!_isDecelerating)
            {
                _decelerationRate = GetDecelerationRate(_currentSpeed);
                _isDecelerating = true;
            }

            _currentSpeed = MoveTowardsStop(_currentSpeed, _decelerationRate, deltaTime);
            if (Mathf.Approximately(_currentSpeed, 0f))
            {
                _isDecelerating = false;
            }
        }

        private float GetNextSpeed(float forwardInput, float deltaTime)
        {
            if (!Mathf.Approximately(forwardInput, 0f))
            {
                return GetSpeed(forwardInput);
            }

            var decelerationRate = _isDecelerating
                ? _decelerationRate
                : GetDecelerationRate(_currentSpeed);
            return MoveTowardsStop(_currentSpeed, decelerationRate, deltaTime);
        }

        private float GetDecelerationRate(float speed)
        {
            if (config.DecelerationDuration <= 0f)
            {
                return float.PositiveInfinity;
            }

            return Mathf.Abs(speed) / config.DecelerationDuration;
        }

        private static float MoveTowardsStop(float speed, float decelerationRate, float deltaTime)
        {
            return Mathf.MoveTowards(speed, 0f, decelerationRate * deltaTime);
        }

        private static float GetAdjustedTurnInput(float speed, float turnInput)
        {
            return speed < 0f ? -turnInput : turnInput;
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
