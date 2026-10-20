using System;
using UnityEngine;

namespace TankGame.Projectiles
{
    /// <summary>
    /// Controls in-flight physics and rotational behavior of 2D projectiles.
    /// Supports automatic rotation along velocity, custom gravity scale, linear damping (drag),
    /// delayed rocket thrust acceleration, and mid-air detonation/splitting triggers (Airburst/Apex).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class ProjectileFlight : MonoBehaviour
    {
        [Header("Rotation Alignment")]
        [Tooltip("If enabled, the projectile rotates along its velocity vector.")]
        [SerializeField] private bool alignToVelocity = true;

        [Tooltip("Angular offset in degrees added to the velocity angle (e.g. 0 if sprite points Right, -90 if sprite points Up).")]
        [SerializeField] private float rotationOffset = 0f;

        [Tooltip("Rotation speed in degrees per second. Set to 0 for instant alignment.")]
        [SerializeField, Min(0f)] private float turnSpeed = 0f;

        [Tooltip("Minimum speed required before aligning rotation, preventing erratic spinning at near-zero velocity.")]
        [SerializeField, Min(0.0001f)] private float minSpeedThreshold = 0.1f;

        [Header("Physics Overrides")]
        [Tooltip("If enabled, overrides Rigidbody2D gravityScale with customGravityScale.")]
        [SerializeField] private bool overrideGravity = false;

        [Tooltip("Custom gravity scale (e.g. 0 for linear laser/railgun, >1 for heavy artillery).")]
        [SerializeField] private float customGravityScale = 1f;

        [Tooltip("If enabled, overrides Rigidbody2D linearDamping with customLinearDamping.")]
        [SerializeField] private bool overrideDrag = false;

        [Tooltip("Custom linear damping / air resistance (e.g. >0 for parachute/floating projectiles).")]
        [SerializeField, Min(0f)] private float customLinearDamping = 0f;

        [Header("Rocket Propulsion")]
        [Tooltip("If enabled, applies forward acceleration along the flight direction after thrustDelay.")]
        [SerializeField] private bool enableRocketThrust = false;

        [Tooltip("Delay in seconds after launch before the rocket engine ignites.")]
        [SerializeField, Min(0f)] private float thrustDelay = 0.2f;

        [Tooltip("Forward acceleration in units per second squared.")]
        [SerializeField, Min(0f)] private float thrustAcceleration = 15f;

        [Tooltip("Maximum velocity magnitude when propelled by rocket.")]
        [SerializeField, Min(0.1f)] private float maxSpeed = 30f;

        [Header("Mid-Air Triggering (Airburst / Apex)")]
        [Tooltip("If enabled, triggers mid-air action based on flight time or reaching trajectory apex.")]
        [SerializeField] private bool enableAirburst = false;

        [Tooltip("Flight time in seconds after launch to trigger mid-air action (when not using triggerOnApex).")]
        [SerializeField, Min(0.01f)] private float airburstTime = 1.5f;

        [Tooltip("If enabled, triggers automatically when the projectile reaches its trajectory apex (vertical velocity drops to/below 0).")]
        [SerializeField] private bool triggerOnApex = false;

        private Rigidbody2D body;
        private float elapsedFlightTime;
        private float previousVelocityY;
        private bool airburstTriggered;

        public event Action<ProjectileFlight> OnAirburstTriggered;

        public bool AlignToVelocity
        {
            get => alignToVelocity;
            set => alignToVelocity = value;
        }

        public float RotationOffset
        {
            get => rotationOffset;
            set => rotationOffset = value;
        }

        public float TurnSpeed
        {
            get => turnSpeed;
            set => turnSpeed = Mathf.Max(0f, value);
        }

        public float MinSpeedThreshold
        {
            get => minSpeedThreshold;
            set => minSpeedThreshold = Mathf.Max(0.0001f, value);
        }

        public bool OverrideGravity
        {
            get => overrideGravity;
            set
            {
                overrideGravity = value;
                ApplyPhysicsOverrides();
            }
        }

        public float CustomGravityScale
        {
            get => customGravityScale;
            set
            {
                customGravityScale = IsFinite(value) ? value : 1f;
                if (overrideGravity)
                    ApplyPhysicsOverrides();
            }
        }

        public bool OverrideDrag
        {
            get => overrideDrag;
            set
            {
                overrideDrag = value;
                ApplyPhysicsOverrides();
            }
        }

        public float CustomLinearDamping
        {
            get => customLinearDamping;
            set
            {
                customLinearDamping = IsFinite(value) ? Mathf.Max(0f, value) : 0f;
                if (overrideDrag)
                    ApplyPhysicsOverrides();
            }
        }

        public bool EnableRocketThrust
        {
            get => enableRocketThrust;
            set => enableRocketThrust = value;
        }

        public float ThrustDelay
        {
            get => thrustDelay;
            set => thrustDelay = IsFinite(value) ? Mathf.Max(0f, value) : 0f;
        }

        public float ThrustAcceleration
        {
            get => thrustAcceleration;
            set => thrustAcceleration = IsFinite(value) ? Mathf.Max(0f, value) : 0f;
        }

        public float MaxSpeed
        {
            get => maxSpeed;
            set => maxSpeed = IsFinite(value) ? Mathf.Max(0.1f, value) : 0.1f;
        }

        public bool EnableAirburst
        {
            get => enableAirburst;
            set => enableAirburst = value;
        }

        public float AirburstTime
        {
            get => airburstTime;
            set => airburstTime = IsFinite(value) ? Mathf.Max(0.01f, value) : 1.5f;
        }

        public bool TriggerOnApex
        {
            get => triggerOnApex;
            set => triggerOnApex = value;
        }

        public float ElapsedFlightTime => elapsedFlightTime;
        public bool IsThrusting => enableRocketThrust && elapsedFlightTime >= thrustDelay;
        public bool AirburstTriggered => airburstTriggered;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ValidateParameters();
            ApplyPhysicsOverrides();
        }

