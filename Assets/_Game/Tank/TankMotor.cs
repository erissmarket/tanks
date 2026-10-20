using System;
using UnityEngine;

namespace TankGame.Tank
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TankMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float movementSpeed = 3f;
        [SerializeField, Min(0.1f)] private float acceleration = 7f;
        [SerializeField, Min(0.1f)] private float braking = 12f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.15f;
        [SerializeField, Range(0f, 80f)] private float maxSlopeAngle = 50f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.12f;
        [SerializeField, Min(1f)] private float alignmentSpeed = 100f;
        [SerializeField, Min(0f)] private float groundAdhesion = 0.8f;
        [SerializeField] private LayerMask groundLayers = Physics2D.DefaultRaycastLayers;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];
        private Rigidbody2D body;
        private float moveDirection;
        public bool IsGrounded { get; private set; }
        public Vector2 GroundNormal { get; private set; } = Vector2.up;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ValidateSettings();
        }

        /// <summary>Sets a sustained world-space direction in [-1, 1].</summary>
        public void Move(float direction)
        {
            if (float.IsNaN(direction) || float.IsInfinity(direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            moveDirection = isActiveAndEnabled ? Mathf.Clamp(direction, -1f, 1f) : 0f;
        }

        /// <summary>Immediately stops driven motion without cancelling a fall.</summary>
        public void Stop()
        {
            bool wasDriven = Mathf.Abs(moveDirection) > 0.001f;
            moveDirection = 0f;
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body == null || !wasDriven) return;
            Vector2 velocity = body.linearVelocity;
            velocity.x = 0f;
            body.linearVelocity = velocity;
        }

        private void FixedUpdate()
        {
            ProbeGround();
            Vector2 velocity = body.linearVelocity;
            float rate = Mathf.Abs(moveDirection) > 0.001f ? acceleration : braking;
            if (IsGrounded)
            {
                Vector2 tangent = new Vector2(GroundNormal.y, -GroundNormal.x);
                float speed = Mathf.MoveTowards(Vector2.Dot(velocity, tangent),
                    moveDirection * movementSpeed, rate * Time.fixedDeltaTime);
                float normalSpeed = Vector2.Dot(velocity, GroundNormal);
                // Keep upward impulses; a small downward bias follows descending hills.
                velocity = tangent * speed + GroundNormal * Mathf.Max(normalSpeed, -groundAdhesion);
                // Tracks supply traction against downhill gravity, including while braking.
                // The solver still applies gravity along the surface normal and while airborne.
                velocity -= tangent * Vector2.Dot(Physics2D.gravity * body.gravityScale, tangent)
                    * Time.fixedDeltaTime;
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                body.angularVelocity = 0f;
                body.MoveRotation(Mathf.MoveTowardsAngle(body.rotation, angle,
                    alignmentSpeed * Time.fixedDeltaTime));
            }
            else
            {
                // Gravity and vertical impulses remain under the physics solver's control.
                velocity.x = Mathf.MoveTowards(velocity.x, moveDirection * movementSpeed,
                    acceleration * airControl * Time.fixedDeltaTime);
            }
            body.linearVelocity = velocity;
        }

        private void ProbeGround()
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(groundLayers);
            filter.useTriggers = false;
            int count = body.Cast(Vector2.down, filter, groundHits, groundProbeDistance);
            float minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
            Vector2 normalSum = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = groundHits[i];
                if (hit.collider == null || hit.rigidbody == body || hit.normal.y < minNormalY)
                    continue;
                // Other dynamic bodies (including the other tank) are not terrain.
                if (hit.rigidbody != null && hit.rigidbody.bodyType == RigidbodyType2D.Dynamic)
                    continue;
                normalSum += hit.normal;
            }
            IsGrounded = normalSum.sqrMagnitude > 0.001f;
            GroundNormal = IsGrounded ? normalSum.normalized : Vector2.up;
            // Do not reattach a tank launched away from the ground by an impulse.
            if (IsGrounded && Vector2.Dot(body.linearVelocity, GroundNormal) > 2f)
                IsGrounded = false;
        }

        private void OnDisable()
        {
            Stop();
            IsGrounded = false;
        }

        private void OnValidate() => ValidateSettings();

        private static float Finite(float value, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;

        private void ValidateSettings()
        {
            movementSpeed = Mathf.Max(0f, Finite(movementSpeed, 3f));
            acceleration = Mathf.Max(0.1f, Finite(acceleration, 7f));
            braking = Mathf.Max(0.1f, Finite(braking, 12f));
            airControl = Mathf.Clamp01(Finite(airControl, 0.15f));
            maxSlopeAngle = Mathf.Clamp(Finite(maxSlopeAngle, 50f), 0f, 80f);
            groundProbeDistance = Mathf.Max(0.01f, Finite(groundProbeDistance, 0.12f));
            alignmentSpeed = Mathf.Max(1f, Finite(alignmentSpeed, 100f));
            groundAdhesion = Mathf.Max(0f, Finite(groundAdhesion, 0.8f));
        }
    }
}

