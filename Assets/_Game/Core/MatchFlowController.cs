using System;
using System.Collections.Generic;
using TankGame.Player;
using TankGame.Projectiles;
using UnityEngine;

namespace TankGame.Core
{
    public sealed class MatchFlowController : MonoBehaviour
    {
        private GameManager gameManager;
        private TurnManager turnManager;
        private readonly IPlayerController[] players = new IPlayerController[2];
        private readonly HealthComponent[] health = new HealthComponent[2];
        private readonly Action[] deathHandlers = new Action[2];
        private Projectile activeProjectile;
        private bool matchRunning;
        private bool coordinating;
        private bool endingMatch;
        private bool completionPending;
        private bool deathPending;
        private bool matchEndedNotified;
        private int matchGeneration;

        public MatchResult FinalResult { get; private set; }

        // Null result means the match was ended without a health-based outcome.
        public event Action<MatchResult> MatchEnded;

        /// <summary>Registers both players in either order, before starting a match.</summary>
        public void Initialize(GameManager gameManager, TurnManager turnManager,
            IPlayerController firstPlayer, IPlayerController secondPlayer)
        {
            if (matchRunning || coordinating || endingMatch)
                throw new InvalidOperationException("End the match before replacing its dependencies.");
            if (gameManager == null)
                throw new ArgumentNullException(nameof(gameManager));
            if (turnManager == null)
                throw new ArgumentNullException(nameof(turnManager));
            ValidatePlayer(firstPlayer, nameof(firstPlayer));
            ValidatePlayer(secondPlayer, nameof(secondPlayer));
            if (firstPlayer.PlayerIndex == secondPlayer.PlayerIndex)
                throw new ArgumentException("Register exactly one player with index 0 and one with index 1.");

            coordinating = true;
            try
            {
                UnsubscribeHealth();
                Array.Clear(health, 0, health.Length);
                DisablePlayers();
                this.gameManager = gameManager;
                this.turnManager = turnManager;
                players[firstPlayer.PlayerIndex] = firstPlayer;
                players[secondPlayer.PlayerIndex] = secondPlayer;
                DisablePlayers();
            }
            finally
            {
                coordinating = false;
            }
        }

        public void RegisterHealth(int playerIndex, HealthComponent healthComponent)
        {
            if (gameManager == null || turnManager == null)
                throw new InvalidOperationException("Initialize the coordinator before registering health.");
            if (matchRunning || coordinating || endingMatch)
                throw new InvalidOperationException("Register health before starting a match or after ending it.");
            if (playerIndex < 0 || playerIndex > 1)
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "Player indices must be 0 and 1.");
            if (healthComponent == null)
                throw new ArgumentNullException(nameof(healthComponent));
            if (!ReferenceEquals(health[playerIndex], null) || health[1 - playerIndex] == healthComponent)
                throw new ArgumentException("Each player must register a distinct health component exactly once.");

            health[playerIndex] = healthComponent;
        }

        /// <summary>The match owner must ResetHealth on both players before restarting; this method does not reset health.</summary>
        public void StartMatch()
        {
            ValidateRegistration();
            ValidateHealth();
            if (!isActiveAndEnabled || matchRunning || coordinating || endingMatch
                || (gameManager.CurrentState != MatchState.None && gameManager.CurrentState != MatchState.MatchEnded))
                throw new InvalidOperationException("Start an enabled coordinator from None or MatchEnded.");
            if (health[0].CurrentHealth <= 0f || health[1].CurrentHealth <= 0f)
                throw new InvalidOperationException("Reset both players' health before starting a match.");

            coordinating = true;
            try
            {
                DetachProjectile();
                completionPending = false;
                deathPending = false;
                FinalResult = null;
                matchEndedNotified = false;
                matchRunning = true;
                SubscribeHealth();
                DisablePlayers();
                if (!matchRunning)
                    return;
                gameManager.ChangeState(MatchState.Starting);
                if (!matchRunning)
                    return;
                turnManager.StartFirstTurn();
                if (matchRunning && !deathPending)
                    ActivateCurrentTurn();
            }
            finally
            {
                coordinating = false;
            }
        }

