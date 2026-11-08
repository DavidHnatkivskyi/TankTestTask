using Zenject;

namespace MapGeneration
{
    /// <summary>
    /// Installs the generated map reference and NavMesh point lookup.
    /// </summary>
    public static class MapGenerationBindings
    {
        public static void Install(DiContainer container)
        {
            container.Bind<GeneratedMap>().FromComponentInHierarchy().AsSingle();
            container.BindInterfacesTo<NavMeshSpawnPointFinder>().AsSingle();
        }
    }
}
