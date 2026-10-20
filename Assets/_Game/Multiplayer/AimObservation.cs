using Unity.Netcode;
using UnityEngine;

namespace TankGame.Multiplayer
{
    /// <summary>Disposable presentation observations; no health, damage, position or lifecycle state.</summary>
    public struct AimObservation : INetworkSerializable
    {
        public const string MessageName = "TankGame.AimObservation.v3";
        public const int WireSize = 57;
        public byte Version;
        public ulong Session, Turn, Frame, ZeroSequence, OneSequence;
        public float ZeroAngle, ZeroPower, OneAngle, OnePower;
        public bool IsValid => Version == GameplayNetworkCommand.ProtocolVersion && Session != 0 && Turn != 0 && Frame != 0
            && Valid(ZeroAngle, ZeroPower) && Valid(OneAngle, OnePower);
        private static bool Valid(float angle, float power) => float.IsFinite(angle) && angle >= 0 && angle <= 90
            && float.IsFinite(power) && power >= 0 && power <= 1;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Version); serializer.SerializeValue(ref Session); serializer.SerializeValue(ref Turn);
            serializer.SerializeValue(ref Frame); serializer.SerializeValue(ref ZeroSequence); serializer.SerializeValue(ref OneSequence);
            serializer.SerializeValue(ref ZeroAngle); serializer.SerializeValue(ref ZeroPower); serializer.SerializeValue(ref OneAngle); serializer.SerializeValue(ref OnePower);
        }
    }

    /// <summary>Two local-receipt-time samples; hold at newest sample rather than extrapolating.</summary>
    public sealed class AimObservationBuffer
    {
        public AimObservation Latest { get; private set; }
        public bool HasValue { get; private set; }
        private AimObservation previous;
        private double previousAt, latestAt;
        public double Age(double now) => HasValue ? now - latestAt : double.PositiveInfinity;
        public bool Push(AimObservation sample, ulong session, ulong turn, double now)
        {
            if (!double.IsFinite(now) || (HasValue && now < latestAt)) return false;
            if (!sample.IsValid || sample.Session != session || sample.Turn != turn) return false;
            if (HasValue && (Latest.Session != session || Latest.Turn != turn)) Reset();
            if (HasValue && sample.Frame <= Latest.Frame) return false;
            if (HasValue && now - latestAt > .3) Reset();
            previous = HasValue ? Latest : sample; previousAt = HasValue ? latestAt : now;
            Latest = sample; latestAt = now; HasValue = true; return true;
        }
        public float Evaluate(int player, double now, float delay)
        {
            if (!HasValue) return 0;
            float t = latestAt <= previousAt ? 1 : Mathf.Clamp01((float)((now - Mathf.Clamp(delay, 0, .075f) - previousAt) / (latestAt - previousAt)));
            return Mathf.Lerp(player == 0 ? previous.ZeroAngle : previous.OneAngle, player == 0 ? Latest.ZeroAngle : Latest.OneAngle, t);
        }
        public void Reset() { HasValue = false; Latest = previous = default; previousAt = latestAt = 0; }
    }
}
