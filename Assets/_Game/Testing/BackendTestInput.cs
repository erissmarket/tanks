using TankGame.Core;
using TankGame.Data;
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
        [SerializeField] private BackendTestPlayerController playerZero = null;
        [SerializeField] private BackendTestPlayerController playerOne = null;
        [SerializeField] private WeaponController playerZeroWeapon = null;
        [SerializeField] private WeaponController playerOneWeapon = null;
        [SerializeField] private WeaponData[] availableWeapons = null;
        [SerializeField] private float aimRate = 60f;

        private int movingPlayer = -1;

        public WeaponData[] AvailableWeapons
        {
            get => availableWeapons;
            set => availableWeapons = value;
        }

        private void Start()
        {
            if (playerZeroWeapon == null && playerZero != null)
                playerZeroWeapon = playerZero.GetComponent<WeaponController>();
            if (playerOneWeapon == null && playerOne != null)
                playerOneWeapon = playerOne.GetComponent<WeaponController>();

            if (availableWeapons != null && availableWeapons.Length > 0 && availableWeapons[0] != null)
            {
                if (playerZeroWeapon != null) playerZeroWeapon.AssignWeapon(availableWeapons[0]);
                if (playerOneWeapon != null) playerOneWeapon.AssignWeapon(availableWeapons[0]);
            }
        }

        private void Update()
        {
            if (bootstrap == null || !bootstrap.CanRestart) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                bootstrap.RestartMatch();
                return;
            }

            if (!bootstrap.CanIssueActions) return;

            int index = turnManager.CurrentPlayerIndex;
            var player = index == 0 ? playerZero : playerOne;
            if (player == null || !player.CanAct) return;

            if (keyboard.digit1Key.wasPressedThisFrame) SelectWeapon(index, 0);
            else if (keyboard.digit2Key.wasPressedThisFrame) SelectWeapon(index, 1);
            else if (keyboard.digit3Key.wasPressedThisFrame) SelectWeapon(index, 2);
            else if (keyboard.digit4Key.wasPressedThisFrame) SelectWeapon(index, 3);
            else if (keyboard.digit5Key.wasPressedThisFrame) SelectWeapon(index, 4);
            else if (keyboard.digit6Key.wasPressedThisFrame) SelectWeapon(index, 5);
            else if (keyboard.digit7Key.wasPressedThisFrame) SelectWeapon(index, 6);

            float move = 0f;
            if (keyboard.dKey.isPressed) move += 1f;
            if (keyboard.aKey.isPressed) move -= 1f;

            if (move != 0f)
            {
                gameplayActions.Move(index, move);
                movingPlayer = index;
            }
            else if (movingPlayer == index)
            {
                gameplayActions.Stop(index);
                movingPlayer = -1;
            }

            float elevation = 0f;
            if (keyboard.wKey.isPressed) elevation += 1f;
            if (keyboard.sKey.isPressed) elevation -= 1f;

            if (elevation != 0f)
            {
                float nextAim = player.CurrentAimAngle + elevation * aimRate * Time.deltaTime;
                gameplayActions.Aim(index, nextAim);
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                gameplayActions.Fire(index);
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
