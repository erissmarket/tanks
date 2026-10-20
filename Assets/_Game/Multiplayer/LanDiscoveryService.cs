using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TankGame.Multiplayer
{
    /// <summary>Nonblocking UDP discovery. Call all methods, including Pump, on the owning/main thread.</summary>
    public sealed class LanDiscoveryService : IDisposable
    {
        public const double RoomLifetimeSeconds = 6;
        private sealed class Seen { internal LanRoom Room; internal double Time; }
        private readonly Dictionary<string, Seen> seen = new Dictionary<string, Seen>();
        private readonly Dictionary<IPEndPoint, ulong> browsers = new Dictionary<IPEndPoint, ulong>();
        private readonly List<LanRoom> rooms = new List<LanRoom>();
        private readonly ReadOnlyCollection<LanRoom> view;
        private readonly byte[] buffer = new byte[LanDiscoveryProtocol.MaximumPacketSize + 1];
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Func<LanDiscoveryNetwork> readNetwork;
        private readonly Func<double> now;
        private Socket socket;
        private LanDiscoveryNetwork network;
        private bool disposed;
        private double nextRequest, nextNetworkCheck;
        private ulong nonce, sequence;
        private Guid roomId;
        private string roomName;
        private int gameplayPort, playerCount;
        public int DiscoveryPort { get; }
        public bool IsBrowsing { get; private set; }
        public bool IsAdvertising { get; private set; }
        public IReadOnlyList<LanRoom> Rooms => view;
        public event Action<LanRoom> RoomAdded;
        public event Action<LanRoom> RoomUpdated;
        public event Action<LanRoom> RoomRemoved;
        public event Action<string> Error;

        public LanDiscoveryService(int discoveryPort = 7778) : this(discoveryPort, LanDiscoveryNetwork.Read, null) { }

        internal LanDiscoveryService(int port, Func<LanDiscoveryNetwork> networks, Func<double> time)
        {
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            DiscoveryPort = port; readNetwork = networks; now = time ?? (() => clock.Elapsed.TotalSeconds);
            view = rooms.AsReadOnly();
        }

        public bool StartBrowsing()
        {
            CheckDisposed();
            if (IsBrowsing) return false;
            if (IsAdvertising) throw new InvalidOperationException("Stop advertising before browsing.");
            network = readNetwork(); OpenSocket(false);
            IsBrowsing = true; Refresh(); nextNetworkCheck = now() + 2;
            return true;
        }

        public void Refresh()
        {
            CheckDisposed();
            if (!IsBrowsing) throw new InvalidOperationException("Start discovery first.");
            ClearRooms(); nonce = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0);
            if (nonce == 0) nonce = 1;
            nextRequest = 0;
        }

        public bool StartAdvertising(string name, int port, int count = 1)
        {
            CheckDisposed();
            if (!LanDiscoveryProtocol.IsValidName(name)) throw new ArgumentException("Room name must be 1-48 characters and at most 96 UTF-8 bytes.");
            if (port < 1 || port > 65535 || port == DiscoveryPort) throw new ArgumentOutOfRangeException(nameof(port));
            if (count != 1) throw new ArgumentOutOfRangeException(nameof(count), "Only a waiting 1v1 host is joinable.");
            if (IsAdvertising) return false;
            if (IsBrowsing) throw new InvalidOperationException("Stop browsing before advertising.");
            network = readNetwork(); OpenSocket(true);
            roomId = Guid.NewGuid(); roomName = name; gameplayPort = port; playerCount = count; sequence = 0;
            browsers.Clear();
            IsAdvertising = true; nextNetworkCheck = now() + 2;
            return true;
        }

        /// <summary>A full room withdraws immediately; existing browser entries expire after six seconds.</summary>
        public void UpdateCapacity(int count, bool joinable)
        {
            if (count < 1 || count > 2) throw new ArgumentOutOfRangeException(nameof(count));
            if (!IsAdvertising) return;
            playerCount = count;
            if (!joinable || count == 2) StopAdvertising();
        }

        public void StopBrowsing()
        {
            if (!IsBrowsing) return;
            IsBrowsing = false; CloseSocket(); ClearRooms(); nonce = 0;
        }

        public void StopAdvertising()
        {
            if (!IsAdvertising) return;
            IsAdvertising = false;
            try
            {
                // Best-effort final unavailable announcement. Lost UDP still falls back to bounded expiry.
                foreach (var browser in browsers)
                {
                    byte[] packet = LanDiscoveryProtocol.Announcement(browser.Value,
                        new LanRoom(roomId, roomName, "", gameplayPort, playerCount, false, ++sequence));
                    try { socket?.SendTo(packet, browser.Key); }
                    catch (SocketException) { /* Teardown must release the socket even when the network disappeared. */ }
                }
            }
            finally { browsers.Clear(); CloseSocket(); }
        }

        public void Pump()
        {
            if (disposed || socket == null) return;
            double time = now();
            try
            {
                if (time >= nextNetworkCheck)
                {
                    nextNetworkCheck = time + 2;
                    var current = readNetwork();
                    if (current.Signature != network.Signature)
                    {
                        bool advertise = IsAdvertising;
                        CloseSocket(); network = current; OpenSocket(advertise);
                        if (IsBrowsing) Refresh();
                    }
                }
                if (IsBrowsing && time >= nextRequest)
                {
                    nextRequest = time + 1;
                    byte[] request = LanDiscoveryProtocol.Request(nonce);
                    int sent = 0; SocketException lastError = null;
                    foreach (var destination in network.Destinations)
                    {
                        try { socket.SendTo(request, new IPEndPoint(destination, DiscoveryPort)); sent++; }
                        catch (SocketException error) { lastError = error; }
                    }
                    if (sent == 0 && lastError != null) throw lastError;
                }
                // Bound work and allocations per frame even on a noisy/untrusted network.
                for (int i = 0; i < 32 && socket.Poll(0, SelectMode.SelectRead); i++)
                {
                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    int length;
                    try { length = socket.ReceiveFrom(buffer, ref remote); }
                    catch (SocketException error) when (error.SocketErrorCode == SocketError.MessageSize) { continue; }
                    var sender = (IPEndPoint)remote;
                    // Loopback is allowed for independent-process diagnostics; no host IP is read from the payload.
                    byte first = sender.Address.GetAddressBytes()[0];
                    if (first == 0 || first >= 224 || !LanDiscoveryProtocol.TryRead(buffer, length, sender.Address.ToString(), out ulong token, out var room)) continue;
                    if (IsAdvertising && room == null)
                    {
                        if (browsers.ContainsKey(sender) || browsers.Count < 64) browsers[sender] = token;
                        var advertisement = new LanRoom(roomId, roomName, "", gameplayPort, playerCount, true, ++sequence);
                        byte[] reply = LanDiscoveryProtocol.Announcement(token, advertisement);
                        socket.SendTo(reply, sender);
                    }
                    else if (IsBrowsing && room != null && token == nonce && sender.Port == DiscoveryPort) Remember(room, time);
                }
                if (IsBrowsing)
                {
                    for (int i = rooms.Count - 1; i >= 0; i--)
                        if (time - seen[rooms[i].Key].Time >= RoomLifetimeSeconds) Remove(rooms[i]);
                }
            }
            catch (SocketException error) when (error.SocketErrorCode == SocketError.WouldBlock) { }
            catch (Exception error) when (error is SocketException || error is NetworkInformationException || error is System.NotSupportedException)
            {
                StopBrowsing(); StopAdvertising(); Error?.Invoke("LAN discovery: " + error.Message + ". Manual IP remains available.");
            }
        }

        private void Remember(LanRoom room, double time)
        {
            if (seen.TryGetValue(room.Key, out var entry))
            {
                if (room.Sequence <= entry.Room.Sequence) return;
                int index = rooms.IndexOf(entry.Room);
                entry.Time = time; entry.Room = room; rooms[index] = room; RoomUpdated?.Invoke(room);
            }
            else if (rooms.Count < 64)
            {
                seen.Add(room.Key, new Seen { Room = room, Time = time }); rooms.Add(room); RoomAdded?.Invoke(room);
            }
        }

        private void Remove(LanRoom room) { seen.Remove(room.Key); rooms.Remove(room); RoomRemoved?.Invoke(room); }
        private void ClearRooms() { while (rooms.Count > 0) Remove(rooms[rooms.Count - 1]); }

        private void OpenSocket(bool advertise)
        {
            var created = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                created.ExclusiveAddressUse = true;
                created.EnableBroadcast = true; created.Blocking = false;
                created.Bind(new IPEndPoint(IPAddress.Any, advertise ? DiscoveryPort : 0));
                socket = created;
            }
            catch { created.Dispose(); throw; }
        }

        private void CloseSocket() { socket?.Dispose(); socket = null; }
        private void CheckDisposed() { if (disposed) throw new ObjectDisposedException(nameof(LanDiscoveryService)); }
        public void Dispose() { if (disposed) return; StopBrowsing(); StopAdvertising(); CloseSocket(); disposed = true; }
    }
}
