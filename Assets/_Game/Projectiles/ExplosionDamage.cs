using System;
using System.Collections.Generic;
using TankGame.Core;
using UnityEngine;

namespace TankGame.Projectiles
{
    public static class ExplosionDamage
    {
        /// <summary>Applies full damage once per damageable component found on a collider or its ancestors.</summary>
        public static void Apply(Vector2 center, float damage, float radius)
        {
            if (float.IsNaN(center.x) || float.IsInfinity(center.x)
                || float.IsNaN(center.y) || float.IsInfinity(center.y))
                throw new ArgumentOutOfRangeException(nameof(center));
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage < 0f)
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (damage == 0f)
                return;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
            var targets = new HashSet<IDamageable>();
            foreach (Collider2D collider in colliders)
            {
                IDamageable target = collider.GetComponentInParent<IDamageable>();
                if (target != null)
                    targets.Add(target);
            }

            foreach (IDamageable target in targets)
            {
                if (target is UnityEngine.Object unityTarget && unityTarget == null)
                    continue;
                target.TakeDamage(damage);
            }
        }
    }
}
