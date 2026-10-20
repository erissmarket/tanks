using System;
using Unity.Netcode;

namespace TankGame.Multiplayer
{
    public enum GameplayNetworkAction : byte { Move, Stop, Aim, Fire, AimPower }
    public enum GameplayCommandResult : byte
    {
        Accepted, Malformed, Unavailable, NotOwner, WrongTurn, Stale, InvalidValue, GameplayRejected
    }

    /// <summary>Intent only. Session and turn tokens are supplied by the host.</summary>
    public readonly struct GameplayNetworkCommand
    {
        public const byte ProtocolVersion = 3;
        public const int WireSize = 39;
        public const string MessageName = "TankGame.Command.v3";
        public readonly byte Version;
        public readonly ulong Session, Turn, Sequence;
        public readonly byte PlayerIndex;
        public readonly GameplayNetworkAction Action;
        public readonly float X, Y, Power;

        public GameplayNetworkCommand(ulong session, ulong turn, ulong sequence, byte playerIndex,
            GameplayNetworkAction action, float x = 0f, float y = 0f, byte version = ProtocolVersion, float power = 1f)
        {
            Version = version; Session = session; Turn = turn; Sequence = sequence;
            PlayerIndex = playerIndex; Action = action; X = x; Y = y; Power = power;
        }

        public bool HasValidValues()
        {
            if (!float.IsFinite(X) || !float.IsFinite(Y) || !float.IsFinite(Power) || Power < 0f || Power > 1f) return false;
            switch (Action)
            {
                case GameplayNetworkAction.Move: return X >= -1f && X <= 1f && Y == 0f;
                case GameplayNetworkAction.Stop: return X == 0f && Y == 0f;
                case GameplayNetworkAction.Aim: return X >= 0f && X <= 90f && Y == 0f;
                case GameplayNetworkAction.Fire:
                    return Math.Abs(X) <= 1f && Math.Abs(Y) <= 1f && X * X + Y * Y >= 0.000001f;
                case GameplayNetworkAction.AimPower:
                    return Math.Abs(X) <= 1f && Math.Abs(Y) <= 1f && Math.Abs(X * X + Y * Y - 1f) < 0.01f;
                default: return false;
            }
        }

        public void WriteTo(ref FastBufferWriter writer)
        {
            writer.WriteValueSafe(Version); writer.WriteValueSafe(Session); writer.WriteValueSafe(Turn);
            writer.WriteValueSafe(Sequence); writer.WriteValueSafe(PlayerIndex);
            writer.WriteValueSafe((byte)Action); writer.WriteValueSafe(X); writer.WriteValueSafe(Y); writer.WriteValueSafe(Power);
        }

        public static bool TryRead(FastBufferReader reader, out GameplayNetworkCommand command)
        {
            command = default;
            if (reader.Length - reader.Position != WireSize || !reader.TryBeginRead(WireSize)) return false;
            reader.ReadValueSafe(out byte version); reader.ReadValueSafe(out ulong session);
            reader.ReadValueSafe(out ulong turn); reader.ReadValueSafe(out ulong sequence);
            reader.ReadValueSafe(out byte player); reader.ReadValueSafe(out byte action);
            reader.ReadValueSafe(out float x); reader.ReadValueSafe(out float y); reader.ReadValueSafe(out float power);
            command = new GameplayNetworkCommand(session, turn, sequence, player, (GameplayNetworkAction)action, x, y, version, power);
            return true;
        }
    }
}
