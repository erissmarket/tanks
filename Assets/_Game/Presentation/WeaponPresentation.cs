using TankGame.Data;
using TankGame.Projectiles;

namespace TankGame.Presentation
{
    public static class WeaponPresentation
    {
        public static string Damage(WeaponData weapon)
        {
            var cluster=weapon.ProjectilePrefab!=null?weapon.ProjectilePrefab.GetComponent<ClusterImpactHandler>():null;
            return cluster!=null?$"{cluster.ClusterCount} x {cluster.SubmunitionDamage:0}":weapon.BaseDamage.ToString("0");
        }
        public static float Radius(WeaponData weapon)
        {
            var cluster=weapon.ProjectilePrefab!=null?weapon.ProjectilePrefab.GetComponent<ClusterImpactHandler>():null;
            return cluster!=null?cluster.SubmunitionExplosionRadius:weapon.ExplosionRadius;
        }
    }
}
