using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TankGame.Tank;

namespace TankGame.Multiplayer
{
    /// <summary>Development-only local-clock measurements. No cross-device clock subtraction.</summary>
    public sealed class LanAimDiagnostics : MonoBehaviour
    {
        [Serializable] public sealed class Counters
        {
            public double seconds, fps, ackMs, observationMs, visualAgeMs, rttMs;
            public int tick, frames, intents, aimSent, aimReceived, aimProcessed, staleAim, snapshotsSent, snapshotsReceived, aimObservations;
            public long payloadBytes;
            public ulong lastAimSequence;
            public ulong lastReceivedAimSequence, lastObservedAimSequence;
            public int staleAimObservations, aimObservationGaps;
            public int acknowledgements, matchedObservations, visualChanges, localAnticipations;
            public float maximumVisualStep;
            public double visualStepSquared, angularTravel;
        }
        private sealed class Pending { public double at; public float angle, power; public ulong sequence; }
        private static readonly Dictionary<Scene, LanAimDiagnostics> instances = new Dictionary<Scene, LanAimDiagnostics>();
        public readonly Counters Data = new Counters();
        private readonly Dictionary<ulong, double> sent = new Dictionary<ulong, double>();
        private readonly List<Pending> pending = new List<Pending>();
        private double started, lastObservation, nextLog, ackTotal, observationTotal;
        private LanGameplaySession session;
        private Transform remotePivot;
        private float previousAngle;
        private string output;
        private bool visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => instances.Clear();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
#endif
        }
        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "LanGameplayTest" || instances.ContainsKey(scene)) return;
            var root = new GameObject("LAN diagnostics (development only)"); SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<LanAimDiagnostics>();
        }
        private void Awake()
        {
            instances[gameObject.scene] = this; started = Time.realtimeSinceStartupAsDouble;
            string[] args = Environment.GetCommandLineArgs(); int dir = Array.IndexOf(args, "-audit-dir"), role = Array.IndexOf(args, "-phase11c");
            if (Array.IndexOf(args, "-phase15") >= 0 && dir >= 0 && role >= 0 && dir + 1 < args.Length && role + 1 < args.Length)
            { Directory.CreateDirectory(args[dir + 1]); output = Path.Combine(args[dir + 1], args[role + 1] + ".metrics.jsonl"); }
        }
        private static LanAimDiagnostics Get(Component source) => source != null && instances.TryGetValue(source.gameObject.scene, out var value) ? value : null;

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Intent(Component source) { var d = Get(source); if (d != null) d.Data.intents++; }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Sent(Component source, GameplayNetworkCommand command, bool wire)
        {
            var d = Get(source); if (d == null) return;
            if (wire) { if (d.sent.Count >= 256) d.sent.Clear(); d.sent[command.Sequence] = Time.realtimeSinceStartupAsDouble; d.Data.payloadBytes += GameplayNetworkCommand.WireSize; }
            if (command.Action != GameplayNetworkAction.Aim && command.Action != GameplayNetworkAction.AimPower) return;
            d.Data.aimSent++; d.Data.lastAimSequence = command.Sequence;
            float angle = command.X;
            var state = d.session != null ? d.session.Stream.Latest : null;
            if (command.Action == GameplayNetworkAction.AimPower && state != null)
            {
                var tank = command.PlayerIndex == 0 ? state.PlayerZero : state.PlayerOne;
                Vector2 local = Quaternion.Euler(0, 0, -tank.Rotation) * new Vector2(command.X, command.Y);
                angle = Mathf.Atan2(local.y, command.PlayerIndex == 1 ? -local.x : local.x) * Mathf.Rad2Deg;
            }
            if (d.pending.Count >= 128) d.pending.RemoveAt(0);
            d.pending.Add(new Pending { at = Time.realtimeSinceStartupAsDouble, angle = Mathf.Clamp(angle, 0, 90), power = command.Power, sequence = command.Sequence });
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Processed(Component source, GameplayNetworkCommand command, GameplayCommandResult result, bool received)
        {
            var d = Get(source); if (d == null || (command.Action != GameplayNetworkAction.Aim && command.Action != GameplayNetworkAction.AimPower)) return;
            if (received) d.Data.aimReceived++;
            if (received) d.Data.lastReceivedAimSequence = command.Sequence;
            if (result == GameplayCommandResult.Accepted) d.Data.aimProcessed++;
            if (result == GameplayCommandResult.Stale) d.Data.staleAim++;
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Ack(Component source, ulong sequence)
        {
            var d = Get(source); if (d == null || !d.sent.TryGetValue(sequence, out double at)) return;
            d.sent.Remove(sequence); d.ackTotal += (Time.realtimeSinceStartupAsDouble - at) * 1000;
            d.Data.ackMs = d.ackTotal / ++d.Data.acknowledgements;
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Snapshot(Component source, GameplayNetworkState state, bool sending, int peers = 1)
        {
            var d = Get(source); if (d == null) return;
            if (sending) { d.Data.snapshotsSent++; d.Data.payloadBytes += GameplayNetworkState.WireSize * peers; }
            else d.Data.snapshotsReceived++;
            d.lastObservation = Time.realtimeSinceStartupAsDouble;
            if (d.session == null || state == null) return;
            var tank = d.session.Adapter.LocalPlayerIndex == 1 ? state.PlayerOne : state.PlayerZero;
            d.Match(tank.Aim, tank.Power);
        }
        private void Match(float angle, float power)
        {
            var d = this;
            for (int i = d.pending.Count - 1; i >= 0; i--)
            {
                var p = d.pending[i];
                if (Mathf.Abs(angle - p.angle) < .05f && Mathf.Abs(power - p.power) < .001f)
                { d.observationTotal += (Time.realtimeSinceStartupAsDouble - p.at) * 1000; d.Data.observationMs = d.observationTotal / ++d.Data.matchedObservations; d.pending.RemoveAt(i); }
                else if (Time.realtimeSinceStartupAsDouble - p.at > 2) d.pending.RemoveAt(i);
            }
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void ObserveAim(Component source, AimObservation sample)
        {
            var d = Get(source); if (d == null || d.session == null) return;
            bool one = d.session.Adapter.LocalPlayerIndex == 1;
            d.Data.lastObservedAimSequence = one ? sample.OneSequence : sample.ZeroSequence;
            d.Match(one ? sample.OneAngle : sample.ZeroAngle, one ? sample.OnePower : sample.ZeroPower);
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void ObservationOrdering(Component source, bool stale, ulong gap)
        { var d = Get(source); if (d == null) return; if (stale) d.Data.staleAimObservations++; d.Data.aimObservationGaps += (int)System.Math.Min(gap, 10000UL); }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Anticipated(Component source) { var d = Get(source); if (d != null) d.Data.localAnticipations++; }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void AimObservation(Component source, int bytes, int peers = 0)
        {
            var d = Get(source); if (d == null) return;
            d.Data.aimObservations++; d.Data.payloadBytes += bytes * peers; d.lastObservation = Time.realtimeSinceStartupAsDouble;
        }
        private void LateUpdate()
        {
            if (session == null) session = FindAnyObjectByType<LanGameplaySession>();
            Data.frames++; Data.seconds = Time.realtimeSinceStartupAsDouble - started; Data.fps = Data.frames / Math.Max(.001, Data.seconds);
            if (session == null) return;
            var manager = session.Service.NetworkManager;
            if (manager != null)
            {
                Data.tick = (int)manager.NetworkConfig.TickRate;
                if (!manager.IsHost && manager.IsConnectedClient) Data.rttMs = manager.NetworkConfig.NetworkTransport.GetCurrentRtt(0);
            }
            Data.visualAgeMs = lastObservation == 0 ? -1 : (Time.realtimeSinceStartupAsDouble - lastObservation) * 1000;
            if (remotePivot == null && session.Adapter.LocalPlayerIndex >= 0)
                foreach (var tank in FindObjectsByType<TankController>())
                    if (tank.name == (session.Adapter.LocalPlayerIndex == 0 ? "Player1" : "Player0"))
                        foreach (var t in tank.GetComponentsInChildren<Transform>()) if (t.name == "BarrelPivot") remotePivot = t;
            if (remotePivot != null)
            {
                float angle = session.AimPresentation != null ? session.AimPresentation.DisplayAngle(session.Adapter.LocalPlayerIndex == 0 ? 1 : 0)
                    : Mathf.DeltaAngle(0, remotePivot.localEulerAngles.z);
                float step = Mathf.Abs(Mathf.DeltaAngle(previousAngle, angle));
                if (step > .001f) { Data.visualChanges++; Data.maximumVisualStep = Mathf.Max(Data.maximumVisualStep, step); Data.visualStepSquared += step * step; Data.angularTravel += step; } previousAngle = angle;
            }
            if (Time.realtimeSinceStartupAsDouble < nextLog) return; nextLog = Time.realtimeSinceStartupAsDouble + .5;
            if (output != null) File.AppendAllText(output, JsonUtility.ToJson(Data) + Environment.NewLine);
            if (visible) UnityEngine.Debug.Log("LAN_METRICS " + JsonUtility.ToJson(Data));
        }
        private void OnGUI()
        {
            Rect safe = Screen.safeArea; if (safe.height <= 0) return;
            var matrix = GUI.matrix; GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax), Quaternion.identity, new Vector3(safe.width / 1000, safe.height / 600, 1));
            if (GUI.Button(new Rect(400, 140, 200, 40), visible ? "HIDE NET METRICS" : "NET METRICS")) visible = !visible;
            if (visible) GUI.Box(new Rect(380, 185, 330, 160), "FPS " + Data.fps.ToString("F1") + "  RTT " + Data.rttMs + "ms / tick " + Data.tick
                + "\nAim sent/rx " + Data.aimSent + "/" + Data.aimReceived + " stale " + Data.staleAim
                + "\nSnapshots " + Data.snapshotsSent + "/" + Data.snapshotsReceived + " Aim obs " + Data.aimObservations
                + "\nAck " + Data.ackMs.ToString("F1") + "ms  State echo " + Data.observationMs.ToString("F1") + "ms"
                + "\nReceive age " + Data.visualAgeMs.ToString("F1") + "ms\nHost FPS: host's metrics; loss: unavailable");
            GUI.matrix = matrix;
        }
        private void OnDestroy() { if (instances.TryGetValue(gameObject.scene, out var d) && d == this) instances.Remove(gameObject.scene); }
    }
}
