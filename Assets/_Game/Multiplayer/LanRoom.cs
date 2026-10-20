using System;

namespace TankGame.Multiplayer
{
    /// <summary>An untrusted LAN advertisement, not proof of gameplay ownership or reachability.</summary>
    public sealed class LanRoom
    {
        public Guid RoomId { get; }
        public string Name { get; }
        public string Address { get; }
        public int GameplayPort { get; }
        public int PlayerCount { get; }
        public int MaxPlayers => 2;
        public bool Joinable { get; }
        public ulong Sequence { get; }
        public string Key => RoomId.ToString("N") + "@" + Address + ":" + GameplayPort;

        internal LanRoom(Guid id, string name, string address, int port, int count, bool joinable, ulong sequence)
        {
            RoomId = id; Name = name; Address = address; GameplayPort = port;
            PlayerCount = count; Joinable = joinable; Sequence = sequence;
        }
    }
}
