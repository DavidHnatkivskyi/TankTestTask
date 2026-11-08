using System.Collections.Generic;
using MapGeneration;
using Tank;
using UnityEngine;
using Zenject;

namespace TankControl
{
    /// <summary>
    /// Owns the lifecycle of computer-tank brains registered by the tank spawning module.
    /// Each brain keeps its own chassis, turret, NavMesh agent and behavior state.
    /// </summary>
    public sealed class ComputerTankControl :
        ITickable,
        IComputerTankControlTargetRegistry,
        IComputerTankMovementRestorer
    {
        private readonly ComputerTankContext _context;
        private readonly Dictionary<ITankChassis, IComputerTank> _tanks = new();

        private IPositionProvider _playerTarget;

        public ComputerTankControl(
            IComputerTankSettings config,
            INavMeshPointSampler navMeshPointFinder)
        {
            _context = new ComputerTankContext(config, navMeshPointFinder);
        }

        public void SetPlayerTarget(IPositionProvider target)
        {
            _playerTarget = target;
        }

        public void ClearPlayerTarget(IPositionProvider target)
        {
            if (ReferenceEquals(_playerTarget, target))
            {
                _playerTarget = null;
            }
        }

        public void RegisterComputer(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankBehavior behavior)
        {
            _tanks[chassis] = CreateBehavior(chassis, turret, navigation, behavior);
        }

        public bool TryRestoreMovementTarget(ITankChassis chassis, Vector3 destination)
        {
            return chassis != null &&
                   _tanks.TryGetValue(chassis, out var tank) &&
                   tank.TryRestoreMovementTarget(destination);
        }

        public void UnregisterComputer(ITankChassis chassis)
        {
            _tanks.Remove(chassis);
        }

        public void Tick()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            foreach (var tank in _tanks.Values)
            {
                tank.Tick(_playerTarget, deltaTime);
            }
        }

        private IComputerTank CreateBehavior(
            ITankChassis chassis,
            ITurret turret,
            IComputerTankNavigation navigation,
            ComputerTankBehavior behavior)
        {
            return behavior switch
            {
                ComputerTankBehavior.ChaseAndOrbit => new ChaseAndOrbitTank(chassis, turret, navigation, _context),
                _ => new RandomPatrolTank(chassis, turret, navigation, _context)
            };
        }
    }
}