        /// <summary>Register the initialized projectile returned by WeaponController.Fire immediately.</summary>
        public void NotifyProjectileFired(Projectile projectile)
        {
            ValidateRegistration();
            if (!matchRunning || coordinating || endingMatch
                || gameManager.CurrentState != MatchState.TurnActive || activeProjectile != null)
                throw new InvalidOperationException("A projectile can only be registered during an active turn.");
            if (projectile == null)
                throw new ArgumentNullException(nameof(projectile));
            if (!projectile.IsInitialized || projectile.IsFinished || !projectile.isActiveAndEnabled
                || projectile.OwnerPlayerIndex != turnManager.CurrentPlayerIndex)
                throw new ArgumentException("Use an active, initialized projectile belonging to the current player.", nameof(projectile));

            int firingPlayerIndex = turnManager.CurrentPlayerIndex;
            coordinating = true;
            try
            {
                activeProjectile = projectile;
                activeProjectile.Completed += OnProjectileCompleted;
                DisablePlayers();
                if (matchRunning)
                    gameManager.ChangeState(MatchState.ProjectileFlying);
                coordinating = false;
                if (completionPending && matchRunning)
                {
                    completionPending = false;
                    OnProjectileCompleted(activeProjectile);
                }
            }
            catch (Exception registrationError)
            {
                coordinating = true;
                completionPending = false;
                DetachProjectile();
                try
                {
                    if (matchRunning)
                    {
                        if (gameManager.CurrentState == MatchState.TurnActive && !projectile.IsFinished
                            && turnManager.CurrentPlayerIndex == firingPlayerIndex)
                        {
                            players[turnManager.CurrentPlayerIndex].EnableControl();
                            if (!players[turnManager.CurrentPlayerIndex].CanAct)
                                throw new InvalidOperationException("Could not restore active player control.");
                        }
                        else
                        {
                            EndMatch();
                        }
                    }
                }
                catch (Exception recoveryError)
                {
                    try { EndMatch(); }
                    catch (Exception endError)
                    {
                        throw new AggregateException(registrationError, recoveryError, endError);
                    }
                    throw new AggregateException(registrationError, recoveryError);
                }
                throw;
            }
            finally
            {
                coordinating = false;
            }
        }

        public void EndMatch()
        {
            if (endingMatch || matchEndedNotified)
                return;
            if (gameManager == null)
                throw new InvalidOperationException("Initialize the coordinator before ending a match.");

            endingMatch = true;
            try
            {
                matchRunning = false;
                completionPending = false;
                DetachProjectile();
                UnsubscribeHealth();
                deathPending = false;
                var errors = new List<Exception>();
                foreach (IPlayerController player in players)
                {
                    if (player == null || (player is UnityEngine.Object unityPlayer && unityPlayer == null))
                        continue;
                    try { player.DisableControl(); }
                    catch (Exception error) { errors.Add(error); }
                }
                try { gameManager.ChangeState(MatchState.MatchEnded); }
                catch (Exception error) { errors.Add(error); }
                matchEndedNotified = true;
                Action<MatchResult> handlers = MatchEnded;
                if (handlers != null)
                {
                    foreach (Action<MatchResult> handler in handlers.GetInvocationList())
                    {
                        try { handler(FinalResult); }
                        catch (Exception error) { errors.Add(error); }
                    }
                }
                if (errors.Count > 0)
                    throw new AggregateException("Match ended, but a lifecycle callback failed.", errors);
            }
            finally
            {
                endingMatch = false;
            }
        }

