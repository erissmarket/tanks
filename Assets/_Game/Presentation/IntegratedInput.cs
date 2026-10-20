using TankGame.Core;
using TankGame.Tank;
using TankGame.Testing;
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
        private readonly bool[] held = new bool[5];
        private bool moving;
        private int pointerFrame = -1;
        private GameplayTouchControls controls;
        private bool focused = true, paused;

        private void OnEnable()
        {
            if (controls == null) controls = gameObject.AddComponent<GameplayTouchControls>();
            controls.enabled = true;
            controls.Bind(() => CanAct, value => actions.Move(0, value), StopIfAuthorized,
                (direction, power) => actions.Aim(0, direction, power), Fire,
                direction => AimPowerJoystick.Constrain(direction, false, aim.transform.eulerAngles.z),
                () => aim.WorldAimDirection, () => actions.GetMovementRemaining(0), false);
        }

        public bool CanAct => isActiveAndEnabled && focused && !paused && gameManager.CurrentState == MatchState.TurnActive
            && turns.CurrentPlayerIndex == 0 && player.CanAct;
        public bool CanRestart => bootstrap.CanRestart;

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
            actions.Fire(0, tip - basis, controls != null ? controls.Power : 1f);
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
            if (moving) StopIfAuthorized();
            moving = false;
            System.Array.Clear(held, 0, held.Length);
        }

        private void OnDisable() { Clear(); if (controls != null) controls.enabled = false; }
        private void StopIfAuthorized()
        {
            if (actions != null && actions.isActiveAndEnabled && gameManager != null && gameManager.CurrentState == MatchState.TurnActive
                && turns.CurrentPlayerIndex == 0 && player.CanAct) actions.Stop(0);
        }
        private void OnApplicationFocus(bool focus) { if (!focus) Clear(); focused = focus; }
        private void OnApplicationPause(bool pause) { if (pause) Clear(); paused = pause; }
        private void OnValidate()
        {
            if (float.IsNaN(aimRate) || float.IsInfinity(aimRate)) aimRate = 60f;
            aimRate = Mathf.Max(1f, aimRate);
        }
    }
}