        private void FixedUpdate()
        {
            Step(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Resets the elapsed flight time and triggers.
        /// </summary>
        public void ResetFlightState()
        {
            elapsedFlightTime = 0f;
            previousVelocityY = 0f;
            airburstTriggered = false;
        }

        /// <summary>
        /// Applies configured gravity and damping overrides to the attached Rigidbody2D.
        /// </summary>
        public void ApplyPhysicsOverrides()
        {
            if (body == null)
                body = GetComponent<Rigidbody2D>();
            if (body == null)
                return;

            if (overrideGravity)
                body.gravityScale = customGravityScale;
            if (overrideDrag)
                body.linearDamping = customLinearDamping;
        }

        /// <summary>
        /// Steps flight logic (airburst triggers, rocket thrust, and rotation alignment) for a given deltaTime.
        /// Can be called manually in unit tests or custom update loops.
        /// </summary>
        public void Step(float deltaTime)
        {
            if (body == null)
                body = GetComponent<Rigidbody2D>();
            if (body == null)
                return;

            elapsedFlightTime += deltaTime;

            // Check mid-air triggering (Airburst / Apex split)
            if (enableAirburst && !airburstTriggered)
            {
                if (triggerOnApex)
                {
                    // Trigger when vertical velocity transitions from upward to zero/downward
                    if (previousVelocityY > 0.05f && body.linearVelocity.y <= 0f)
                    {
                        airburstTriggered = true;
                        OnAirburstTriggered?.Invoke(this);
                    }
                }
                else if (elapsedFlightTime >= airburstTime)
                {
                    airburstTriggered = true;
                    OnAirburstTriggered?.Invoke(this);
                }
            }
            previousVelocityY = body.linearVelocity.y;

            // Apply forward rocket propulsion if active
            if (enableRocketThrust && elapsedFlightTime >= thrustDelay && thrustAcceleration > 0f)
            {
                float headingRad = (body.rotation - rotationOffset) * Mathf.Deg2Rad;
                Vector2 forwardDir = new Vector2(Mathf.Cos(headingRad), Mathf.Sin(headingRad));
                Vector2 newVel = body.linearVelocity + forwardDir * (thrustAcceleration * deltaTime);
                if (newVel.sqrMagnitude > maxSpeed * maxSpeed)
                {
                    newVel = newVel.normalized * maxSpeed;
                }
                body.linearVelocity = newVel;
            }

            // Align rotation to velocity
            if (!alignToVelocity)
                return;

            Vector2 velocity = body.linearVelocity;
            float sqrSpeed = velocity.sqrMagnitude;
            if (sqrSpeed < minSpeedThreshold * minSpeedThreshold)
                return;

            float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + rotationOffset;

            if (turnSpeed <= 0f)
            {
                body.SetRotation(targetAngle);
            }
            else
            {
                float currentAngle = body.rotation;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * deltaTime);
                body.SetRotation(newAngle);
            }
        }

        private void OnValidate()
        {
            ValidateParameters();
            if (Application.isPlaying)
                ApplyPhysicsOverrides();
        }

        private void ValidateParameters()
        {
            if (!IsFinite(turnSpeed))
                turnSpeed = 0f;
            turnSpeed = Mathf.Max(0f, turnSpeed);

            if (!IsFinite(minSpeedThreshold))
                minSpeedThreshold = 0.1f;
            minSpeedThreshold = Mathf.Max(0.0001f, minSpeedThreshold);

            if (!IsFinite(rotationOffset))
                rotationOffset = 0f;

            if (!IsFinite(customGravityScale))
                customGravityScale = 1f;

            if (!IsFinite(customLinearDamping))
                customLinearDamping = 0f;
            customLinearDamping = Mathf.Max(0f, customLinearDamping);

            if (!IsFinite(thrustDelay))
                thrustDelay = 0.2f;
            thrustDelay = Mathf.Max(0f, thrustDelay);

            if (!IsFinite(thrustAcceleration))
                thrustAcceleration = 15f;
            thrustAcceleration = Mathf.Max(0f, thrustAcceleration);

            if (!IsFinite(maxSpeed))
                maxSpeed = 30f;
            maxSpeed = Mathf.Max(0.1f, maxSpeed);

            if (!IsFinite(airburstTime))
                airburstTime = 1.5f;
            airburstTime = Mathf.Max(0.01f, airburstTime);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
