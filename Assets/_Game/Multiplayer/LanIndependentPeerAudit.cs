#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using TankGame.Core;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Terrain;
using TankGame.Testing;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;

namespace TankGame.Multiplayer
{
    /// <summary>Explicit command-line-only development-player audit driver. Never starts in normal play.</summary>
    public sealed class LanIndependentPeerAudit : MonoBehaviour
    {
#pragma warning disable CS0649 // Populated by JsonUtility from the audit command file.
        [Serializable] private sealed class Request
        {
            public int id;
            public string op;
            public float x, y;
            public float power = 1f, duration;
            public int player = 1, version = GameplayNetworkCommand.ProtocolVersion;
            public ulong session, turn, sequence;
        }
#pragma warning restore CS0649
        [Serializable] private sealed class Record
        {
            public string utc, kind, detail, role;
            public int pid, request, slot, count, player, outcome, craters, localCraters, projectiles, managers, owner, effects;
            public float[] surface;
            public ulong session, turn, frame, shot, completed;
            public bool connected, canAct, aborted, active, simulated;
            public string state;
            public Vector2 zero, one, actualZero, actualOne, projectile;
            public float aimZero, aimOne, healthZero, healthOne, localHealthZero, localHealthOne, actualAimZero, actualAimOne;
            public float budgetZero, budgetOne, powerZero, powerOne, displayAimZero, displayAimOne;
            public Vector2 velocity;
        }
        private string role, directory, output, input;
        private int readLines, request, pid;
        private float nextSample;
        private ulong sequence = 100000;
        private LanGameplaySession session;
        private LanRoomBrowser discoveryAudit;
        private string discoveredAddress;
        private float heldMove, holdUntil, nextMove;
        private float sweepUntil, sweepStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-phase11c");
            int path = Array.IndexOf(args, "-audit-dir");
            if (at < 0 || at + 1 >= args.Length || path < 0 || path + 1 >= args.Length
                || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LanGameplayTest") return;
            var driver = new GameObject("Independent peer audit (explicit opt-in)").AddComponent<LanIndependentPeerAudit>();
            driver.role = args[at + 1]; driver.directory = args[path + 1];
        }

