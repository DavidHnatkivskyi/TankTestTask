namespace Projectile
{
    /// <summary>
    /// Internal bridge between a configured turret and projectile simulation.
    /// </summary>
    public interface IProjectileEmitter
    {
        void Emit(ProjectileShot shot);
    }
}
