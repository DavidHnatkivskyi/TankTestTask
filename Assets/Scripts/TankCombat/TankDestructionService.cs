using Effects;
using GameFlow.Signals;
using Projectile;
using TankSpawning;
using UnityEngine;
using Zenject;

namespace TankCombat
{
    internal sealed class TankDestructionService : IProjectileImpactHandler, ITickable
    {
        private readonly ITankSpawner _tankSpawner;
        private readonly TankRuntimeRegistry _runtimeRegistry;
        private readonly SignalBus _signalBus;
        private readonly ComputerTankRespawner _computerTankRespawner;
        private readonly TankCollisionDetector _collisionDetector = new();

        public TankDestructionService(
            ITankSpawner tankSpawner,
            TankRuntimeRegistry runtimeRegistry,
            SignalBus signalBus,
            ComputerTankRespawner computerTankRespawner)
        {
            _tankSpawner = tankSpawner;
            _runtimeRegistry = runtimeRegistry;
            _signalBus = signalBus;
            _computerTankRespawner = computerTankRespawner;
        }

        public void Tick()
        {
            if (_runtimeRegistry.ActiveHandles.Count < 2)
            {
                return;
            }

            if (!_collisionDetector.TryFindCollidingTanks(
                    _runtimeRegistry.ActiveHandles,
                    out var firstTank,
                    out var secondTank))
            {
                return;
            }

            TryDestroy(firstTank);
            TryDestroy(secondTank);
        }

        public bool TryHandleImpact(Collider collider)
        {
            return _runtimeRegistry.TryFind(collider, out var handle) && TryDestroy(handle);
        }

        private bool TryDestroy(TankSpawnHandle handle)
        {
            var explosionPosition = handle.Chassis.Position;
            var explosionRotation = Quaternion.LookRotation(handle.Chassis.Forward);
            if (!_tankSpawner.Despawn(handle))
            {
                return false;
            }

            _signalBus.Fire(new EffectSpawnRequested(
                EffectType.TankExplosion,
                explosionPosition,
                explosionRotation));
            if (handle.ControlType == TankControlType.LocalPlayer)
            {
                _signalBus.Fire(new PlayerTankDestroyedSignal());
            }
            else
            {
                _computerTankRespawner.Schedule(handle.SpawnData);
            }

            return true;
        }
    }
}
