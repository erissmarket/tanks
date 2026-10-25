using TankGame.Core;
using TankGame.Tank;
using TankGame.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Presentation
{
    public sealed class BattleInterface : MonoBehaviour
    {
        private MatchFlowController match;
        private IntegratedInput input;
        private WeaponController weapon;
        private HealthComponent player,enemy;
        private Text playerText,enemyText,turnText,weaponText,angleText,fireText;
        private RectTransform playerHp,enemyHp,playerShield,enemyShield;
        private Button[] choices;
        private Text[] ammoLabels;
        private Button fire,left,right;
        public void Build(MatchFlowController flow,IntegratedInput controls,WeaponController weapons,HealthComponent playerHealth,HealthComponent enemyHealth,UnityEngine.Events.UnityAction pause)
        {
            match=flow;input=controls;weapon=weapons;player=playerHealth;enemy=enemyHealth;
            var header=InterfaceLayout.Rect(transform,"Battle Header",new Vector2(.5f,1),new Vector2(0,-24),new Vector2(1824,230));header.pivot=new Vector2(.5f,1);
            var fit=header.gameObject.AddComponent<FitInterfacePanel>();fit.designSize=header.sizeDelta;
            BuildHealth(header,"Player",-640,new Color(.31f,.67f,.32f),out playerText,out playerHp,out playerShield);
            BuildHealth(header,"Enemy",640,new Color(.81f,.28f,.25f),out enemyText,out enemyHp,out enemyShield);
            InterfaceLayout.Panel(header,"Turn Track",new Vector2(0,47),new Vector2(620,114),InterfaceLayout.Ink);
            turnText=InterfaceLayout.Label(header,"Turn","",new Vector2(0,47),new Vector2(620,114),34);
            InterfaceLayout.Button(header,"Pause","PAUSE",new Vector2(0,-60),new Vector2(210,80),pause);
            var footer=InterfaceLayout.Rect(transform,"Battle Controls",new Vector2(.5f,0),new Vector2(0,24),new Vector2(1824,218));footer.pivot=new Vector2(.5f,0);
            var backing=footer.gameObject.AddComponent<Image>();backing.color=new Color(.04f,.075f,.10f,.62f);
            var dockFit=footer.gameObject.AddComponent<FitInterfacePanel>();dockFit.designSize=footer.sizeDelta;
            InterfaceLayout.Label(footer,"Move label","MOVE",new Vector2(-770,61),new Vector2(280,48),32);
            left=Hold(footer,"Move Left","LEFT",new Vector2(-840,-39),0);
            right=Hold(footer,"Move Right","RIGHT",new Vector2(-700,-39),1);
            angleText=InterfaceLayout.Label(footer,"Aim label","AIM",new Vector2(445,61),new Vector2(280,48),32);
            var up=Hold(footer,"Aim Up","UP",new Vector2(380,-39),2);
            var down=Hold(footer,"Aim Down","DOWN",new Vector2(510,-39),3);
            input.BindHoldButtons(new[]{left,right,up,down});
            fire=InterfaceLayout.Button(footer,"Fire","FIRE",new Vector2(750,-9),new Vector2(250,176),input.Fire);fire.GetComponent<Image>().color=new Color(.78f,.27f,.16f);
            fireText=fire.GetComponentInChildren<Text>();fireText.fontSize=45;
            weaponText=InterfaceLayout.Label(footer,"Selected weapon","",new Vector2(-160,62),new Vector2(820,80),29);
            choices=new Button[input.AvailableWeapons.Length];ammoLabels=new Text[choices.Length];
            for(int i=0;i<choices.Length;i++)
            {
                int index=i;var data=input.AvailableWeapons[i];
                choices[i]=InterfaceLayout.Button(footer,"Select weapon "+i,"",new Vector2(-160+(i-3)*116,-39),new Vector2(108,108),()=>{if(input.CanAct)input.SelectWeapon(index);});
                InterfaceLayout.Picture(choices[i].transform,"Icon",data.Icon,new Vector2(0,12),new Vector2(78,68));
                ammoLabels[i]=choices[i].GetComponentInChildren<Text>();ammoLabels[i].rectTransform.anchoredPosition=new Vector2(0,-37);ammoLabels[i].rectTransform.sizeDelta=new Vector2(104,32);ammoLabels[i].fontSize=25;
            }
        }
        private Button Hold(Transform parent,string name,string title,Vector2 position,int command)
        {
            var button=InterfaceLayout.Button(parent,name,title,position,new Vector2(120,120),null);
            button.gameObject.AddComponent<IntegratedHoldButton>().Configure(input,command);return button;
        }
        private static void BuildHealth(Transform parent,string name,float x,Color color,out Text text,out RectTransform hp,out RectTransform shield)
        {
            var panel=InterfaceLayout.Panel(parent,name+" Status",new Vector2(x,39),new Vector2(520,152),InterfaceLayout.Ink);
            text=InterfaceLayout.Label(panel,"Details","",new Vector2(0,27),new Vector2(496,80),30);
            hp=Bar(panel,"Health",-29,14,color);shield=Bar(panel,"Shield",-55,10,new Color(.27f,.64f,.84f));
        }
        private static RectTransform Bar(Transform parent,string name,float y,float height,Color color)
        {
            var back=InterfaceLayout.Panel(parent,name+" Track",new Vector2(0,y),new Vector2(480,height),new Color(.02f,.04f,.05f));
            var fill=InterfaceLayout.Panel(back,name+" Fill",Vector2.zero,new Vector2(480,height),color);fill.anchorMin=fill.anchorMax=new Vector2(0,.5f);fill.pivot=new Vector2(0,.5f);return fill;
        }
        private static void UpdateHealth(HealthComponent health,Text text,RectTransform hp,RectTransform shield,string fallback)
        {
            var loadout=health.GetComponent<TankLoadout>();string title=loadout!=null&&loadout.Definition!=null?loadout.Definition.displayName.ToUpperInvariant():fallback;
            text.text=$"{fallback} / {title}\nHP {Mathf.CeilToInt(health.CurrentHealth)}/{health.MaxHealth:0}   SH {health.CurrentShield:0}/{health.MaxShield:0}   ARM {health.ArmorReduction*100:0}%";
            hp.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,480*Mathf.Clamp01(health.CurrentHealth/health.MaxHealth));
            shield.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,health.MaxShield>0?480*Mathf.Clamp01(health.CurrentShield/health.MaxShield):0);
        }
        private void LateUpdate()
        {
            if(match==null||choices==null)return;
            UpdateHealth(player,playerText,playerHp,playerShield,"PLAYER");UpdateHealth(enemy,enemyText,enemyHp,enemyShield,"ENEMY");
            string phase=match.State==MatchState.ProjectileFlying?"SHOT IN FLIGHT":match.State==MatchState.Resolving?"RESOLVING":input.CanAct?"YOUR TURN":"ENEMY TURN";
            turnText.text=$"{phase}   /   TURN {match.CompletedTurns+1}/{match.Rules.maximumTurns}\nTIME {Mathf.CeilToInt(match.RemainingTurnSeconds)}s   MOVE {match.RemainingMovement:0.00}";
            angleText.text=$"AIM {input.AimAngle:0} DEG";
            fire.interactable=input.CanFire;fireText.text=input.CanAct?(weapon.CanFire?"FIRE":"EMPTY"):"WAIT";
            left.interactable=right.interactable=input.CanAct&&match.RemainingMovement>.001f;
            var current=weapon.CurrentWeapon;
            if(current!=null)
            {
                int count=weapon.RemainingAmmo(current);string ammo=count<0?"INF":count.ToString();
                weaponText.text=$"{current.WeaponName.ToUpperInvariant()}\nDMG {WeaponPresentation.Damage(current)}   BLAST {WeaponPresentation.Radius(current):0.0}   AMMO {ammo}";
            }
            for(int i=0;i<choices.Length;i++)
            {
                var data=input.AvailableWeapons[i];int count=weapon.RemainingAmmo(data);
                choices[i].interactable=input.CanAct;choices[i].GetComponent<Image>().color=data==current?InterfaceLayout.Accent:InterfaceLayout.Card;
                ammoLabels[i].text=$"{i+1} / {(count<0?"INF":count.ToString())}";
            }
        }
    }
}
