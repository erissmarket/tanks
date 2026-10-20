using TankGame.Projectiles;
using UnityEngine;

namespace TankGame.Data
{
    [CreateAssetMenu(menuName = "TankGame/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string weaponId = "";
        [SerializeField] private string weaponName = "";
        [SerializeField] private Projectile projectilePrefab = null;
        [SerializeField, Min(0f)] private float baseDamage = 10f;
        [SerializeField, Min(0f)] private float launchSpeed = 10f;
        [SerializeField, Min(0f)] private float explosionRadius = 1f;
        [SerializeField, Range(0.05f, 1f)] private float minimumLaunchPower = 0.25f;
        [SerializeField, Range(1f, 2f)] private float maximumLaunchPower = 1f;

        public string WeaponId => weaponId;
        public string WeaponName => weaponName;
        public Projectile ProjectilePrefab => projectilePrefab;
        public float BaseDamage => baseDamage;
        public float LaunchSpeed => launchSpeed;
        public float ExplosionRadius => explosionRadius;
        public float MinimumLaunchPower => minimumLaunchPower;
        public float MaximumLaunchPower => maximumLaunchPower;
        public float BaseSpeedPower => maximumLaunchPower == minimumLaunchPower ? 1f
            : (1f - minimumLaunchPower) / (maximumLaunchPower - minimumLaunchPower);
        public float GetLaunchSpeed(float normalizedPower)
        {
            if (!float.IsFinite(normalizedPower) || normalizedPower < 0f || normalizedPower > 1f)
                throw new System.ArgumentOutOfRangeException(nameof(normalizedPower));
            return launchSpeed * Mathf.Lerp(minimumLaunchPower, maximumLaunchPower, normalizedPower);
        }

        private void OnEnable() => ValidateValues();
        private void OnValidate() => ValidateValues();

        private void ValidateValues()
        {
            baseDamage = NonNegativeFinite(baseDamage);
            launchSpeed = NonNegativeFinite(launchSpeed);
            explosionRadius = NonNegativeFinite(explosionRadius);
            minimumLaunchPower = float.IsFinite(minimumLaunchPower) ? Mathf.Clamp(minimumLaunchPower, 0.05f, 1f) : 0.25f;
            maximumLaunchPower = float.IsFinite(maximumLaunchPower) ? Mathf.Clamp(maximumLaunchPower, 1f, 2f) : 1f;
        }

        private static float NonNegativeFinite(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }
    }
}
