using UnityEngine;

namespace TankGame.Projectiles
{
    /// <summary>
    /// Interface for modular projectile impact behaviors (bouncing, digging, cluster splitting, etc.).
    /// </summary>
    public interface IProjectileImpactHandler
    {
        /// <summary>
        /// Handles a 2D collision impact.
        /// </summary>
        /// <param name="projectile">The projectile that collided.</param>
        /// <param name="collision">Collision2D data from Unity physics.</param>
        /// <returns>True if the projectile should trigger final explosion and be destroyed; false if it continues flight.</returns>
        bool HandleImpact(Projectile projectile, Collision2D collision);
    }
}
