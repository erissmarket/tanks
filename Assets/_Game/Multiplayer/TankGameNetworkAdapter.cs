using System;
using System.Collections.Generic;
using TankGame.Core;
using TankGame.Player;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TankGame.Multiplayer
{
    /// <summary>Host-owned command boundary. A queued client command is not an accepted command.</summary>
    public sealed class TankGameNetworkAdapter : MonoBehaviour
    {
        private const string ContextMessage = "TankGame.Context.v3";
        private const string ResultMessage = "TankGame.Result.v3";
        private const string AimMessage = "TankGame.AimIntent.v3";
        private const string AimResultMessage = "TankGame.AimResult.v3";
        private NgoMultiplayerService service;
        private NetworkManager manager;
        private GameManager game;
        private TurnManager turns;
        private MatchFlowController flow;
        private GameplayActionController actions;
        private IPlayerController[] players;
        private readonly Dictionary<ulong, ulong> lastSequences = new Dictionary<ulong, ulong>();
        private ulong? remoteOwner;
        private ulong nextSequence;
        private readonly Dictionary<ulong, ulong> lastAimSequences = new Dictionary<ulong, ulong>();
        private readonly ulong[] appliedAim = new ulong[2];
        private readonly AimSampleCoalescer aimSamples = new AimSampleCoalescer();
        private ulong aimSession, aimTurn;
        public ulong LastAimSentSequence { get; private set; }
        public event Action<Vector2, float> AimIntentAccepted;
        public ulong GetAppliedAimSequence(int player) => player >= 0 && player < 2 ? appliedAim[player] : 0;

        public int LocalPlayerIndex { get; private set; } = -1;
        public ulong SessionToken { get; private set; }
        public ulong TurnToken { get; private set; }
        public bool SessionAborted { get; private set; }
        public MatchState ObservedState { get; private set; }
        public int ObservedPlayerIndex { get; private set; } = -1;
        public bool CanSubmit => service != null && service.IsConnected && !SessionAborted
            && LocalPlayerIndex >= 0 && ObservedState == MatchState.TurnActive
            && ObservedPlayerIndex == LocalPlayerIndex && SessionToken != 0;

        public event Action<ulong, ulong, GameplayCommandResult> CommandProcessed;
        public event Action SessionEnded;

        /// <summary>Only the host supplies gameplay references. Client adapters need only a service.</summary>
        public void Initialize(NgoMultiplayerService multiplayer, GameManager gameManager = null,
            TurnManager turnManager = null, MatchFlowController matchFlow = null,
            GameplayActionController gameplayActions = null, IPlayerController first = null, IPlayerController second = null)
        {
            if (service != null) throw new InvalidOperationException("Initialize an adapter once.");
            if (multiplayer == null) throw new ArgumentNullException(nameof(multiplayer));
            if (gameManager != null && (turnManager == null || matchFlow == null || gameplayActions == null
                || first == null || second == null || first.PlayerIndex != 0 || second.PlayerIndex != 1))
                throw new ArgumentException("Host requires the match services and controllers in slots 0 and 1.");
            service = multiplayer; game = gameManager; turns = turnManager; flow = matchFlow; actions = gameplayActions;
            players = new[] { first, second };
            if (game != null) game.StateChanged += OnMatchState;
        }

        public bool Move(float direction) => Submit(GameplayNetworkAction.Move, direction, 0f);
        public bool Stop() => Submit(GameplayNetworkAction.Stop, 0f, 0f);
        public bool Aim(float angle) => Submit(GameplayNetworkAction.Aim, angle, 0f);
        public bool Aim(Vector2 direction, float power)
        {
            LanAimDiagnostics.Intent(this);
            direction = Normalized(direction);
            if (!float.IsFinite(power) || power < 0 || power > 1) throw new ArgumentOutOfRangeException(nameof(power));
            if (!CanSubmit || manager == null) return false;
            if (manager.IsHost) return Submit(GameplayNetworkAction.AimPower, direction.x, direction.y, power);
            aimSamples.Offer(direction, power);
            AimIntentAccepted?.Invoke(direction, power);
            return true;
        }
        public bool CommitAim(Vector2 direction, float power)
        { aimSamples.Reset(); direction = Normalized(direction); return Submit(GameplayNetworkAction.AimPower, direction.x, direction.y, power); }
        public bool Fire(Vector2 direction, float power = 1f)
        {
            aimSamples.Reset();
            direction = Normalized(direction);
            return Submit(GameplayNetworkAction.Fire, direction.x, direction.y, power);
        }
        public float GetMovementRemaining(int index) => actions != null ? actions.GetMovementRemaining(index) : 0f;
        public float GetShotPower(int index) => actions != null ? actions.GetShotPower(index) : 1f;
        private static Vector2 Normalized(Vector2 direction)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(direction.sqrMagnitude)
                || direction.sqrMagnitude < 0.000001f) throw new ArgumentOutOfRangeException(nameof(direction));
            return direction.normalized;
        }

        private bool Submit(GameplayNetworkAction action, float x, float y, float power = 1f, bool disposable = false)
        {
            var command = new GameplayNetworkCommand(SessionToken, TurnToken, ++nextSequence,
                (byte)Mathf.Max(0, LocalPlayerIndex), action, x, y, power: power);
            if (!command.HasValidValues()) throw new ArgumentOutOfRangeException(nameof(x), "Invalid command values.");
            if (!CanSubmit || manager == null) return false;
            LanAimDiagnostics.Sent(this, command, !manager.IsHost);
            if (action == GameplayNetworkAction.AimPower) LastAimSentSequence = command.Sequence;
            if (manager.IsHost) return Process(manager.LocalClientId, command) == GameplayCommandResult.Accepted;
            using (var writer = new FastBufferWriter(GameplayNetworkCommand.WireSize, Allocator.Temp))
            {
                var buffer = writer;
                command.WriteTo(ref buffer);
                manager.CustomMessagingManager.SendNamedMessage(disposable ? AimMessage : GameplayNetworkCommand.MessageName,
                    NetworkManager.ServerClientId, writer, disposable ? NetworkDelivery.Unreliable : NetworkDelivery.ReliableSequenced);
            }
            return true;
        }

        private void Update()
        {
            if (service == null) return;
            var current = service.NetworkManager;
            if (!service.IsRunning || service.State == MultiplayerState.Stopping)
            {
                if (manager != null) { AbortSession(); Unbind(); }
                return;
            }
            if (current == null || current.CustomMessagingManager == null) return;
            if (manager != current) Bind(current);
            if (aimSession != SessionToken || aimTurn != TurnToken || !CanSubmit)
            { aimSamples.Reset(); aimSession = SessionToken; aimTurn = TurnToken; }
            if (CanSubmit && !manager.IsHost && aimSamples.TryTake(Time.realtimeSinceStartupAsDouble, out var direction, out float power))
                Submit(GameplayNetworkAction.AimPower, direction.x, direction.y, power, true);
        }

        private void Bind(NetworkManager current)
        {
            Unbind();
            manager = current;
            SessionAborted = false;
            remoteOwner = null;
            lastSequences.Clear();
            lastAimSequences.Clear(); Array.Clear(appliedAim, 0, appliedAim.Length); aimSamples.Reset();
            nextSequence = 0;
            LocalPlayerIndex = manager.IsHost ? 0 : -1;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(GameplayNetworkCommand.MessageName, OnCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(ContextMessage, OnContext);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(ResultMessage, OnResult);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(AimMessage, OnAimCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(AimResultMessage, OnResult);
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
            if (manager.IsHost)
            {
                SessionToken = NewToken();
                TurnToken = game != null && game.CurrentState == MatchState.TurnActive ? 1UL : 0UL;
                foreach (ulong id in manager.ConnectedClientsIds) AssignOwner(id);
                BroadcastContext();
            }
        }

        private static ulong NewToken()
        {
            ulong value = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0);
            return value == 0 ? 1UL : value;
        }

        private void OnConnected(ulong id)
        {
            if (manager == null || !manager.IsHost) return;
            AssignOwner(id);
            BroadcastContext();
        }

        private void AssignOwner(ulong id)
        {
            if (!SessionAborted && id != manager.LocalClientId && !remoteOwner.HasValue) remoteOwner = id;
        }

        private void OnDisconnected(ulong id)
        {
            if (manager == null) return;
            if ((manager.IsHost && remoteOwner == id) || id == manager.LocalClientId) AbortSession();
        }

        private void OnMatchState(MatchState previous, MatchState current)
        {
            if (manager == null || !manager.IsHost) return;
            if (current == MatchState.Starting)
            {
                SessionToken = NewToken(); lastSequences.Clear(); lastAimSequences.Clear(); Array.Clear(appliedAim, 0, appliedAim.Length);
            }
            if (current == MatchState.TurnActive) TurnToken++;
            BroadcastContext();
        }

        private void BroadcastContext()
        {
            if (manager == null || !manager.IsHost) return;
            ObservedState = game != null ? game.CurrentState : MatchState.None;
            ObservedPlayerIndex = turns != null ? turns.CurrentPlayerIndex : -1;
            foreach (ulong id in manager.ConnectedClientsIds)
            {
                if (id == manager.LocalClientId) continue;
                int slot = remoteOwner == id ? 1 : -1;
                using (var writer = new FastBufferWriter(30, Allocator.Temp))
                {
                    writer.WriteValueSafe(GameplayNetworkCommand.ProtocolVersion);
                    writer.WriteValueSafe(SessionToken); writer.WriteValueSafe(TurnToken);
                    writer.WriteValueSafe(slot); writer.WriteValueSafe((int)ObservedState);
                    writer.WriteValueSafe(ObservedPlayerIndex); writer.WriteValueSafe(SessionAborted);
                    manager.CustomMessagingManager.SendNamedMessage(ContextMessage, id, writer, NetworkDelivery.ReliableSequenced);
                }
            }
        }

        private void OnContext(ulong sender, FastBufferReader reader)
        {
            if (manager == null || manager.IsServer || sender != NetworkManager.ServerClientId || reader.Length - reader.Position != 30) return;
            reader.ReadValueSafe(out byte version); if (version != GameplayNetworkCommand.ProtocolVersion) return;
            reader.ReadValueSafe(out ulong session); reader.ReadValueSafe(out ulong turn);
            reader.ReadValueSafe(out int slot); reader.ReadValueSafe(out int state);
            reader.ReadValueSafe(out int player); reader.ReadValueSafe(out bool aborted);
            if (slot < -1 || slot > 1 || state < 0 || state > (int)MatchState.MatchEnded || player < -1 || player > 1) return;
            SessionToken = session; TurnToken = turn; LocalPlayerIndex = slot;
            ObservedState = (MatchState)state; ObservedPlayerIndex = player;
            SessionAborted = aborted;
        }

        private void OnCommand(ulong sender, FastBufferReader reader)
        {
            if (manager == null || !manager.IsHost) return;
            if (!GameplayNetworkCommand.TryRead(reader, out var command))
            {
                Respond(sender, 0, GameplayCommandResult.Malformed);
                return;
            }
            Process(sender, command);
        }

        private void OnAimCommand(ulong sender, FastBufferReader reader)
        {
            if (manager == null || !manager.IsHost || !GameplayNetworkCommand.TryRead(reader, out var command)
                || command.Action != GameplayNetworkAction.AimPower) return;
            Process(sender, command, true);
        }

        private GameplayCommandResult Process(ulong sender, GameplayNetworkCommand command, bool disposable = false)
        {
            GameplayCommandResult result = Validate(sender, command, disposable);
            if (result == GameplayCommandResult.Accepted)
            {
                if (!disposable) lastSequences[sender] = command.Sequence;
                if (command.Action == GameplayNetworkAction.AimPower) lastAimSequences[sender] = command.Sequence;
                try
                {
                    switch (command.Action)
                    {
                        case GameplayNetworkAction.Move: actions.Move(command.PlayerIndex, command.X); break;
                        case GameplayNetworkAction.Stop: actions.Stop(command.PlayerIndex); break;
                        case GameplayNetworkAction.Aim: actions.Aim(command.PlayerIndex, command.X); break;
                        case GameplayNetworkAction.Fire: actions.Fire(command.PlayerIndex, new Vector2(command.X, command.Y), command.Power); break;
                        case GameplayNetworkAction.AimPower: actions.Aim(command.PlayerIndex, new Vector2(command.X, command.Y), command.Power); break;
                    }
                    if (command.Action == GameplayNetworkAction.AimPower || command.Action == GameplayNetworkAction.Aim)
                        appliedAim[command.PlayerIndex] = command.Sequence;
                }
                catch (ArgumentException) { result = GameplayCommandResult.GameplayRejected; }
                catch (InvalidOperationException) { result = GameplayCommandResult.GameplayRejected; }
                catch (Exception error)
                {
                    Debug.LogException(error);
                    AbortSession();
                    result = GameplayCommandResult.GameplayRejected;
                }
            }
            Respond(sender, command.Sequence, result, disposable);
            LanAimDiagnostics.Processed(this, command, result, sender != manager.LocalClientId);
            return result;
        }

        private GameplayCommandResult Validate(ulong sender, GameplayNetworkCommand command, bool disposable = false)
        {
            if (command.Version != GameplayNetworkCommand.ProtocolVersion) return GameplayCommandResult.Malformed;
            if (!command.HasValidValues()) return GameplayCommandResult.InvalidValue;
            if (manager == null || !manager.IsHost || service == null || !service.IsConnected || SessionAborted
                || game == null || turns == null || flow == null || actions == null) return GameplayCommandResult.Unavailable;
            bool connected = false;
            foreach (ulong id in manager.ConnectedClientsIds) if (id == sender) connected = true;
            int slot = sender == manager.LocalClientId ? 0 : remoteOwner == sender ? 1 : -1;
            if (!connected || slot < 0 || command.PlayerIndex != slot) return GameplayCommandResult.NotOwner;
            if (command.Session != SessionToken || command.Turn != TurnToken || command.Sequence == 0
                || (!disposable && lastSequences.TryGetValue(sender, out ulong previous) && command.Sequence <= previous)
                || (command.Action == GameplayNetworkAction.AimPower && lastAimSequences.TryGetValue(sender, out ulong previousAim) && command.Sequence <= previousAim)) return GameplayCommandResult.Stale;
            if (!game.isActiveAndEnabled || !flow.isActiveAndEnabled || game.CurrentState != MatchState.TurnActive
                || turns.CurrentPlayerIndex != slot || players[slot] == null
                || (players[slot] is UnityEngine.Object player && player == null)
                || players[slot].PlayerIndex != slot || !players[slot].CanAct) return GameplayCommandResult.WrongTurn;
            return GameplayCommandResult.Accepted;
        }

        private void Respond(ulong sender, ulong sequence, GameplayCommandResult result, bool disposable = false)
        {
            if (manager != null && manager.IsHost && sender != manager.LocalClientId)
                using (var writer = new FastBufferWriter(9, Allocator.Temp))
                {
                    writer.WriteValueSafe(sequence); writer.WriteValueSafe((byte)result);
                    manager.CustomMessagingManager.SendNamedMessage(disposable ? AimResultMessage : ResultMessage, sender, writer,
                        disposable ? NetworkDelivery.Unreliable : NetworkDelivery.ReliableSequenced);
                }
            PublishResult(sender, sequence, result);
        }

        private void OnResult(ulong sender, FastBufferReader reader)
        {
            if (manager == null || manager.IsServer || sender != NetworkManager.ServerClientId || reader.Length - reader.Position != 9) return;
            reader.ReadValueSafe(out ulong sequence); reader.ReadValueSafe(out byte result);
            LanAimDiagnostics.Ack(this, sequence);
            if (result <= (byte)GameplayCommandResult.GameplayRejected)
                PublishResult(manager.LocalClientId, sequence, (GameplayCommandResult)result);
        }

        private void PublishResult(ulong sender, ulong sequence, GameplayCommandResult result)
        {
            if (CommandProcessed == null) return;
            foreach (Action<ulong, ulong, GameplayCommandResult> handler in CommandProcessed.GetInvocationList())
                try { handler(sender, sequence, result); } catch (Exception error) { Debug.LogException(error); }
        }

        private void AbortSession()
        {
            if (SessionAborted) return;
            SessionAborted = true;
            if (manager != null && manager.IsHost && flow != null && game != null
                && game.CurrentState != MatchState.None && game.CurrentState != MatchState.MatchEnded) flow.EndMatch();
            if (manager != null && manager.IsHost) BroadcastContext();
            if (SessionEnded == null) return;
            foreach (Action handler in SessionEnded.GetInvocationList())
                try { handler(); } catch (Exception error) { Debug.LogException(error); }
        }

        private void Unbind()
        {
            if (manager != null)
            {
                manager.OnClientConnectedCallback -= OnConnected;
                manager.OnClientDisconnectCallback -= OnDisconnected;
                if (manager.CustomMessagingManager != null)
                {
                    manager.CustomMessagingManager.UnregisterNamedMessageHandler(GameplayNetworkCommand.MessageName);
                    manager.CustomMessagingManager.UnregisterNamedMessageHandler(ContextMessage);
                    manager.CustomMessagingManager.UnregisterNamedMessageHandler(ResultMessage);
                    manager.CustomMessagingManager.UnregisterNamedMessageHandler(AimMessage);
                    manager.CustomMessagingManager.UnregisterNamedMessageHandler(AimResultMessage);
                }
            }
            manager = null;
            aimSamples.Reset();
            LocalPlayerIndex = -1;
            SessionToken = TurnToken = 0;
        }

        private void OnDisable()
        {
            try { AbortSession(); }
            finally { Unbind(); }
        }

        private void OnDestroy()
        {
            if (game != null) game.StateChanged -= OnMatchState;
        }
    }
}
