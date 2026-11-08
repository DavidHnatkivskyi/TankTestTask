using System.Collections.Generic;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Config/Projectile Catalog")]
    public sealed class ProjectileConfig : ScriptableObject
    {
        [SerializeField] private List<ProjectileSetConfig> projectileSets = new();

        public IReadOnlyList<ProjectileSetConfig> ProjectileSets => projectileSets;

        public bool Contains(ProjectileSetConfig projectileSet)
        {
            return projectileSet != null && projectileSets.Contains(projectileSet);
        }
    }
}
