using MapGeneration;
using Tank;
using UnityEngine;

namespace TankControl
{
    internal interface IComputerTank
    {
        void Tick(IPositionProvider player, float deltaTime);

        bool TryRestoreMovementTarget(Vector3 destination);
    }

    internal abstract class ComputerTank : IComputerTank
    {
        protected readonly ITankChassis Chassis;
        protected readonly ITurret Turret;
        protected readonly IComputerTankNavigation Navigation;
        protected readonly IComputerTankSettings Config;
        protected readonly INavMeshPointSampler NavMeshPointFinder;
        private readonly float _fireDistance;

        protected ComputerTank(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankContext context)
        {
            Chassis = chassis;
            Turret = turret;
            Navigation = navigation;
            Config = context.Settings;
            NavMeshPointFinder = context.NavMeshPointFinder;
            _fireDistance = Random.Range(
                Mathf.Min(Config.ComputerFireMinimumDistance, Config.ComputerFireMaximumDistance),
                Mathf.Max(Config.ComputerFireMinimumDistance, Config.ComputerFireMaximumDistance));
        }

        public abstract void Tick(IPositionProvider player, float deltaTime);

        public virtual bool TryRestoreMovementTarget(Vector3 destination)
        {
            return Navigation.TrySetDestination(destination);
        }

        protected void AimAndFire(IPositionProvider player, float deltaTime)
        {
            var playerPosition = player.Position;
            Turret.RotateTowards(playerPosition, deltaTime);

            var direction = playerPosition - Turret.Position;
            direction.y = 0f;
            var distance = direction.magnitude;
            if (direction.sqrMagnitude > Mathf.Epsilon &&
                distance <= _fireDistance &&
                Vector3.Angle(Turret.Forward, direction) <= Config.ComputerFireAlignmentAngle)
            {
                Turret.TryFire();
            }
        }

        protected static float GetHorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }
    }
}
