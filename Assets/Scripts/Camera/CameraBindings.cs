using Zenject;

namespace CameraControl
{
    /// <summary>
    /// Installs the gameplay camera controller.
    /// </summary>
    public static class CameraBindings
    {
        public static void Install(DiContainer container)
        {
            container.BindInterfacesAndSelfTo<TankCameraController>().AsSingle().NonLazy();
            container.BindExecutionOrder<TankCameraController>(-200);
        }
    }
}
