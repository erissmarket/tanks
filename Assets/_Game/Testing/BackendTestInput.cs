using System;
using TankGame.Core;
using TankGame.Tank;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TankGame.Testing
{
    public sealed class BackendTestInput : MonoBehaviour
    {
        [SerializeField] private GameplayActionController gameplayActions = null;
        [SerializeField] private TurnManager turnManager = null;
        [SerializeField] private BackendTestBootstrap bootstrap = null;
        [SerializeField] private TankAim playerZeroAim = null;
        [SerializeField] private TankAim playerOneAim = null;
        [SerializeField] private Transform playerZeroMuzzle = null;
        [SerializeField] private Transform playerOneMuzzle = null;
        [SerializeField, Min(1f)] private float aimRate = 60f;
        [SerializeField] private bool playerOneIsAI = false;
        private int movingPlayer = -1;

        private void Start()
        {
            if (gameplayActions == null || turnManager == null || bootstrap == null
                || playerZeroAim == null || playerOneAim == null || playerZeroMuzzle == null || playerOneMuzzle == null)
                throw new InvalidOperationException("Assign all BackendTestInput references.");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (bootstrap.CanRestart && keyboard.rKey.wasPressedThisFrame)
            {
                movingPlayer = -1;
                bootstrap.RestartTestMatch();
                return;
            }
            if (!bootstrap.CanIssueActions)
            {
                movingPlayer = -1;
                return;
            }

            int index = turnManager.CurrentPlayerIndex;
            if (playerOneIsAI && index == 1)
            {
                movingPlayer = -1;
                return;
            }
            float movement = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            if (movement != 0f)
            {
                gameplayActions.Move(index, movement);
                movingPlayer = index;
            }
            else if (movingPlayer == index)
            {
                gameplayActions.Stop(index);
                movingPlayer = -1;
            }

            float aimInput = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            if (aimInput != 0f)
            {
                TankAim aim = index == 0 ? playerZeroAim : playerOneAim;
                gameplayActions.Aim(index, aim.CurrentAimAngle + aimInput * aimRate * Time.deltaTime);
            }
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                Transform muzzle = index == 0 ? playerZeroMuzzle : playerOneMuzzle;
                Vector2 direction = muzzle.TransformVector(Vector3.right).normalized;
                gameplayActions.Fire(index, direction);
                movingPlayer = -1;
            }
        }

        private void OnDisable()
        {
            if (movingPlayer >= 0 && bootstrap != null && bootstrap.CanIssueActions
                && turnManager.CurrentPlayerIndex == movingPlayer)
                gameplayActions.Stop(movingPlayer);
            movingPlayer = -1;
        }

        private void OnValidate()
        {
            if (float.IsNaN(aimRate) || float.IsInfinity(aimRate)) aimRate = 60f;
            aimRate = Mathf.Max(1f, aimRate);
        }
    }
}
