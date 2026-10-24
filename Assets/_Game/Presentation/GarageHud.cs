using TankGame.Core;
using TankGame.Data;
using TankGame.Testing;
using UnityEngine;
using UnityEngine.UI;
using TankGame.Tank;

namespace TankGame.Presentation
{
    public sealed class GarageHud : MonoBehaviour
    {
        [SerializeField] private BackendTestBootstrap bootstrap;
        [SerializeField] private MatchFlowController match;
        private GameObject overlay;
        private Button[] tanks,shields;
        private Text[] tankDetails;
        private Text subtitle,startText;
        private Button start,close;
        private Button[] unlocks,upgradeButtons;
        private Text soundLabel;
        private Font font;
        private bool pausedBeforeOpen;
        [SerializeField] private bool externalNavigation;
        public event System.Action Closed,PreparationRequested;
        public bool IsOpen => overlay!=null&&overlay.activeSelf;
        private void Start()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var open=Button(transform,"Garage","GARAGE",new Vector2(0,1),new Vector2(122,-174),new Vector2(168,64));
            open.onClick.AddListener(Open);
            open.gameObject.SetActive(!externalNavigation);
            var cover=Panel(transform,"Garage Overlay",Vector2.zero,Vector2.zero,Vector2.zero,new Color(.02f,.04f,.06f,.94f));
            cover.anchorMin=Vector2.zero;cover.anchorMax=Vector2.one;cover.offsetMin=cover.offsetMax=Vector2.zero;overlay=cover.gameObject;
            var panel=Panel(cover,"Garage",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1680,980),new Color(.06f,.10f,.14f,1));
            panel.gameObject.AddComponent<FitInterfacePanel>().designSize=new Vector2(1680,980);
            Label(panel,"Title","GARAGE / CHOOSE YOUR TANK",new Vector2(-100,430),new Vector2(1150,64),40);
            subtitle=Label(panel,"Subtitle","",new Vector2(0,365),new Vector2(1550,68),26);
            var sound=Button(panel,"Sound","SOUND ON",new Vector2(.5f,.5f),new Vector2(685,432),new Vector2(220,48));
            sound.onClick.AddListener(()=>{if(bootstrap.Progression!=null)bootstrap.Progression.ToggleSound();});soundLabel=sound.GetComponentInChildren<Text>();
            tanks=new Button[bootstrap.TankOptions.Length];tankDetails=new Text[tanks.Length];
            unlocks=new Button[tanks.Length];
            for(int i=0;i<tanks.Length;i++)
            {
                int index=i;var tank=bootstrap.TankOptions[i];
                tanks[i]=Button(panel,"Tank "+tank.tankId,"",new Vector2(.5f,.5f),new Vector2((i-1)*520,100),new Vector2(480,400));
                tanks[i].onClick.AddListener(()=>bootstrap.SelectNextTank(index));
                Label(tanks[i].transform,"Name",tank.displayName.ToUpperInvariant(),new Vector2(0,155),new Vector2(450,48),34);
                var picture=new GameObject("Thumbnail",typeof(RectTransform),typeof(Image));picture.transform.SetParent(tanks[i].transform,false);
                Position((RectTransform)picture.transform,new Vector2(.5f,.5f),new Vector2(0,72),new Vector2(350,125));
                picture.GetComponent<Image>().sprite=tank.thumbnail;picture.GetComponent<Image>().preserveAspect=true;picture.GetComponent<Image>().raycastTarget=false;
                tankDetails[i]=Label(tanks[i].transform,"Stats","",new Vector2(0,-68),new Vector2(450,150),26);
                unlocks[i]=Button(tanks[i].transform,"Unlock","",new Vector2(.5f,.5f),new Vector2(0,-168),new Vector2(440,50));
                unlocks[i].onClick.AddListener(()=>
                {
                    var progress=bootstrap.Progression;
                    if(progress==null||progress.Store.OwnsTank(tank)){bootstrap.SelectNextTank(index);return;}
                    if(progress.Store.Unlock(tank))bootstrap.SelectNextTank(index);
                });
            }
            Label(panel,"Shield title","EQUIPMENT / SHIELD",new Vector2(0,-135),new Vector2(1500,44),27);
            shields=new Button[bootstrap.ShieldOptions.Length];
            for(int i=0;i<shields.Length;i++)
            {
                int index=i;var shield=bootstrap.ShieldOptions[i];
                shields[i]=Button(panel,"Shield "+shield.displayName,$"{shield.displayName.ToUpperInvariant()} / {shield.capacity:0}",new Vector2(.5f,.5f),new Vector2((i-1)*445,-193),new Vector2(420,64));
                shields[i].onClick.AddListener(()=>
                {
                    var progress=bootstrap.Progression;
                    if(progress!=null&&!progress.Store.OwnsShield(shield)&&!progress.Store.Unlock(shield))return;
                    bootstrap.SelectNextShield(index);
                });
            }
            Label(panel,"Upgrade title","UPGRADES / SELECTED TANK",new Vector2(0,-254),new Vector2(1500,44),27);
            upgradeButtons=new Button[3];
            for(int i=0;i<3;i++)
            {
                var kind=(UpgradeKind)i;
                upgradeButtons[i]=Button(panel,"Upgrade "+kind,"",new Vector2(.5f,.5f),new Vector2((i-1)*445,-312),new Vector2(420,64));
                upgradeButtons[i].onClick.AddListener(()=>{var progress=bootstrap.Progression;if(progress!=null)progress.Store.Upgrade(bootstrap.NextTank,kind);});
            }
            close=Button(panel,"Close","BACK TO MATCH",new Vector2(.5f,.5f),new Vector2(-320,-417),new Vector2(540,86));close.onClick.AddListener(Close);
            start=Button(panel,"Start","START BATTLE",new Vector2(.5f,.5f),new Vector2(320,-417),new Vector2(540,86));start.onClick.AddListener(Begin);
            startText=start.GetComponentInChildren<Text>();
            overlay.SetActive(false);
            if(bootstrap.StartInGarage&&!externalNavigation)Open();
        }
        public void Open()
        {
            if(IsOpen)return;
            pausedBeforeOpen=match.IsPaused;
            if(match.IsRunning)match.SetPaused(true);
            overlay.SetActive(true);transform.SetAsLastSibling();
        }
        public void Close()
        {
            if(externalNavigation){Hide();Closed?.Invoke();return;}
            if(!match.IsRunning&&!bootstrap.CanRestart)return;
            overlay.SetActive(false);
            if(match.IsRunning&&!pausedBeforeOpen)match.SetPaused(false);
        }
        public void Begin()
        {
            if(!bootstrap.CanStartConfiguredMatch)return;
            if(externalNavigation){Hide();PreparationRequested?.Invoke();return;}
            bootstrap.StartConfiguredMatch();if(match.IsRunning)overlay.SetActive(false);
        }
        public void Hide(){if(overlay!=null)overlay.SetActive(false);}
        private void LateUpdate()
        {
            if(!IsOpen)return;
            var selected=bootstrap.NextTank;var shield=bootstrap.NextShield;
            var progress=bootstrap.Progression;var store=progress!=null?progress.Store:null;
            subtitle.text=match.IsRunning?"Changes apply NEXT MATCH. Current battle is paused.":"Compare tanks, equip a shield, then start your battle.";
            if(store!=null)subtitle.text=$"COINS {store.Coins}   WINS {store.Wins}   TOTAL SCORE {store.TotalScore}\n"+(string.IsNullOrEmpty(store.Status)?subtitle.text:store.Status);
            soundLabel.text=store==null||store.SoundEnabled?"SOUND ON":"SOUND OFF";
            for(int i=0;i<tanks.Length;i++)
            {
                var tank=bootstrap.TankOptions[i];
                bool owned=store==null||store.OwnsTank(tank);
                tanks[i].GetComponent<Image>().color=tank==selected?new Color(.19f,.37f,.28f):new Color(.12f,.18f,.24f);
                var levels=store!=null?store.Upgrades(tank.tankId):null;var stats=new TankStats(tank,levels,progress!=null?progress.Rules:null);
                float movement=match.Rules.movementPerTurn*stats.MovementMultiplier*(shield!=null&&tank.Supports(shield)?shield.movementMultiplier:1);
                tankDetails[i].text=$"HP {stats.Health:0}   ARMOR {stats.Armor*100:0}%\nSPEED {stats.Speed:0.0}   MOVE {movement:0.00}\nSHIELD CAP {tank.shieldCapacity:0}\n{tank.description}";
                unlocks[i].interactable=owned||store.CanUnlock(tank);
                unlocks[i].GetComponentInChildren<Text>().text=owned?(tank==selected?"EQUIPPED":"SELECT"):$"UNLOCK {tank.unlockCost} COINS / {tank.requiredWins} WINS";
            }
            for(int i=0;i<shields.Length;i++)
            {
                var option=bootstrap.ShieldOptions[i];bool compatible=selected.Supports(option);bool owned=store==null||store.OwnsShield(option);shields[i].interactable=compatible&&(owned||store.CanUnlock(option));
                shields[i].GetComponent<Image>().color=option==shield?new Color(.19f,.42f,.34f):new Color(.16f,.23f,.31f);
                shields[i].GetComponentInChildren<Text>().text=!compatible?$"{option.displayName.ToUpperInvariant()} / TOO HEAVY":owned?$"{option.displayName.ToUpperInvariant()} / {option.capacity:0}":$"UNLOCK {option.unlockCost} C / {option.requiredWins} WINS";
            }
            for(int i=0;i<3;i++)
            {
                var kind=(UpgradeKind)i;int level=store!=null?store.Upgrades(selected.tankId).Level(kind):0;int maximum=progress!=null?progress.Rules.maximumUpgradeLevel:3;
                upgradeButtons[i].interactable=store!=null&&store.CanUpgrade(selected,kind);
                upgradeButtons[i].GetComponentInChildren<Text>().text=$"{kind.ToString().ToUpperInvariant()} {level}/{maximum}\n"+(level>=maximum?"MAX LEVEL":store!=null?$"{store.UpgradeCost(selected.tankId,kind)} COINS":"UNAVAILABLE");
            }
            start.interactable=bootstrap.CanStartConfiguredMatch;startText.text=match.IsRunning?"AVAILABLE AFTER MATCH":externalNavigation?"BATTLE SETUP":"START BATTLE";
            close.interactable=externalNavigation||match.IsRunning||bootstrap.CanRestart;
        }
        private RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;
            Position(rect,anchor,offset,size);go.GetComponent<Image>().color=color;return rect;
        }
        private static void Position(RectTransform rect,Vector2 anchor,Vector2 offset,Vector2 size)
        {rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=offset;rect.sizeDelta=size;}
        private Text Label(Transform parent,string name,string content,Vector2 offset,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Position((RectTransform)go.transform,new Vector2(.5f,.5f),offset,size);
            var label=go.GetComponent<Text>();label.font=font;label.text=content;label.fontSize=fontSize;label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;
        }
        private Button Button(Transform parent,string name,string text,Vector2 anchor,Vector2 offset,Vector2 size)
        {
            var rect=Panel(parent,name,anchor,offset,size,new Color(.16f,.23f,.31f));var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
            button.onClick.AddListener(InterfaceLayout.NotifyClick);
            Label(rect,"Label",text,Vector2.zero,size,27);return button;
        }
        private void OnDisable(){if(match!=null&&match.IsRunning&&IsOpen&&!pausedBeforeOpen)match.SetPaused(false);}
    }
}