        private void OnProjectileCompleted(Projectile projectile)
        {
            if (!matchRunning || projectile != activeProjectile)
                return;

            if (coordinating)
            {
                completionPending = true;
                return;
            }

            coordinating = true;
            try
            {
                // Clear identity and subscription before invoking external callbacks.
                DetachProjectile();
                gameManager.ChangeState(MatchState.Resolving);
                if (!matchRunning)
                    return;
                if (TryEndFromHealth())
                    return;
                ValidateRegistration();
                DisablePlayers();
                if (!matchRunning)
                    return;
                turnManager.AdvanceTurn();
                if (matchRunning)
                    ActivateCurrentTurn();
            }
            catch (Exception completionError)
            {
                try { EndMatch(); }
                catch (Exception endError) { throw new AggregateException(completionError, endError); }
                throw;
            }
            finally
            {
                coordinating = false;
            }
        }

        private void ActivateCurrentTurn()
        {
            if (deathPending)
                return;
            players[turnManager.CurrentPlayerIndex].EnableControl();
            if (matchRunning && !deathPending)
                gameManager.ChangeState(MatchState.TurnActive);
        }

        private void SubscribeHealth()
        {
            UnsubscribeHealth();
            int generation = ++matchGeneration;
            for (int index = 0; index < health.Length; index++)
            {
                deathHandlers[index] = () => OnPlayerDied(generation);
                health[index].Died += deathHandlers[index];
            }
        }

        private void UnsubscribeHealth()
        {
            for (int index = 0; index < health.Length; index++)
            {
                if (!ReferenceEquals(health[index], null) && deathHandlers[index] != null)
                    health[index].Died -= deathHandlers[index];
                deathHandlers[index] = null;
            }
        }

        private void OnPlayerDied(int generation)
        {
            if (!matchRunning || generation != matchGeneration || deathPending)
                return;

            deathPending = true;
            DisablePlayers();
            if (gameManager.CurrentState == MatchState.TurnActive || gameManager.CurrentState == MatchState.Starting)
                gameManager.ChangeState(MatchState.Resolving);
        }

        private void LateUpdate()
        {
            if (matchRunning && deathPending && !coordinating)
                TryEndFromHealth();
        }

        private bool TryEndFromHealth()
        {
            ValidateHealth();
            bool playerZeroDead = health[0].CurrentHealth <= 0f;
            bool playerOneDead = health[1].CurrentHealth <= 0f;
            if (!playerZeroDead && !playerOneDead)
            {
                deathPending = false;
                return false;
            }

            FinalResult = playerZeroDead && playerOneDead
                ? new MatchResult(null, null)
                : new MatchResult(playerZeroDead ? 1 : 0, playerZeroDead ? 0 : 1);
            EndMatch();
            return true;
        }

        private void ValidateHealth()
        {
            if (health[0] == null || health[1] == null || health[0] == health[1])
                throw new InvalidOperationException("Register distinct health components for players 0 and 1 before starting.");
        }

        private void DisablePlayers()
        {
            foreach (IPlayerController player in players)
            {
                if (player != null && !(player is UnityEngine.Object unityPlayer && unityPlayer == null))
                    player.DisableControl();
            }
        }

        private void DetachProjectile()
        {
            if (activeProjectile != null)
                activeProjectile.Completed -= OnProjectileCompleted;
            activeProjectile = null;
        }

        private void ValidateRegistration()
        {
            if (gameManager == null || turnManager == null)
                throw new InvalidOperationException("Initialize the coordinator with both managers and both players first.");
            for (int index = 0; index < players.Length; index++)
            {
                ValidatePlayer(players[index], "players");
                if (players[index].PlayerIndex != index)
                    throw new InvalidOperationException("Registered player indices changed; register players again before starting.");
            }
        }

        private static void ValidatePlayer(IPlayerController player, string parameterName)
        {
            if (player == null || (player is UnityEngine.Object unityPlayer && unityPlayer == null))
                throw new ArgumentNullException(parameterName, "Both players are required.");
            if (player.PlayerIndex < 0 || player.PlayerIndex > 1)
                throw new ArgumentOutOfRangeException(parameterName, "Player indices must be 0 and 1.");
        }

        private void OnDisable()
        {
            if (matchRunning && gameManager != null)
                EndMatch();
            else
            {
                DetachProjectile();
                UnsubscribeHealth();
                DisablePlayers();
            }
        }
    }
}
