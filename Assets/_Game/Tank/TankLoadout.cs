using System;
using TankGame.Core;
using TankGame.Data;
using UnityEngine;

namespace TankGame.Tank
{
    [RequireComponent(typeof(HealthComponent),typeof(TankMotor))]
    public sealed class TankLoadout : MonoBehaviour
    {
        public TankDefinition Definition { get; private set; }
        private float appliedMovementMultiplier=1;
        public float MovementMultiplier => appliedMovementMultiplier;
        public void Apply(TankDefinition definition, ShieldData shield, bool enemy,TankUpgrades upgrades=null,EconomyRules rules=null)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            definition.Validate();
            if(!definition.Supports(shield))throw new InvalidOperationException("This shield exceeds the tank's equipment capacity.");
            Definition=definition;
            var stats=new TankStats(definition,upgrades,rules);appliedMovementMultiplier=stats.MovementMultiplier;
            GetComponent<HealthComponent>().ConfigureTank(stats.Health,stats.Armor);
            GetComponent<HealthComponent>().EquipShield(shield);
            GetComponent<TankMotor>().ConfigureSpeed(stats.Speed);
            foreach(var renderer in GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.name=="Tracks")continue;
                var color=enemy?new Color(.82f,.25f,.22f):definition.color;
                if(renderer.name=="Body"&&definition.bodySprite!=null)renderer.sprite=definition.bodySprite;
                if(renderer.name=="Turret"&&definition.turretSprite!=null)renderer.sprite=definition.turretSprite;
                renderer.color=renderer.name=="Turret"?color*new Color(.9f,.9f,.9f,1):color;
            }
        }
    }
}
