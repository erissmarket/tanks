using UnityEngine;

namespace TankGame.Data
{
    [CreateAssetMenu(menuName = "TankGame/Shield Data")]
    public sealed class ShieldData : ScriptableObject
    {
        public string shieldId="light";
        [Min(0)] public int unlockCost,requiredWins;
        public string displayName = "Light";
        [Min(0)] public float capacity = 30f;
        [Range(.25f, 1f)] public float movementMultiplier = 1f;
        private void OnValidate()
        {
            capacity = float.IsFinite(capacity) ? Mathf.Max(0, capacity) : 0;
            movementMultiplier = float.IsFinite(movementMultiplier) ? Mathf.Clamp(movementMultiplier,.25f,1f) : 1f;
        }
    }
}
