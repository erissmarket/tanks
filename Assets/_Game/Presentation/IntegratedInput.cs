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
        private bool waitingForNeutral;
        private int pointerFrame = -1;
        public bool UiBlocked { get; private set; }
        public float AimAngle=>aim!=null?aim.CurrentAimAngle:0;
        public WeaponData CurrentWeapon=>weaponController!=null?weaponController.CurrentWeapon:null;
        public void SetUiBlocked(bool blocked){UiBlocked=blocked;if(blocked)Clear();}
        public void BindHoldButtons(Button[] buttons)=>holdButtons=buttons;

        public bool CanAct => isActiveAndEnabled && !UiBlocked && !actions.IsPaused && gameManager.CurrentState == MatchState.TurnActive
            && turns.CurrentPlayerIndex == 0 && player.CanAct;
        public bool CanRestart => !UiBlocked && bootstrap.CanRestart;
        public bool CanFire => CanAct && weaponController != null && weaponController.CanFire;

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
                if (player != null) player.GetComponent<TankGame.Tank.TankMotor>().Stop();
                moving = false;
            }
        }

        public void Fire()
        {
            pointerFrame = Time.frameCount;
            if (!CanFire) return;
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
            Keyboard keyboard = Keyboard.current;
            bool pointer = System.Array.Exists(held, value => value) || pointerFrame == Time.frameCount;
            if (CanRestart && keyboard != null && !pointer && keyboard.rKey.wasPressedThisFrame) { Restart(); return; }
            if (!CanAct) { Clear(); return; }

            // A key held during pause or the opponent's turn cannot become a new command on resume.
            if (waitingForNeutral)
            {
                if (keyboard != null && (keyboard.aKey.isPressed || keyboard.dKey.isPressed || keyboard.wKey.isPressed || keyboard.sKey.isPressed || keyboard.spaceKey.isPressed)) return;
                waitingForNeutral = false;
            }
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

        private void Clear()
        {
            waitingForNeutral = true;
            if (moving && player != null) player.GetComponent<TankGame.Tank.TankMotor>().Stop();
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
