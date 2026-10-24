using System;
using TankGame.Player;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Weapons;
using UnityEngine;

namespace TankGame.Core
{
    public sealed class GameplayActionController : MonoBehaviour
    {
        private sealed class PlayerRegistration
        {
            public IPlayerController Controller;
            public TankMotor Motor;
            public TankAim Aim;
            public WeaponController Weapon;
            public Transform Muzzle;
        }

        private readonly PlayerRegistration[] players = new PlayerRegistration[2];
        private GameManager gameManager;
        private TurnManager turnManager;
        private MatchFlowController matchFlow;
        private bool firing;
        public bool IsPaused => matchFlow != null && matchFlow.IsPaused;

        public void Initialize(GameManager gameManager, TurnManager turnManager, MatchFlowController matchFlow)
        {
            if (firing || (this.gameManager != null && this.gameManager.CurrentState != MatchState.None
                && this.gameManager.CurrentState != MatchState.MatchEnded))
                throw new InvalidOperationException("Initialize actions before starting a match or after ending it.");
            if (gameManager == null) throw new ArgumentNullException(nameof(gameManager));
            if (turnManager == null) throw new ArgumentNullException(nameof(turnManager));
            if (matchFlow == null) throw new ArgumentNullException(nameof(matchFlow));

            Unsubscribe();
            StopRegisteredMotors();
            this.gameManager = gameManager;
            this.turnManager = turnManager;
            this.matchFlow = matchFlow;
            Array.Clear(players, 0, players.Length);
            if (isActiveAndEnabled)
                gameManager.StateChanged += OnStateChanged;
        }

        public void RegisterPlayer(int playerIndex, IPlayerController controller, TankMotor motor,
            TankAim aim, WeaponController weapon, Transform muzzle)
        {
            ValidateDependencies();
            ValidateIndex(playerIndex);
            if (firing || (gameManager.CurrentState != MatchState.None && gameManager.CurrentState != MatchState.MatchEnded))
                throw new InvalidOperationException("Register players before starting a match or after ending it.");
            if (players[playerIndex] != null)
                throw new InvalidOperationException("This player index is already registered.");
            if (controller == null || (controller is UnityEngine.Object unityController && unityController == null))
                throw new ArgumentNullException(nameof(controller));
            if (controller.PlayerIndex != playerIndex)
                throw new ArgumentException("Controller index must match the registered player index.", nameof(controller));
            if (motor == null) throw new ArgumentNullException(nameof(motor));
            if (aim == null) throw new ArgumentNullException(nameof(aim));
            if (weapon == null) throw new ArgumentNullException(nameof(weapon));
            if (muzzle == null) throw new ArgumentNullException(nameof(muzzle));
            int other = 1 - playerIndex;
            if (players[other] != null && (ReferenceEquals(players[other].Controller, controller)
                || players[other].Motor == motor || players[other].Aim == aim || players[other].Weapon == weapon))
                throw new ArgumentException("Players must have distinct controllers and gameplay components.");

            players[playerIndex] = new PlayerRegistration
            {
                Controller = controller, Motor = motor, Aim = aim, Weapon = weapon, Muzzle = muzzle
            };
        }

        public void Move(int playerIndex, float direction)
        {
            PlayerRegistration player = Authorize(playerIndex);
            ValidateFinite(direction, nameof(direction));
            player.Motor.Move(direction);
        }

        public void Stop(int playerIndex)
        {
            Authorize(playerIndex).Motor.Stop();
        }

        public void Aim(int playerIndex, float angle)
        {
            PlayerRegistration player = Authorize(playerIndex);
            ValidateFinite(angle, nameof(angle));
            player.Aim.SetAimAngle(angle);
        }

        public Projectile Fire(int playerIndex, Vector2 direction)
        {
            PlayerRegistration player = Authorize(playerIndex);
            if (!player.Weapon.CanFire) return null;
            firing = true;
            Projectile projectile = null;
            try
            {
                player.Aim.SetWorldAimDirection(direction);
                projectile = player.Weapon.Fire(playerIndex, player.Muzzle.position, player.Aim.WorldAimDirection);
                if (projectile == null || !projectile.IsInitialized || projectile.IsFinished
                    || !projectile.isActiveAndEnabled || projectile.OwnerPlayerIndex != playerIndex)
                    throw new InvalidOperationException("Weapon did not create a valid active projectile.");
                // The muzzle can overlap the owner's hull as the barrel tilts. The shell must
                // leave that hull; radial explosion damage still includes its owner.
                foreach(var shellCollider in projectile.GetComponentsInChildren<Collider2D>())
                    foreach(var ownerCollider in player.Motor.GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(shellCollider,ownerCollider,true);
                matchFlow.NotifyProjectileFired(projectile);
                return projectile;
            }
            catch
            {
                if (projectile != null)
                {
                    Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
                    if (body != null) body.simulated = false;
                    Destroy(projectile.gameObject);
                }
                throw;
            }
            finally
            {
                firing = false;
                if (gameManager != null && gameManager.CurrentState != MatchState.TurnActive)
                    StopRegisteredMotors();
            }
        }

        private PlayerRegistration Authorize(int playerIndex)
        {
            ValidateDependencies();
            ValidateIndex(playerIndex);
            if (!isActiveAndEnabled || !matchFlow.isActiveAndEnabled || matchFlow.IsPaused || firing)
                throw new InvalidOperationException("Actions are disabled or a fire command is already in progress.");
            for (int index = 0; index < players.Length; index++)
            {
                PlayerRegistration registered = players[index];
                if (registered == null || registered.Controller == null
                    || (registered.Controller is UnityEngine.Object unityController && unityController == null)
                    || registered.Controller.PlayerIndex != index || registered.Motor == null
                    || registered.Aim == null || registered.Weapon == null || registered.Muzzle == null)
                    throw new InvalidOperationException("Both players must have valid registrations with indices 0 and 1.");
            }
            PlayerRegistration player = players[playerIndex];
            if (gameManager.CurrentState != MatchState.TurnActive
                || turnManager.CurrentPlayerIndex != playerIndex || !player.Controller.CanAct)
                throw new InvalidOperationException("Only the active player with control permission may act during TurnActive.");
            if (!player.Motor.isActiveAndEnabled || !player.Aim.isActiveAndEnabled || !player.Weapon.isActiveAndEnabled)
                throw new InvalidOperationException("The player's gameplay components must be enabled.");
            return player;
        }

        private void ValidateDependencies()
        {
            if (gameManager == null || turnManager == null || matchFlow == null)
                throw new InvalidOperationException("Initialize the action controller with the match's managers first.");
        }

        private static void ValidateIndex(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 1)
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "Player index must be 0 or 1.");
        }

        private static void ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
        }

        private void OnStateChanged(MatchState previous, MatchState current)
        {
            if (current != MatchState.TurnActive)
                StopRegisteredMotors();
        }

        private void StopRegisteredMotors()
        {
            foreach (PlayerRegistration player in players)
                if (player != null && player.Motor != null)
                    player.Motor.Stop();
        }

        private void OnEnable()
        {
            if (gameManager != null)
                gameManager.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopRegisteredMotors();
        }

        private void Unsubscribe()
        {
            if (gameManager != null)
                gameManager.StateChanged -= OnStateChanged;
        }
    }
}
