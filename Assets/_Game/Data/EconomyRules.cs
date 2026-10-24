using UnityEngine;

namespace TankGame.Data
{
    [CreateAssetMenu(menuName="TankGame/Economy Rules")]
    public sealed class EconomyRules : ScriptableObject
    {
        [Min(0)] public int winCoins=80,drawCoins=35,lossCoins=15,performanceCap=30;
        [Range(0,1)] public float coinsPerScore=.2f;
        [Range(1,5)] public int maximumUpgradeLevel=3;
        [Min(1)] public int hullBaseCost=60,armorBaseCost=80,engineBaseCost=60;
        [Range(0,.2f)] public float hullPerLevel=.05f,armorPerLevel=.02f,enginePerLevel=.05f;
        private void OnValidate()
        {
            winCoins=Mathf.Clamp(winCoins,0,10000);drawCoins=Mathf.Clamp(drawCoins,0,10000);lossCoins=Mathf.Clamp(lossCoins,0,10000);performanceCap=Mathf.Clamp(performanceCap,0,10000);
            coinsPerScore=float.IsFinite(coinsPerScore)?Mathf.Clamp01(coinsPerScore):.2f;maximumUpgradeLevel=Mathf.Clamp(maximumUpgradeLevel,1,5);
            hullBaseCost=Mathf.Clamp(hullBaseCost,1,100000);armorBaseCost=Mathf.Clamp(armorBaseCost,1,100000);engineBaseCost=Mathf.Clamp(engineBaseCost,1,100000);
            hullPerLevel=float.IsFinite(hullPerLevel)?Mathf.Clamp(hullPerLevel,0,.2f):.05f;
            armorPerLevel=float.IsFinite(armorPerLevel)?Mathf.Clamp(armorPerLevel,0,.2f):.02f;
            enginePerLevel=float.IsFinite(enginePerLevel)?Mathf.Clamp(enginePerLevel,0,.2f):.05f;
        }
    }
}
