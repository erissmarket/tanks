using System;
using UnityEngine;

namespace TankGame.Tank
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TankMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float movementSpeed = 3f;
        private Rigidbody2D body;
        private float moveDirection;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ValidateSpeed();
        }

        /// <summary>Sets a sustained direction in [-1, 1]; use zero to stop.</summary>
        public void Move(float direction)
        {
            if (float.IsNaN(direction) || float.IsInfinity(direction))
                throw new ArgumentOutOfRangeException(nameof(direction));

            moveDirection = isActiveAndEnabled ? Mathf.Clamp(direction, -1f, 1f) : 0f;
        }

        /// <summary>Immediately stops horizontal movement while preserving vertical velocity.</summary>
        public void Stop()
        {
            moveDirection = 0f;
            if (body == null)
                body = GetComponent<Rigidbody2D>();

            if (body != null)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.x = 0f;
                body.linearVelocity = velocity;
            }
        }

        private void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = moveDirection * movementSpeed;
            body.linearVelocity = velocity;
        }

        private void OnDisable()
        {
            moveDirection = 0f;
            if (body != null)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.x = 0f;
                body.linearVelocity = velocity;
            }
        }

        private void OnValidate()
        {
            ValidateSpeed();
        }

        private void ValidateSpeed()
        {
            if (float.IsNaN(movementSpeed) || float.IsInfinity(movementSpeed))
                movementSpeed = 3f;
            movementSpeed = Mathf.Max(0f, movementSpeed);
        }
    }
}
