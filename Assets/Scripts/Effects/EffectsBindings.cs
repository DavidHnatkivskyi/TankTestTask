using Zenject;

namespace Effects
{
    /// <summary>
    /// Installs effect spawning and the signal that requests an effect.
    /// </summary>
    public static class EffectsBindings
    {
        public static void Install(DiContainer container, EffectConfig config)
        {
            container.DeclareSignal<EffectSpawnRequested>();
            container.BindInstance(config).AsSingle();

            container.BindInterfacesAndSelfTo<EffectSpawner>().AsSingle().NonLazy();
            container.BindInitializableExecutionOrder<EffectSpawner>(-100);
            container.BindTickableExecutionOrder<EffectSpawner>(100);
        }
    }
}
