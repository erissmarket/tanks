using System;
using UnityEngine;

namespace TankGame.Core
{
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }

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
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f)
                throw new ArgumentOutOfRangeException(nameof(amount), "Damage must be finite and non-negative.");

            if (amount == 0f || CurrentHealth <= 0f)
                return;

            CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, maxHealth);
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
