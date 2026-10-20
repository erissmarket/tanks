using System;
using UnityEngine;

namespace TankGame.Presentation
{
    public static class AimPowerJoystick
    {
        public static bool TryMap(Vector2 displacement, float radius, float deadZone, out Vector2 direction, out float power)
        {
            direction = Vector2.zero; power = 0f;
            if (!float.IsFinite(displacement.x) || !float.IsFinite(displacement.y) || !float.IsFinite(displacement.sqrMagnitude)
                || !float.IsFinite(radius) || radius <= 0 || !float.IsFinite(deadZone) || deadZone < 0 || deadZone >= 1)
                throw new ArgumentOutOfRangeException(nameof(displacement));
            float magnitude = displacement.magnitude / radius;
            if (magnitude <= deadZone) return false;
            direction = displacement.normalized;
            power = Mathf.Clamp01((magnitude - deadZone) / (1f - deadZone));
            return true;
        }

        public static Vector2 Constrain(Vector2 worldDirection, bool mirrored, float bodyRotation, float minimum = 0, float maximum = 90)
        {
            if (!float.IsFinite(bodyRotation) || !float.IsFinite(worldDirection.x) || !float.IsFinite(worldDirection.y)
                || !float.IsFinite(worldDirection.sqrMagnitude) || worldDirection.sqrMagnitude < 0.000001f
                || !float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum < 0 || maximum > 90 || maximum < minimum)
                throw new ArgumentOutOfRangeException(nameof(worldDirection));
            Vector2 local = Quaternion.Euler(0, 0, -bodyRotation) * worldDirection;
            float sign = mirrored ? -1 : 1;
            float angle = Mathf.Clamp(Mathf.Atan2(local.y, local.x * sign) * Mathf.Rad2Deg, minimum, maximum) * Mathf.Deg2Rad;
            return (Vector2)(Quaternion.Euler(0, 0, bodyRotation) * new Vector2(sign * Mathf.Cos(angle), Mathf.Sin(angle)));
        }
    }
}
