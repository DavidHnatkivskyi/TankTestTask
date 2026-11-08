using UnityEngine;

namespace Projectile
{
    public interface IProjectileHitFilter
    {
        bool ShouldIgnore(Collider collider);
    }
}
