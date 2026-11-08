using UnityEngine;

namespace Projectile
{
    /// <summary>
    /// Handles a projectile hitting a collider owned by another module.
    /// </summary>
    public interface IProjectileImpactHandler
    {
        bool TryHandleImpact(Collider collider);
    }
}
