using System;
using UnityEngine;

namespace TankGame.Tank
{
    public sealed class TankAim : MonoBehaviour
    {
        [SerializeField] private Transform aimPivot = null;
        [SerializeField, Range(-180f, 180f)] private float minAimAngle;
        [SerializeField, Range(-180f, 180f)] private float maxAimAngle = 180f;

        public float CurrentAimAngle { get; private set; }

        private void Awake()
        {
            ValidateLimits();
            SetAimAngle(CurrentAimAngle);
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
                maxAimAngle = 180f;

            minAimAngle = Mathf.Clamp(minAimAngle, -180f, 180f);
            maxAimAngle = Mathf.Clamp(maxAimAngle, minAimAngle, 180f);
        }
    }
}
