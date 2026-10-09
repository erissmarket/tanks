using TankGame.Core;
using TankGame.Projectiles;
using UnityEngine;

namespace TankGame.Testing
{
    public sealed class BackendTestDiagnostics : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager = null;
        [SerializeField] private TurnManager turnManager = null;
        [SerializeField] private MatchFlowController matchFlow = null;
        [SerializeField] private HealthComponent playerZeroHealth = null;
        [SerializeField] private HealthComponent playerOneHealth = null;
        private Projectile observedProjectile;

        private void OnEnable()
        {
            gameManager.StateChanged += OnStateChanged;
            turnManager.TurnChanged += OnTurnChanged;
            matchFlow.MatchEnded += OnMatchEnded;
            playerZeroHealth.HealthChanged += OnPlayerZeroHealthChanged;
            playerOneHealth.HealthChanged += OnPlayerOneHealthChanged;
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.StateChanged -= OnStateChanged;
            if (turnManager != null) turnManager.TurnChanged -= OnTurnChanged;
            if (matchFlow != null) matchFlow.MatchEnded -= OnMatchEnded;
            if (playerZeroHealth != null) playerZeroHealth.HealthChanged -= OnPlayerZeroHealthChanged;
            if (playerOneHealth != null) playerOneHealth.HealthChanged -= OnPlayerOneHealthChanged;
            if (!ReferenceEquals(observedProjectile, null)) observedProjectile.Completed -= OnProjectileCompleted;
        }

        private void OnStateChanged(MatchState previous, MatchState current)
        {
            Debug.Log($"[BackendTest] State: {previous} -> {current}");
            if (current == MatchState.Starting) Debug.Log("[BackendTest] Match started");
            if (current != MatchState.ProjectileFlying) return;
            if (!ReferenceEquals(observedProjectile, null)) observedProjectile.Completed -= OnProjectileCompleted;
            observedProjectile = FindAnyObjectByType<Projectile>();
            if (observedProjectile != null)
            {
                observedProjectile.Completed += OnProjectileCompleted;
                Debug.Log($"[BackendTest] Projectile fired by player {observedProjectile.OwnerPlayerIndex}");
            }
        }

        private void OnProjectileCompleted(Projectile projectile)
        {
            projectile.Completed -= OnProjectileCompleted;
            observedProjectile = null;
            Debug.Log($"[BackendTest] Projectile completed for player {projectile.OwnerPlayerIndex}");
        }

        private void OnTurnChanged(int index) => Debug.Log($"[BackendTest] Turn: player {index}");
        private void OnPlayerZeroHealthChanged(float current, float max) => Debug.Log($"[BackendTest] Player0 health: {current}/{max}");
        private void OnPlayerOneHealthChanged(float current, float max) => Debug.Log($"[BackendTest] Player1 health: {current}/{max}");
        private void OnMatchEnded(MatchResult result)
        {
            Debug.Log(result == null ? "[BackendTest] Match ended without result"
                : result.IsDraw ? "[BackendTest] Match ended: draw"
                : $"[BackendTest] Match ended: winner={result.WinnerPlayerIndex}, loser={result.LoserPlayerIndex}");
        }
    }
}
