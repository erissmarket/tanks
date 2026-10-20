using System.Collections.Generic;
using TankGame.Core;
using UnityEngine;

namespace TankGame.Multiplayer
{
    /// <summary>Sprite-only proxies leave the authoritative barrel, muzzle and physics untouched.</summary>
    public sealed class LanAimPresentation : MonoBehaviour
    {
        private LanGameplaySession session;
        private LanAimStream stream;
        private readonly Transform[] pivots = new Transform[2], proxies = new Transform[2];
        private readonly List<SpriteRenderer> originals = new List<SpriteRenderer>();
        private readonly List<bool> enabledStates = new List<bool>();
        private ulong generation, turn;
        private MatchState phase;
        private bool anticipating;
        private float predictedAngle, predictedPower;
        private double predictedAt;
        private float interpolationDelay = .035f;
        public float InterpolationDelay { get => interpolationDelay; set { if (!float.IsFinite(value) || value < 0 || value > .075f) throw new System.ArgumentOutOfRangeException(nameof(value)); interpolationDelay = value; } }
        public bool AnticipationEnabled { get; set; } = true;
        public float DisplayAngle(int player) => proxies[player] != null ? Mathf.Clamp(Mathf.DeltaAngle(0, proxies[player].localEulerAngles.z), 0, 90) : 0;
        public void Initialize(LanGameplaySession session, LanAimStream stream, Transform zero, Transform one)
        {
            this.session = session; this.stream = stream; pivots[0] = zero; pivots[1] = one;
            for (int i = 0; i < 2; i++)
            {
                var root = new GameObject("LAN barrel visual (presentation only)");
                proxies[i] = root.transform; proxies[i].SetParent(pivots[i].parent, false);
                proxies[i].localPosition = pivots[i].localPosition; proxies[i].localRotation = pivots[i].localRotation; proxies[i].localScale = pivots[i].localScale;
                foreach (var original in pivots[i].GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var visual = new GameObject(original.name + " visual"); visual.transform.SetParent(proxies[i], false);
                    visual.transform.localPosition = pivots[i].InverseTransformPoint(original.transform.position);
                    visual.transform.localRotation = Quaternion.Inverse(pivots[i].rotation) * original.transform.rotation;
                    Vector3 scale = Vector3.one;
                    for (var t = original.transform; t != pivots[i] && t != null; t = t.parent) scale = Vector3.Scale(scale, t.localScale);
                    visual.transform.localScale = scale;
                    var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = original.sprite; renderer.sharedMaterial = original.sharedMaterial;
                    renderer.color = original.color; renderer.flipX = original.flipX; renderer.flipY = original.flipY;
                    renderer.sortingLayerID = original.sortingLayerID; renderer.sortingOrder = original.sortingOrder;
                    renderer.drawMode = original.drawMode; renderer.size = original.size; renderer.maskInteraction = original.maskInteraction;
                    renderer.enabled = original.enabled; originals.Add(original); enabledStates.Add(original.enabled); original.enabled = false;
                }
            }
            session.Adapter.AimIntentAccepted += Anticipate;
            session.Adapter.CommandProcessed += OnResult;
        }
        private void Anticipate(Vector2 direction, float power)
        {
            if (!AnticipationEnabled || !session.Adapter.CanSubmit || session.Service.IsHost) return;
            int player = session.Adapter.LocalPlayerIndex;
            if (player < 0) return;
            Vector2 local = pivots[player].parent.InverseTransformVector(direction);
            predictedAngle = Mathf.Clamp(Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg, 0, 90);
            predictedPower = power; predictedAt = Time.realtimeSinceStartupAsDouble; anticipating = true;
            // Same input callback, not a future snapshot or frame.
            proxies[player].localRotation = Quaternion.Euler(0, 0, predictedAngle);
            LanAimDiagnostics.Anticipated(this);
        }
        private void OnResult(ulong sender, ulong sequence, GameplayCommandResult result)
        { if (result != GameplayCommandResult.Accepted && result != GameplayCommandResult.Stale
                && sequence >= session.Adapter.LastAimSentSequence) anticipating = false; }
        private void LateUpdate()
        {
            if (session == null || stream == null) return;
            var adapter = session.Adapter;
            bool reset = generation != adapter.SessionToken || turn != adapter.TurnToken || phase != adapter.ObservedState;
            if (reset || !session.Service.IsConnected || !adapter.CanSubmit) anticipating = false;
            generation = adapter.SessionToken; turn = adapter.TurnToken; phase = adapter.ObservedState;
            bool fresh = stream.Buffer.HasValue && stream.Buffer.Latest.Session == generation && stream.Buffer.Latest.Turn == turn
                && stream.Buffer.Age(Time.realtimeSinceStartupAsDouble) < .3;
            for (int i = 0; i < 2; i++)
            {
                float authoritative = Mathf.Clamp(Mathf.DeltaAngle(0, pivots[i].localEulerAngles.z), 0, 90);
                bool local = i == adapter.LocalPlayerIndex;
                float target = !reset && fresh && (!local || !session.Service.IsHost)
                    ? stream.Buffer.Evaluate(i, Time.realtimeSinceStartupAsDouble, local ? 0 : InterpolationDelay) : authoritative;
                if (local && anticipating)
                {
                    var sample = stream.Buffer.Latest;
                    float angle = i == 0 ? sample.ZeroAngle : sample.OneAngle, power = i == 0 ? sample.ZeroPower : sample.OnePower;
                    if (fresh && Mathf.Abs(angle - predictedAngle) < .05f && Mathf.Abs(power - predictedPower) < .001f) anticipating = false;
                    else if (Time.realtimeSinceStartupAsDouble - predictedAt > .6) anticipating = false;
                    if (anticipating) target = predictedAngle;
                }
                // Reconcile local rejection/expiry over a few frames; turn/generation changes reset immediately.
                if (local && !anticipating && !reset && AnticipationEnabled && !session.Service.IsHost)
                    target = Mathf.Lerp(DisplayAngle(i), target, 1 - Mathf.Exp(-40 * Time.unscaledDeltaTime));
                proxies[i].localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(target, 0, 90));
            }
        }
        private void OnDestroy()
        {
            if (session != null && session.Adapter != null)
            { session.Adapter.AimIntentAccepted -= Anticipate; session.Adapter.CommandProcessed -= OnResult; }
            for (int i = 0; i < originals.Count; i++) if (originals[i] != null) originals[i].enabled = enabledStates[i];
            foreach (var proxy in proxies) if (proxy != null) Destroy(proxy.gameObject);
        }
    }
}
