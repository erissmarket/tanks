using System;
using UnityEngine;

namespace TankGame.Projectiles
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float maximumLifetime = 10f;
        private Rigidbody2D body;
        private float damage;
        private float explosionRadius;
        private float elapsedLifetime;
        private bool initialized;
        private bool finished;

        public int OwnerPlayerIndex { get; private set; } = -1;
        public bool IsInitialized => initialized;
        public bool IsFinished => finished;

        public event Action<Projectile> Completed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ValidateLifetime();
        }

        public void Initialize(int ownerPlayerIndex, float damage, float explosionRadius, Vector2 launchVelocity)
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
            OwnerPlayerIndex = ownerPlayerIndex;
            this.damage = damage;
            this.explosionRadius = explosionRadius;
            elapsedLifetime = 0f;
            initialized = true;
            body.linearVelocity = launchVelocity;
        }

        private void Update()
        {
            if (finished)
                return;

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

            // Set the guard before damage callbacks or deferred destruction can run.
            finished = true;
            body.simulated = false;
            Vector2 impactPosition = collision.contactCount > 0
                ? collision.GetContact(0).point : (Vector2)transform.position;
            try
            {
                ExplosionDamage.Apply(impactPosition, damage, explosionRadius);
            }
            finally
            {
                CompleteAndDestroy();
            }
        }

        private void CompleteAndDestroy()
        {
            try
            {
                Completed?.Invoke(this);
            }
            finally
            {
                Destroy(gameObject);
            }
        }

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
