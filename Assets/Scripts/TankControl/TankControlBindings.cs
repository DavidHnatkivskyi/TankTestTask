using Zenject;

namespace TankControl
{
    /// <summary>
    /// Installs player and computer tank control.
    /// </summary>
    public static class TankControlBindings
    {
        public static void Install(DiContainer container)
        {
            container.Bind<ITankMovementConstraint>().To<NavMeshMovementConstraint>().AsSingle();

            container.BindInterfacesAndSelfTo<UserTankControl>().AsSingle().NonLazy();
            container.BindInterfacesAndSelfTo<ComputerTankControl>().AsSingle().NonLazy();

            container.BindTickableExecutionOrder<UserTankControl>(0);
            container.BindTickableExecutionOrder<ComputerTankControl>(0);
        }
    }
}
