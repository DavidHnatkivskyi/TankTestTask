using GamePause.Signals;
using Zenject;

namespace GamePause
{
    /// <summary>
    /// Installs the pause service and the signal that requests a pause toggle.
    /// </summary>
    public static class PauseBindings
    {
        public static void Install(DiContainer container)
        {
            container.DeclareSignal<PauseRequestedSignal>().OptionalSubscriber();
            container.BindInterfacesAndSelfTo<GamePauseService>().AsSingle().NonLazy();
        }
    }
}
