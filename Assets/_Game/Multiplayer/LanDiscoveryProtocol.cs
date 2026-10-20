using System;
using System.IO;
using System.Text;

namespace TankGame.Multiplayer
{
    /// <summary>Small fixed-endian, bounded discovery protocol, separate from gameplay messages.</summary>
    public static class LanDiscoveryProtocol
    {
        public const byte Version = 3;
        public const int MaximumPacketSize = 160;
        private const uint Magic = 0x444E414C; // LAND
        private const uint Game = 0x54415254; // Application-specific Tank Artillery MVP identifier.
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 48) return false;
            foreach (char c in name) if (char.IsControl(c)) return false;
            try { return Utf8.GetByteCount(name) <= 96; }
            catch (EncoderFallbackException) { return false; }
        }

        public static byte[] Request(ulong nonce) => Encode(nonce, null);

        public static byte[] Announcement(ulong nonce, LanRoom room) => Encode(nonce, room ?? throw new ArgumentNullException(nameof(room)));

        private static byte[] Encode(ulong nonce, LanRoom room)
        {
            if (nonce == 0) throw new ArgumentOutOfRangeException(nameof(nonce));
            if (room != null && (!IsValidName(room.Name) || room.RoomId == Guid.Empty || room.Sequence == 0
                || room.GameplayPort < 1 || room.GameplayPort > 65535 || room.PlayerCount < 1 || room.PlayerCount > 2
                || (room.Joinable && room.PlayerCount >= 2))) throw new ArgumentException("Invalid room metadata.");
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write((byte)(room == null ? 1 : 2));
                writer.Write(Game); writer.Write(nonce);
                if (room != null)
                {
                    writer.Write(room.RoomId.ToByteArray()); writer.Write(room.Sequence);
                    writer.Write((ushort)room.GameplayPort); writer.Write((byte)room.PlayerCount);
                    writer.Write((byte)2); writer.Write((byte)(room.Joinable ? 1 : 0));
                    byte[] name = Utf8.GetBytes(room.Name); writer.Write((byte)name.Length); writer.Write(name);
                }
                return stream.ToArray();
            }
        }

        // Address is deliberately absent from the wire. The receiver supplies the actual UDP sender.
        public static bool TryRead(byte[] packet, int length, string sender, out ulong nonce, out LanRoom room)
        {
            nonce = 0; room = null;
            if (packet == null || length < 18 || length > MaximumPacketSize || length > packet.Length) return false;
            try
            {
                using (var stream = new MemoryStream(packet, 0, length, false))
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadUInt32() != Magic || reader.ReadByte() != Version) return false;
                    byte kind = reader.ReadByte();
                    if (reader.ReadUInt32() != Game) return false;
                    nonce = reader.ReadUInt64();
                    if (nonce == 0) return false;
                    if (kind == 1) return stream.Position == length;
                    if (kind != 2 || length < 49) return false;
                    var id = new Guid(reader.ReadBytes(16)); ulong sequence = reader.ReadUInt64();
                    int port = reader.ReadUInt16(), count = reader.ReadByte(), max = reader.ReadByte(), available = reader.ReadByte();
                    int size = reader.ReadByte();
                    if (size < 1 || size > 96 || stream.Position + size != length) return false;
                    string name = Utf8.GetString(reader.ReadBytes(size));
                    if (id == Guid.Empty || sequence == 0 || port == 0 || count < 1 || count > 2 || max != 2
                        || available > 1 || (available == 1 && count == 2) || !IsValidName(name)) return false;
                    room = new LanRoom(id, name, sender, port, count, available == 1, sequence);
                    return true;
                }
            }
            catch (IOException) { return false; }
            catch (ArgumentException) { return false; }
        }
    }
}
