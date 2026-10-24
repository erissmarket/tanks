using TankGame.Core;
using TankGame.Data;
using TankGame.Testing;
using TankGame.Weapons;
using TankGame.Projectiles;
using TankGame.Tank;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Presentation
{
    public sealed class PhaseTwoHud : MonoBehaviour
    {
        [SerializeField] private IntegratedInput input;
        [SerializeField] private WeaponController playerWeapon;
        [SerializeField] private HealthComponent playerHealth, enemyHealth;
        [SerializeField] private MatchFlowController match;
        [SerializeField] private BackendTestBootstrap bootstrap;
        private Text stats, playerShield, enemyShield;
        private Button[] weapons;
        private Text[] labels;
        private Text pauseShield, resultShield;
        private Font font;
        private Image weaponIcon;

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playerShield = Label(transform,"Player Shield",new Vector2(0,1),new Vector2(230,-111),new Vector2(440,48),25);
            enemyShield = Label(transform,"Enemy Shield",new Vector2(1,1),new Vector2(-230,-111),new Vector2(440,48),25);
            foreach(var label in new[]{playerShield,enemyShield})
            {
                label.color=new Color(.10f,.18f,.25f);
                label.fontStyle=FontStyle.Bold;
            }
            var card = new GameObject("Weapon Loadout",typeof(RectTransform),typeof(Image));
            card.transform.SetParent(transform,false);
            Position((RectTransform)card.transform,new Vector2(.5f,1),new Vector2(0,-253),new Vector2(1400,220));
            card.GetComponent<Image>().color = new Color(.06f,.10f,.14f,.94f);
            stats = Label(card.transform,"Stats",new Vector2(.5f,1),new Vector2(35,-36),new Vector2(1170,65),27);
            var iconObject=new GameObject("Selected Icon",typeof(RectTransform),typeof(Image));iconObject.transform.SetParent(card.transform,false);
            Position((RectTransform)iconObject.transform,new Vector2(0,1),new Vector2(52,-39),new Vector2(65,65));
            weaponIcon=iconObject.GetComponent<Image>();weaponIcon.preserveAspect=true;weaponIcon.raycastTarget=false;
            weapons = new Button[input.AvailableWeapons.Length]; labels = new Text[weapons.Length];
            for(int i=0;i<weapons.Length;i++)
            {
                int selected=i;
                weapons[i]=Button(card.transform,"Weapon "+(i+1),new Vector2(.5f,.5f),new Vector2((i-3)*193,-27),new Vector2(181,96));
                labels[i]=weapons[i].GetComponentInChildren<Text>();
                weapons[i].onClick.AddListener(()=>{if(input.CanAct)input.SelectWeapon(selected);});
            }
            var phaseOne=transform.parent.Find("Phase One HUD");
            if(phaseOne!=null)
            {
                pauseShield=ShieldChoice(phaseOne.Find("Pause Panel"),new Vector2(0,15));
                resultShield=ShieldChoice(phaseOne.Find("Result Panel"),new Vector2(0,-246));
            }
        }
        private Text ShieldChoice(Transform parent,Vector2 offset)
        {
            var button=Button(parent,"Next Match Shield",new Vector2(.5f,.5f),offset,new Vector2(760,62));
            button.onClick.AddListener(bootstrap.CycleNextShield);
            return button.GetComponentInChildren<Text>();
        }
        private void LateUpdate()
        {
            if(stats==null)return;
            playerShield.text=Defense(playerHealth);
            enemyShield.text=Defense(enemyHealth);
            var current=playerWeapon.CurrentWeapon;
            if(current!=null)
            {
                weaponIcon.sprite=current.Icon;weaponIcon.enabled=current.Icon!=null;
                var cluster=current.ProjectilePrefab!=null?current.ProjectilePrefab.GetComponent<ClusterImpactHandler>():null;
                string damage=cluster!=null?$"{cluster.ClusterCount} x {cluster.SubmunitionDamage:0}":current.BaseDamage.ToString("0");
                float radius=cluster!=null?cluster.SubmunitionExplosionRadius:current.ExplosionRadius;
                string ammo=Ammo(current);
                stats.text=$"{current.WeaponName.ToUpperInvariant()}   |   DMG {damage}   RADIUS {radius:0.0}   AMMO {ammo}\n"+
                    (playerWeapon.CanFire ? "Choose a weapon  |  A/D Move  W/S Aim  SPACE Fire" : "OUT OF AMMO - choose another weapon");
            }
            string[] shortNames={"ROCKET","MORTAR","RAILGUN","FLOATER","BOUNCY","CLUSTER","DIGGER"};
            for(int i=0;i<weapons.Length;i++)
            {
                var weapon=input.AvailableWeapons[i];
                labels[i].text=$"{i+1}  {(i<shortNames.Length?shortNames[i]:weapon.WeaponName)}\n{Ammo(weapon)}";
                weapons[i].interactable=input.CanAct;
                weapons[i].GetComponent<Image>().color=weapon==current?new Color(.22f,.48f,.30f):new Color(.15f,.23f,.31f);
            }
            var next=bootstrap.NextShield;
            string choice=next!=null?$"NEXT MATCH: {next.displayName.ToUpperInvariant()} SHIELD {next.capacity:0}  |  CLICK TO CHANGE":"NEXT MATCH: NO SHIELD";
            if(pauseShield!=null)pauseShield.text=choice;
            if(resultShield!=null)resultShield.text=choice;
        }
        private string Ammo(WeaponData weapon)=>playerWeapon.RemainingAmmo(weapon)<0?"INF":playerWeapon.RemainingAmmo(weapon).ToString();
        private static string Defense(HealthComponent health)
        {
            var loadout=health.GetComponent<TankLoadout>();
            string name=loadout!=null&&loadout.Definition!=null?loadout.Definition.displayName.ToUpperInvariant()+" ":"";
            return $"{name}SH {health.CurrentShield:0}/{health.MaxShield:0} AR {health.ArmorReduction*100:0}%";
        }
        private static void Position(RectTransform rect,Vector2 anchor,Vector2 offset,Vector2 size)
        {rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=offset;rect.sizeDelta=size;}
        private Text Label(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            Position((RectTransform)go.transform,anchor,offset,size);
            var text=go.GetComponent<Text>();text.font=font;text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;return text;
        }
        private Button Button(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            Position((RectTransform)go.transform,anchor,offset,size);go.GetComponent<Image>().color=new Color(.15f,.23f,.31f);
            Label(go.transform,"Label",new Vector2(.5f,.5f),Vector2.zero,size,25);
            var button=go.GetComponent<Button>();button.targetGraphic=go.GetComponent<Image>();return button;
        }
    }
}
