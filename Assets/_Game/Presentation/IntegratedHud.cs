using TankGame.Core;
using TankGame.Testing;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Presentation
{
    public sealed class IntegratedHud : MonoBehaviour
    {
        [SerializeField] private HealthComponent playerHealth = null;
        [SerializeField] private HealthComponent enemyHealth = null;
        [SerializeField] private Image playerFill = null;
        [SerializeField] private Image enemyFill = null;
        [SerializeField] private Text playerLabel = null;
        [SerializeField] private Text enemyLabel = null;
        [SerializeField] private Text turnLabel = null;
        [SerializeField] private GameManager gameManager = null;
        [SerializeField] private TurnManager turnManager = null;
        [SerializeField] private MatchFlowController matchFlow = null;
        [SerializeField] private IntegratedInput input = null;
        [SerializeField] private Button fire = null;
        [SerializeField] private Button restart = null;

        private void OnEnable()
        {
            fire.onClick.AddListener(Fire);
            restart.onClick.AddListener(Restart);
        }

        private void OnDisable()
        {
            fire.onClick.RemoveListener(Fire);
            restart.onClick.RemoveListener(Restart);
        }

        private void Fire() => input.Fire();
        private void Restart() => input.Restart();

        private void LateUpdate()
        {
            UpdateHealth(playerHealth, playerFill, playerLabel, "PLAYER");
            UpdateHealth(enemyHealth, enemyFill, enemyLabel, "ENEMY");
            fire.interactable = input.CanFire;
            restart.interactable = input.CanRestart;
            if (gameManager.CurrentState == MatchState.MatchEnded)
                turnLabel.text = matchFlow.FinalResult == null ? "MATCH ENDED" : matchFlow.FinalResult.IsDraw
                    ? "DRAW" : matchFlow.FinalResult.WinnerPlayerIndex == 0 ? "PLAYER WINS" : "ENEMY WINS";
            else if (gameManager.CurrentState == MatchState.TurnActive)
                turnLabel.text = turnManager.CurrentPlayerIndex == 0 ? "PLAYER TURN" : "ENEMY TURN";
            else turnLabel.text = gameManager.CurrentState == MatchState.ProjectileFlying ? "SHOT IN FLIGHT" : "RESOLVING";
        }

        private static void UpdateHealth(HealthComponent health, Image fill, Text label, string prefix)
        {
            float fraction = health.MaxHealth > 0f ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) : 0f;
            fill.fillAmount = fraction;
            label.text = $"{prefix} {Mathf.CeilToInt(health.CurrentHealth)}/{health.MaxHealth:0}";
        }
    }
}
