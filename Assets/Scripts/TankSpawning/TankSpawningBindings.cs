using Zenject;

namespace TankSpawning
{
    /// <summary>
    /// Installs tank spawning and persistence services that are internal to this assembly.
    /// </summary>
    public static class TankSpawningBindings
    {
        public static void Install(DiContainer container, TankSpawningConfig config)
        {
            container.BindInterfacesAndSelfTo<TankSpawningConfig>().FromInstance(config).AsSingle();
            container.Bind<TankRuntimeRegistry>().AsSingle();
            container.Bind<TankControlRegistrationService>().AsSingle();
            container.BindInterfacesAndSelfTo<TankSpawner>().AsSingle();
            container.BindInterfacesTo<TankStatePersistence>().AsSingle();
            container.BindInterfacesAndSelfTo<ComputerTankRespawner>().AsSingle();
            container.Bind<InitialComputerTankSpawner>().AsSingle();
            container.BindInterfacesTo<InitialTankSpawner>().AsSingle().NonLazy();
            container.BindInitializableExecutionOrder<InitialTankSpawner>(-200);
            container.BindInterfacesTo<PlayerTankRespawnHandler>().AsSingle().NonLazy();
        }
    }
}
