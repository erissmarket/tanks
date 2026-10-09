using System;
using TankGame.Core;
using TankGame.Tank;
using TankGame.Weapons;
using UnityEngine;

namespace TankGame.Testing
{
    public sealed class BackendTestBootstrap : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager = null;
        [SerializeField] private TurnManager turnManager = null;
        [SerializeField] private MatchFlowController matchFlow = null;
        [SerializeField] private GameplayActionController gameplayActions = null;
        [SerializeField] private BackendTestPlayerController playerZero = null;
        [SerializeField] private BackendTestPlayerController playerOne = null;
        [SerializeField] private Transform playerZeroMuzzle = null;
        [SerializeField] private Transform playerOneMuzzle = null;
        [SerializeField] private float playerZeroDefaultAim = 45f;
        [SerializeField] private float playerOneDefaultAim = 135f;
        private Vector3 playerZeroStartPosition;
        private Vector3 playerOneStartPosition;

        public bool CanRestart => gameManager != null && gameManager.CurrentState == MatchState.MatchEnded;
        public bool CanIssueActions => gameManager != null && gameManager.CurrentState == MatchState.TurnActive;

        private void Start()
        {
            if (gameManager == null || turnManager == null || matchFlow == null || gameplayActions == null
                || playerZero == null || playerOne == null || playerZeroMuzzle == null || playerOneMuzzle == null)
                throw new InvalidOperationException("BackendTest bootstrap references must all be assigned.");

            playerZeroStartPosition = playerZero.transform.position;
            playerOneStartPosition = playerOne.transform.position;
            InitializePlayer(playerZero, 0);
            InitializePlayer(playerOne, 1);
            matchFlow.Initialize(gameManager, turnManager, playerZero, playerOne);
            gameplayActions.Initialize(gameManager, turnManager, matchFlow);
            RegisterPlayer(playerZero, 0, playerZeroMuzzle);
            RegisterPlayer(playerOne, 1, playerOneMuzzle);
            playerZero.GetComponent<TankAim>().SetAimAngle(playerZeroDefaultAim);
            playerOne.GetComponent<TankAim>().SetAimAngle(playerOneDefaultAim);
            matchFlow.StartMatch();
        }

        public void RestartTestMatch()
        {
            if (!CanRestart)
                throw new InvalidOperationException("Restart BackendTest only after MatchEnded.");
            RestorePlayer(playerZero, playerZeroStartPosition, playerZeroDefaultAim);
            RestorePlayer(playerOne, playerOneStartPosition, playerOneDefaultAim);
            Physics2D.SyncTransforms();
            matchFlow.StartMatch();
        }

        private static void RestorePlayer(BackendTestPlayerController player, Vector3 position, float angle)
        {
            player.GetComponent<HealthComponent>().ResetHealth();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            player.GetComponent<TankAim>().SetAimAngle(angle);
        }

        private static void InitializePlayer(BackendTestPlayerController player, int index)
        {
            player.Initialize(index);
            TankController tank = player.GetComponent<TankController>();
            HealthComponent health = player.GetComponent<HealthComponent>();
            if (tank == null || health == null)
                throw new InvalidOperationException("Each BackendTest player requires TankController and HealthComponent.");
            tank.Initialize(index);
            health.ResetHealth();
        }

        private void RegisterPlayer(BackendTestPlayerController player, int index, Transform muzzle)
        {
            gameplayActions.RegisterPlayer(index, player, player.GetComponent<TankMotor>(),
                player.GetComponent<TankAim>(), player.GetComponent<WeaponController>(), muzzle);
            matchFlow.RegisterHealth(index, player.GetComponent<HealthComponent>());
        }
    }
}
