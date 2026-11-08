using MapGeneration;
using Tank;
using UnityEngine;

namespace TankControl
{
    internal sealed class ChaseAndOrbitTank : ComputerTank
    {
        private readonly float _orbitRadius;

        private int _orbitPointIndex;
        private int _orbitPointStep;
        private float _nextPlayerPathRefreshTime;
        private float _nextOrbitSearchTime;
        private bool _isOrbiting;
        private bool _hasOrbitDestination;
        private bool _hasRestoredDestination;

        public ChaseAndOrbitTank(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankContext context)
            : base(chassis, turret, navigation, context)
        {
            var minimumRadius = Mathf.Min(Config.ComputerOrbitMinimumRadius, Config.ComputerOrbitMaximumRadius);
            var maximumRadius = Mathf.Max(Config.ComputerOrbitMinimumRadius, Config.ComputerOrbitMaximumRadius);
            _orbitRadius = Random.Range(minimumRadius, maximumRadius);
        }

        public override void Tick(IPositionProvider player, float deltaTime)
        {
            if (player == null)
            {
                Navigation.Stop();
                return;
            }

            if (FollowRestoredPath())
            {
                AimAndFire(player, deltaTime);
                return;
            }

            var playerPosition = player.Position;
            if (GetHorizontalDistance(Chassis.Position, playerPosition) > _orbitRadius)
            {
                _isOrbiting = false;
                FollowPlayer(playerPosition);
            }
            else
            {
                if (!_isOrbiting)
                {
                    StartOrbit(playerPosition);
                }

                FollowOrbit(playerPosition);
            }

            AimAndFire(player, deltaTime);
        }

        public override bool TryRestoreMovementTarget(Vector3 destination)
        {
            if (!base.TryRestoreMovementTarget(destination))
            {
                return false;
            }

            _hasRestoredDestination = true;
            return true;
        }

        private bool FollowRestoredPath()
        {
            if (!_hasRestoredDestination)
            {
                return false;
            }

            if (Navigation.HasReachedDestination(Config.ComputerOrbitPointArrivalDistance))
            {
                _hasRestoredDestination = false;
                return false;
            }

            return true;
        }

        private void FollowPlayer(Vector3 playerPosition)
        {
            if (Time.time >= _nextPlayerPathRefreshTime)
            {
                Navigation.TrySetDestination(playerPosition);
                _nextPlayerPathRefreshTime = Time.time + Config.ComputerPlayerPathRefreshDuration;
            }
        }

        private void StartOrbit(Vector3 playerPosition)
        {
            _orbitPointIndex = FindClosestOrbitPointIndex(playerPosition);
            _orbitPointStep = SelectOrbitPointStep(playerPosition);
            _hasOrbitDestination = false;
            _isOrbiting = true;
        }

        private void FollowOrbit(Vector3 playerPosition)
        {
            if (_hasOrbitDestination &&
                !Navigation.HasReachedDestination(Config.ComputerOrbitPointArrivalDistance))
            {
                return;
            }

            if (Time.time < _nextOrbitSearchTime)
            {
                return;
            }

            if (!TryCreateNextOrbitPath(playerPosition))
            {
                _nextOrbitSearchTime = Time.time + Config.ComputerPathRetryDelay;
                Navigation.Stop();
            }
        }

        private bool TryGetOrbitPoint(
            Vector3 playerPosition,
            int pointIndex,
            out Vector3 orbitPoint)
        {
            var desiredPosition = GetOrbitPointPosition(playerPosition, pointIndex);

            if (!NavMeshPointFinder.TryProject(
                    desiredPosition,
                    Config.ComputerOrbitPointProjectionDistance,
                    out orbitPoint) ||
                GetHorizontalDistance(orbitPoint, playerPosition) > _orbitRadius + Config.ComputerOrbitPointProjectionDistance)
            {
                orbitPoint = default;
                return false;
            }

            return true;
        }

        private bool TryCreateNextOrbitPath(Vector3 playerPosition)
        {
            for (var attempt = 0; attempt < Config.ComputerOrbitPointCount; attempt++)
            {
                _orbitPointIndex = WrapOrbitPointIndex(_orbitPointIndex + _orbitPointStep);
                if (!TryGetOrbitPoint(playerPosition, _orbitPointIndex, out var orbitPoint))
                {
                    continue;
                }

                if (Navigation.TrySetDestination(orbitPoint))
                {
                    _hasOrbitDestination = true;
                    return true;
                }
            }

            return false;
        }

        private int FindClosestOrbitPointIndex(Vector3 playerPosition)
        {
            var offset = Chassis.Position - playerPosition;
            offset.y = 0f;
            var angle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            var anglePerPoint = 360f / Config.ComputerOrbitPointCount;
            return WrapOrbitPointIndex(Mathf.RoundToInt(angle / anglePerPoint));
        }

        private int SelectOrbitPointStep(Vector3 playerPosition)
        {
            var nextPointDirection = GetOrbitPointPosition(
                playerPosition,
                WrapOrbitPointIndex(_orbitPointIndex + 1)) - Chassis.Position;
            var previousPointDirection = GetOrbitPointPosition(
                playerPosition,
                WrapOrbitPointIndex(_orbitPointIndex - 1)) - Chassis.Position;
            nextPointDirection.y = 0f;
            previousPointDirection.y = 0f;
            return Vector3.Angle(Chassis.Forward, nextPointDirection) <=
                   Vector3.Angle(Chassis.Forward, previousPointDirection)
                ? 1
                : -1;
        }

        private Vector3 GetOrbitPointPosition(Vector3 playerPosition, int pointIndex)
        {
            var angle = 360f / Config.ComputerOrbitPointCount * pointIndex * Mathf.Deg2Rad;
            return playerPosition + new Vector3(
                Mathf.Cos(angle) * _orbitRadius,
                0f,
                Mathf.Sin(angle) * _orbitRadius);
        }

        private int WrapOrbitPointIndex(int pointIndex)
        {
            var pointCount = Config.ComputerOrbitPointCount;
            return (pointIndex % pointCount + pointCount) % pointCount;
        }
    }
}
