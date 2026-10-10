using TankGame.Projectiles;
using UnityEngine;

namespace TankGame.Presentation
{
    [RequireComponent(typeof(Projectile), typeof(Rigidbody2D))]
    public sealed class ProjectilePresentation : MonoBehaviour
    {
        [SerializeField] private Transform visual = null;
        [SerializeField] private GameObject explosionVisual = null;
        private Rigidbody2D body;
        private Projectile projectile;
        private bool impactShown;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            projectile = GetComponent<Projectile>();
        }

        private void LateUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude > 0.000001f)
                visual.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (impactShown || !projectile.IsInitialized) return;
            impactShown = true;
            Vector2 position = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            GameObject effect = Instantiate(explosionVisual, position, Quaternion.identity);
            Animator animator = effect.GetComponent<Animator>();
            animator.Play("Base Layer.Explode", 0, 0f);
            animator.Update(0f);
            // Uses scaled time like the animator; the art clip stops at 0.5833333 seconds.
            Destroy(effect, 0.6f);
        }
    }
}
