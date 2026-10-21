using System;
using TankGame.Core;
using TankGame.Data;
using TankGame.Tank;
using TankGame.Weapons;
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
        [SerializeField] private WeaponController playerZeroWeapon = null;
        [SerializeField] private WeaponController playerOneWeapon = null;
        [SerializeField] private Transform playerZeroMuzzle = null;
        [SerializeField] private Transform playerOneMuzzle = null;
        [SerializeField] private WeaponData[] availableWeapons = null;
        [SerializeField, Min(1f)] private float aimRate = 60f;
        [SerializeField] private bool playerOneIsAI = false;
        private int movingPlayer = -1;

        public WeaponData[] AvailableWeapons
        {
            get => availableWeapons;
            set => availableWeapons = value;
        }

        private void Start()
        {
            if (gameplayActions == null || turnManager == null || bootstrap == null
                || playerZeroAim == null || playerOneAim == null || playerZeroMuzzle == null || playerOneMuzzle == null)
                throw new InvalidOperationException("Assign all BackendTestInput references.");

            if (playerZeroWeapon == null)
                playerZeroWeapon = playerZeroAim.GetComponent<WeaponController>();
            if (playerOneWeapon == null)
                playerOneWeapon = playerOneAim.GetComponent<WeaponController>();

            // Equip first weapon by default if available
            if (availableWeapons != null && availableWeapons.Length > 0 && availableWeapons[0] != null)
            {
                if (playerZeroWeapon != null) playerZeroWeapon.AssignWeapon(availableWeapons[0]);
                if (playerOneWeapon != null) playerOneWeapon.AssignWeapon(availableWeapons[0]);
            }
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

            // Weapon switching with numeric keys 1 to 7
            CheckWeaponSwitch(index, keyboard);

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

        private void CheckWeaponSwitch(int playerIndex, Keyboard keyboard)
        {
            if (availableWeapons == null || availableWeapons.Length == 0) return;

            for (int i = 0; i < Mathf.Min(availableWeapons.Length, 7); i++)
            {
                Key digitKey = Key.Digit1 + i;
                Key numpadKey = Key.Numpad1 + i;
                if (keyboard[digitKey].wasPressedThisFrame || keyboard[numpadKey].wasPressedThisFrame)
                {
                    SelectWeapon(playerIndex, i);
                    break;
                }
            }
        }

        public void SelectWeapon(int playerIndex, int weaponSlot)
        {
            if (availableWeapons == null || weaponSlot < 0 || weaponSlot >= availableWeapons.Length || availableWeapons[weaponSlot] == null)
                return;

            WeaponController weaponCtrl = playerIndex == 0 ? playerZeroWeapon : playerOneWeapon;
            if (weaponCtrl != null)
            {
                weaponCtrl.AssignWeapon(availableWeapons[weaponSlot]);
                Debug.Log($"[Weapon Switch] Player {playerIndex} selected: {availableWeapons[weaponSlot].WeaponName}");
            }
        }

        private void OnGUI()
        {
            if (turnManager == null || bootstrap == null) return;

            int index = turnManager.CurrentPlayerIndex;
            WeaponController weaponCtrl = index == 0 ? playerZeroWeapon : playerOneWeapon;
            var currentWeapon = weaponCtrl != null ? weaponCtrl.CurrentWeapon : null;
            string currentWeaponName = currentWeapon != null ? currentWeapon.WeaponName : "None";
            Sprite currentWeaponIcon = currentWeapon != null ? currentWeapon.Icon : null;

            GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            GUI.Box(new Rect(10, 10, 480, 85), GUIContent.none);

            if (currentWeaponIcon != null && currentWeaponIcon.texture != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(20, 18, 70, 70), currentWeaponIcon.texture, ScaleMode.ScaleToFit);
            }

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(100, 15, 380, 75));
            GUILayout.Label($"<b>Turn:</b> Player {index + 1} | <b>Active Weapon:</b> <color=#FFA726>{currentWeaponName}</color>");
            GUILayout.Label("<size=11><b>Keys [1-7]:</b> Rocket | Mortar | Railgun | Floater | Bouncy | Cluster | Digger</size>");
            GUILayout.Label("<size=11><b>Controls:</b> [A/D] Move | [W/S] Aim | [Space] Fire | [R] Restart Match</size>");
            GUILayout.EndArea();
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
