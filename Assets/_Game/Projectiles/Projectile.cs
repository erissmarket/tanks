using System;
using UnityEngine;

namespace TankGame.Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float maximumLifetime = 10f;
        private Rigidbody2D body;
        private IProjectileImpactHandler impactHandler;
        private float damage;
        private float explosionRadius;
        private float elapsedLifetime;
        private bool initialized;
        private bool finished;
        private bool completionRaised;
        public ShotContext Shot { get; private set; }
        public float MinimumDamageFraction { get; private set; } = 1f;
        public float ShieldPenetration { get; private set; }

        public void ConfigureDamage(float minimumFraction, float penetration)
        {
            if (!float.IsFinite(minimumFraction) || !float.IsFinite(penetration))
                throw new ArgumentOutOfRangeException(nameof(minimumFraction));
            MinimumDamageFraction = Mathf.Clamp01(minimumFraction);
            ShieldPenetration = Mathf.Clamp01(penetration);
        }

        public int OwnerPlayerIndex { get; private set; } = -1;
        public bool IsInitialized => initialized;
        public bool IsFinished => finished;
        public float Damage => damage;
        public float ExplosionRadius => explosionRadius;
        public float ElapsedLifetime => elapsedLifetime;
        public float MaximumLifetime => maximumLifetime;
        public Rigidbody2D Body => body != null ? body : (body = GetComponent<Rigidbody2D>());

        public event Action<Projectile> Completed;
        public static event Action<Projectile> Spawned;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            impactHandler = GetComponent<IProjectileImpactHandler>();
            ValidateLifetime();
        }

        public void Initialize(int ownerPlayerIndex, float damage, float explosionRadius, Vector2 launchVelocity, ShotContext shot = null)
        {
            if (initialized || finished)
                throw new InvalidOperationException("A projectile can only be initialized once.");
            if (ownerPlayerIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(ownerPlayerIndex));
            if (!IsNonNegativeFinite(damage))
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (!IsNonNegativeFinite(explosionRadius))
                throw new ArgumentOutOfRangeException(nameof(explosionRadius));
            if (float.IsNaN(launchVelocity.x) || float.IsInfinity(launchVelocity.x)
                || float.IsNaN(launchVelocity.y) || float.IsInfinity(launchVelocity.y))
                throw new ArgumentOutOfRangeException(nameof(launchVelocity));

            if (body == null)
                body = GetComponent<Rigidbody2D>();
            if (impactHandler == null)
                impactHandler = GetComponent<IProjectileImpactHandler>();

            if (shot != null && (shot.OwnerPlayerIndex != ownerPlayerIndex || shot.IsCancelled))
                throw new ArgumentException("Child projectile must retain a valid parent shot.", nameof(shot));
            Shot = shot != null ? shot : new ShotContext(ownerPlayerIndex);
            Shot.Add(this);
            OwnerPlayerIndex = ownerPlayerIndex;
            this.damage = damage;
            this.explosionRadius = explosionRadius;
            elapsedLifetime = 0f;
            initialized = true;
            body.linearVelocity = launchVelocity;
            Spawned?.Invoke(this);
        }

        private void Update()
        {
            if (!initialized || finished)
                return;

            if (Shot != null && !Shot.Contains(transform.position)) { CompleteAndDestroy(); return; }
            elapsedLifetime += Time.deltaTime;
            if (elapsedLifetime >= maximumLifetime)
            {
                finished = true;
                if (body != null)
                    body.simulated = false;
                CompleteAndDestroy();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!initialized || finished)
                return;

            if (impactHandler == null)
                impactHandler = GetComponent<IProjectileImpactHandler>();

            // Delegate to custom impact handler if present
            if (impactHandler != null)
            {
                bool shouldFinish = impactHandler.HandleImpact(this, collision);
                if (!shouldFinish)
                    return; // Projectile continues flight/bouncing/burrowing
            }

            // Standard final explosion & destruction
            finished = true;
            if (body != null)
                body.simulated = false;

            Vector2 impactPosition = collision.contactCount > 0
                ? collision.GetContact(0).point : (Vector2)transform.position;
            try
            {
                ExplosionDamage.Apply(impactPosition, damage, explosionRadius, this);
            }
            finally
            {
                CompleteAndDestroy();
            }
        }

        /// <summary>
        /// Explicitly processes collision logic (used by automated tests and custom collision dispatchers).
        /// </summary>
        public void ProcessCollision(Collision2D collision)
        {
            OnCollisionEnter2D(collision);
        }

        /// <summary>
        /// Manually triggers an explosion at the specified world position using this projectile's damage & radius.
        /// </summary>
        public void TriggerExplosion(Vector2 center)
        {
            TriggerExplosion(center, damage, explosionRadius);
        }

        /// <summary>
        /// Manually triggers an explosion at the specified position with custom damage and radius.
        /// </summary>
        public void TriggerExplosion(Vector2 center, float customDamage, float customRadius)
        {
            if (!initialized || finished || (Shot != null && Shot.IsCancelled)) return;
            ExplosionDamage.Apply(center, customDamage, customRadius, this);
        }

        /// <summary>
        /// Completes lifecycle events and destroys the projectile game object.
        /// </summary>
        public void CompleteAndDestroy()
        {
            if (completionRaised) return;
            completionRaised = true;
            StopAllCoroutines();
            if (Shot != null) Shot.Remove(this);
            finished = true;
            if (body != null)
                body.simulated = false;

            try
            {
                Completed?.Invoke(this);
            }
            finally
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        public void CancelWithoutExplosion()
        {
            StopAllCoroutines();
            foreach (MonoBehaviour behavior in GetComponents<MonoBehaviour>())
                if (behavior != this) { behavior.StopAllCoroutines(); behavior.enabled = false; }
            CompleteAndDestroy();
        }
        private void OnDestroy() { if (Shot != null) Shot.Remove(this); }
        private void OnValidate() => ValidateLifetime();

        private void ValidateLifetime()
        {
            if (float.IsNaN(maximumLifetime) || float.IsInfinity(maximumLifetime))
                maximumLifetime = 10f;
            maximumLifetime = Mathf.Max(0.01f, maximumLifetime);
        }

        private static bool IsNonNegativeFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
