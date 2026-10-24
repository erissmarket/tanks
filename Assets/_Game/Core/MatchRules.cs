using UnityEngine;
namespace TankGame.Core
{
    [CreateAssetMenu(menuName = "TankGame/Match Rules")]
    public sealed class MatchRules : ScriptableObject
    {
        [Min(.1f)] public float turnSeconds = 30f;
        [Min(0f)] public float movementPerTurn = 3f;
        [Min(2)] public int maximumTurns = 40;
        [Min(.1f)] public float shotTimeoutSeconds = 15f;
        [Min(0f)] public float minimumSettleSeconds = .3f;
        [Min(.1f)] public float maximumSettleSeconds = 1.5f;
        public float minimumX = -8f;
        public float maximumX = 8f;
        public float killY = -8f;
        [Min(0)] public int winScore = 100;
        [Min(0)] public int drawScore = 25;
        [Min(0)] public int survivalScore = 50;
        public void Validate()
        {
            turnSeconds = Positive(turnSeconds, 30f, .1f);
            movementPerTurn = Positive(movementPerTurn, 3f, 0f);
            maximumTurns = Mathf.Max(2, maximumTurns);
            shotTimeoutSeconds = Positive(shotTimeoutSeconds, 15f, .1f);
            minimumSettleSeconds = Positive(minimumSettleSeconds, .3f, 0f);
            maximumSettleSeconds = Mathf.Max(minimumSettleSeconds, Positive(maximumSettleSeconds, 1.5f, .1f));
            if (!float.IsFinite(minimumX)) minimumX = -8f;
            if (!float.IsFinite(maximumX) || maximumX <= minimumX) maximumX = minimumX + 16f;
            if (!float.IsFinite(killY)) killY = -8f;
            winScore = Mathf.Max(0, winScore); drawScore = Mathf.Max(0, drawScore); survivalScore = Mathf.Max(0, survivalScore);
        }
        private static float Positive(float value, float fallback, float min) => Mathf.Max(min, float.IsFinite(value) ? value : fallback);
        private void OnValidate() => Validate();
    }
    public enum MatchEndReason { None, Elimination, Fall, TurnLimit, Surrender, Cancelled }
}
