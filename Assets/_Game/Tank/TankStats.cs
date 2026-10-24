using TankGame.Core;
using TankGame.Data;
using UnityEngine;

namespace TankGame.Tank
{
    public readonly struct TankStats
    {
        public readonly float Health,Armor,Speed,MovementMultiplier;
        public TankStats(TankDefinition tank,TankUpgrades upgrades=null,EconomyRules rules=null)
        {
            int maximum=rules!=null?rules.maximumUpgradeLevel:0;
            int hull=Mathf.Clamp(upgrades!=null?upgrades.hull:0,0,maximum);
            int armor=Mathf.Clamp(upgrades!=null?upgrades.armor:0,0,maximum);
            int engine=Mathf.Clamp(upgrades!=null?upgrades.engine:0,0,maximum);
            Health=tank.maxHealth*(1+hull*(rules!=null?rules.hullPerLevel:0));
            Armor=Mathf.Clamp(tank.armorReduction+armor*(rules!=null?rules.armorPerLevel:0),0,.6f);
            float engineFactor=1+engine*(rules!=null?rules.enginePerLevel:0);
            Speed=tank.speed*engineFactor;MovementMultiplier=tank.movementMultiplier*engineFactor;
        }
    }
}