        private void Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            Directory.CreateDirectory(directory);
            output = Path.Combine(directory, role + ".jsonl");
            input = Path.Combine(directory, role + ".commands.jsonl");
            session = FindAnyObjectByType<LanGameplaySession>();
            bool metricsOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "-phase15") >= 0;
            session.Adapter.CommandProcessed += (sender, seq, result) => { if (!metricsOnly) Write("ack", sender + ":" + seq + ":" + result); };
            session.Service.ClientConnected += id => Write("connected", id.ToString());
            session.Service.ClientDisconnected += id => Write("disconnected", id.ToString());
            session.Service.ConnectionFailed += reason => Write("failure", reason);
            session.Service.StateChanged += value => Write("lifecycle", value.ToString());
            session.Stream.PresentationReceived += value => Write("presentation", value.Sequence + ":" + value.Kind + ":replay=" + value.IsReplay);
            session.Stream.SnapshotReceived += value => { if (value.State != lastState) { lastState = value.State; Write("state", value.State.ToString()); } };
            Write("boot", Application.unityVersion);
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                if (Array.IndexOf(args, "-phase12-discovery") >= 0 && role != "observer")
                {
                    discoveryAudit = new LanRoomBrowser(session.Service, session.StartHost, session.Connect,
                        () => !session.Adapter.SessionAborted && session.Stream.Latest != null);
                    discoveryAudit.Error += message => Write("exception", message);
                    discoveryAudit.Discovery.RoomAdded += room => Write("room", room.Key);
                    if (role == "host") discoveryAudit.CreateRoom("Phase12 Audit");
                    else discoveryAudit.FindRooms();
                }
                else if (role == "host") session.StartHost();
                else session.Connect("127.0.0.1");
            }
            catch (Exception error) { Write("exception", error.ToString()); }
        }
        private MatchState lastState;

        private void OnDestroy() => discoveryAudit?.Dispose();

        private void Update()
        {
            if (session == null) return;
            if (Time.unscaledTime < sweepUntil && session.Adapter.CanSubmit)
            {
                float phase = (Time.unscaledTime - sweepStarted) * 2;
                float angle = (45 + 15 * Mathf.Sin(phase)) * Mathf.Deg2Rad;
                session.Adapter.Aim(new Vector2((session.Adapter.LocalPlayerIndex == 1 ? -1 : 1) * Mathf.Cos(angle), Mathf.Sin(angle)), .6f + .2f * Mathf.Sin(phase));
            }
            if (Time.unscaledTime < holdUntil && Time.unscaledTime >= nextMove && session.Adapter.CanSubmit)
            { session.Adapter.Move(heldMove); nextMove = Time.unscaledTime + 0.08f; }
            if (discoveryAudit != null)
            {
                discoveryAudit.Pump();
                // Explicit opt-in test selection only. The normal room UI never auto-joins.
                if (role == "client" && discoveredAddress == null && !session.Service.IsRunning)
                    foreach (var room in discoveryAudit.Discovery.Rooms)
                        if (room.Name == "Phase12 Audit" && room.Joinable)
                        {
                            discoveredAddress = room.Address;
                            Write("discovery-join", room.Key);
                            discoveryAudit.Join(room);
                            break;
                        }
            }
            if (Time.unscaledTime >= nextSample) { nextSample = Time.unscaledTime + 0.2f; Write("sample", ""); }
            if (!File.Exists(input)) return;
            string[] lines;
            try { lines = File.ReadAllLines(input); } catch (IOException) { return; }
            while (readLines < lines.Length)
            {
                string line = lines[readLines++];
                if (string.IsNullOrWhiteSpace(line)) continue;
                try { Execute(JsonUtility.FromJson<Request>(line)); }
                catch (Exception error) { Write("exception", error.GetType().Name + ":" + error.Message); }
            }
        }

        private void Execute(Request command)
        {
            request = command.id;
            bool accepted = false;
            switch (command.op)
            {
                case "sweep": sweepStarted = Time.unscaledTime; sweepUntil = sweepStarted + Mathf.Clamp(command.duration, 0, 30); accepted = true; break;
                case "snaprate": session.Stream.SnapshotRate = command.x; accepted = true; break;
                case "aimconfig": session.AimStream.Rate = command.x; session.AimPresentation.InterpolationDelay = command.y; session.AimPresentation.AnticipationEnabled = command.power > 0; accepted = true; break;
                case "aimcommit": accepted = session.Adapter.CommitAim(new Vector2(command.x, command.y), command.power); break;
                case "rapidfire": session.Adapter.Aim(new Vector2(command.x, command.y), command.power); accepted = session.Adapter.Fire(new Vector2(command.x, command.y), command.power); break;
                case "rawaim":
                    Send(new GameplayNetworkCommand(command.session == 0 ? session.Adapter.SessionToken : command.session,
                        command.turn == 0 ? session.Adapter.TurnToken : command.turn, command.sequence, (byte)command.player,
                        GameplayNetworkAction.AimPower, command.x, command.y, (byte)command.version, command.power), true); accepted = true; break;
                case "impair":
                    RequireHost();
                    var transport = (Unity.Netcode.Transports.UTP.UnityTransport)session.Service.NetworkManager.NetworkConfig.NetworkTransport;
                    transport.GetNetworkDriver().ModifyNetworkSimulatorParameters(new NetworkSimulatorParameter
                    { SendDelayMS = (uint)Mathf.Clamp(command.x, 0, 200), SendJitterMS = command.x > 0 ? 20u : 0u,
                        SendPacketLossPercent = command.x > 0 ? 5 : 0, ReceivePacketLossPercent = command.x > 0 ? 5 : 0, SendDuplicatePercent = command.x > 0 ? 5 : 0 });
                    accepted = true; break;
                case "move": accepted = session.Adapter.Move(command.x); break;
                case "stop": accepted = session.Adapter.Stop(); break;
                case "aim": accepted = session.Adapter.Aim(command.x); break;
                case "fire": accepted = session.Adapter.Fire(new Vector2(command.x, command.y)); break;
                case "aimpower": accepted = session.Adapter.Aim(new Vector2(command.x, command.y), command.power); break;
                case "powerfire": accepted = session.Adapter.Fire(new Vector2(command.x, command.y), command.power); break;
                case "holdmove":
                    if (!float.IsFinite(command.duration) || command.duration < 0 || command.duration > 10) throw new ArgumentOutOfRangeException(nameof(command.duration));
                    accepted = session.Adapter.Move(command.x); heldMove = command.x; holdUntil = Time.unscaledTime + command.duration; nextMove = 0; break;
                case "raw":
                    var packet = new GameplayNetworkCommand(command.session == 0 ? session.Adapter.SessionToken : command.session,
                        command.turn == 0 ? session.Adapter.TurnToken : command.turn,
                        command.sequence == 0 ? ++sequence : command.sequence, (byte)command.player,
                        (GameplayNetworkAction)(int)command.x, command.y, 0, (byte)command.version);
                    Send(packet); accepted = true; break;
                case "rawfire":
                    Send(new GameplayNetworkCommand(command.session == 0 ? session.Adapter.SessionToken : command.session,
                        command.turn == 0 ? session.Adapter.TurnToken : command.turn,
                        command.sequence == 0 ? ++sequence : command.sequence, (byte)command.player,
                        GameplayNetworkAction.Fire, command.x, command.y, (byte)command.version)); accepted = true; break;
                case "nan":
                    Send(new GameplayNetworkCommand(session.Adapter.SessionToken, session.Adapter.TurnToken,
                        ++sequence, 1, GameplayNetworkAction.Move, float.NaN)); accepted = true; break;
                case "snapshot":
                    var state = session.Stream.Latest;
                    var forged = new GameplayNetworkState(command.session == 0 ? state.Session : command.session,
                        command.sequence == 0 ? state.Frame : command.sequence, MatchState.MatchEnded, 0, 1,
                        new NetworkTankState(state.PlayerZero.Position, state.PlayerZero.Rotation, state.PlayerZero.Aim, 0, 100),
                        state.PlayerOne, state.Shot, false, -1, Vector2.zero, Vector2.zero,
                        state.CompletedShot, state.CompletionPosition, state.CraterCount);
                    using (var writer = new FastBufferWriter(GameplayNetworkState.WireSize, Allocator.Temp))
                    {
                        writer.WriteValueSafe(forged);
                        session.Service.NetworkManager.CustomMessagingManager.SendNamedMessage(GameplayNetworkState.MessageName,
                            session.Service.IsHost ? 1UL : NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
                    }
                    accepted = true; break;
                case "presentationReplay":
                    RequireHost();
                    using (var writer = new FastBufferWriter(GameplayPresentationEvent.WireSize, Allocator.Temp))
                    {
                        writer.WriteValueSafe(new GameplayPresentationEvent(command.session == 0 ? session.Adapter.SessionToken : command.session,
                            command.sequence == 0 ? 1UL : command.sequence, GameplayPresentationKind.TerrainCarved, Vector2.zero, 2));
                        session.Service.NetworkManager.CustomMessagingManager.SendNamedMessage(GameplayPresentationEvent.MessageName,
                            1UL, writer, NetworkDelivery.ReliableSequenced);
                    }
                    accepted = true; break;
                case "damage":
                    RequireHost();
                    foreach (var tank in FindObjectsByType<TankController>())
                        if (tank.PlayerIndex == command.player) tank.GetComponent<HealthComponent>().TakeDamage(command.x);
                    accepted = true; break;
                case "draw":
                    RequireHost();
                    foreach (var tank in FindObjectsByType<TankController>()) tank.GetComponent<HealthComponent>().TakeDamage(1000);
                    accepted = true; break;
                case "end": RequireHost(); FindAnyObjectByType<MatchFlowController>().EndMatch(); accepted = true; break;
                case "restart": accepted = session.RestartMatch(); break;
                case "repeatstart": session.Service.StartHost(); break;
                case "badip": session.Service.StartClient("999.1.1.1"); break;
                case "badport": session.Service.StartClient("127.0.0.1", 65536); break;
                case "disconnect": session.Disconnect(); accepted = true; break;
                case "reconnect": session.Service.StartClient("127.0.0.1"); accepted = true; break;
                case "quit": session.Disconnect(); Application.Quit(); accepted = true; break;
                default: throw new ArgumentException("Unknown audit command.");
            }
            Write("command", command.op + ":" + accepted);
        }

        private void RequireHost()
        {
            if (!session.Service.IsHost) throw new InvalidOperationException("Host-only audit fault injection.");
        }

        private void Send(GameplayNetworkCommand packet, bool disposable = false)
        {
            var manager = session.Service.NetworkManager;
            if (manager == null || !session.Service.IsConnected || manager.IsHost)
                throw new InvalidOperationException("Raw wire probe requires a connected remote peer.");
            using (var writer = new FastBufferWriter(GameplayNetworkCommand.WireSize, Allocator.Temp))
            {
                var copy = writer; packet.WriteTo(ref copy);
                manager.CustomMessagingManager.SendNamedMessage(disposable ? "TankGame.AimIntent.v3" : GameplayNetworkCommand.MessageName,
                    NetworkManager.ServerClientId, writer, disposable ? NetworkDelivery.Unreliable : NetworkDelivery.ReliableSequenced);
            }
        }

        private void Write(string kind, string detail)
        {
            if (output == null) return;
            var adapter = session.Adapter;
            var state = session.Stream.Latest;
            var row = new Record { utc = DateTime.UtcNow.ToString("O"), kind = kind, detail = detail, role = role, pid = pid,
                request = request, slot = adapter.LocalPlayerIndex, count = session.Service.ConnectedClientCount,
                session = adapter.SessionToken, turn = adapter.TurnToken, connected = session.Service.IsConnected,
                canAct = adapter.CanSubmit, aborted = adapter.SessionAborted,
                managers = FindObjectsByType<NetworkManager>().Length, projectiles = FindObjectsByType<Projectile>().Length };
            if (session.AimPresentation != null)
            { row.displayAimZero = session.AimPresentation.DisplayAngle(0); row.displayAimOne = session.AimPresentation.DisplayAngle(1); }
            if (state != null)
            {
                row.frame = state.Frame; row.state = state.State.ToString(); row.player = state.CurrentPlayer;
                row.outcome = state.Outcome; row.zero = state.PlayerZero.Position; row.one = state.PlayerOne.Position;
                row.aimZero = state.PlayerZero.Aim; row.aimOne = state.PlayerOne.Aim;
                row.budgetZero = state.PlayerZero.MovementRemaining; row.budgetOne = state.PlayerOne.MovementRemaining;
                row.powerZero = state.PlayerZero.Power; row.powerOne = state.PlayerOne.Power; row.velocity = state.ProjectileVelocity;
                row.healthZero = state.PlayerZero.Health; row.healthOne = state.PlayerOne.Health;
                row.shot = state.Shot; row.completed = state.CompletedShot; row.active = state.ProjectileActive;
                row.projectile = state.ProjectilePosition; row.craters = state.CraterCount;
                row.owner = state.ProjectileOwner;
            }
            foreach (var tank in FindObjectsByType<TankController>())
            {
                bool zero = tank.name == "Player0";
                if (zero) { row.actualZero = tank.transform.position; row.localHealthZero = tank.GetComponent<HealthComponent>().CurrentHealth; }
                else { row.actualOne = tank.transform.position; row.localHealthOne = tank.GetComponent<HealthComponent>().CurrentHealth; }
                row.simulated |= tank.GetComponent<Rigidbody2D>().simulated;
                foreach (var child in tank.GetComponentsInChildren<Transform>())
                    if (child.name == "BarrelPivot")
                    { if (zero) row.actualAimZero = child.localEulerAngles.z; else row.actualAimOne = child.localEulerAngles.z; }
            }
            var ground = FindAnyObjectByType<DestructibleTerrain>();
            row.localCraters = ground != null ? ground.CraterCount : -1;
            if (ground != null)
            {
                row.surface = new float[25];
                for (int i = 0; i < row.surface.Length; i++) row.surface[i] = ground.SurfaceHeightAt(-6 + i * 0.5f);
            }
            foreach (var animator in FindObjectsByType<Animator>())
                if (animator.name.StartsWith("BasicExplosion", StringComparison.Ordinal)) row.effects++;
            File.AppendAllText(output, JsonUtility.ToJson(row) + Environment.NewLine);
        }
    }
}
#endif
