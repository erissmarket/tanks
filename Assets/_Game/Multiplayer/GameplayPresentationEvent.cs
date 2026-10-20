using Unity.Netcode;
using UnityEngine;

namespace TankGame.Multiplayer
{
    public enum GameplayPresentationKind : byte { TerrainCarved, ProjectileImpacted }

    /// <summary>Ordered host outcomes. Replaying these never applies damage.</summary>
    public struct GameplayPresentationEvent : INetworkSerializable
    {
        public const string MessageName = "TankGame.Presentation.v3";
        public const int WireSize = 31;
        private byte version;
        private ulong session, sequence;
        private GameplayPresentationKind kind;
        private Vector2 position;
        private float radius;
        private bool replay;
        public ulong Session => session;
        public ulong Sequence => sequence;
        public GameplayPresentationKind Kind => kind;
        public Vector2 Position => position;
        public float Radius => radius;
        public bool IsReplay => replay;
        public GameplayPresentationEvent AsReplay => new GameplayPresentationEvent(session, sequence, kind, position, radius, true);
        public bool IsValid => version == GameplayNetworkCommand.ProtocolVersion && session != 0 && sequence != 0
            && kind >= GameplayPresentationKind.TerrainCarved && kind <= GameplayPresentationKind.ProjectileImpacted
            && float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(radius) && radius >= 0;

        public GameplayPresentationEvent(ulong session, ulong sequence, GameplayPresentationKind kind, Vector2 position, float radius, bool replay = false)
        { version = GameplayNetworkCommand.ProtocolVersion; this.session = session; this.sequence = sequence;
            this.kind = kind; this.position = position; this.radius = radius; this.replay = replay; }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref version); serializer.SerializeValue(ref session); serializer.SerializeValue(ref sequence);
            serializer.SerializeValue(ref kind); serializer.SerializeValue(ref position); serializer.SerializeValue(ref radius);
            serializer.SerializeValue(ref replay);
        }
    }
}
