using MapGeneration;

namespace TankControl
{
    /// <summary>
    /// Shared services used by every computer-controlled tank brain.
    /// </summary>
    internal sealed class ComputerTankContext
    {
        public ComputerTankContext(
            IComputerTankSettings settings,
            INavMeshPointSampler navMeshPointFinder)
        {
            Settings = settings;
            NavMeshPointFinder = navMeshPointFinder;
        }

        public IComputerTankSettings Settings { get; }

        public INavMeshPointSampler NavMeshPointFinder { get; }
    }
}
