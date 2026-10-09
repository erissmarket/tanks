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

        public string WeaponId => weaponId;
        public string WeaponName => weaponName;
        public Projectile ProjectilePrefab => projectilePrefab;
        public float BaseDamage => baseDamage;
        public float LaunchSpeed => launchSpeed;
        public float ExplosionRadius => explosionRadius;

        private void OnEnable() => ValidateValues();
        private void OnValidate() => ValidateValues();

        private void ValidateValues()
        {
            baseDamage = NonNegativeFinite(baseDamage);
            launchSpeed = NonNegativeFinite(launchSpeed);
            explosionRadius = NonNegativeFinite(explosionRadius);
        }

        private static float NonNegativeFinite(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }
    }
}
