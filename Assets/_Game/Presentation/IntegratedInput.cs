using TankGame.Core;
using TankGame.Data;
using TankGame.Tank;
using TankGame.Testing;
using TankGame.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TankGame.Presentation
{
    public sealed class IntegratedInput : MonoBehaviour
    {
        [SerializeField] private GameplayActionController actions = null;
        [SerializeField] private GameManager gameManager = null;
        [SerializeField] private TurnManager turns = null;
        [SerializeField] private BackendTestPlayerController player = null;
        [SerializeField] private BackendTestBootstrap bootstrap = null;
        [SerializeField] private TankAim aim = null;
        [SerializeField] private Transform barrel = null;
        [SerializeField] private float aimRate = 60f;
        [SerializeField] private Button[] holdButtons = null;
        [SerializeField] private WeaponData[] availableWeapons = null;
        [SerializeField] private WeaponController weaponController = null;

        private readonly bool[] held = new bool[5];
        private bool moving;
        private int pointerFrame = -1;

        public bool CanAct => isActiveAndEnabled && gameManager.CurrentState == MatchState.TurnActive
            && turns.CurrentPlayerIndex == 0 && player.CanAct;
        public bool CanRestart => bootstrap.CanRestart;

        public WeaponData[] AvailableWeapons
        {
            get => availableWeapons;
            set => availableWeapons = value;
        }

        private void Start()
        {
            if (weaponController == null && player != null)
                weaponController = player.GetComponent<WeaponController>();

            // Assign first weapon by default if configured
            if (availableWeapons != null && availableWeapons.Length > 0 && availableWeapons[0] != null)
            {
                if (weaponController != null)
                    weaponController.AssignWeapon(availableWeapons[0]);
            }
        }

        public void SelectWeapon(int index)
        {
            if (availableWeapons == null || index < 0 || index >= availableWeapons.Length || availableWeapons[index] == null)
                return;

            if (weaponController == null && player != null)
                weaponController = player.GetComponent<WeaponController>();

            if (weaponController != null)
            {
                weaponController.AssignWeapon(availableWeapons[index]);
                Debug.Log($"[IntegratedInput] Switched to weapon: {availableWeapons[index].WeaponName}");
            }
        }

        public void MoveLeft(bool down)  => SetHeld(0, down);
        public void MoveRight(bool down) => SetHeld(1, down);
        public void AimUp(bool down)     => SetHeld(2, down);
        public void AimDown(bool down)   => SetHeld(3, down);
        public void Fire()               => SetHeld(4, true);

        private void SetHeld(int index, bool down)
        {
            if (index < 0 || index >= held.Length) return;
            held[index] = down;
            pointerFrame = Time.frameCount;
            if (index == 4 && down && CanAct)
            {
                held[4] = false;
                actions.Fire(0);
            }
        }

        private void Update()
        {
            if (bootstrap.CanRestart && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                bootstrap.RestartMatch();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && CanAct)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SelectWeapon(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) SelectWeapon(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) SelectWeapon(2);
                else if (keyboard.digit4Key.wasPressedThisFrame) SelectWeapon(3);
                else if (keyboard.digit5Key.wasPressedThisFrame) SelectWeapon(4);
                else if (keyboard.digit6Key.wasPressedThisFrame) SelectWeapon(5);
                else if (keyboard.digit7Key.wasPressedThisFrame) SelectWeapon(6);
            }

            if (!CanAct)
            {
                Clear();
                return;
            }

            bool pointer = Time.frameCount == pointerFrame;
            float move = (held[1] ? 1f : 0f) - (held[0] ? 1f : 0f);
            float elevation = (held[2] ? 1f : 0f) - (held[3] ? 1f : 0f);
            if (keyboard != null && !pointer)
            {
                move = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                elevation = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            }
            if (move != 0f) { actions.Move(0, move); moving = true; }
            else if (moving) { actions.Stop(0); moving = false; }
            if (elevation != 0f) actions.Aim(0, aim.CurrentAimAngle + elevation * aimRate * Time.deltaTime);
            if (keyboard != null && !pointer && keyboard.spaceKey.wasPressedThisFrame) Fire();
        }

        private void OnGUI()
        {
            if (turns == null || player == null) return;

            var currentWeapon = weaponController != null ? weaponController.CurrentWeapon : null;
            string weaponName = currentWeapon != null ? currentWeapon.WeaponName : "Standard";
            Sprite weaponIcon = currentWeapon != null ? currentWeapon.Icon : null;

            GUI.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            GUI.Box(new Rect(15, 15, 480, 85), GUIContent.none);

            if (weaponIcon != null && weaponIcon.texture != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(25, 22, 70, 70), weaponIcon.texture, ScaleMode.ScaleToFit);
            }

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(105, 20, 380, 75));
            GUILayout.Label($"<b>Weapon:</b> <color=#FFA726>{weaponName}</color> | <b>Turn:</b> Player {turns.CurrentPlayerIndex + 1}");
            GUILayout.Label("<size=11><b>Keys [1-7]:</b> Rocket | Mortar | Railgun | Floater | Bouncy | Cluster | Digger</size>");
            GUILayout.Label("<size=11><b>Controls:</b> [A/D] Move | [W/S] Aim | [Space] Fire | [R] Restart</size>");
            GUILayout.EndArea();
        }

        private void Clear()
        {
            if (moving && CanAct) actions.Stop(0);
            moving = false;
            System.Array.Clear(held, 0, held.Length);
        }

        private void OnDisable() => Clear();
        private void OnApplicationFocus(bool focus) { if (!focus) Clear(); }
        private void OnApplicationPause(bool pause) { if (pause) Clear(); }
        private void OnValidate()
        {
            if (float.IsNaN(aimRate) || float.IsInfinity(aimRate)) aimRate = 60f;
            aimRate = Mathf.Max(1f, aimRate);
        }
    }
}
