using System;
using System.Collections.Generic;
using TankGame.Core;
using TankGame.Tank;
using TankGame.Terrain;
using TankGame.Projectiles;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TankGame.Multiplayer
{
    /// <summary>Small 10 Hz observation stream; only a host reads simulation state.</summary>
    public sealed class TankGameStateStream : MonoBehaviour
    {
        private const string PresentationRequest = "TankGame.PresentationRequest.v3";
        private NgoMultiplayerService service;
        private TankGameNetworkAdapter adapter;
        private NetworkManager manager;
        private GameManager game;
        private TurnManager turns;
        private MatchFlowController flow;
        private TankController zero, one;
        private DestructibleTerrain terrain;
        private Projectile tracked;
        private ulong frame, shot, completedShot;
        private Vector2 completionPosition;
        private float nextSend;
        private float snapshotRate = 10;
        public float SnapshotRate { get => snapshotRate; set { if (!float.IsFinite(value) || value < 1 || value > 60) throw new ArgumentOutOfRangeException(nameof(value)); snapshotRate = value; } }
        private ulong observedSession, presentationSequence;
        private ulong requestedPresentationSession;
        private readonly List<GameplayPresentationEvent> presentationHistory = new List<GameplayPresentationEvent>();
        public GameplayNetworkState Latest { get; private set; }
        public event Action<GameplayNetworkState> SnapshotReceived;
        public event Action<GameplayPresentationEvent> PresentationReceived;
        public event Action ObservationReset;

        public void Initialize(NgoMultiplayerService service, TankGameNetworkAdapter adapter, GameManager game = null,
            TurnManager turns = null, MatchFlowController flow = null, TankController zero = null,
            TankController one = null, DestructibleTerrain terrain = null)
        {
            if (this.service != null) throw new InvalidOperationException("Initialize the state stream once.");
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            this.game = game; this.turns = turns; this.flow = flow; this.zero = zero; this.one = one; this.terrain = terrain;
            if (game != null) game.StateChanged += OnMatchState;
            if (terrain != null) terrain.Carved += OnTerrainCarved;
        }

        private void OnMatchState(MatchState before, MatchState after)
        {
            if (manager == null || !manager.IsHost) return;
            if (after == MatchState.Starting)
            {
                ClearProjectile(); shot = completedShot = 0; completionPosition = Vector2.zero;
                presentationHistory.Clear(); presentationSequence = 0;
            }
            if (after == MatchState.ProjectileFlying)
                foreach (var projectile in FindObjectsByType<Projectile>())
                    if (projectile.gameObject.scene == game.gameObject.scene && projectile.isActiveAndEnabled
                        && projectile.IsInitialized && !projectile.IsFinished)
                    {
                        ClearProjectile(); tracked = projectile; shot++;
                        tracked.Completed += OnCompleted;
                        break;
                    }
            nextSend = 0f;
            SendSnapshot();
        }

        private void OnCompleted(Projectile projectile)
        {
            completedShot = shot;
            completionPosition = projectile.transform.position;
            if (projectile.HasImpacted)
                PublishPresentation(GameplayPresentationKind.ProjectileImpacted, projectile.ImpactPosition, projectile.ExplosionRadius);
            ClearProjectile();
            nextSend = 0f;
        }

        private void Update()
        {
            if (service == null || adapter == null) return;
            if (!service.IsRunning || service.State == MultiplayerState.Stopping)
            {
                Unbind(); ResetObservations(0); return;
            }
            var current = service.NetworkManager;
            if (current != manager)
            {
                Unbind(); manager = current; frame = 0; Latest = null;
                if (manager != null && manager.CustomMessagingManager != null)
                {
                    manager.CustomMessagingManager.RegisterNamedMessageHandler(GameplayNetworkState.MessageName, Receive);
                    manager.CustomMessagingManager.RegisterNamedMessageHandler(GameplayPresentationEvent.MessageName, ReceivePresentation);
                    manager.CustomMessagingManager.RegisterNamedMessageHandler(PresentationRequest, OnPresentationRequest);
                }
            }
            ResetObservations(adapter.SessionToken);
            if (manager != null && !manager.IsHost && service.IsConnected && adapter.SessionToken != 0
                && requestedPresentationSession != adapter.SessionToken)
            {
                // Request history only after this peer has bound its handlers and received host context.
                requestedPresentationSession = adapter.SessionToken;
                using (var writer = new FastBufferWriter(sizeof(ulong), Allocator.Temp))
                {
                    writer.WriteValueSafe(adapter.SessionToken);
                    manager.CustomMessagingManager.SendNamedMessage(PresentationRequest, NetworkManager.ServerClientId,
                        writer, NetworkDelivery.ReliableSequenced);
                }
            }
            if (manager == null || !service.IsConnected || !manager.IsHost || adapter.SessionToken == 0 || Time.unscaledTime < nextSend) return;
            SendSnapshot();
        }

        private void SendSnapshot()
        {
            if (manager == null || !manager.IsHost || !service.IsConnected || adapter.SessionToken == 0) return;
            if (game == null || turns == null || flow == null || zero == null || one == null) return;
            nextSend = Time.unscaledTime + 1f / Mathf.Clamp(SnapshotRate, 1f, 60f);
            var result = flow.FinalResult;
            bool active = tracked != null && !tracked.IsFinished;
            var snapshot = new GameplayNetworkState(adapter.SessionToken, ++frame, game.CurrentState, turns.CurrentPlayerIndex,
                result == null ? -2 : result.IsDraw ? -1 : result.WinnerPlayerIndex.Value,
                Capture(zero), Capture(one), shot, active, active ? tracked.OwnerPlayerIndex : -1,
                active ? (Vector2)tracked.transform.position : Vector2.zero,
                active ? tracked.GetComponent<Rigidbody2D>().linearVelocity : Vector2.zero,
                completedShot, completionPosition, terrain != null ? terrain.CraterCount : 0);
            Publish(snapshot);
            LanAimDiagnostics.Snapshot(this, snapshot, true, manager.ConnectedClientsIds.Count - 1);
            using (var writer = new FastBufferWriter(GameplayNetworkState.WireSize, Allocator.Temp))
            {
                writer.WriteValueSafe(snapshot);
                foreach (ulong id in manager.ConnectedClientsIds)
                    if (id != manager.LocalClientId)
                        manager.CustomMessagingManager.SendNamedMessage(GameplayNetworkState.MessageName, id, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        private NetworkTankState Capture(TankController tank)
        {
            var health = tank.GetComponent<HealthComponent>();
            return new NetworkTankState(tank.transform.position, tank.transform.eulerAngles.z,
                tank.Aim.CurrentAimAngle, health.CurrentHealth, health.MaxHealth,
                adapter.GetMovementRemaining(tank.PlayerIndex), adapter.GetShotPower(tank.PlayerIndex));
        }

        private void Receive(ulong sender, FastBufferReader reader)
        {
            if (manager == null || !service.IsConnected || manager.IsServer || sender != NetworkManager.ServerClientId
                || reader.Length - reader.Position != GameplayNetworkState.WireSize) return;
            reader.ReadValueSafe(out GameplayNetworkState state);
            ResetObservations(adapter.SessionToken);
            if (!state.IsValid || state.Session != adapter.SessionToken
                || (Latest != null && Latest.Session == state.Session && state.Frame <= Latest.Frame)) return;
            Publish(state);
            LanAimDiagnostics.Snapshot(this, state, false);
        }

        private void ResetObservations(ulong token)
        {
            if (observedSession == token && (token != 0 || Latest == null)) return;
            observedSession = token; Latest = null;
            requestedPresentationSession = 0;
            if (manager == null || !manager.IsHost) presentationSequence = 0;
            if (ObservationReset != null)
                foreach (Action handler in ObservationReset.GetInvocationList())
                    try { handler(); } catch (Exception error) { Debug.LogException(error); }
        }

        private void OnTerrainCarved(Vector2 position, float radius)
        {
            // A transport failure must not interrupt the authoritative damage pass.
            try { PublishPresentation(GameplayPresentationKind.TerrainCarved, position, radius); }
            catch (Exception error) { Debug.LogException(error); }
        }

        private void PublishPresentation(GameplayPresentationKind kind, Vector2 position, float radius)
        {
            if (manager == null || !manager.IsHost || !service.IsConnected || adapter.SessionToken == 0) return;
            var entry = new GameplayPresentationEvent(adapter.SessionToken, ++presentationSequence, kind, position, radius);
            presentationHistory.Add(entry);
            foreach (ulong id in manager.ConnectedClientsIds)
                if (id != manager.LocalClientId) SendPresentation(id, entry);
        }

        private void OnPresentationRequest(ulong id, FastBufferReader reader)
        {
            if (manager == null || !manager.IsHost || !service.IsConnected || id == manager.LocalClientId
                || reader.Length - reader.Position != sizeof(ulong)) return;
            reader.ReadValueSafe(out ulong token);
            if (token != adapter.SessionToken) return;
            bool connected = false;
            foreach (ulong peer in manager.ConnectedClientsIds) if (peer == id) connected = true;
            if (!connected) return;
            foreach (var entry in presentationHistory) SendPresentation(id, entry.AsReplay);
        }

        private void SendPresentation(ulong id, GameplayPresentationEvent entry)
        {
            using (var writer = new FastBufferWriter(GameplayPresentationEvent.WireSize, Allocator.Temp))
            {
                writer.WriteValueSafe(entry);
                manager.CustomMessagingManager.SendNamedMessage(GameplayPresentationEvent.MessageName, id, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        private void ReceivePresentation(ulong sender, FastBufferReader reader)
        {
            if (manager == null || manager.IsServer || !service.IsConnected || sender != NetworkManager.ServerClientId
                || reader.Length - reader.Position != GameplayPresentationEvent.WireSize) return;
            reader.ReadValueSafe(out GameplayPresentationEvent entry);
            ResetObservations(adapter.SessionToken);
            if (!entry.IsValid || entry.Session != observedSession || entry.Sequence != presentationSequence + 1) return;
            presentationSequence = entry.Sequence;
            if (PresentationReceived != null)
                foreach (Action<GameplayPresentationEvent> handler in PresentationReceived.GetInvocationList())
                    try { handler(entry); } catch (Exception error) { Debug.LogException(error); }
        }

        private void Publish(GameplayNetworkState state)
        {
            Latest = state;
            if (SnapshotReceived == null) return;
            foreach (Action<GameplayNetworkState> handler in SnapshotReceived.GetInvocationList())
                try { handler(state); } catch (Exception error) { Debug.LogException(error); }
        }

        private void ClearProjectile()
        {
            if (!ReferenceEquals(tracked, null)) tracked.Completed -= OnCompleted;
            tracked = null;
        }

        private void Unbind()
        {
            if (manager != null && manager.CustomMessagingManager != null)
            {
                manager.CustomMessagingManager.UnregisterNamedMessageHandler(GameplayNetworkState.MessageName);
                manager.CustomMessagingManager.UnregisterNamedMessageHandler(GameplayPresentationEvent.MessageName);
                manager.CustomMessagingManager.UnregisterNamedMessageHandler(PresentationRequest);
            }
            manager = null;
            ClearProjectile();
        }

        private void OnDisable() { Unbind(); ResetObservations(0); }
        private void OnDestroy()
        {
            if (game != null) game.StateChanged -= OnMatchState;
            if (terrain != null) terrain.Carved -= OnTerrainCarved;
        }
    }
}
