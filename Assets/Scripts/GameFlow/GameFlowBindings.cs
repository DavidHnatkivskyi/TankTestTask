using GameFlow.Signals;
using Zenject;

namespace GameFlow
{
    /// <summary>
    /// Declares the signals that describe the gameplay session flow.
    /// </summary>
    public static class GameFlowBindings
    {
        public static void Install(DiContainer container)
        {
            container.Bind<IGameExitService>().To<GameExitService>().AsSingle();
            container.DeclareSignal<GameplayStartedSignal>().OptionalSubscriber();
            container.DeclareSignal<PlayerTankDestroyedSignal>().OptionalSubscriber();
            container.DeclareSignal<PlayerRespawnRequestedSignal>().OptionalSubscriber();
            container.DeclareSignal<PlayerTankRespawnedSignal>().OptionalSubscriber();
        }
    }
}
