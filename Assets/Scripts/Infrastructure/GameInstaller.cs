using CameraControl;
using Config;
using Effects;
using GameFlow;
using GamePause;
using GameInput;
using GameSaving;
using GameUI;
using MapGeneration;
using Projectile;
using TankCombat;
using TankControl;
using TankSpawning;
using UnityEngine;
using Zenject;

namespace Infrastructure
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private TankSpawningConfig tankSpawningConfig;
        [SerializeField] private ProjectileConfig projectileConfig;
        [SerializeField] private EffectConfig effectConfig;

        public override void InstallBindings()
        {
            if (gameCamera == null)
            {
                throw new System.InvalidOperationException("Assign the gameplay camera to GameInstaller.");
            }

            if (gameConfig == null)
            {
                throw new System.InvalidOperationException("Assign GameConfig to GameInstaller.");
            }

            SignalBusInstaller.Install(Container);

            PauseBindings.Install(Container);
            TankInputBindings.Install(Container);
            GameFlowBindings.Install(Container);
            GameUIBindings.Install(Container);

            Container.BindInstance(gameCamera).AsSingle();
            Container.BindInstance(gameConfig).AsSingle();
            MapGenerationBindings.Install(Container);

            TankControlBindings.Install(Container);
            CameraBindings.Install(Container);
            GameSavingBindings.Install(Container);

            Container.BindTickableExecutionOrder<TankInputHandler>(-100);

            InstallTankSpawning();
        }

        private void InstallTankSpawning()
        {
            if (tankSpawningConfig == null)
            {
                throw new System.InvalidOperationException("Assign TankSpawningConfig to GameInstaller.");
            }

            TankSpawningBindings.Install(Container, tankSpawningConfig);
            TankCombatBindings.Install(Container);
            InstallProjectileSimulation();
        }

        private void InstallProjectileSimulation()
        {
            if (projectileConfig == null)
            {
                throw new System.InvalidOperationException("Assign ProjectileConfig to GameInstaller.");
            }

            if (effectConfig == null)
            {
                throw new System.InvalidOperationException("Assign EffectConfig to GameInstaller.");
            }

            EffectsBindings.Install(Container, effectConfig);
            ProjectileBindings.Install(Container, projectileConfig);
        }
    }
}
