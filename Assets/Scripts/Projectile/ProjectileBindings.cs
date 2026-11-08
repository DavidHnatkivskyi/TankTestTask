using Config;
using Zenject;

namespace Projectile
{
    /// <summary>
    /// Installs projectile simulation.
    /// </summary>
    public static class ProjectileBindings
    {
        public static void Install(DiContainer container, ProjectileConfig config)
        {
            container.BindInstance(config).AsSingle();

            container.BindInterfacesAndSelfTo<ProjectileSimulation>().AsSingle().NonLazy();
            container.BindInitializableExecutionOrder<ProjectileSimulation>(-100);
            container.BindTickableExecutionOrder<ProjectileSimulation>(100);
        }
    }
}
