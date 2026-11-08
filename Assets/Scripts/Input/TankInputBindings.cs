using GameInput.Signals;
using Zenject;

namespace GameInput
{
    /// <summary>
    /// Installs the active input source and the handler shared by all sources.
    /// </summary>
    public static class TankInputBindings
    {
        public static void Install(DiContainer container)
        {
            container.Bind<KeyboardMouseTankInputSource>().AsSingle();
            container.BindInterfacesTo<SimpleInputTankInputSource>().AsSingle();
            InstallSharedBindings(container);
        }

        private static void InstallSharedBindings(DiContainer container)
        {
            container.Bind<IScreenPointProjector>().To<TopDownScreenPointProjector>().AsSingle();
            container.DeclareSignal<TankMovementInputSignal>().OptionalSubscriber();
            container.DeclareSignal<TankAimInputSignal>().OptionalSubscriber();
            container.DeclareSignal<TankFireInputSignal>().OptionalSubscriber();
            container.DeclareSignal<CameraModeSwitchRequestedSignal>().OptionalSubscriber();
            container.DeclareSignal<CameraViewModeRequestedSignal>().OptionalSubscriber();
            container.DeclareSignal<CameraViewModeChangedSignal>().OptionalSubscriber();
            container.DeclareSignal<CameraLookInputSignal>().OptionalSubscriber();
            container.BindInterfacesAndSelfTo<TankInputHandler>().AsSingle().NonLazy();
        }
    }
}
