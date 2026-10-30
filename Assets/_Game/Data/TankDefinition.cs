using UnityEngine;

namespace TankGame.Data
{
    [CreateAssetMenu(menuName = "TankGame/Tank Definition")]
    public sealed class TankDefinition : ScriptableObject
    {
        public string tankId = "vanguard";
        public string displayName = "Vanguard";
        [Min(0)] public int unlockCost,requiredWins;
        [TextArea] public string description;
        [Min(1)] public float maxHealth = 100;
        [Range(0,.6f)] public float armorReduction;
        [Min(.1f)] public float speed = 3;
        [Range(.25f,2)] public float movementMultiplier = 1;
        [Min(0)] public float shieldCapacity = 60;
        public Color color = new Color(.32f,.64f,.25f);
        public Sprite bodySprite, turretSprite, thumbnail;
        public bool useSourceArtwork;
        public Sprite enemyBodySprite, barrelSprite, tracksSprite;
        public bool detailedArtwork;
        public Sprite enemyBarrelSprite, enemyTracksSprite;

        public bool Supports(ShieldData shield) => shield == null || shield.capacity <= shieldCapacity;
        public void Validate()
        {
            maxHealth = float.IsFinite(maxHealth) ? Mathf.Max(1,maxHealth) : 100;
            armorReduction = float.IsFinite(armorReduction) ? Mathf.Clamp(armorReduction,0,.6f) : 0;
            speed = float.IsFinite(speed) ? Mathf.Max(.1f,speed) : 3;
            movementMultiplier = float.IsFinite(movementMultiplier) ? Mathf.Clamp(movementMultiplier,.25f,2) : 1;
            shieldCapacity = float.IsFinite(shieldCapacity) ? Mathf.Max(0,shieldCapacity) : 0;
        }
        private void OnValidate()=>Validate();
    }
}
