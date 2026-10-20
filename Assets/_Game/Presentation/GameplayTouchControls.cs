using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TankGame.Presentation
{
    /// <summary>Reusable runtime test controls; pointer IDs keep movement, aiming and Fire independent.</summary>
    public sealed class GameplayTouchControls : MonoBehaviour
    {
        private readonly Dictionary<int, int> pointers = new Dictionary<int, int>();
        private Func<bool> canAct;
        private Func<Vector2, Vector2> constrain;
        private Func<Vector2> currentDirection;
        private Func<float> remaining;
        private Action<float> move;
        private Action stop, fire, finishAim;
        private Action<Vector2, float> aim;
        private bool fullControls, wasActive, driving;
        private bool focused = true, paused;
        private bool Allowed => isActiveAndEnabled && focused && !paused && canAct != null && canAct();
        private float nextRenewal;
        private Vector2 displacement;
        public Vector2 Direction { get; private set; } = new Vector2(0.7071068f, 0.7071068f);
        public float Power { get; private set; } = 1f;
        public static Rect LeftRect => new Rect(640, 492, 105, 78);
        public static Rect RightRect => new Rect(880, 492, 105, 78);
        public static Rect FireRect => new Rect(755, 492, 115, 78);
        public static Vector2 JoystickCenter => new Vector2(815, 330);
        public const float JoystickRadius = 105;

        public void Bind(Func<bool> permission, Action<float> move, Action stop, Action<Vector2, float> aim,
            Action fire, Func<Vector2, Vector2> constrain, Func<Vector2> currentDirection, Func<float> remaining, bool fullControls, Action finishAim = null)
        {
            CancelInput(); canAct = permission; this.move = move; this.stop = stop; this.aim = aim; this.fire = fire;
            this.constrain = constrain; this.currentDirection = currentDirection; this.remaining = remaining; this.fullControls = fullControls;
            this.finishAim = finishAim;
        }

        // Coordinates use the 1000x600 safe-area canvas, Y up for the intended firing vector.
        public void PointerDown(int id, Vector2 point)
        {
            if (!Allowed || pointers.ContainsKey(id)) return;
            int role = fullControls && LeftRect.Contains(point) ? 1 : fullControls && RightRect.Contains(point) ? 2
                : fullControls && FireRect.Contains(point) ? 4
                : (point - JoystickCenter).sqrMagnitude <= JoystickRadius * JoystickRadius && !pointers.ContainsValue(3) ? 3 : 0;
            if (role == 0) return;
            pointers.Add(id, role);
            if (role == 4) fire();
            else if (role == 3) PointerDrag(id, point);
            else RenewMovement();
        }

        public void PointerDrag(int id, Vector2 point)
        {
            if (!pointers.TryGetValue(id, out int role)) return;
            if (!Allowed) { CancelInput(); return; }
            if ((role == 1 && !LeftRect.Contains(point)) || (role == 2 && !RightRect.Contains(point))) { PointerUp(id); return; }
            if (role != 3) return;
            Vector2 delta = point - JoystickCenter;
            if (!AimPowerJoystick.TryMap(new Vector2(delta.x, -delta.y), JoystickRadius, 0.15f, out var direction, out float power)) return;
            Direction = constrain(direction).normalized; Power = power;
            displacement = Vector2.ClampMagnitude(delta, JoystickRadius);
            aim(Direction, Power);
        }

        public void PointerUp(int id)
        {
            if (!pointers.TryGetValue(id, out int role)) return;
            pointers.Remove(id);
            if (role == 1 || role == 2) RenewMovement();
            if (role == 3 && Allowed) finishAim?.Invoke();
            // Joystick release deliberately preserves direction/power and never fires.
        }

        public void CancelInput() => CancelInput(true);
        public void CancelInput(bool commitAim)
        {
            bool hadAim = pointers.ContainsValue(3);
            pointers.Clear();
            if (hadAim && Allowed && commitAim) finishAim?.Invoke();
            if (driving) stop?.Invoke();
            driving = false; nextRenewal = 0;
        }

        private void RenewMovement()
        {
            float value = (pointers.ContainsValue(2) ? 1 : 0) - (pointers.ContainsValue(1) ? 1 : 0);
            if (Allowed)
            {
                if (value != 0) { move(value); driving = true; }
                else if (driving) { stop(); driving = false; }
            }
            nextRenewal = Time.unscaledTime + 0.08f;
        }

        private Vector2 CanvasPoint(Vector2 screen)
        {
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / 1000, safe.height / 600);
            return new Vector2((screen.x - safe.x) / scale, (safe.yMax - screen.y) / scale);
        }

        private void Update()
        {
            bool active = Allowed;
            if (!active) { CancelInput(); wasActive = false; return; }
            if (!wasActive) { Direction = currentDirection(); Power = 1f; displacement = Vector2.zero; wasActive = true; }
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                {
                    int id = touch.touchId.ReadValue(); var phase = touch.phase.ReadValue();
                    Vector2 point = CanvasPoint(touch.position.ReadValue());
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        // Consume this frame's aim samples before any separate Fire touch.
                        if (!fullControls || !FireRect.Contains(point)) PointerDown(id, point);
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled) PointerUp(id);
                    else if (touch.press.isPressed) PointerDrag(id, point);
                }
            if (fullControls && Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    {
                        Vector2 point = CanvasPoint(touch.position.ReadValue());
                        if (FireRect.Contains(point)) PointerDown(touch.touchId.ReadValue(), point);
                    }
            if (Mouse.current != null && (Touchscreen.current == null || !Touchscreen.current.primaryTouch.press.isPressed))
            {
                var mouse = Mouse.current; Vector2 point = CanvasPoint(mouse.position.ReadValue());
                if (mouse.leftButton.wasPressedThisFrame) PointerDown(int.MinValue, point);
                else if (mouse.leftButton.wasReleasedThisFrame) PointerUp(int.MinValue);
                else if (mouse.leftButton.isPressed) PointerDrag(int.MinValue, point);
            }
            if (driving && Time.unscaledTime >= nextRenewal) RenewMovement();
        }

        private void OnGUI()
        {
            if (canAct == null) return;
            Rect safe = Screen.safeArea; float scale = Mathf.Min(safe.width / 1000, safe.height / 600);
            if (scale <= 0) return;
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = canAct() ? Color.white : Color.gray;
            var label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(705, 220, 220, 220), "");
            GUI.Label(new Rect(640, 155, 350, 60), "World aim " + Mathf.RoundToInt(Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg)
                + "°  Power " + Mathf.RoundToInt(Power * 100) + "%", label);
            GUI.Box(new Rect(JoystickCenter.x + displacement.x - 20, JoystickCenter.y + displacement.y - 20, 40, 40), "+");
            if (fullControls)
            {
                GUI.Box(LeftRect, "LEFT"); GUI.Box(RightRect, "RIGHT"); GUI.Box(FireRect, "FIRE");
                GUI.Label(new Rect(640, 438, 350, 45), "Movement left " + remaining().ToString("0.00"), label);
            }
            GUI.matrix = matrix; GUI.color = color;
        }

        private void OnDisable() { CancelInput(); wasActive = false; }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelInput(); this.focused = focused; }
        private void OnApplicationPause(bool paused) { if (paused) CancelInput(); this.paused = paused; }
    }
}
