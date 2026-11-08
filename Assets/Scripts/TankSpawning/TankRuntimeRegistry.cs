using System.Collections.Generic;
using UnityEngine;

namespace TankSpawning
{
    internal sealed class TankRuntimeRegistry
    {
        private readonly HashSet<TankSpawnHandle> _activeHandles = new();

        public IReadOnlyCollection<TankSpawnHandle> ActiveHandles => _activeHandles;

        public int ComputerPopulationVersion { get; private set; }

        public void Add(TankSpawnHandle handle)
        {
            _activeHandles.Add(handle);
        }

        public bool Remove(TankSpawnHandle handle)
        {
            return _activeHandles.Remove(handle);
        }

        public void InvalidateComputerPopulation()
        {
            ComputerPopulationVersion++;
        }

        public bool TryFind(Collider collider, out TankSpawnHandle tankHandle)
        {
            foreach (var handle in _activeHandles)
            {
                if (handle.Instance.ContainsCollider(collider))
                {
                    tankHandle = handle;
                    return true;
                }
            }

            tankHandle = null;
            return false;
        }
    }
}
