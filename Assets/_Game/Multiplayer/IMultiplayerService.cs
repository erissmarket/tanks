using System;

namespace TankGame.Multiplayer
{
    public enum MultiplayerState
    {
        Stopped,
        Starting,
        Running,
        Stopping
    }

    public interface IMultiplayerService
    {
        MultiplayerState State { get; }
        bool IsRunning { get; }
        bool IsHost { get; }
        bool IsClient { get; }
        bool IsConnected { get; }
        // Includes the host's local client.
        int ConnectedClientCount { get; }

        event Action<MultiplayerState> StateChanged;
        event Action<ulong> ClientConnected;
        event Action<ulong> ClientDisconnected;
        event Action<string> ConnectionFailed;

        bool StartHost(int port = 7777);
        bool StartClient(string ip, int port = 7777);
        void Disconnect();
    }
}
