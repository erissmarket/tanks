using System;
using UnityEngine;

namespace TankGame.Multiplayer
{
    [RequireComponent(typeof(NgoMultiplayerService))]
    public sealed class LanTestPanel : MonoBehaviour
    {
        private IMultiplayerService service;
        private string ip = "127.0.0.1";
        private string port = "7777";
        private string message = "Ready";

        private void Awake() => service = GetComponent<NgoMultiplayerService>();

        private void OnEnable()
        {
            service.StateChanged += OnStateChanged;
            service.ConnectionFailed += OnConnectionFailed;
        }

        private void OnDisable()
        {
            service.StateChanged -= OnStateChanged;
            service.ConnectionFailed -= OnConnectionFailed;
        }

        private void OnStateChanged(MultiplayerState state) => message = state.ToString();
        private void OnConnectionFailed(string reason) => message = reason;

        private void OnGUI()
        {
            float scale = Mathf.Max(0.5f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(24, 24, 440, 460), GUI.skin.box);
            GUILayout.Label("LAN foundation test — no gameplay synchronization");
            GUILayout.Label("IP address");
            ip = GUILayout.TextField(ip, 64, GUILayout.Height(36));
            GUILayout.Label("Port");
            port = GUILayout.TextField(port, 5, GUILayout.Height(36));
            GUI.enabled = !service.IsRunning;
            if (GUILayout.Button("HOST", GUILayout.Height(44))) Run(() => service.StartHost(ReadPort()));
            if (GUILayout.Button("CONNECT", GUILayout.Height(44))) Run(() => service.StartClient(ip, ReadPort()));
            GUI.enabled = service.IsRunning;
            if (GUILayout.Button("DISCONNECT", GUILayout.Height(44))) service.Disconnect();
            GUI.enabled = true;
            GUILayout.Label("State: " + service.State + "  Connected: " + service.IsConnected);
            GUILayout.Label("Connected clients (including host): " + service.ConnectedClientCount);
            GUILayout.Label(message);
            GUILayout.EndArea();
            GUI.matrix = previous;
        }

        private int ReadPort()
        {
            if (!int.TryParse(port, out int value)) throw new ArgumentException("Enter a numeric port.");
            return value;
        }

        private void Run(Func<bool> command)
        {
            try { if (!command()) message = "Start failed; wait for shutdown before retrying."; }
            catch (Exception error) { message = error.Message; }
        }
    }
}
