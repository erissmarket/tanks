using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TankGame.Presentation;

namespace TankGame.Multiplayer
{
    [RequireComponent(typeof(LanGameplaySession))]
    public sealed class LanGameplayPanel : MonoBehaviour
    {
        private LanGameplaySession session;
        [SerializeField] private int discoveryPort = 7778;
        private LanRoomBrowser browser;
        private string roomName = "Tank Room", selectedRoom;
        private Vector2 roomScroll;
        private string ip = "", port = "7777", status = "Choose HOST or CONNECT";
        private GameplayTouchControls controls;
        private int editingField;
        private TouchScreenKeyboard keyboard;
        private GUIStyle labelStyle, buttonStyle, fieldStyle;

        private void Awake()
        {
            session = GetComponent<LanGameplaySession>();
            Application.runInBackground = true;
        }
        private void OnEnable()
        {
            session.Adapter.CommandProcessed += OnCommand;
            browser = new LanRoomBrowser(session.Service, session.StartHost, session.Connect,
                () => !session.Adapter.SessionAborted && session.Stream.Latest != null, discoveryPort);
            browser.Error += OnDiscoveryError;
            if (controls == null) controls = gameObject.AddComponent<GameplayTouchControls>();
            controls.enabled = true;
            controls.Bind(() => session.Adapter.CanSubmit && session.Stream.Latest != null && keyboard == null,
                value => Run(() => session.Adapter.Move(value)), () => Run(session.Adapter.Stop),
                (direction, power) => Run(() => session.Adapter.Aim(direction, power)), () => Run(Fire),
                direction => AimPowerJoystick.Constrain(direction, session.Adapter.LocalPlayerIndex == 1, ObservedTank().Rotation),
                () => (Vector2)(Quaternion.Euler(0, 0, ObservedTank().Rotation) * new Vector2(
                    (session.Adapter.LocalPlayerIndex == 1 ? -1 : 1) * Mathf.Cos(ObservedTank().Aim * Mathf.Deg2Rad),
                    Mathf.Sin(ObservedTank().Aim * Mathf.Deg2Rad))),
                () => ObservedTank().MovementRemaining, true, () => Run(() => session.Adapter.CommitAim(controls.Direction, controls.Power)));
        }
        private void OnDisable()
        {
            StopMovement();
            if (controls != null) controls.enabled = false;
            session.Adapter.CommandProcessed -= OnCommand;
            if (keyboard != null) keyboard.active = false;
            browser.Error -= OnDiscoveryError;
            browser.Dispose(); browser = null;
        }
        private void OnDiscoveryError(string message) => status = message;
        private void OnCommand(ulong sender, ulong sequence, GameplayCommandResult result) => status = "Command " + sequence + ": " + result;

        private void Update()
        {
            browser.Pump();
            if (keyboard == null) return;
            if (keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                if (editingField == 0) ip = keyboard.text;
                else if (editingField == 1) port = keyboard.text;
                else roomName = keyboard.text;
                keyboard = null;
            }
            else if (keyboard.status == TouchScreenKeyboard.Status.Canceled
                || keyboard.status == TouchScreenKeyboard.Status.LostFocus) keyboard = null;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) StopMovement(); }
        private void OnApplicationPause(bool paused)
        {
            if (paused) StopMovement();
            if (browser != null) Run(() => { browser.SetPaused(paused); return true; });
        }

