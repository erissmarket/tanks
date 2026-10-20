using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using TankGame.Core;
using TankGame.Tank;

namespace TankGame.Multiplayer
{
    public sealed class LanAimStream : MonoBehaviour
    {
        private NgoMultiplayerService service;
        private TankGameNetworkAdapter adapter;
        private TankAim zero, one;
        private NetworkManager manager;
        private ulong session, turn, frame;
        private MatchState phase;
        private double nextSend;
        private float rate = 20;
        public float Rate { get => rate; set { if (!float.IsFinite(value) || value < 10 || value > 30) throw new System.ArgumentOutOfRangeException(nameof(value)); rate = value; } }
        public readonly AimObservationBuffer Buffer = new AimObservationBuffer();
        public void Initialize(NgoMultiplayerService service, TankGameNetworkAdapter adapter, TankAim zero, TankAim one)
        { this.service = service; this.adapter = adapter; this.zero = zero; this.one = one; }
        private void Update()
        {
            if (service == null || adapter == null) return;
            var current = service.IsRunning && service.State != MultiplayerState.Stopping ? service.NetworkManager : null;
            if (current != manager)
            {
                Unbind(); manager = current;
                if (manager != null && manager.CustomMessagingManager != null)
                    manager.CustomMessagingManager.RegisterNamedMessageHandler(AimObservation.MessageName, Receive);
            }
            if (session != adapter.SessionToken || turn != adapter.TurnToken || phase != adapter.ObservedState)
            { Buffer.Reset(); session = adapter.SessionToken; turn = adapter.TurnToken; phase = adapter.ObservedState; nextSend = 0; }
            if (manager == null || !manager.IsHost || !service.IsConnected || session == 0 || turn == 0
                || phase != MatchState.TurnActive || Time.realtimeSinceStartupAsDouble < nextSend) return;
            nextSend = Time.realtimeSinceStartupAsDouble + 1d / Mathf.Clamp(Rate, 10, 30);
            var sample = new AimObservation { Version = GameplayNetworkCommand.ProtocolVersion, Session = session, Turn = turn, Frame = ++frame,
                ZeroAngle = zero.CurrentAimAngle, OneAngle = one.CurrentAimAngle, ZeroPower = adapter.GetShotPower(0), OnePower = adapter.GetShotPower(1),
                ZeroSequence = adapter.GetAppliedAimSequence(0), OneSequence = adapter.GetAppliedAimSequence(1) };
            Buffer.Push(sample, session, turn, Time.realtimeSinceStartupAsDouble);
            using (var writer = new FastBufferWriter(AimObservation.WireSize, Allocator.Temp))
            {
                writer.WriteValueSafe(sample);
                foreach (ulong peer in manager.ConnectedClientsIds) if (peer != manager.LocalClientId)
                    manager.CustomMessagingManager.SendNamedMessage(AimObservation.MessageName, peer, writer, NetworkDelivery.Unreliable);
            }
            LanAimDiagnostics.AimObservation(this, AimObservation.WireSize, manager.ConnectedClientsIds.Count - 1);
        }
        private void Receive(ulong sender, FastBufferReader reader)
        {
            if (manager == null || manager.IsHost || !service.IsConnected || sender != NetworkManager.ServerClientId
                || adapter.ObservedState != MatchState.TurnActive || reader.Length - reader.Position != AimObservation.WireSize) return;
            reader.ReadValueSafe(out AimObservation sample);
            if (sample.IsValid && sample.Session == adapter.SessionToken && sample.Turn == adapter.TurnToken && Buffer.HasValue)
                LanAimDiagnostics.ObservationOrdering(this, sample.Frame <= Buffer.Latest.Frame,
                    sample.Frame > Buffer.Latest.Frame + 1 ? sample.Frame - Buffer.Latest.Frame - 1 : 0);
            if (!Buffer.Push(sample, adapter.SessionToken, adapter.TurnToken, Time.realtimeSinceStartupAsDouble)) return;
            LanAimDiagnostics.AimObservation(this, 0);
            LanAimDiagnostics.ObserveAim(this, sample);
        }
        private void Unbind()
        {
            if (manager != null && manager.CustomMessagingManager != null) manager.CustomMessagingManager.UnregisterNamedMessageHandler(AimObservation.MessageName);
            manager = null; Buffer.Reset(); session = turn = frame = 0;
        }
        private void OnDisable() => Unbind();
    }
}
