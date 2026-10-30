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
            ApplyVisuals(gameObject,definition,enemy);
        }
        public static void ApplyVisuals(GameObject root,TankDefinition definition, bool enemy)
        {
            if(definition.useSourceArtwork)
            {
                var pivot=root.transform.Find("TankVisual/TurretPivot/BarrelPivot");
                if(pivot!=null)pivot.localPosition=new Vector3(-.1f,-.08f,0);
            }
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>())
            {
                if(definition.useSourceArtwork)
                {
                    switch(renderer.name)
                    {
                        case "Body": Fit(renderer,enemy&&definition.enemyBodySprite!=null?definition.enemyBodySprite:definition.bodySprite,new Vector2(1.7f,.65f)); break;
                        case "Tracks": Fit(renderer,enemy&&definition.enemyTracksSprite!=null?definition.enemyTracksSprite:definition.tracksSprite,new Vector2(1.7f,.38f)); break;
                        case "Barrel":
                            Fit(renderer,enemy&&definition.enemyBarrelSprite!=null?definition.enemyBarrelSprite:definition.barrelSprite,new Vector2(.9f,.13f));
                            var muzzle=renderer.transform.Find("Muzzle");
                            if(muzzle!=null&&renderer.sprite!=null)
                                muzzle.localPosition=new Vector3(renderer.sprite.bounds.max.x,0,0);
                            break;
                        // The source hull includes its turret housing; retain the aiming pivots.
                        case "Turret": renderer.sprite=definition.turretSprite; break;
                        default: continue;
                    }
                    renderer.color=renderer.name=="Body"?(enemy?new Color(.82f,.9f,1):new Color(.64f,.79f,.62f)):
                        renderer.name=="Tracks"?new Color(.57f,.64f,.63f):new Color(.61f,.68f,.67f);
                    if(definition.detailedArtwork)renderer.color=Color.white;
                    continue;
                }
                if(renderer.name=="Tracks")continue;
                var color=enemy?new Color(.82f,.25f,.22f):definition.color;
                if(renderer.name=="Body"&&definition.bodySprite!=null)renderer.sprite=definition.bodySprite;
                if(renderer.name=="Turret"&&definition.turretSprite!=null)renderer.sprite=definition.turretSprite;
                renderer.color=renderer.name=="Turret"?color*new Color(.9f,.9f,.9f,1):color;
            }
        }
        private static void Fit(SpriteRenderer renderer,Sprite sprite,Vector2 size)
        {
            renderer.sprite=sprite;
            if(sprite==null)return;
            var bounds=sprite.bounds.size;
            renderer.transform.localScale=new Vector3(size.x/bounds.x,size.y/bounds.y,1);
        }
    }
}
