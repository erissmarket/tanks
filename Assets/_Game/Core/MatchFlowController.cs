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
        private Projectile activeProjectile;
        private bool matchRunning;
        private bool coordinating;
        private bool endingMatch;
        private bool completionPending;

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

        public void StartMatch()
        {
            ValidateRegistration();
            if (!isActiveAndEnabled || matchRunning || coordinating || endingMatch
                || (gameManager.CurrentState != MatchState.None && gameManager.CurrentState != MatchState.MatchEnded))
                throw new InvalidOperationException("Start an enabled coordinator from None or MatchEnded.");

            coordinating = true;
            try
            {
                DetachProjectile();
                completionPending = false;
                matchRunning = true;
                DisablePlayers();
                if (!matchRunning)
                    return;
                gameManager.ChangeState(MatchState.Starting);
                if (!matchRunning)
                    return;
                turnManager.StartFirstTurn();
                if (matchRunning)
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
            if (endingMatch)
                return;
            if (gameManager == null)
                throw new InvalidOperationException("Initialize the coordinator before ending a match.");

            endingMatch = true;
            try
            {
                matchRunning = false;
                completionPending = false;
                DetachProjectile();
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
            players[turnManager.CurrentPlayerIndex].EnableControl();
            if (matchRunning)
                gameManager.ChangeState(MatchState.TurnActive);
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
                DisablePlayers();
            }
        }
    }
}
