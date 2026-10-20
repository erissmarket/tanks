using System;
using TankGame.Data;
using UnityEngine;

namespace TankGame.AI
{
    public enum AIDifficulty { Easy, Normal, Hard }

    /// <summary>Provisional balancing values, sampled once per shot after the final muzzle correction.</summary>
    public readonly struct AIDifficultyProfile
    {
        public float AngleError { get; }
        public float SpeedVariation { get; }
        private AIDifficultyProfile(float angleError, float speedVariation)
        { AngleError = angleError; SpeedVariation = speedVariation; }

        public static AIDifficultyProfile For(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return new AIDifficultyProfile(10f, 0.12f);
                case AIDifficulty.Normal: return new AIDifficultyProfile(5f, 0.06f);
                case AIDifficulty.Hard: return new AIDifficultyProfile(1f, 0.015f);
                default: throw new ArgumentOutOfRangeException(nameof(difficulty));
            }
        }

        public float ApplyAngle(float solution, float signedSample, float minimum, float maximum)
        {
            if (!float.IsFinite(solution) || !float.IsFinite(signedSample) || !float.IsFinite(minimum)
                || !float.IsFinite(maximum) || minimum > maximum) throw new ArgumentOutOfRangeException(nameof(solution));
            return Mathf.Clamp(solution + Mathf.Clamp(signedSample, -1f, 1f) * AngleError, minimum, maximum);
        }

        public float GetPower(WeaponData weapon, float signedSample)
        {
            if (weapon == null) throw new ArgumentNullException(nameof(weapon));
            if (!float.IsFinite(signedSample)) throw new ArgumentOutOfRangeException(nameof(signedSample));
            float multiplier = Mathf.Clamp(1f + Mathf.Clamp(signedSample, -1f, 1f) * SpeedVariation,
                weapon.MinimumLaunchPower, weapon.MaximumLaunchPower);
            return weapon.MaximumLaunchPower == weapon.MinimumLaunchPower ? 1f
                : Mathf.Clamp01((multiplier - weapon.MinimumLaunchPower) / (weapon.MaximumLaunchPower - weapon.MinimumLaunchPower));
        }
    }
}
