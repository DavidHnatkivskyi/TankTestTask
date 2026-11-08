using MapGeneration;
using Tank;
using UnityEngine;

namespace TankControl
{
    internal sealed class RandomPatrolTank : ComputerTank
    {
        private readonly float _patrolRadius;
        private readonly int _searchAttempts;
        private readonly Vector3 _patrolCenter;

        private float _nextPathSearchTime;
        private bool _hasDestination;

        public RandomPatrolTank(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankContext context)
            : base(chassis, turret, navigation, context)
        {
            _patrolRadius = Config.ComputerPatrolRadius;
            _searchAttempts = Config.NavMeshPointSearchAttempts;
            _patrolCenter = Chassis.Position;
        }

        public override void Tick(IPositionProvider player, float deltaTime)
        {
            FollowPatrolPath();
            if (player != null)
            {
                AimAndFire(player, deltaTime);
            }
        }

        public override bool TryRestoreMovementTarget(Vector3 destination)
        {
            if (!base.TryRestoreMovementTarget(destination))
            {
                return false;
            }

            _hasDestination = true;
            return true;
        }

        private void FollowPatrolPath()
        {
            if ((!_hasDestination || Navigation.HasReachedDestination(Config.ComputerWaypointArrivalDistance)) &&
                !TryCreatePatrolPath())
            {
                Navigation.Stop();
                return;
            }
        }

        private bool TryCreatePatrolPath()
        {
            if (Time.time < _nextPathSearchTime ||
                !NavMeshPointFinder.TryFind(
                    _patrolCenter,
                    _patrolRadius,
                    _searchAttempts,
                    out var destination) ||
                !Navigation.TrySetDestination(destination))
            {
                _hasDestination = false;
                _nextPathSearchTime = Time.time + Config.ComputerPathRetryDelay;
                return false;
            }

            _hasDestination = true;
            return true;
        }
    }
}
