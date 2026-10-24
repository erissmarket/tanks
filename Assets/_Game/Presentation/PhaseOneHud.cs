using TankGame.Core;
using TankGame.Testing;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Presentation
{
    public sealed class PhaseOneHud : MonoBehaviour
    {
        [SerializeField] private MatchFlowController match;
        [SerializeField] private BackendTestBootstrap bootstrap;
        [SerializeField] private Text status;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Text pauseTitle;
        [SerializeField] private Button pauseButton, resumeButton, surrenderButton, confirmButton, backButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitle, resultDetails;
        [SerializeField] private Button restartButton;
        private bool confirming;
        private void OnEnable()
        {
            pauseButton.onClick.AddListener(Pause); resumeButton.onClick.AddListener(Resume);
            surrenderButton.onClick.AddListener(AskSurrender); confirmButton.onClick.AddListener(Surrender);
            backButton.onClick.AddListener(CancelSurrender); restartButton.onClick.AddListener(Restart);
        }
        private void OnDisable()
        {
            pauseButton.onClick.RemoveListener(Pause); resumeButton.onClick.RemoveListener(Resume);
            surrenderButton.onClick.RemoveListener(AskSurrender); confirmButton.onClick.RemoveListener(Surrender);
            backButton.onClick.RemoveListener(CancelSurrender); restartButton.onClick.RemoveListener(Restart);
        }
        private void Pause() { confirming = false; match.SetPaused(true); }
        private void Resume() { confirming = false; match.SetPaused(false); }
        private void AskSurrender() => confirming = true;
        private void CancelSurrender() => confirming = false;
        private void Surrender() { confirming = false; match.Surrender(0); }
        private void Restart() { confirming = false; if (bootstrap.CanRestart) bootstrap.RestartTestMatch(); }
        private void LateUpdate()
        {
            if (match == null) return;
            status.text = $"TURN {Mathf.Min(match.CompletedTurns+1,match.Rules.maximumTurns)}/{match.Rules.maximumTurns}    TIME {Mathf.CeilToInt(match.RemainingTurnSeconds)}s    MOVE {match.RemainingMovement:0.00}";
            pauseButton.interactable = match.IsRunning && !match.IsPaused;
            pausePanel.SetActive(match.IsPaused && match.IsRunning);
            pauseTitle.text = confirming ? "SURRENDER THIS MATCH?" : "PAUSED";
            resumeButton.gameObject.SetActive(!confirming); surrenderButton.gameObject.SetActive(!confirming);
            confirmButton.gameObject.SetActive(confirming); backButton.gameObject.SetActive(confirming);
            bool ended = !match.IsRunning && match.EndReason != MatchEndReason.None;
            resultPanel.SetActive(ended);
            if (!ended) return;
            var result = match.FinalResult;
            resultTitle.text = result == null ? "MATCH CANCELLED" : result.IsDraw ? "DRAW" : result.WinnerPlayerIndex == 0 ? "VICTORY" : "DEFEAT";
            resultDetails.text = $"SCORE  {match.GetScore(0)}\nReason: {match.EndReason}\nTurns completed: {match.CompletedTurns}";
            if(bootstrap.Progression!=null)
            {
                var progression=bootstrap.Progression;
                resultDetails.text=$"SCORE {match.GetScore(0)}  |  COINS +{progression.LastReward}\nWALLET {progression.Store.Coins}  |  {match.EndReason}\n"+
                    (progression.RewardSaved?"Progress saved":progression.Store.Status);
            }
        }
    }
}
