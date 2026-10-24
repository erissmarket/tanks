using System;
using UnityEngine;
using TankGame.Data;

namespace TankGame.Core
{
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private ShieldData shield;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float CurrentShield { get; private set; }
        public float ArmorReduction { get; private set; }
        public void ConfigureTank(float health, float armor)
        {
            if(!float.IsFinite(health)||health<1||!float.IsFinite(armor)||armor<0||armor>.6f)
                throw new ArgumentOutOfRangeException(nameof(health));
            maxHealth=health;ArmorReduction=armor;ResetHealth();
        }
        public float MaxShield => shield != null ? shield.capacity : 0f;
        public ShieldData Shield => shield;
        public float ShieldMovementMultiplier => shield != null ? shield.movementMultiplier : 1f;
        public event Action<float, float> ShieldChanged;
        // Actual health loss, absorbed shield damage, and whether this hit broke the shield.
        public event Action<float, float, bool> Damaged;

        public void EquipShield(ShieldData value)
        {
            shield = value;
            CurrentShield = MaxShield;
            ShieldChanged?.Invoke(CurrentShield, MaxShield);
        }

        // Arguments are current health and maximum health.
        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            ResetHealth();
        }

        private void OnValidate()
        {
            ValidateMaxHealth();
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        }

        /// <summary>Restores full health and allows a new death event next match.</summary>
        public void ResetHealth()
        {
            ValidateMaxHealth();
            CurrentHealth = maxHealth;
            CurrentShield = MaxShield;
            ShieldChanged?.Invoke(CurrentShield, MaxShield);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            ApplyDamage(amount, 0f);
        }

        public void Eliminate() => ApplyDamage(CurrentHealth, 1f, true);

        public void ApplyDamage(float amount, float shieldPenetration, bool bypassArmor = false)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount), "Damage must be finite and non-negative.");
            if (!float.IsFinite(shieldPenetration) || shieldPenetration < 0f || shieldPenetration > 1f)
                throw new ArgumentOutOfRangeException(nameof(shieldPenetration));

            if (amount == 0f || CurrentHealth <= 0f)
                return;

            float previousHealth = CurrentHealth;
            float absorbed = Mathf.Min(CurrentShield, amount * (1f - shieldPenetration));
            CurrentShield -= absorbed;
            CurrentHealth = Mathf.Clamp(CurrentHealth - (amount - absorbed) * (bypassArmor ? 1f : 1f-ArmorReduction), 0f, maxHealth);
            Damaged?.Invoke(previousHealth - CurrentHealth, absorbed, absorbed > 0f && CurrentShield <= 0f);
            if (absorbed > 0f) ShieldChanged?.Invoke(CurrentShield, MaxShield);
            bool died = CurrentHealth == 0f;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            if (died)
                Died?.Invoke();
        }

        private void ValidateMaxHealth()
        {
            if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth))
                maxHealth = 100f;
            maxHealth = Mathf.Max(1f, maxHealth);
        }
    }
}