        private void StopMovement()
        {
            if (controls != null) controls.CancelInput();
        }

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
                fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 24 };
            }
            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / 1000f, safe.height / 600f);
            if (scale <= 0f) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0), Quaternion.identity,
                new Vector3(scale, scale, 1));
            GUILayout.BeginArea(new Rect(8, 8, 360, 584), GUI.skin.box);
            roomScroll = GUILayout.BeginScrollView(roomScroll);
            GUILayout.Label("LAN rooms / Wi-Fi or hotspot", labelStyle);
            GUI.enabled = !session.Service.IsRunning && keyboard == null;
            if (!session.Service.IsRunning)
            {
                if (browser.Discovery.IsBrowsing)
                {
                    GUILayout.BeginHorizontal();
                    if (Button("REFRESH")) Run(() => { browser.Refresh(); selectedRoom = null; return true; });
                    if (Button("BACK")) browser.Back();
                    GUILayout.EndHorizontal();
                    LanRoom selected = null;
                    // GUI input can stop/clear discovery; render a stable copy of this frame's list.
                    var entries = new System.Collections.Generic.List<LanRoom>(browser.Discovery.Rooms);
                    foreach (var room in entries)
                    {
                        if (GUILayout.Toggle(selectedRoom == room.Key, room.Name + "  " + room.PlayerCount + "/" + room.MaxPlayers
                            + (room.Joinable ? " - available" : " - full"), buttonStyle, GUILayout.Height(58))) selectedRoom = room.Key;
                        if (selectedRoom == room.Key) selected = room;
                    }
                    if (entries.Count == 0) GUILayout.Label("Searching... Broadcast may be blocked; manual IP is below.", labelStyle);
                    GUI.enabled = selected != null && selected.Joinable && keyboard == null;
                    if (Button("JOIN SELECTED")) Run(() => browser.Join(selected));
                    GUI.enabled = !session.Service.IsRunning && keyboard == null;
                }
                else
                {
                    ConnectionField("Room name", ref roomName, 2, 48);
                    if (Button("CREATE ROOM")) Run(() => browser.CreateRoom(roomName, ReadPort()));
                    if (Button("FIND ROOMS")) Run(browser.FindRooms);
                }
                GUILayout.Label("Optional manual fallback", labelStyle);
                ConnectionField("Host IPv4", ref ip, 0, 64);
                ConnectionField("Gameplay port", ref port, 1, 5);
                if (Button("CONNECT MANUALLY")) Run(() => browser.ConnectManually(ip, ReadPort()));
            }
            else if (session.Service.IsHost)
                GUILayout.Label(roomName + (session.Service.ConnectedClientCount < 2 ? " - Waiting for Player 1" : " - Player 1 connected")
                    + (browser.Discovery.IsAdvertising ? " / discoverable" : ""), labelStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled = session.Service.IsRunning;
            if (Button("DISCONNECT")) { StopMovement(); session.Disconnect(); }
            GUI.enabled = !session.Service.IsRunning;
            if (Button("NEW SESSION")) SceneManager.LoadScene(gameObject.scene.name);
            GUILayout.EndHorizontal();
            GUI.enabled = session.Service.IsHost && !session.Adapter.SessionAborted
                && session.Stream.Latest != null && session.Stream.Latest.State == TankGame.Core.MatchState.MatchEnded;
            if (Button("RESTART MATCH")) Run(session.RestartMatch);
            GUI.enabled = true;
            GUILayout.Label("Transport: " + session.Service.State + " / Clients: " + session.Service.ConnectedClientCount, labelStyle);
            GUILayout.Label("You control: Player " + session.Adapter.LocalPlayerIndex, labelStyle);
            GUILayout.Label(status, labelStyle);
            if (session.Stream.Latest != null)
            {
                var s = session.Stream.Latest;
                GUILayout.Label(s.State + " / Player " + s.CurrentPlayer, labelStyle);
                GUILayout.Label("Health " + s.PlayerZero.Health + " / " + s.PlayerOne.Health + " | Craters " + s.CraterCount, labelStyle);
            }
            GUILayout.Label("After disconnect: wait for Stopped, then NEW SESSION on both peers. No ownership reclaim.", labelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.matrix = previousMatrix;
            GUI.enabled = previousEnabled;
        }

        private bool Button(string text, float height = 52) => GUILayout.Button(text, buttonStyle, GUILayout.Height(height));

        private void ConnectionField(string title, ref string value, int field, int maximumLength)
        {
            GUILayout.Label(title, labelStyle);
            if (!Application.isMobilePlatform) value = GUILayout.TextField(value, maximumLength, fieldStyle, GUILayout.Height(46));
            else if (GUILayout.Button(string.IsNullOrEmpty(value) ? "Tap to enter" : value, buttonStyle, GUILayout.Height(46)))
            {
                editingField = field;
                keyboard = TouchScreenKeyboard.Open(value, field == 2 ? TouchScreenKeyboardType.Default : TouchScreenKeyboardType.NumbersAndPunctuation,
                    false, false, false, false, title, maximumLength);
            }
        }

        private bool Fire()
        {
            bool accepted = session.Adapter.Fire(controls.Direction, controls.Power);
            if (accepted) controls.CancelInput(false);
            return accepted;
        }

        private NetworkTankState ObservedTank() => session.Stream.Latest == null ? default
            : session.Adapter.LocalPlayerIndex == 1 ? session.Stream.Latest.PlayerOne : session.Stream.Latest.PlayerZero;

        private int ReadPort() { if (!int.TryParse(port, out int value)) throw new ArgumentException("Invalid port."); return value; }
        private void Run(Func<bool> action) { try { if (!action()) status = "Not queued/accepted; check turn and connection."; } catch (Exception e) { status = e.Message; } }
    }
}
