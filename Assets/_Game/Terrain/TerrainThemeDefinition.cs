using UnityEngine;

namespace TankGame.Terrain
{
    [CreateAssetMenu(menuName = "TankGame/Terrain Theme")]
    public sealed class TerrainThemeDefinition : ScriptableObject
    {
        public string displayName = "Grass";
        public Sprite soil;
        public Sprite surface;
        public Color skyTint = Color.white;
        public Color farMountainTint = Color.white;
        public Color nearMountainTint = Color.white;
        public Color accentColor = new Color(0.35f, 0.7f, 0.25f);
    }
}
