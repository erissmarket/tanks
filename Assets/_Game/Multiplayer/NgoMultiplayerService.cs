using System;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TankGame.Multiplayer
{
    public sealed class NgoMultiplayerService : MonoBehaviour, IMultiplayerService
    {
        private NetworkManager manager;
        private GameObject sessionRoot;
        private bool shutdownRequested;
        private bool destroyingManager;
        private bool failureReported;
        private ushort connectionProtocol;

        public void ConfigureProtocol(ushort version)
        {
            if (IsRunning) throw new InvalidOperationException("Configure compatibility before starting the service.");
            connectionProtocol = version;
        }

        public MultiplayerState State { get; private set; }
        /// <summary>True until asynchronous shutdown and manager disposal finish.</summary>
        public bool IsRunning => State != MultiplayerState.Stopped;
        public bool IsHost => State != MultiplayerState.Stopping && manager != null && manager.IsHost;
        public bool IsClient => State != MultiplayerState.Stopping && manager != null && manager.IsClient;
        public bool IsConnected => State != MultiplayerState.Stopping && manager != null && manager.IsConnectedClient;
        public int ConnectedClientCount => IsConnected ? manager.ConnectedClientsIds.Count : 0;

        // NGO-specific adapters may use messaging without adding game concepts to this service.
        public NetworkManager NetworkManager => manager;

        public event Action<MultiplayerState> StateChanged;
        public event Action<ulong> ClientConnected;
        public event Action<ulong> ClientDisconnected;
        public event Action<string> ConnectionFailed;

        public bool StartHost(int port = 7777)
        {
            ValidatePort(port);
            return StartSession(true, "127.0.0.1", port);
        }

        public bool StartClient(string ip, int port = 7777)
        {
            ValidatePort(port);
            // Manual IPv4 unicast only; do not silently resolve DNS or accept abbreviated IPs.
            string value = ip?.Trim();
            if (string.IsNullOrEmpty(value) || value.Split('.').Length != 4 ||
                !IPAddress.TryParse(value, out IPAddress address) ||
                address.AddressFamily != AddressFamily.InterNetwork || address.ToString() != value ||
                address.GetAddressBytes()[0] == 0 || address.GetAddressBytes()[0] >= 224)
                throw new ArgumentException("Enter a valid IPv4 unicast address.", nameof(ip));
            return StartSession(false, value, port);
        }

        /// <summary>Idempotent. Wait for Stopped before starting another session.</summary>
        public void Disconnect()
        {
            if (State == MultiplayerState.Stopped || State == MultiplayerState.Stopping) return;
            ChangeState(MultiplayerState.Stopping);
        }

        private static void ValidatePort(int port)
        {
            if (port < 1 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
        }

        private bool StartSession(bool host, string ip, int port)
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                throw new InvalidOperationException("The multiplayer service must be enabled in Play Mode.");
            if (IsRunning)
                throw new InvalidOperationException("A session is already starting, running, or stopping.");

            State = MultiplayerState.Starting;
            shutdownRequested = false;
            destroyingManager = false;
            failureReported = false;
            try
            {
                sessionRoot = new GameObject("LAN Network Session");
                // NGO owns persistence for this root; this service owns its disposal.
                manager = sessionRoot.AddComponent<NetworkManager>();
                var transport = sessionRoot.AddComponent<UnityTransport>();
                manager.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    EnableSceneManagement = false,
                    ProtocolVersion = connectionProtocol,
                    PlayerPrefab = null
                };
                transport.SetConnectionData(ip, (ushort)port, host ? "0.0.0.0" : null);
                manager.OnConnectionEvent += OnConnectionEvent;
                manager.OnTransportFailure += OnTransportFailure;
                manager.OnClientStopped += OnStopped;
                manager.OnServerStopped += OnStopped;

                bool started = host ? manager.StartHost() : manager.StartClient();
                if (!started)
                {
                    Fail("NGO could not start the " + (host ? "host." : "client."));
                    return false;
                }
                // A synchronous host callback may already have changed the state.
                if (State == MultiplayerState.Starting)
                    ChangeState(host ? MultiplayerState.Running : MultiplayerState.Starting, true);
                return true;
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        private void OnConnectionEvent(NetworkManager source, ConnectionEventData data)
        {
            if (source != manager || State == MultiplayerState.Stopped) return;
            // A host receives both server and peer notifications for the same remote client.
            if (manager.IsServer && (data.EventType == ConnectionEvent.PeerConnected ||
                                     data.EventType == ConnectionEvent.PeerDisconnected)) return;
            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                case ConnectionEvent.PeerConnected:
                    if (State == MultiplayerState.Stopping) return;
                    if (data.ClientId == manager.LocalClientId) ChangeState(MultiplayerState.Running);
                    Publish(ClientConnected, data.ClientId);
                    break;
                case ConnectionEvent.ClientDisconnected:
                case ConnectionEvent.PeerDisconnected:
                    bool local = data.ClientId == manager.LocalClientId;
                    if (local && State != MultiplayerState.Stopping)
                        Fail(string.IsNullOrEmpty(manager.DisconnectReason) ? "Disconnected from the session." : manager.DisconnectReason);
                    Publish(ClientDisconnected, data.ClientId);
                    break;
            }
        }

        private void OnTransportFailure() => Fail("Unity Transport reported a connection failure.");

        private void OnStopped(bool wasHost)
        {
            if (State == MultiplayerState.Starting) Fail("Connection stopped before it was established.");
            else Disconnect();
        }

        private void Fail(string reason)
        {
            Disconnect();
            if (failureReported) return;
            failureReported = true;
            Publish(ConnectionFailed, reason);
        }

        private void Update()
        {
            if (State != MultiplayerState.Stopping) return;
            if (manager != null)
            {
                if (!shutdownRequested)
                {
                    shutdownRequested = true;
                    manager.Shutdown(true);
                }
                if (manager.IsListening || manager.ShutdownInProgress) return;
                if (!destroyingManager)
                {
                    Unsubscribe();
                    destroyingManager = true;
                    Destroy(sessionRoot);
                }
                return;
            }
            if (sessionRoot != null)
            {
                Destroy(sessionRoot);
                return;
            }
            ChangeState(MultiplayerState.Stopped);
        }

        private void Unsubscribe()
        {
            if (manager == null) return;
            manager.OnConnectionEvent -= OnConnectionEvent;
            manager.OnTransportFailure -= OnTransportFailure;
            manager.OnClientStopped -= OnStopped;
            manager.OnServerStopped -= OnStopped;
        }

        private void OnDisable()
        {
            Disconnect();
            // Update cannot finish disposal while disabled; NGO's OnDestroy performs shutdown.
            Unsubscribe();
            if (manager != null)
            {
                manager.Shutdown(true);
            }
            if (sessionRoot != null) Destroy(sessionRoot);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (manager != null)
            {
                manager.Shutdown(true);
            }
            if (sessionRoot != null) Destroy(sessionRoot);
        }

        private void ChangeState(MultiplayerState state, bool notifyUnchanged = false)
        {
            if (State == state && !notifyUnchanged) return;
            State = state;
            Publish(StateChanged, state);
        }

        private static void Publish<T>(Action<T> handlers, T value)
        {
            if (handlers == null) return;
            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try { handler(value); }
                catch (Exception error) { Debug.LogException(error); }
            }
        }
    }
}
