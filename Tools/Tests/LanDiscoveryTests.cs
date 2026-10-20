using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TankGame.Multiplayer;

internal static class LanDiscoveryTests
{
    private static int checks;
    private static void Check(bool ok, string name)
    { Console.WriteLine((ok ? "PASS " : "FAIL ") + name); if (!ok) throw new Exception(name); checks++; }
    private static bool Rejected(Action action) { try { action(); return false; } catch (ArgumentException) { return true; } catch (InvalidOperationException) { return true; } catch (SocketException error) when (error.SocketErrorCode == SocketError.AddressAlreadyInUse) { return true; } }
    private static LanDiscoveryNetwork Loop(string signature = "test") => new LanDiscoveryNetwork(signature, new[] { IPAddress.Loopback });
    private static LanRoom Room(Guid id, ulong seq = 1, int count = 1, bool available = true, int port = 7777)
        => new LanRoom(id, "Test Room", "127.0.0.1", port, count, available, seq);
    private static bool Read(byte[] bytes) => LanDiscoveryProtocol.TryRead(bytes, bytes.Length, "127.0.0.1", out _, out _);

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0) return Peer(args[0]);
            Protocol(); Sockets(); Routing();
            Console.WriteLine("TOTAL PASS " + checks); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Protocol()
    {
        var id = Guid.NewGuid(); var bytes = LanDiscoveryProtocol.Announcement(1, Room(id));
        Check(LanDiscoveryProtocol.TryRead(bytes, bytes.Length, "192.168.10.2", out var token, out var room)
            && token == 1 && room.RoomId == id && room.Address == "192.168.10.2" && room.MaxPlayers == 2, "announcement roundtrip uses received endpoint");
        Check(Read(LanDiscoveryProtocol.Request(5)), "request roundtrip");
        var bad = (byte[])bytes.Clone(); bad[4] = (byte)(LanDiscoveryProtocol.Version + 1); Check(!Read(bad), "unsupported version rejected");
        bad = (byte[])bytes.Clone(); bad[6] ^= 1; Check(!Read(bad), "unexpected game rejected");
        bad = (byte[])bytes.Clone(); bad[5] = 99; Check(!Read(bad), "unknown message rejected");
        bad = (byte[])bytes.Clone(); bad[42] = bad[43] = 0; Check(!Read(bad), "zero gameplay port rejected");
        bad = (byte[])bytes.Clone(); bad[44] = 3; Check(!Read(bad), "invalid player count rejected");
        bad = (byte[])bytes.Clone(); bad[45] = 3; Check(!Read(bad), "non-1v1 capacity rejected");
        bad = (byte[])bytes.Clone(); bad[46] = 2; Check(!Read(bad), "nonboolean join flag rejected");
        bad = (byte[])bytes.Clone(); bad[44] = 2; Check(!Read(bad), "full room cannot advertise joinable");
        bad = (byte[])bytes.Clone(); Array.Clear(bad, 18, 16); Check(!Read(bad), "empty room identifier rejected");
        bad = (byte[])bytes.Clone(); Array.Clear(bad, 34, 8); Check(!Read(bad), "zero announcement sequence rejected");
        bad = (byte[])bytes.Clone(); Array.Clear(bad, 10, 8); Check(!Read(bad), "zero scan nonce rejected");
        bad = (byte[])bytes.Clone(); bad[48] = 0xff; Check(!Read(bad), "invalid UTF8 rejected");
        Check(!Read(new byte[161]) && !Read(new byte[0]), "oversized/empty packets rejected");
        bool truncated = true; for (int i = 0; i < bytes.Length; i++) truncated &= !LanDiscoveryProtocol.TryRead(bytes, i, "", out _, out _);
        Check(truncated, "all truncated announcement lengths rejected");
        var trailing = new byte[bytes.Length + 1]; Array.Copy(bytes, trailing, bytes.Length); Check(!Read(trailing), "trailing bytes rejected");
        Check(Rejected(() => LanDiscoveryProtocol.Request(0)), "invalid request nonce throws");
        Check(Rejected(() => LanDiscoveryProtocol.Announcement(1, Room(id, port: 65536))), "out-of-range port throws");
        Check(!LanDiscoveryProtocol.IsValidName(" ") && !LanDiscoveryProtocol.IsValidName("x\ny")
            && !LanDiscoveryProtocol.IsValidName(new string('a', 49)) && !LanDiscoveryProtocol.IsValidName("\ud800"), "invalid names bounded");
        var random = new Random(9); for (int i = 0; i < 3000; i++) { bad = new byte[random.Next(0, 180)]; random.NextBytes(bad); Read(bad); }
        Check(true, "3000 malformed packets cannot throw");
    }

    private static void Sockets()
    {
        const int port = 19778; double now = 0; string network = "A";
        using (var host = new LanDiscoveryService(port, () => Loop(), null))
        using (var browser = new LanDiscoveryService(port, () => Loop(network), () => now))
        {
            Check(host.StartAdvertising("Room", 19777), "advertiser starts");
            Check(!host.StartAdvertising("Room", 19777), "repeated advertiser start is idempotent");
            using (var duplicate = new LanDiscoveryService(port, () => Loop(), null))
                Check(Rejected(() => duplicate.StartAdvertising("Other", 19777)) || !duplicate.IsAdvertising, "duplicate bind prevented");
            Check(browser.StartBrowsing() && !browser.StartBrowsing(), "browser repeated start has one socket");
            int added = 0, updated = 0, removed = 0;
            browser.RoomAdded += _ => added++; browser.RoomUpdated += _ => updated++; browser.RoomRemoved += _ => removed++;
            browser.Pump(); host.Pump(); browser.Pump();
            Check(browser.Rooms.Count == 1 && added == 1, "actual UDP request and unicast announcement");
            now = 1.1; browser.Pump(); host.Pump(); browser.Pump();
            Check(browser.Rooms.Count == 1 && updated == 1, "room deduplication and update");
            network = "B"; now = 2.2; browser.Pump();
            Check(removed == 1 && browser.Rooms.Count == 0, "network interface change invalidates entries and socket");
            host.Pump(); browser.Pump(); Check(browser.Rooms.Count == 1, "scan resumes after interface change");
            browser.Refresh(); Check(browser.Rooms.Count == 0, "refresh clears stale rooms");
            browser.Pump(); host.Pump(); browser.Pump(); Check(browser.Rooms.Count == 1, "refresh receives room");
            host.UpdateCapacity(2, false); Check(!host.IsAdvertising, "capacity full immediately closes advertiser");
            browser.Pump(); Check(browser.Rooms.Count == 1 && !browser.Rooms[0].Joinable && browser.Rooms[0].PlayerCount == 2, "withdrawal immediately updates known browser capacity");
            now += 6.1; browser.Pump(); Check(browser.Rooms.Count == 0, "room expiry after host withdrawal");
            Check(host.StartAdvertising("New", 19777), "advertiser restart after shutdown releases bind");
            browser.StopBrowsing(); browser.StopBrowsing(); Check(!browser.IsBrowsing && browser.Rooms.Count == 0, "cancel idempotent and clears entries");
            Check(browser.StartBrowsing(), "scan can restart");
            host.StopAdvertising(); host.StopAdvertising(); Check(!host.IsAdvertising, "shutdown idempotent");
        }
        using (var rebound = new LanDiscoveryService(port, () => Loop(), null)) Check(rebound.StartAdvertising("Rebound", 19777), "Dispose releases discovery port");

        // A real UDP sender emits multiple room identities and stale/out-of-order packets.
        using (var sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        using (var browser = new LanDiscoveryService(port, () => Loop(), null))
        {
            sender.Bind(new IPEndPoint(IPAddress.Any, port)); sender.ReceiveTimeout = 2000;
            browser.StartBrowsing(); browser.Pump();
            byte[] data = new byte[161]; EndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
            int length = sender.ReceiveFrom(data, ref endpoint);
            LanDiscoveryProtocol.TryRead(data, length, "", out ulong nonce, out _);
            Guid id = Guid.NewGuid();
            Action<LanRoom, ulong> send = (room, token) => sender.SendTo(LanDiscoveryProtocol.Announcement(token, room), endpoint);
            send(Room(id), nonce + 1); browser.Pump(); Check(browser.Rooms.Count == 0, "stale scan nonce rejected");
            send(Room(id), nonce); send(Room(id), nonce); send(Room(Guid.NewGuid()), nonce); browser.Pump();
            Check(browser.Rooms.Count == 2, "multiple rooms and duplicate response deduplicated");
            send(Room(id, 2, 2, false), nonce); browser.Pump(); Check(!browser.Rooms[0].Joinable && browser.Rooms[0].PlayerCount == 2, "capacity update reflected");
            send(Room(id, 1), nonce); browser.Pump(); Check(!browser.Rooms[0].Joinable, "old announcement cannot reopen full room");
        }
        Check(Rejected(() => new LanDiscoveryService(0)), "invalid discovery port rejected");
    }

    private static void Routing()
    {
        var service = new FakeService(); int hostCalls = 0, clientCalls = 0; string address = null; int selectedPort = 0;
        using (var browser = new LanRoomBrowser(service,
            port => { hostCalls++; service.State = MultiplayerState.Running; service.Host = true; return true; },
            (ip, port) => { clientCalls++; address = ip; selectedPort = port; service.State = MultiplayerState.Starting; return true; }, () => true, 19779))
        {
            Check(Rejected(() => browser.CreateRoom("")), "invalid room name before NGO startup");
            Check(Rejected(() => browser.CreateRoom("Name", 19779)), "discovery/gameplay port collision rejected");
            Check(hostCalls == 0, "invalid host configuration cannot start NGO");
            Check(browser.FindRooms() && !browser.FindRooms(), "coordinator repeated scan guarded");
            browser.SetPaused(true); Check(!browser.Discovery.IsBrowsing, "pause disposes browser socket");
            browser.SetPaused(false); Check(browser.Discovery.IsBrowsing, "resume restarts requested scan");
            Check(Rejected(() => browser.Join(Room(Guid.NewGuid()))), "unselected/expired room cannot join");
            browser.Back(); Check(!browser.Discovery.IsBrowsing, "Back cancels discovery");
            Check(browser.ConnectManually("192.168.4.1", 7777) && address == "192.168.4.1" && selectedPort == 7777, "manual fallback uses existing client path");
            Check(Rejected(() => browser.ConnectManually("192.168.4.1")) && clientCalls == 1, "repeated Join/manual connection prevented");
            service.State = MultiplayerState.Stopped;
            Check(browser.CreateRoom("Host room"), "existing host path starts playing host"); browser.Pump();
            Check(browser.Discovery.IsAdvertising && hostCalls == 1, "advertises only after ready NGO host");
            service.Count = 2; browser.Pump(); Check(!browser.Discovery.IsAdvertising, "client join withdraws full host");
            service.State = MultiplayerState.Stopped; service.Host = false; browser.Pump(); Check(!browser.Discovery.IsAdvertising, "host shutdown withdraws discovery");
        }
        // Selection routing uses an actual received room, not a mock room-added callback.
        using (var host = new LanDiscoveryService(19779, () => Loop(), null))
        using (var browser = new LanRoomBrowser(service, _ => false, (ip, port) => { clientCalls++; address = ip; selectedPort = port; service.State = MultiplayerState.Starting; return true; }, () => true, 19779))
        {
            host.StartAdvertising("Selected", 19888); browser.FindRooms();
            var timer = Stopwatch.StartNew();
            while (browser.Discovery.Rooms.Count == 0 && timer.Elapsed.TotalSeconds < 3) { browser.Pump(); host.Pump(); browser.Pump(); Thread.Sleep(10); }
            Check(browser.Discovery.Rooms.Count > 0, "room selection has real broadcast evidence");
            Check(browser.Join(browser.Discovery.Rooms[0]) && selectedPort == 19888 && IPAddress.TryParse(address, out _), "selected endpoint routes into existing connect delegate");
            Check(!browser.Discovery.IsBrowsing && Rejected(() => browser.Join(Room(Guid.NewGuid()))), "joining stops scan and repeated Join rejected");
        }
    }

    private static int Peer(string role)
    {
        Console.WriteLine("PID=" + Environment.ProcessId + " role=" + role);
        using (var service = new LanDiscoveryService(19780))
        {
            service.Error += message => throw new Exception(message);
            if (role == "host") service.StartAdvertising("Independent UDP", 19777); else service.StartBrowsing();
            service.RoomAdded += room => Console.WriteLine("ROOM " + room.Key + " " + room.Name);
            service.RoomRemoved += room => Console.WriteLine("REMOVED " + room.Key);
            var timer = Stopwatch.StartNew(); bool found = false, expired = false;
            while (timer.Elapsed.TotalSeconds < (role == "host" ? 5 : 13))
            {
                service.Pump(); if (service.Rooms.Count > 0) found = true;
                if (found && service.Rooms.Count == 0) expired = true;
                Thread.Sleep(10);
            }
            if (role != "host") Check(found && expired, "independent-process production broadcast discovery and host shutdown expiry");
        }
        return 0;
    }

    private sealed class FakeService : IMultiplayerService
    {
        public MultiplayerState State { get; set; }
        public bool Host; public int Count = 1;
        public bool IsRunning => State != MultiplayerState.Stopped;
        public bool IsHost => Host;
        public bool IsClient => IsRunning;
        public bool IsConnected => State == MultiplayerState.Running;
        public int ConnectedClientCount => Count;
        public event Action<MultiplayerState> StateChanged { add { } remove { } }
        public event Action<ulong> ClientConnected { add { } remove { } }
        public event Action<ulong> ClientDisconnected { add { } remove { } }
        public event Action<string> ConnectionFailed { add { } remove { } }
        public bool StartHost(int port = 7777) => throw new NotSupportedException();
        public bool StartClient(string ip, int port = 7777) => throw new NotSupportedException();
        public void Disconnect() { State = MultiplayerState.Stopped; }
    }
}
