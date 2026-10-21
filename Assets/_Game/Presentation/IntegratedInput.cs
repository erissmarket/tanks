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
                Debug.Log($"[MVP Weapon Switch] Selected: {availableWeapons[index].WeaponName}");
            }
        }

        public void Hold(int command, bool pressed)
        {
            if (command < 0 || command >= held.Length) throw new System.ArgumentOutOfRangeException(nameof(command));
            pointerFrame = Time.frameCount;
            held[command] = pressed && CanAct;
            if (!pressed && command < 2 && moving)
            {
                if (CanAct) actions.Stop(0);
                moving = false;
            }
        }

        public void Fire()
        {
            pointerFrame = Time.frameCount;
            if (!CanAct) return;
            Vector2 tip = barrel.TransformPoint(new Vector3(0.45f, 0f, 0f));
            Vector2 basis = barrel.TransformPoint(new Vector3(-0.45f, 0f, 0f));
            actions.Fire(0, tip - basis);
        }

        public void Restart()
        {
            pointerFrame = Time.frameCount;
            if (CanRestart) bootstrap.RestartTestMatch();
        }

        private void Update()
        {
            foreach (Button button in holdButtons) button.interactable = CanAct;
            Keyboard keyboard = Application.isEditor ? Keyboard.current : null;
            bool pointer = System.Array.Exists(held, value => value) || pointerFrame == Time.frameCount;
            if (CanRestart && keyboard != null && !pointer && keyboard.rKey.wasPressedThisFrame) { Restart(); return; }
            if (!CanAct) { Clear(); return; }

            // Weapon switching with numeric keys 1 to 7
            if (keyboard != null && availableWeapons != null && availableWeapons.Length > 0)
            {
                for (int i = 0; i < Mathf.Min(availableWeapons.Length, 7); i++)
                {
                    Key digitKey = Key.Digit1 + i;
                    Key numpadKey = Key.Numpad1 + i;
                    if (keyboard[digitKey].wasPressedThisFrame || keyboard[numpadKey].wasPressedThisFrame)
                    {
                        SelectWeapon(i);
                        break;
                    }
                }
            }

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
