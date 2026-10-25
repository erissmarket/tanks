using System;
using System.Collections.Generic;
using TankGame.Player;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Terrain;
using UnityEngine;

namespace TankGame.Core
{
    public sealed class MatchFlowController : MonoBehaviour
    {
        [SerializeField] private MatchRules rules;
        [SerializeField] private LavaHazard lava;
        private MatchRules fallbackRules;
        private GameManager gameManager;
        private TurnManager turnManager;
        private readonly IPlayerController[] players = new IPlayerController[2];
        private readonly HealthComponent[] health = new HealthComponent[2];
        private readonly TankMotor[] motors = new TankMotor[2];
        private readonly float[] startingHealth = new float[2];
        private readonly float[] creditedDamage = new float[2];
        private readonly bool[] fallen = new bool[2];
        private readonly int[] finalScores = new int[2];
        private bool scoresFinalized;
        private ShotContext activeShot;
        private bool matchRunning, coordinating, endingMatch, matchEndedNotified, deathPending;
        private float shotElapsed, settleElapsed, savedTimeScale = 1f;
        public MatchResult FinalResult { get; private set; }
        public MatchEndReason EndReason { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsRunning => matchRunning;
        public MatchState State=>gameManager!=null?gameManager.CurrentState:MatchState.None;
        public int CompletedTurns { get; private set; }
        public int MatchNumber { get; private set; }
        public float RemainingTurnSeconds { get; private set; }
        public int ActiveShotProjectileCount => activeShot != null ? activeShot.ActiveCount : 0;
        public int CurrentPlayerIndex => turnManager != null ? turnManager.CurrentPlayerIndex : -1;
        public float RemainingMovement => CurrentPlayerIndex >= 0 && motors[CurrentPlayerIndex] != null
            ? motors[CurrentPlayerIndex].RemainingMovement : 0f;
        public MatchRules Rules
        {
            get
            {
                if (rules != null) return rules;
                if (fallbackRules == null) fallbackRules = ScriptableObject.CreateInstance<MatchRules>();
                return fallbackRules;
            }
        }
        public event Action<MatchResult> MatchEnded;

        public void ConfigureRules(MatchRules value)
        {
            if (matchRunning) throw new InvalidOperationException("Configure rules before starting the match.");
            if (value == null) throw new ArgumentNullException(nameof(value));
            rules = value; rules.Validate();
        }
        public void Initialize(GameManager gameManager, TurnManager turnManager, IPlayerController firstPlayer, IPlayerController secondPlayer)
        {
            if (matchRunning || coordinating || endingMatch) throw new InvalidOperationException("End the current match before initialization.");
            if (gameManager == null) throw new ArgumentNullException(nameof(gameManager));
            if (turnManager == null) throw new ArgumentNullException(nameof(turnManager));
            ValidatePlayer(firstPlayer); ValidatePlayer(secondPlayer);
            if (firstPlayer.PlayerIndex == secondPlayer.PlayerIndex) throw new ArgumentException("Player indices must be distinct.");
            Unsubscribe(); DisablePlayers();
            this.gameManager = gameManager; this.turnManager = turnManager;
            Array.Clear(health, 0, 2); Array.Clear(motors, 0, 2);
            players[firstPlayer.PlayerIndex] = firstPlayer; players[secondPlayer.PlayerIndex] = secondPlayer;
            DisablePlayers();
        }
        public void RegisterHealth(int playerIndex, HealthComponent healthComponent)
        {
            if (gameManager == null || matchRunning || coordinating || endingMatch) throw new InvalidOperationException("Register health after initialization and before starting.");
            if (playerIndex < 0 || playerIndex > 1) throw new ArgumentOutOfRangeException(nameof(playerIndex));
            if (healthComponent == null) throw new ArgumentNullException(nameof(healthComponent));
            if (health[playerIndex] != null || health[1-playerIndex] == healthComponent) throw new ArgumentException("Health registrations must be distinct and unique.");
            health[playerIndex] = healthComponent;
            motors[playerIndex] = healthComponent.GetComponent<TankMotor>();
        }
        public void StartMatch()
        {
            ValidateRegistration();
            if (!isActiveAndEnabled || matchRunning || coordinating || endingMatch
                || (gameManager.CurrentState != MatchState.None && gameManager.CurrentState != MatchState.MatchEnded))
                throw new InvalidOperationException("Start from None or MatchEnded after restoring both players.");
            if (health[0].CurrentHealth <= 0f || health[1].CurrentHealth <= 0f) throw new InvalidOperationException("Reset health before restarting.");
            Rules.Validate(); Unsubscribe();
            SetPaused(false);
            activeShot = null; deathPending = false; FinalResult = null; EndReason = MatchEndReason.None;
            CompletedTurns = 0; MatchNumber++; matchEndedNotified = false; scoresFinalized = false;
            Array.Clear(creditedDamage, 0, 2); Array.Clear(fallen, 0, 2);
            for (int i = 0; i < 2; i++) { startingHealth[i] = health[i].CurrentHealth; health[i].Died += OnPlayerDied; }
            ExplosionDamage.DamageApplied += OnDamageApplied;
            matchRunning = true; coordinating = true;
            try
            {
                DisablePlayers(); gameManager.ChangeState(MatchState.Starting);
                if (!matchRunning) return;
                turnManager.StartFirstTurn();
                if (matchRunning) ActivateCurrentTurn();
            }
            finally { coordinating = false; }
        }
        public void NotifyProjectileFired(Projectile projectile)
        {
            ValidateRegistration();
            if (!matchRunning || IsPaused || coordinating || endingMatch || gameManager.CurrentState != MatchState.TurnActive || activeShot != null)
                throw new InvalidOperationException("Only one shot can be accepted during the active turn.");
            if (projectile == null || !projectile.IsInitialized || projectile.IsFinished || !projectile.isActiveAndEnabled
                || projectile.OwnerPlayerIndex != CurrentPlayerIndex || projectile.Shot == null)
                throw new ArgumentException("Register an active projectile owned by the active player.", nameof(projectile));
            activeShot = projectile.Shot; shotElapsed = 0f;
            activeShot.SetBounds(Rules.minimumX-24f, Rules.maximumX+24f, Rules.killY-4f, 40f);
            DisablePlayers();
            gameManager.ChangeState(MatchState.ProjectileFlying);
        }
        private void Update() => AdvanceSimulation(Time.deltaTime);

        // A deterministic tick also lets isolated Play Mode tests cover long rules without waiting minutes.
        public void AdvanceSimulation(float deltaTime)
        {
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!matchRunning || IsPaused || coordinating || endingMatch) return;
            for (int i = 0; i < 2; i++)
            {
                if (health[i] == null || health[i].CurrentHealth <= 0f) continue;
                bool inLava = lava != null && lava.isActiveAndEnabled && lava.Contains(health[i]);
                if (inLava || health[i].transform.position.y < Rules.killY)
                {
                    fallen[i] = true;
                    if (inLava) lava.Engulf(health[i]);
                    health[i].Eliminate();
                }
            }
            if (!matchRunning) return;
            if (gameManager.CurrentState == MatchState.TurnActive)
            {
                if (deathPending) { BeginResolution(); return; }
                RemainingTurnSeconds = Mathf.Max(0f, RemainingTurnSeconds - deltaTime);
                if (RemainingTurnSeconds <= 0f) BeginResolution();
            }
            else if (gameManager.CurrentState == MatchState.ProjectileFlying)
            {
                shotElapsed += deltaTime;
                if (activeShot != null && shotElapsed >= Rules.shotTimeoutSeconds) activeShot.Cancel();
                if (activeShot == null || activeShot.ActiveCount == 0) BeginResolution();
            }
            else if (gameManager.CurrentState == MatchState.Resolving)
            {
                settleElapsed += deltaTime;
                if (settleElapsed >= Rules.maximumSettleSeconds || (settleElapsed >= Rules.minimumSettleSeconds && TanksSettled())) ResolveTurn();
            }
        }
        private void BeginResolution()
        {
            if (!matchRunning || gameManager.CurrentState == MatchState.Resolving) return;
            settleElapsed = 0f; DisablePlayers();
            gameManager.ChangeState(MatchState.Resolving);
        }
        private bool TanksSettled()
        {
            for (int i = 0; i < 2; i++)
            {
                if (health[i].CurrentHealth <= 0f) continue;
                var body = health[i].GetComponent<Rigidbody2D>();
                if (body != null && (body.linearVelocity.sqrMagnitude > .01f || Mathf.Abs(body.angularVelocity) > 5f)) return false;
            }
            return true;
        }
        private void ResolveTurn()
        {
            coordinating = true;
            try
            {
                CompletedTurns++;
                bool dead0 = health[0].CurrentHealth <= 0f, dead1 = health[1].CurrentHealth <= 0f;
                if (dead0 || dead1)
                {
                    FinalResult = dead0 && dead1 ? new MatchResult(null, null) : new MatchResult(dead0 ? 1 : 0, dead0 ? 0 : 1);
                    FinishMatch(fallen[0] || fallen[1] ? MatchEndReason.Fall : MatchEndReason.Elimination); return;
                }
                if (CompletedTurns >= Rules.maximumTurns)
                {
                    float a = health[0].CurrentHealth / health[0].MaxHealth, b = health[1].CurrentHealth / health[1].MaxHealth;
                    FinalResult = Mathf.Abs(a-b) < .001f ? new MatchResult(null, null) : new MatchResult(a > b ? 0 : 1, a > b ? 1 : 0);
                    FinishMatch(MatchEndReason.TurnLimit); return;
                }
                activeShot = null; deathPending = false;
                turnManager.AdvanceTurn();
                if (matchRunning) ActivateCurrentTurn();
            }
            finally { coordinating = false; }
        }
        private void ActivateCurrentTurn()
        {
            RemainingTurnSeconds = Rules.turnSeconds;
            if (motors[CurrentPlayerIndex] != null)
            {
                var loadout=health[CurrentPlayerIndex].GetComponent<TankLoadout>();
                float tankMultiplier=loadout!=null?loadout.MovementMultiplier:1f;
                motors[CurrentPlayerIndex].BeginTurnBudget(Rules.movementPerTurn * tankMultiplier * health[CurrentPlayerIndex].ShieldMovementMultiplier, Rules.minimumX, Rules.maximumX);
            }
            players[CurrentPlayerIndex].EnableControl();
            if (matchRunning) gameManager.ChangeState(MatchState.TurnActive);
        }
        private void OnPlayerDied()
        {
            if (!matchRunning) return;
            deathPending = true; DisablePlayers();
            // Death does not finish the match while children of the accepted shot are alive.
            if (activeShot == null && !coordinating) BeginResolution();
        }
        private void OnDamageApplied(ShotContext shot, HealthComponent target, float actual)
        {
            if (!matchRunning || activeShot != shot || shot.IsCancelled || actual <= 0f) return;
            int owner = shot.OwnerPlayerIndex;
            if (owner < 0 || owner > 1 || target != health[1-owner]) return;
            creditedDamage[owner] = Mathf.Min(startingHealth[1-owner], creditedDamage[owner] + actual);
        }
        public int GetScore(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 1) throw new ArgumentOutOfRangeException(nameof(playerIndex));
            if (scoresFinalized) return finalScores[playerIndex];
            if (EndReason == MatchEndReason.Cancelled || (EndReason == MatchEndReason.Surrender && FinalResult != null && FinalResult.LoserPlayerIndex == playerIndex)) return 0;
            int score = Mathf.FloorToInt(creditedDamage[playerIndex] + .0001f);
            if (FinalResult == null) return score;
            if (FinalResult.IsDraw) return score + Rules.drawScore;
            if (FinalResult.WinnerPlayerIndex == playerIndex)
                score += Rules.winScore + Mathf.FloorToInt(Rules.survivalScore * Mathf.Clamp01(health[playerIndex].CurrentHealth / health[playerIndex].MaxHealth));
            return score;
        }
        public void Surrender(int playerIndex = 0)
        {
            if (!matchRunning) return;
            if (playerIndex < 0 || playerIndex > 1) throw new ArgumentOutOfRangeException(nameof(playerIndex));
            FinalResult = new MatchResult(1-playerIndex, playerIndex); FinishMatch(MatchEndReason.Surrender);
        }
        public void EndMatch()
        {
            if (!matchRunning || matchEndedNotified || endingMatch) return;
            FinalResult = null; FinishMatch(MatchEndReason.Cancelled);
        }
        private void FinishMatch(MatchEndReason reason)
        {
            if (endingMatch || matchEndedNotified) return;
            endingMatch = true; matchRunning = false; EndReason = reason;
            finalScores[0] = GetScore(0); finalScores[1] = GetScore(1); scoresFinalized = true;
            try
            {
                Unsubscribe(); SetPaused(false);
                if (activeShot != null) activeShot.Cancel();
                activeShot = null; DisablePlayers();
                matchEndedNotified = true;
                var errors = new List<Exception>();
                try { gameManager.ChangeState(MatchState.MatchEnded); } catch (Exception e) { errors.Add(e); }
                if (MatchEnded != null) foreach (Action<MatchResult> handler in MatchEnded.GetInvocationList())
                    try { handler(FinalResult); } catch (Exception e) { errors.Add(e); }
                if (errors.Count > 0) throw new AggregateException(errors);
            }
            finally { endingMatch = false; }
        }
        public void SetPaused(bool value)
        {
            if (value == IsPaused || (value && !matchRunning)) return;
            IsPaused = value;
            if (value) { savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; DisablePlayers(); }
            else
            {
                Time.timeScale = savedTimeScale;
                if (matchRunning && gameManager != null && gameManager.CurrentState == MatchState.TurnActive && !deathPending)
                    players[CurrentPlayerIndex].EnableControl();
            }
        }
        private void OnApplicationPause(bool pause) { if (pause) SetPaused(true); }
        private void OnApplicationFocus(bool focus) { if (!focus) SetPaused(true); }
        private void DisablePlayers()
        {
            for (int i = 0; i < 2; i++)
            {
                var player = players[i];
                if (player != null && (!(player is UnityEngine.Object obj) || obj != null)) player.DisableControl();
                if (motors[i] != null) motors[i].Stop();
            }
        }
        private void Unsubscribe()
        {
            ExplosionDamage.DamageApplied -= OnDamageApplied;
            for (int i = 0; i < 2; i++) if (health[i] != null) health[i].Died -= OnPlayerDied;
        }
        private void ValidateRegistration()
        {
            if (gameManager == null || turnManager == null) throw new InvalidOperationException("Initialize managers first.");
            for (int i = 0; i < 2; i++) { ValidatePlayer(players[i]); if (players[i].PlayerIndex != i || health[i] == null) throw new InvalidOperationException("Both player/health registrations are required."); }
            if (health[0] == health[1]) throw new InvalidOperationException("Players require distinct health components.");
        }
        private static void ValidatePlayer(IPlayerController player)
        {
            if (player == null || (player is UnityEngine.Object obj && obj == null) || player.PlayerIndex < 0 || player.PlayerIndex > 1)
                throw new ArgumentException("Players must have valid indices 0 and 1.");
        }
        private void OnDisable() { if (matchRunning) EndMatch(); else { Unsubscribe(); SetPaused(false); } }
        private void OnDestroy() { Unsubscribe(); if (fallbackRules != null) Destroy(fallbackRules); }
    }
}
