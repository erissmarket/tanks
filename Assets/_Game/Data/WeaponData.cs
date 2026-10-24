using TankGame.Projectiles;
using UnityEngine;

namespace TankGame.Data
{
    [CreateAssetMenu(menuName = "TankGame/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string weaponId = "";
        [SerializeField] private string weaponName = "";
        [SerializeField,TextArea] private string description="";
        [SerializeField] private Sprite icon = null;
        [SerializeField] private Projectile projectilePrefab = null;
        [SerializeField, Min(0f)] private float baseDamage = 10f;
        [SerializeField, Min(0f)] private float launchSpeed = 10f;
        [SerializeField, Min(0f)] private float explosionRadius = 1f;
        [SerializeField, Min(-1)] private int ammunition = -1;
        [SerializeField, Range(0f, 1f)] private float minimumDamageFraction = 1f;
        [SerializeField, Range(0f, 1f)] private float shieldPenetration;

        public string WeaponId => weaponId;
        public string WeaponName => weaponName;
        public string Description=>description;
        public Sprite Icon => icon;
        public Projectile ProjectilePrefab => projectilePrefab;
        public float BaseDamage => baseDamage;
        public float LaunchSpeed => launchSpeed;
        public float ExplosionRadius => explosionRadius;
        public int Ammunition => ammunition;
        public float MinimumDamageFraction => minimumDamageFraction;
        public float ShieldPenetration => shieldPenetration;

        private void OnEnable() => ValidateValues();
        private void OnValidate() => ValidateValues();

        private void ValidateValues()
        {
            baseDamage = NonNegativeFinite(baseDamage);
            launchSpeed = NonNegativeFinite(launchSpeed);
            explosionRadius = NonNegativeFinite(explosionRadius);
            ammunition = Mathf.Max(-1, ammunition);
            minimumDamageFraction = Mathf.Clamp01(NonNegativeFinite(minimumDamageFraction));
            shieldPenetration = Mathf.Clamp01(NonNegativeFinite(shieldPenetration));
        }

        private static float NonNegativeFinite(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        }
    }
}
