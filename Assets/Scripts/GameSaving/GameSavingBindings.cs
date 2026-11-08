using Zenject;

namespace GameSaving
{
    /// <summary>
    /// Installs the game save service.
    /// </summary>
    public static class GameSavingBindings
    {
        public static void Install(DiContainer container)
        {
            container.BindInterfacesTo<GameSaveService>().AsSingle();
        }
    }
}
