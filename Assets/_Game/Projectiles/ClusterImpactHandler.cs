using System;
using UnityEngine;

namespace TankGame.Projectiles
{
    /// <summary>
    /// Implements cluster bomb behavior.
    /// Splits into multiple smaller submunitions upon ground collision or mid-air airburst trigger.
    /// </summary>
    [RequireComponent(typeof(Projectile))]
    [DisallowMultipleComponent]
    public sealed class ClusterImpactHandler : MonoBehaviour, IProjectileImpactHandler
    {
        [Tooltip("Prefab for smaller submunitions released upon split.")]
        [SerializeField] private Projectile submunitionPrefab = null;

        [Tooltip("Number of submunitions spawned.")]
        [SerializeField, Range(2, 10)] private int clusterCount = 4;

        [Tooltip("Total fan spread arc angle in degrees for distributing submunitions.")]
        [SerializeField, Range(10f, 180f)] private float spreadAngle = 90f;

        [Tooltip("Ejection speed imparted to each submunition.")]
        [SerializeField, Min(0.1f)] private float ejectionSpeed = 8f;

        [Tooltip("Fraction of parent projectile's velocity inherited by submunitions.")]
        [SerializeField, Range(0f, 1f)] private float velocityInheritance = 0.25f;

        [Tooltip("If true, automatically hooks into ProjectileFlight.OnAirburstTriggered to split in mid-air.")]
        [SerializeField] private bool triggerOnAirburst = true;

        [Tooltip("Damage dealt by each submunition explosion.")]
        [SerializeField, Min(0f)] private float submunitionDamage = 15f;

        [Tooltip("Explosion radius of each submunition.")]
        [SerializeField, Min(0.1f)] private float submunitionExplosionRadius = 1.2f;

        private Projectile parentProjectile;
        private ProjectileFlight flight;
        private bool hasSplit;

        public bool HasSplit => hasSplit;

        public Projectile SubmunitionPrefab
        {
            get => submunitionPrefab;
            set => submunitionPrefab = value;
        }

        public int ClusterCount
        {
            get => clusterCount;
            set => clusterCount = Mathf.Clamp(value, 2, 10);
        }

        public float SpreadAngle
        {
            get => spreadAngle;
            set => spreadAngle = Mathf.Clamp(value, 10f, 180f);
        }

        public float EjectionSpeed
        {
            get => ejectionSpeed;
            set => ejectionSpeed = Mathf.Max(0.1f, value);
        }

        public float SubmunitionDamage
        {
            get => submunitionDamage;
            set => submunitionDamage = Mathf.Max(0f, value);
        }

        public float SubmunitionExplosionRadius
        {
            get => submunitionExplosionRadius;
            set => submunitionExplosionRadius = Mathf.Max(0.1f, value);
        }

        public bool TriggerOnAirburst
        {
            get => triggerOnAirburst;
            set => triggerOnAirburst = value;
        }

        public event Action<int> ClusterSplit;

        private void Awake()
        {
            parentProjectile = GetComponent<Projectile>();
            flight = GetComponent<ProjectileFlight>();
            if (triggerOnAirburst && flight != null)
            {
                flight.OnAirburstTriggered += OnFlightAirburst;
            }
        }

        private void OnDestroy()
        {
            if (flight != null)
            {
                flight.OnAirburstTriggered -= OnFlightAirburst;
            }
        }

        private void OnFlightAirburst(ProjectileFlight f)
        {
            if (hasSplit || parentProjectile == null || parentProjectile.IsFinished)
                return;

            Vector2 baseDir = parentProjectile.Body != null && parentProjectile.Body.linearVelocity.sqrMagnitude > 0.01f
                ? parentProjectile.Body.linearVelocity.normalized : Vector2.down;

            TriggerSplit(transform.position, baseDir);
            parentProjectile.CompleteAndDestroy();
        }

        public bool HandleImpact(Projectile projectile, Collision2D collision)
        {
            if (hasSplit)
                return true;

            Vector2 normal = Vector2.up;
            if (collision != null && collision.contactCount > 0)
                normal = collision.GetContact(0).normal;

            Vector2 spawnOrigin = (Vector2)transform.position + normal * 0.15f;
            TriggerSplit(spawnOrigin, normal);
            return true; // Destroy parent shell
        }

        /// <summary>
        /// Spawns the cluster submunitions dispersed along a fan arc.
        /// </summary>
        public void TriggerSplit(Vector2 origin, Vector2 outwardNormal)
        {
            if (hasSplit)
                return;

            hasSplit = true;
            if (parentProjectile == null)
                parentProjectile = GetComponent<Projectile>();

            if (submunitionPrefab == null)
            {
                ClusterSplit?.Invoke(0);
                return;
            }

            if (outwardNormal.sqrMagnitude < 0.0001f)
                outwardNormal = Vector2.up;
            outwardNormal = outwardNormal.normalized;

            float baseAngle = Mathf.Atan2(outwardNormal.y, outwardNormal.x) * Mathf.Rad2Deg;
            float halfSpread = spreadAngle * 0.5f;
            float startAngle = baseAngle - halfSpread;
            float angleStep = clusterCount > 1 ? spreadAngle / (clusterCount - 1) : 0f;

            Vector2 parentVel = (parentProjectile != null && parentProjectile.Body != null)
                ? parentProjectile.Body.linearVelocity : Vector2.zero;

            int ownerIndex = parentProjectile != null ? parentProjectile.OwnerPlayerIndex : 0;
            if (ownerIndex < 0) ownerIndex = 0;

            for (int i = 0; i < clusterCount; i++)
            {
                float currentAngle = startAngle + i * angleStep;
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector2 launchVel = dir * ejectionSpeed + parentVel * velocityInheritance;
                Quaternion rot = Quaternion.Euler(0f, 0f, currentAngle);

                Projectile sub = Instantiate(submunitionPrefab, origin, rot);
                sub.Initialize(ownerIndex, submunitionDamage, submunitionExplosionRadius, launchVel);
            }

            ClusterSplit?.Invoke(clusterCount);
        }

        private void OnValidate()
        {
            clusterCount = Mathf.Clamp(clusterCount, 2, 10);
            spreadAngle = Mathf.Clamp(spreadAngle, 10f, 180f);
            ejectionSpeed = Mathf.Max(0.1f, ejectionSpeed);
            submunitionDamage = Mathf.Max(0f, submunitionDamage);
            submunitionExplosionRadius = Mathf.Max(0.1f, submunitionExplosionRadius);
        }
    }
}
