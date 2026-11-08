using Config;
using UnityEngine;

namespace Projectile
{
    public readonly struct ProjectileShot
    {
        public ProjectileShot(
            ProjectileSetConfig projectileSet,
            Vector3 origin,
            Vector3 direction,
            IProjectileHitFilter hitFilter)
        {
            ProjectileSet = projectileSet;
            Origin = origin;
            Direction = direction;
            HitFilter = hitFilter;
        }

        public ProjectileSetConfig ProjectileSet { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public IProjectileHitFilter HitFilter { get; }
    }
}
