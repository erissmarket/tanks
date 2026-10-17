using System;
using UnityEngine;

namespace TankGame.Tank
{
    public sealed class TankAim : MonoBehaviour
    {
        [SerializeField] private Transform aimPivot = null;
        [SerializeField, Range(0f, 90f)] private float minAimAngle;
        [SerializeField, Range(0f, 90f)] private float maxAimAngle = 90f;

        public float CurrentAimAngle { get; private set; }

        private void Awake()
        {
            ValidateLimits();
            if (aimPivot == null) aimPivot = transform.Find("TurretPivot/BarrelPivot");
            SetAimAngle(aimPivot != null ? Mathf.DeltaAngle(0f, aimPivot.localEulerAngles.z) : CurrentAimAngle);
        }

        /// <summary>Sets the pivot's local Z angle in degrees, clamped to configured limits.</summary>
        public void SetAimAngle(float angle)
        {
            if (float.IsNaN(angle) || float.IsInfinity(angle))
                throw new ArgumentOutOfRangeException(nameof(angle));

            CurrentAimAngle = Mathf.Clamp(angle, minAimAngle, maxAimAngle);
            if (aimPivot != null)
                aimPivot.localRotation = Quaternion.Euler(0f, 0f, CurrentAimAngle);
        }

        public Vector2 WorldAimDirection => aimPivot != null
            ? ((Vector2)aimPivot.TransformVector(Vector3.right)).normalized
            : ((Vector2)transform.TransformVector(Quaternion.Euler(0, 0, CurrentAimAngle) * Vector3.right)).normalized;

        public void SetWorldAimDirection(Vector2 direction)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude < 0.000001f)
                throw new ArgumentOutOfRangeException(nameof(direction));
            Transform reference = aimPivot != null && aimPivot.parent != null ? aimPivot.parent : transform;
            Vector2 local = reference.InverseTransformVector(direction);
            SetAimAngle(Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
        }

        private void OnValidate()
        {
            ValidateLimits();
            CurrentAimAngle = Mathf.Clamp(CurrentAimAngle, minAimAngle, maxAimAngle);
        }

        private void ValidateLimits()
        {
            if (float.IsNaN(minAimAngle) || float.IsInfinity(minAimAngle))
                minAimAngle = 0f;
            if (float.IsNaN(maxAimAngle) || float.IsInfinity(maxAimAngle))
                maxAimAngle = 90f;

            minAimAngle = Mathf.Clamp(minAimAngle, 0f, 90f);
            maxAimAngle = Mathf.Clamp(maxAimAngle, minAimAngle, 90f);
        }
    }
}
