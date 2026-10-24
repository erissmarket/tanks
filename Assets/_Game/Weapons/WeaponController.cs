using System;
using System.Collections.Generic;
using TankGame.Data;
using TankGame.Projectiles;
using UnityEngine;

namespace TankGame.Weapons
{
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponData currentWeapon = null;

        public WeaponData CurrentWeapon => currentWeapon;
        private readonly Dictionary<WeaponData, int> roundsUsed = new Dictionary<WeaponData, int>();
        public int RemainingAmmo(WeaponData weapon)
        {
            if (weapon == null) return 0;
            if (weapon.Ammunition < 0) return -1;
            roundsUsed.TryGetValue(weapon, out int used);
            return Mathf.Max(0, weapon.Ammunition - used);
        }
        public bool CanFire => currentWeapon != null && currentWeapon.ProjectilePrefab != null && RemainingAmmo(currentWeapon) != 0;
        public void ResetAmmunition() => roundsUsed.Clear();

        // Assigning null clears the weapon slot.
        public void AssignWeapon(WeaponData weapon)
        {
            currentWeapon = weapon;
        }

        /// <summary>Launches from a world position along a normalized world direction.</summary>
        public Projectile Fire(int ownerPlayerIndex, Vector2 position, Vector2 direction)
        {
            if (ownerPlayerIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(ownerPlayerIndex));
            if (!IsFinite(position))
                throw new ArgumentOutOfRangeException(nameof(position));
            if (!IsFinite(direction) || direction.sqrMagnitude < 0.000001f || float.IsInfinity(direction.sqrMagnitude))
                throw new ArgumentOutOfRangeException(nameof(direction), "A finite, non-zero direction is required.");
            if (currentWeapon == null || currentWeapon.ProjectilePrefab == null)
                throw new InvalidOperationException("Assign a weapon with a projectile prefab before firing.");
            if (RemainingAmmo(currentWeapon) == 0) return null;

            Vector2 launchDirection = direction.normalized;
            float damage = currentWeapon.BaseDamage;
            float radius = currentWeapon.ExplosionRadius;
            Vector2 velocity = launchDirection * currentWeapon.LaunchSpeed;
            Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg);
            Projectile projectile = Instantiate(currentWeapon.ProjectilePrefab, position, rotation);
            try
            {
                projectile.Initialize(ownerPlayerIndex, damage, radius, velocity,
                    new ShotContext(ownerPlayerIndex) { VisualId = currentWeapon.WeaponId });
                projectile.ConfigureDamage(currentWeapon.MinimumDamageFraction, currentWeapon.ShieldPenetration);
                if (currentWeapon.Ammunition >= 0)
                {
                    roundsUsed.TryGetValue(currentWeapon, out int used);
                    roundsUsed[currentWeapon] = used + 1;
                }
            }
            catch
            {
                Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
                if (body != null) body.simulated = false;
                Destroy(projectile.gameObject);
                throw;
            }
            return projectile;
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }
    }
}
