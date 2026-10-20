using System;
using TankGame.Player;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Weapons;
using UnityEngine;

namespace TankGame.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameplayActionController : MonoBehaviour
    {
        private sealed class PlayerRegistration
        {
            public IPlayerController Controller;
            public TankMotor Motor;
            public TankAim Aim;
            public WeaponController Weapon;
            public Transform Muzzle;
            public Rigidbody2D Body;
            public bool Moving;
            public double MoveExpires;
            public float LastX, DistanceUsed, Power = 1f;
        }

        private readonly PlayerRegistration[] players = new PlayerRegistration[2];
        private GameManager gameManager;
        private TurnManager turnManager;
        private MatchFlowController matchFlow;
        private bool firing;
        [SerializeField, Min(0f)] private float movementBudget = 3f;
        [SerializeField, Range(0.1f, 2f)] private float movementLeaseSeconds = 0.35f;

        public float GetMovementRemaining(int playerIndex)
        { ValidateIndex(playerIndex); return Mathf.Max(0, movementBudget - (players[playerIndex]?.DistanceUsed ?? 0)); }
        public float GetShotPower(int playerIndex)
        { ValidateIndex(playerIndex); return players[playerIndex]?.Power ?? 1f; }

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
                Controller = controller, Motor = motor, Aim = aim, Weapon = weapon, Muzzle = muzzle,
                Body = motor.GetComponent<Rigidbody2D>()
            };
        }

        public void Move(int playerIndex, float direction)
        {
            PlayerRegistration player = Authorize(playerIndex);
            ValidateFinite(direction, nameof(direction));
            if (Mathf.Abs(direction) < 0.001f || GetMovementRemaining(playerIndex) <= 0f) { Stop(playerIndex); return; }
            if (!player.Moving) player.LastX = player.Body.position.x;
            player.Moving = true;
            player.MoveExpires = Time.unscaledTimeAsDouble + movementLeaseSeconds;
            player.Motor.Move(direction);
        }

        public void Stop(int playerIndex)
        {
            var player = Authorize(playerIndex);
            AccountMovement(player);
            player.Moving = false;
            player.Motor.Stop();
        }

        public void Aim(int playerIndex, float angle)
        {
            PlayerRegistration player = Authorize(playerIndex);
            ValidateFinite(angle, nameof(angle));
            player.Aim.SetAimAngle(angle);
        }

        public void Aim(int playerIndex, Vector2 worldDirection, float normalizedPower)
        {
            var player = Authorize(playerIndex);
            ValidatePower(normalizedPower);
            player.Aim.SetWorldAimDirection(worldDirection);
            player.Power = normalizedPower;
        }

        public Projectile Fire(int playerIndex, Vector2 direction)
        {
            var player = Authorize(playerIndex);
            return Fire(playerIndex, direction, player.Weapon.CurrentWeapon != null ? player.Weapon.CurrentWeapon.BaseSpeedPower : 1f);
        }

        public Projectile Fire(int playerIndex, Vector2 direction, float normalizedPower)
        {
            PlayerRegistration player = Authorize(playerIndex);
            ValidatePower(normalizedPower);
            firing = true;
            Projectile projectile = null;
            try
            {
                player.Aim.SetWorldAimDirection(direction);
                projectile = player.Weapon.Fire(playerIndex, player.Muzzle.position, player.Aim.WorldAimDirection, normalizedPower);
                if (projectile == null || !projectile.IsInitialized || projectile.IsFinished
                    || !projectile.isActiveAndEnabled || projectile.OwnerPlayerIndex != playerIndex)
                    throw new InvalidOperationException("Weapon did not create a valid active projectile.");
                matchFlow.NotifyProjectileFired(projectile);
                player.Power = normalizedPower;
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
            if (!isActiveAndEnabled || !matchFlow.isActiveAndEnabled || firing)
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
            if (current == MatchState.Starting)
                foreach (var player in players) if (player != null) { player.DistanceUsed = 0f; player.Power = 1f; }
            if (current == MatchState.TurnActive && turnManager.CurrentPlayerIndex >= 0)
            {
                var player = players[turnManager.CurrentPlayerIndex];
                if (player != null) player.DistanceUsed = 0f;
            }
            if (current != MatchState.TurnActive)
                StopRegisteredMotors();
        }

        private static void ValidatePower(float power)
        {
            if (!float.IsFinite(power) || power < 0f || power > 1f)
                throw new ArgumentOutOfRangeException(nameof(power), "Normalized power must be finite and in [0,1].");
        }

        private static void AccountMovement(PlayerRegistration player)
        {
            if (!player.Moving || player.Body == null) return;
            float x = player.Body.position.x;
            player.DistanceUsed += Mathf.Abs(x - player.LastX);
            player.LastX = x;
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player == null || !player.Moving) continue;
                AccountMovement(player);
                if (gameManager == null || gameManager.CurrentState != MatchState.TurnActive
                    || turnManager.CurrentPlayerIndex != i || !player.Controller.CanAct
                    || Time.unscaledTimeAsDouble >= player.MoveExpires || GetMovementRemaining(i) <= 0f)
                { player.Moving = false; player.Motor.Stop(); }
            }
        }

        private void OnValidate()
        {
            movementBudget = float.IsFinite(movementBudget) ? Mathf.Max(0f, movementBudget) : 3f;
            movementLeaseSeconds = float.IsFinite(movementLeaseSeconds) ? Mathf.Clamp(movementLeaseSeconds, 0.1f, 2f) : 0.35f;
        }

        private void StopRegisteredMotors()
        {
            foreach (PlayerRegistration player in players)
                if (player != null && player.Motor != null)
                { AccountMovement(player); player.Moving = false; player.Motor.Stop(); }
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
