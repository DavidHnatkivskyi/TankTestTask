using GameUI.Signals;
using Zenject;

namespace GameUI
{
    /// <summary>
    /// Installs the UI services and the signals owned by the UI module.
    /// </summary>
    public static class GameUIBindings
    {
        public static void Install(DiContainer container)
        {
            container.DeclareSignal<ShowStartWindowSignal>().OptionalSubscriber();
            container.DeclareSignal<StartWindowDismissedSignal>().OptionalSubscriber();
            container.DeclareSignal<ModalWindowVisibilityChangedSignal>().OptionalSubscriber();

            container.Bind<UISceneRoot>().FromComponentInHierarchy().AsSingle();
            container.BindInterfacesTo<UIManager>().AsSingle().NonLazy();
        }
    }
}
