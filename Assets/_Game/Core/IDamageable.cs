namespace TankGame.Core
{
    public interface IDamageable
    {
        /// <summary>Applies a finite, non-negative damage amount.</summary>
        void TakeDamage(float amount);
    }
}
