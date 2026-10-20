using System;
using System.Collections;
using System.Collections.Generic;
using TankGame.Core;
using TankGame.Terrain;
using UnityEngine;

namespace TankGame.Projectiles
{
    /// <summary>
    /// Implements burrowing/tunneling behavior for projectiles.
    /// Drills into terrain upon impact, carving a subterranean tunnel before detonating underground.
    /// </summary>
    [RequireComponent(typeof(Projectile))]
    [DisallowMultipleComponent]
    public sealed class DiggerImpactHandler : MonoBehaviour, IProjectileImpactHandler
    {
        [Tooltip("Number of drilling steps executed into the soil.")]
        [SerializeField, Range(1, 20)] private int digSteps = 6;

        [Tooltip("Distance moved forward per drill step (in units).")]
        [SerializeField, Min(0.05f)] private float digStepDistance = 0.35f;

        [Tooltip("Radius of soil carved out at each drill step.")]
        [SerializeField, Min(0.1f)] private float digCarveRadius = 0.5f;

        [Tooltip("Time delay in seconds between drill steps for visual drilling effect.")]
        [SerializeField, Min(0f)] private float digStepInterval = 0.05f;

        [Tooltip("If true, tunnels along the impact trajectory vector; otherwise drills straight down.")]
        [SerializeField] private bool alignAlongVelocity = true;

        [Tooltip("If true, detonates immediately if striking a tank/IDamageable directly instead of drilling.")]
        [SerializeField] private bool explodeOnDirectTankHit = true;

        private bool isDigging;
        private Projectile projectile;

        public bool IsDigging => isDigging;

        public int DigSteps
        {
            get => digSteps;
            set => digSteps = Mathf.Clamp(value, 1, 20);
        }

        public float DigStepDistance
        {
            get => digStepDistance;
            set => digStepDistance = Mathf.Max(0.05f, value);
        }

        public float DigCarveRadius
        {
            get => digCarveRadius;
            set => digCarveRadius = Mathf.Max(0.1f, value);
        }

        public float DigStepInterval
        {
            get => digStepInterval;
            set => digStepInterval = Mathf.Max(0f, value);
        }

        public bool AlignAlongVelocity
        {
            get => alignAlongVelocity;
            set => alignAlongVelocity = value;
        }

        public bool ExplodeOnDirectTankHit
        {
            get => explodeOnDirectTankHit;
            set => explodeOnDirectTankHit = value;
        }

        public event Action<int, Vector2> DigStepCompleted;
        public event Action<Vector2> DigCompleted;

        private void Awake()
        {
            projectile = GetComponent<Projectile>();
        }

        public bool HandleImpact(Projectile proj, Collision2D collision)
        {
            if (isDigging)
                return false;

            // Direct tank hit triggers standard surface detonation
            if (explodeOnDirectTankHit && collision != null && collision.collider != null)
            {
                IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                    return true;
            }

            Vector2 inVelocity = proj.Body != null ? proj.Body.linearVelocity : Vector2.down;
            Vector2 drillDirection = Vector2.down;

            if (alignAlongVelocity && inVelocity.sqrMagnitude > 0.1f)
            {
                drillDirection = inVelocity.normalized;
            }
            else if (collision != null && collision.contactCount > 0)
            {
                drillDirection = -collision.GetContact(0).normal;
            }

            StartBurrowing(proj, drillDirection);
            return false; // Handled asynchronously, do not destroy projectile yet
        }

        /// <summary>
        /// Begins the underground burrowing sequence.
        /// </summary>
        public void StartBurrowing(Projectile proj, Vector2 direction)
        {
            if (isDigging)
                return;

            isDigging = true;
            if (projectile == null)
                projectile = proj;

            // Disable physics body and colliders while burrowing underground
            if (proj.Body != null)
            {
                proj.Body.simulated = false;
                proj.Body.linearVelocity = Vector2.zero;
            }

            Collider2D col = proj.GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            if (Application.isPlaying && digStepInterval > 0f)
            {
                StartCoroutine(BurrowRoutine(proj, direction.normalized));
            }
            else
            {
                // Synchronous execution for edit-mode tests or zero-interval drilling
                ExecuteDrillSynchronous(proj, direction.normalized);
            }
        }

        private IEnumerator BurrowRoutine(Projectile proj, Vector2 direction)
        {
            WaitForSeconds wait = new WaitForSeconds(digStepInterval);

            for (int i = 0; i < digSteps; i++)
            {
                StepDrill(proj, direction, i);
                yield return wait;
            }

            FinishDrill(proj);
        }

        /// <summary>
        /// Executes all drill steps synchronously (useful for automated testing or instant borer).
        /// </summary>
        public void ExecuteDrillSynchronous(Projectile proj, Vector2 direction)
        {
            for (int i = 0; i < digSteps; i++)
            {
                StepDrill(proj, direction, i);
            }
            FinishDrill(proj);
        }

        private void StepDrill(Projectile proj, Vector2 direction, int stepIndex)
        {
            if (proj == null)
                return;

            Vector3 newPos = proj.transform.position + (Vector3)(direction * digStepDistance);
            proj.transform.position = newPos;

            CarveTerrainAt(newPos, digCarveRadius);
            DigStepCompleted?.Invoke(stepIndex + 1, newPos);
        }

        private void FinishDrill(Projectile proj)
        {
            if (proj == null)
                return;

            Vector2 finalPos = proj.transform.position;
            DigCompleted?.Invoke(finalPos);

            try
            {
                proj.TriggerExplosion(finalPos);
            }
            finally
            {
                proj.CompleteAndDestroy();
            }
        }

        private static void CarveTerrainAt(Vector2 center, float radius)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
            if (colliders == null || colliders.Length == 0)
                return;

            for (int i = 0; i < colliders.Length; i++)
            {
                var terrain = colliders[i].GetComponentInParent<DestructibleTerrain>();
                if (terrain != null)
                {
                    terrain.Carve(center, radius);
                }
            }
        }

        private void OnValidate()
        {
            digSteps = Mathf.Clamp(digSteps, 1, 20);
            digStepDistance = Mathf.Max(0.05f, digStepDistance);
            digCarveRadius = Mathf.Max(0.1f, digCarveRadius);
            digStepInterval = Mathf.Max(0f, digStepInterval);
        }
    }
}
