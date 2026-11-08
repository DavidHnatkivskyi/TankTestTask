using Zenject;

namespace TankCombat
{
    /// <summary>
    /// Installs tank destruction handling that is internal to the tank assembly.
    /// </summary>
    public static class TankCombatBindings
    {
        public static void Install(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<TankDestructionService>().AsSingle().NonLazy();
            container.BindTickableExecutionOrder<TankDestructionService>(50);
        }
    }
}
