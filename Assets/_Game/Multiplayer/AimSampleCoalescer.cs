using System;
using UnityEngine;

namespace TankGame.Multiplayer
{
    /// <summary>One replaceable intent, bounded send rate, and a refresh while input is held.</summary>
    public sealed class AimSampleCoalescer
    {
        public float Rate = 20f, AngleDeadband = .25f, PowerDeadband = .005f;
        private Vector2 desired, sent;
        private float power, sentPower;
        private bool offered, hasSent;
        private double nextSend, lastSent;
        public void Offer(Vector2 direction, float power)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(direction.sqrMagnitude)
                || Mathf.Abs(direction.sqrMagnitude - 1) > .01f || !float.IsFinite(power) || power < 0 || power > 1)
                throw new ArgumentOutOfRangeException(nameof(direction));
            desired = direction; this.power = power; offered = true;
        }
        public bool TryTake(double now, out Vector2 direction, out float selectedPower)
        {
            direction = desired; selectedPower = power;
            if (!offered || now < nextSend) return false;
            if (!double.IsFinite(now)) throw new ArgumentOutOfRangeException(nameof(now));
            if (hasSent && Vector2.Angle(sent, desired) < (float.IsFinite(AngleDeadband) ? Mathf.Clamp(AngleDeadband, 0, 2) : .25f)
                && Mathf.Abs(sentPower - power) < (float.IsFinite(PowerDeadband) ? Mathf.Clamp(PowerDeadband, 0, .1f) : .005f) && now - lastSent < .25) return false;
            sent = desired; sentPower = power; hasSent = true; lastSent = now;
            nextSend = now + 1d / (float.IsFinite(Rate) ? Mathf.Clamp(Rate, 10, 30) : 20); return true;
        }
        public void Reset() { offered = hasSent = false; nextSend = lastSent = 0; }
    }
}
