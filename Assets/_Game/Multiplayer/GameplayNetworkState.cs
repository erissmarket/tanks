using TankGame.Core;
using Unity.Netcode;
using UnityEngine;

namespace TankGame.Multiplayer
{
    public struct NetworkTankState : INetworkSerializable
    {
        private Vector2 position;
        private float rotation, aim, health, maxHealth, movementRemaining, power;
        public Vector2 Position => position;
        public float Rotation => rotation;
        public float Aim => aim;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public float MovementRemaining => movementRemaining;
        public float Power => power;

        public NetworkTankState(Vector2 position, float rotation, float aim, float health, float maxHealth, float movementRemaining = 0, float power = 1)
        { this.position = position; this.rotation = rotation; this.aim = aim; this.health = health; this.maxHealth = maxHealth; this.movementRemaining = movementRemaining; this.power = power; }

        public bool IsValid => float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(rotation)
            && float.IsFinite(aim) && aim >= 0 && aim <= 90 && float.IsFinite(health) && float.IsFinite(maxHealth)
            && maxHealth > 0 && health >= 0 && health <= maxHealth
            && float.IsFinite(movementRemaining) && movementRemaining >= 0 && float.IsFinite(power) && power >= 0 && power <= 1;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position); serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref aim); serializer.SerializeValue(ref health); serializer.SerializeValue(ref maxHealth);
            serializer.SerializeValue(ref movementRemaining); serializer.SerializeValue(ref power);
        }
    }

    /// <summary>Read-only authoritative observations, never a client command payload.</summary>
    public sealed class GameplayNetworkState : INetworkSerializable
    {
        public const string MessageName = "TankGame.State.v3";
        public const int WireSize = 142;
        private byte version = GameplayNetworkCommand.ProtocolVersion;
        private ulong session, frame, shot, completedShot;
        private MatchState state;
        private int currentPlayer, outcome, projectileOwner, craterCount;
        private NetworkTankState playerZero, playerOne;
        private bool projectileActive;
        private Vector2 projectilePosition, projectileVelocity, completionPosition;
        public ulong Session => session;
        public ulong Frame => frame;
        public MatchState State => state;
        public int CurrentPlayer => currentPlayer;
        // -2 = no result/aborted, -1 = draw, 0/1 = winner.
        public int Outcome => outcome;
        public NetworkTankState PlayerZero => playerZero;
        public NetworkTankState PlayerOne => playerOne;
        public ulong Shot => shot;
        public bool ProjectileActive => projectileActive;
        public int ProjectileOwner => projectileOwner;
        public Vector2 ProjectilePosition => projectilePosition;
        public Vector2 ProjectileVelocity => projectileVelocity;
        public ulong CompletedShot => completedShot;
        public Vector2 CompletionPosition => completionPosition;
        public int CraterCount => craterCount;

        public GameplayNetworkState() { }
        public GameplayNetworkState(ulong session, ulong frame, MatchState state, int currentPlayer, int outcome,
            NetworkTankState zero, NetworkTankState one, ulong shot, bool projectileActive, int owner,
            Vector2 position, Vector2 velocity, ulong completedShot, Vector2 completionPosition, int craterCount)
        {
            this.session = session; this.frame = frame; this.state = state; this.currentPlayer = currentPlayer;
            this.outcome = outcome; playerZero = zero; playerOne = one; this.shot = shot;
            this.projectileActive = projectileActive; projectileOwner = owner;
            projectilePosition = position; projectileVelocity = velocity; this.completedShot = completedShot;
            this.completionPosition = completionPosition; this.craterCount = craterCount;
        }

        public bool IsValid => version == GameplayNetworkCommand.ProtocolVersion && session != 0 && frame != 0
            && state >= MatchState.None && state <= MatchState.MatchEnded && currentPlayer >= -1 && currentPlayer <= 1
            && outcome >= -2 && outcome <= 1 && playerZero.IsValid && playerOne.IsValid
            && projectileOwner >= -1 && projectileOwner <= 1 && completedShot <= shot && craterCount >= 0
            && Finite(projectilePosition) && Finite(projectileVelocity) && Finite(completionPosition);
        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref version); serializer.SerializeValue(ref session); serializer.SerializeValue(ref frame);
            serializer.SerializeValue(ref state); serializer.SerializeValue(ref currentPlayer);
            serializer.SerializeValue(ref playerZero); serializer.SerializeValue(ref playerOne); serializer.SerializeValue(ref outcome);
            serializer.SerializeValue(ref shot); serializer.SerializeValue(ref projectileActive);
            serializer.SerializeValue(ref projectileOwner); serializer.SerializeValue(ref projectilePosition);
            serializer.SerializeValue(ref projectileVelocity); serializer.SerializeValue(ref completedShot);
            serializer.SerializeValue(ref completionPosition); serializer.SerializeValue(ref craterCount);
        }
    }
}
