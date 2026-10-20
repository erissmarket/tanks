using System;
using TankGame.Core;
using UnityEngine;

namespace TankGame.Projectiles
{
    /// <summary>
    /// Implements bouncing/ricochet behavior for projectiles.
    /// Deflects off terrain or obstacles up to maxBounces times before detonating.
    /// </summary>
    [RequireComponent(typeof(Projectile))]
    [DisallowMultipleComponent]
    public sealed class BouncyImpactHandler : MonoBehaviour, IProjectileImpactHandler
    {
        [Tooltip("Maximum number of bounces allowed before triggering final detonation.")]
        [SerializeField, Min(1)] private int maxBounces = 3;

        [Tooltip("Velocity retention multiplier after each bounce (0.1 = soft bounce, 1.0 = full elasticity).")]
        [SerializeField, Range(0.1f, 1f)] private float bounciness = 0.75f;

        [Tooltip("Minimum speed required to continue bouncing. If slower, detonates immediately.")]
        [SerializeField, Min(0.1f)] private float minBounceSpeed = 1.0f;

        [Tooltip("If true, detonates immediately upon direct collision with any IDamageable target (e.g., enemy tank).")]
        [SerializeField] private bool explodeOnTankHit = true;

        private int currentBounces;

        public int CurrentBounces => currentBounces;

        public int MaxBounces
        {
            get => maxBounces;
            set => maxBounces = Mathf.Max(1, value);
        }

        public float Bounciness
        {
            get => bounciness;
            set => bounciness = Mathf.Clamp(value, 0.05f, 1f);
        }

        public float MinBounceSpeed
        {
            get => minBounceSpeed;
            set => minBounceSpeed = Mathf.Max(0.1f, value);
        }

        public bool ExplodeOnTankHit
        {
            get => explodeOnTankHit;
            set => explodeOnTankHit = value;
        }

        public event Action<int, Vector2> Bounced;

        public void ResetBounces()
        {
            currentBounces = 0;
        }

        public bool HandleImpact(Projectile projectile, Collision2D collision)
        {
            if (projectile == null)
                return true;

            // Direct hit on damageable target (tank) triggers immediate explosion
            if (explodeOnTankHit && collision != null && collision.collider != null)
            {
                IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                    return true;
            }

            currentBounces++;
            if (currentBounces >= maxBounces)
                return true; // Exceeded maximum bounces, trigger final explosion

            Vector2 normal = Vector2.up;
            if (collision != null && collision.contactCount > 0)
                normal = collision.GetContact(0).normal;

            Rigidbody2D body = projectile.Body;
            if (body == null)
                return true;

            Vector2 inVelocity = body.linearVelocity;
            Vector2 reflected = Vector2.Reflect(inVelocity, normal) * bounciness;

            if (reflected.sqrMagnitude < minBounceSpeed * minBounceSpeed)
                return true; // Speed depleted below threshold, trigger explosion

            body.linearVelocity = reflected;
            Bounced?.Invoke(currentBounces, reflected);
            return false; // Keep projectile alive for next bounce
        }

        private void OnValidate()
        {
            maxBounces = Mathf.Max(1, maxBounces);
            bounciness = Mathf.Clamp(bounciness, 0.05f, 1f);
            minBounceSpeed = Mathf.Max(0.1f, minBounceSpeed);
        }
    }
}
