using System.Collections.Generic;
using TankSpawning;
using UnityEngine;

namespace TankCombat
{
    /// <summary>
    /// Finds the first pair of active tanks whose colliders overlap.
    /// </summary>
    internal sealed class TankCollisionDetector
    {
        private readonly List<TankSpawnHandle> _candidates = new();

        public bool TryFindCollidingTanks(
            IReadOnlyCollection<TankSpawnHandle> activeHandles,
            out TankSpawnHandle firstTank,
            out TankSpawnHandle secondTank)
        {
            _candidates.Clear();
            _candidates.AddRange(activeHandles);
            var hasSyncedTransforms = false;

            for (var firstIndex = 0; firstIndex < _candidates.Count - 1; firstIndex++)
            {
                var firstCandidate = _candidates[firstIndex];
                for (var secondIndex = firstIndex + 1; secondIndex < _candidates.Count; secondIndex++)
                {
                    var secondCandidate = _candidates[secondIndex];
                    if (!AreWithinReach(firstCandidate, secondCandidate))
                    {
                        continue;
                    }

                    if (!hasSyncedTransforms)
                    {
                        Physics.SyncTransforms();
                        hasSyncedTransforms = true;
                    }

                    if (!firstCandidate.Instance.IsOverlapping(secondCandidate.Instance))
                    {
                        continue;
                    }

                    firstTank = firstCandidate;
                    secondTank = secondCandidate;
                    return true;
                }
            }

            firstTank = null;
            secondTank = null;
            return false;
        }

        private static bool AreWithinReach(TankSpawnHandle first, TankSpawnHandle second)
        {
            var reach = first.Instance.GetBoundingRadius() + second.Instance.GetBoundingRadius();
            return (first.Instance.transform.position - second.Instance.transform.position).sqrMagnitude <= reach * reach;
        }
    }
}
