using System;
using System.Collections.Generic;
using TankGame.Core;
using TankGame.Terrain;
using UnityEngine;

namespace TankGame.Projectiles
{
    public static class ExplosionDamage
    {
        public static event Action<ShotContext, HealthComponent, float> DamageApplied;
        public static event Action<Vector2, float, Projectile> Detonated;

        /// <summary>Damages each target once, using its nearest collider for radial falloff.</summary>
        public static void Apply(Vector2 center, float damage, float radius, Projectile source = null)
        {
            if (float.IsNaN(center.x) || float.IsInfinity(center.x)
                || float.IsNaN(center.y) || float.IsInfinity(center.y))
                throw new ArgumentOutOfRangeException(nameof(center));
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage < 0f)
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));

            Detonated?.Invoke(center, radius, source);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
            var targets = new HashSet<IDamageable>();
            var distances = new Dictionary<IDamageable, float>();
            var terrains = new HashSet<DestructibleTerrain>();
            foreach (Collider2D collider in colliders)
            {
                var terrain = collider.GetComponentInParent<DestructibleTerrain>();
                if (terrain != null) terrains.Add(terrain);
                IDamageable target = collider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    targets.Add(target);
                    float distance = Vector2.Distance(center, collider.ClosestPoint(center));
                    if (!distances.TryGetValue(target, out float previous) || distance < previous)
                        distances[target] = distance;
                }
            }

            foreach (DestructibleTerrain terrain in terrains)
                terrain.Carve(center, radius);
            if (damage == 0f) return;

            foreach (IDamageable target in targets)
            {
                if (target is UnityEngine.Object unityTarget && unityTarget == null)
                    continue;
                HealthComponent targetHealth = target as HealthComponent;
                float before = targetHealth != null ? targetHealth.CurrentHealth : 0f;
                float minimum = source != null ? source.MinimumDamageFraction : 1f;
                float fraction = radius > 0f ? Mathf.Clamp01(distances[target] / radius) : 0f;
                float effectiveDamage = damage * Mathf.Lerp(1f, minimum, fraction);
                if (targetHealth != null)
                    targetHealth.ApplyDamage(effectiveDamage, source != null ? source.ShieldPenetration : 0f);
                else target.TakeDamage(effectiveDamage);
                if (source != null && source.Shot != null && targetHealth != null)
                {
                    float actual = Mathf.Max(0f, before - targetHealth.CurrentHealth);
                    if (actual > 0f) DamageApplied?.Invoke(source.Shot, targetHealth, actual);
                }
            }
        }
    }
}
