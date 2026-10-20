using System;

namespace TankGame.Multiplayer
{
    /// <summary>Composes room discovery with the existing session's start paths, without gameplay knowledge.</summary>
    public sealed class LanRoomBrowser : IDisposable
    {
        private readonly IMultiplayerService service;
        private readonly Func<int, bool> startHost;
        private readonly Func<string, int, bool> startClient;
        private readonly Func<bool> hostReady;
        private string roomName;
        private int gameplayPort;
        private bool paused, resumeBrowsing, advertiseFailed, disposed;
        public LanDiscoveryService Discovery { get; }
        public event Action<string> Error;

        public LanRoomBrowser(IMultiplayerService service, Func<int, bool> startHost,
            Func<string, int, bool> startClient, Func<bool> hostReady, int discoveryPort = 7778)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.startHost = startHost ?? throw new ArgumentNullException(nameof(startHost));
            this.startClient = startClient ?? throw new ArgumentNullException(nameof(startClient));
            this.hostReady = hostReady ?? throw new ArgumentNullException(nameof(hostReady));
            Discovery = new LanDiscoveryService(discoveryPort);
            Discovery.Error += OnError;
        }

        public bool CreateRoom(string name, int port = 7777)
        {
            EnsureStopped();
            if (!LanDiscoveryProtocol.IsValidName(name)) throw new ArgumentException("Enter a room name (1-48 characters).");
            if (port < 1 || port > 65535 || port == Discovery.DiscoveryPort) throw new ArgumentOutOfRangeException(nameof(port));
            Discovery.StopBrowsing(); roomName = name.Trim(); gameplayPort = port; advertiseFailed = false;
            bool started = startHost(port);
            if (!started) roomName = null;
            return started;
        }

        public bool FindRooms() { EnsureStopped(); return Discovery.StartBrowsing(); }
        public void Refresh() { EnsureStopped(); Discovery.Refresh(); }
        public void Back() { resumeBrowsing = false; Discovery.StopBrowsing(); }

        public bool Join(LanRoom selected)
        {
            EnsureStopped();
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            LanRoom current = null;
            foreach (var room in Discovery.Rooms) if (room.Key == selected.Key) { current = room; break; }
            if (current == null || !current.Joinable || current.PlayerCount >= current.MaxPlayers)
                throw new InvalidOperationException("This room expired or is no longer joinable. Refresh rooms.");
            return ConnectManually(current.Address, current.GameplayPort);
        }

        public bool ConnectManually(string address, int port = 7777)
        {
            EnsureStopped();
            Discovery.StopBrowsing(); resumeBrowsing = false;
            return startClient(address, port);
        }

        public void Pump()
        {
            if (disposed || paused) return;
            if (service.IsRunning) Discovery.StopBrowsing();
            bool ready = roomName != null && service.State == MultiplayerState.Running && service.IsHost
                && service.IsConnected && service.ConnectedClientCount == 1 && hostReady();
            if (!ready)
            {
                if (service.IsHost && service.IsConnected && service.ConnectedClientCount >= 2)
                    Discovery.UpdateCapacity(2, false);
                else Discovery.StopAdvertising();
            }
            else if (!Discovery.IsAdvertising && !advertiseFailed)
            {
                try { Discovery.StartAdvertising(roomName, gameplayPort); }
                catch (Exception error) when (error is System.Net.Sockets.SocketException || error is System.Net.NetworkInformation.NetworkInformationException || error is NotSupportedException)
                { advertiseFailed = true; OnError("Room host is running, but discovery failed: " + error.Message + ". Use manual IP."); }
            }
            if (service.State == MultiplayerState.Stopped) { roomName = null; advertiseFailed = false; }
            Discovery.Pump();
        }

        public void SetPaused(bool value)
        {
            if (disposed || paused == value) return;
            paused = value;
            if (value)
            {
                resumeBrowsing = Discovery.IsBrowsing;
                Discovery.StopBrowsing(); Discovery.StopAdvertising();
            }
            else
            {
                advertiseFailed = false;
                if (resumeBrowsing && !service.IsRunning) Discovery.StartBrowsing();
                resumeBrowsing = false;
            }
        }

        private void OnError(string message) { advertiseFailed = true; Error?.Invoke(message); }
        private void EnsureStopped()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LanRoomBrowser));
            if (paused || service.IsRunning) throw new InvalidOperationException("Wait for the current session to stop before starting or joining.");
        }
        public void Dispose() { if (disposed) return; disposed = true; Discovery.Error -= OnError; Discovery.Dispose(); }
    }
}
