using System;
using System.Collections.Generic;
using TankGame.Core;
using TankGame.Data;
using TankGame.Tank;
using TankGame.Testing;
using TankGame.Terrain;
using TankGame.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TankGame.Presentation
{
    public enum InterfaceScreen { Home, Preparation, Garage, Settings, Help, Battle, Pause, ConfirmLeave, Result, Rotate }

    [DefaultExecutionOrder(200)]
    public sealed class GameInterface : MonoBehaviour
    {
        [SerializeField] private BackendTestBootstrap bootstrap;
        [SerializeField] private MatchFlowController match;
        [SerializeField] private IntegratedInput input;
        [SerializeField] private GarageHud garage;
        [SerializeField] private TerrainThemeController themes;
        [SerializeField] private HealthComponent playerHealth,enemyHealth;
        [SerializeField] private WeaponController weapon;
        private readonly Dictionary<InterfaceScreen,GameObject> pages=new Dictionary<InterfaceScreen,GameObject>();
        private GameObject battle;
        private Text wallet,homeLoadout,setupLoadout,setupHint,settingsText,resultTitle,resultText,pauseText;
        private Text soundText;
        private Image homeTank,setupTank;
        private Button[] themeButtons,startingWeapons;
        private int startingWeaponIndex;
        private InterfaceScreen secondaryReturn,garageReturn,rotationReturn;
        private bool leaving,built;
        public InterfaceScreen CurrentScreen { get; private set; }
        public int StartingWeaponIndex=>startingWeaponIndex;
        public bool IsBuilt=>built;
        private ProgressionController Progress=>bootstrap.Progression;

        private RectTransform Page(InterfaceScreen screen,string title,Vector2 size)
        {
            var panel=InterfaceLayout.Page(transform,screen.ToString(),size);pages[screen]=panel.parent.gameObject;
            InterfaceLayout.Label(panel,"Title",title,new Vector2(0,size.y*.5f-70),new Vector2(size.x-100,80),50);return panel;
        }
        private void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
            Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;Screen.orientation=ScreenOrientation.AutoRotation;
#endif
            BuildHome();BuildPreparation();BuildSettings();BuildHelp();BuildPause();BuildResult();BuildConfirmation();BuildRotation();
            var battleRect=InterfaceLayout.Rect(transform,"Battle Interface",new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);InterfaceLayout.Stretch(battleRect);battle=battleRect.gameObject;
            battle.AddComponent<BattleInterface>().Build(match,input,weapon,playerHealth,enemyHealth,Pause);
            if(Progress!=null){string saved=Progress.Store.Snapshot.startingWeapon;int selected=Array.FindIndex(input.AvailableWeapons,value=>value!=null&&value.WeaponId==saved);startingWeaponIndex=selected>=0?selected:0;}
            garage.Closed+=ReturnFromGarage;garage.PreparationRequested+=ShowPreparation;
            built=true;match.MatchEnded-=OnMatchEnded;match.MatchEnded+=OnMatchEnded;SetScreen(InterfaceScreen.Home);
        }
        private void BuildHome()
        {
            var panel=Page(InterfaceScreen.Home,"TANK BATTLE",new Vector2(1540,960));
            InterfaceLayout.Label(panel,"Tagline","TACTICAL ARTILLERY  /  EVERY SHOT SHAPES THE BATTLE",new Vector2(0,320),new Vector2(1360,70),27).color=new Color(.62f,.72f,.76f);
            var card=InterfaceLayout.Panel(panel,"Selected Loadout",new Vector2(-390,20),new Vector2(620,470),InterfaceLayout.Card);
            homeTank=InterfaceLayout.Picture(card,"Tank",null,new Vector2(0,88),new Vector2(460,195));
            homeLoadout=InterfaceLayout.Label(card,"Loadout","",new Vector2(0,-102),new Vector2(560,155),34);
            InterfaceLayout.Button(panel,"Play","PLAY",new Vector2(350,145),new Vector2(520,118),ShowPreparation).GetComponent<Image>().color=InterfaceLayout.Accent;
            InterfaceLayout.Button(panel,"Garage","GARAGE",new Vector2(350,0),new Vector2(520,106),ShowGarage);
            InterfaceLayout.Button(panel,"Settings","SETTINGS",new Vector2(350,-135),new Vector2(520,106),ShowSettings);
            InterfaceLayout.Button(panel,"Help","HOW TO PLAY",new Vector2(350,-270),new Vector2(520,106),ShowHelp);
            wallet=InterfaceLayout.Label(panel,"Profile","",new Vector2(-390,-335),new Vector2(620,122),29);
        }
        private void BuildPreparation()
        {
            var panel=Page(InterfaceScreen.Preparation,"BATTLE SETUP",new Vector2(1680,980));
            var card=InterfaceLayout.Panel(panel,"Tank and Shield",new Vector2(-555,240),new Vector2(430,260),InterfaceLayout.Card);
            setupTank=InterfaceLayout.Picture(card,"Tank",null,new Vector2(0,36),new Vector2(300,130));
            setupLoadout=InterfaceLayout.Label(card,"Equipment","",new Vector2(0,-73),new Vector2(406,91),29);
            InterfaceLayout.Button(panel,"Change equipment","TANK / EQUIPMENT",new Vector2(-555,65),new Vector2(430,86),ShowGarage);
            var environment=InterfaceLayout.Panel(panel,"Environment",new Vector2(240,275),new Vector2(1000,200),InterfaceLayout.Card);
            InterfaceLayout.Label(environment,"Title","ENVIRONMENT",new Vector2(0,65),new Vector2(900,44),32);
            themeButtons=new Button[themes.Themes.Length];
            string[] names={"GRASS","SNOW","ROCK"};
            for(int i=0;i<themeButtons.Length;i++)
            {int index=i;themeButtons[i]=InterfaceLayout.Button(environment,"Theme "+i,i<names.Length?names[i]:themes.Themes[i].displayName,new Vector2((i-1)*320,0),new Vector2(290,88),()=>SelectTheme(index));}
            InterfaceLayout.Label(environment,"Hint","Saved for your next battle",new Vector2(0,-73),new Vector2(900,42),26);
            InterfaceLayout.Label(panel,"Weapons title","STARTING WEAPON",new Vector2(240,100),new Vector2(1000,50),32);
            startingWeapons=new Button[input.AvailableWeapons.Length];
            for(int i=0;i<startingWeapons.Length;i++)
            {
                int index=i;var data=input.AvailableWeapons[i];
                var choice=InterfaceLayout.Button(panel,"Starting weapon "+i,"",new Vector2(-585+(i%4)*390,i<4?-66:-250),new Vector2(354,166),()=>SelectStartingWeapon(index));
                InterfaceLayout.Picture(choice.transform,"Icon",data.Icon,new Vector2(-129,45),new Vector2(74,64));
                InterfaceLayout.Label(choice.transform,"Name",data.WeaponName.ToUpperInvariant(),new Vector2(43,48),new Vector2(244,52),25);
                InterfaceLayout.Label(choice.transform,"Stats",$"DMG {WeaponPresentation.Damage(data)}   R {WeaponPresentation.Radius(data):0.0}   AMMO {(data.Ammunition<0?"INF":data.Ammunition.ToString())}",new Vector2(0,-7),new Vector2(334,42),24);
                InterfaceLayout.Label(choice.transform,"Description",data.Description,new Vector2(0,-54),new Vector2(334,54),24);startingWeapons[i]=choice;
            }
            InterfaceLayout.Label(panel,"Ammo note","All seven weapons are available.\nSpecial ammunition resets\neach new match.",new Vector2(585,-250),new Vector2(354,166),28);
            InterfaceLayout.Button(panel,"Back","BACK",new Vector2(-555,-425),new Vector2(430,94),Back);
            InterfaceLayout.Button(panel,"Start battle","START BATTLE",new Vector2(555,-425),new Vector2(430,94),StartBattle).GetComponent<Image>().color=InterfaceLayout.Accent;
            setupHint=InterfaceLayout.Label(panel,"Start status","",new Vector2(0,-425),new Vector2(620,90),26);
        }
        private void BuildSettings()
        {
            var panel=Page(InterfaceScreen.Settings,"SETTINGS",new Vector2(1200,800));
            var sound=InterfaceLayout.Button(panel,"Sound","SOUND ON",new Vector2(0,140),new Vector2(720,110),()=>{if(Progress!=null)Progress.ToggleSound();});soundText=sound.GetComponentInChildren<Text>();
            settingsText=InterfaceLayout.Label(panel,"Save status","",new Vector2(0,-45),new Vector2(1040,200),32);
            InterfaceLayout.Button(panel,"Back","BACK",new Vector2(0,-280),new Vector2(520,106),Back);
        }
        private void BuildHelp()
        {
            var panel=Page(InterfaceScreen.Help,"HOW TO PLAY",new Vector2(1540,930));
            InterfaceLayout.Label(panel,"Rules","ONE SHOT PER TURN / 30 SECONDS\nMove along the ground, aim from 0 to 90 degrees, then fire.\nCluster fragments and drilling finish before the next turn.\n\nSHIELD > ARMOR > HEALTH\nWin by eliminating the enemy. Falling out of the map is a loss.\nAt 40 turns, the higher remaining health percentage wins.\n\nTOUCH: hold LEFT / RIGHT and UP / DOWN, tap a weapon, then FIRE.\nKEYBOARD: A/D move, W/S aim, 1-7 select, SPACE fire, ESC pause.\nGarage changes and upgrades apply to the next match.",new Vector2(0,0),new Vector2(1400,660),32);
            InterfaceLayout.Button(panel,"Back","BACK",new Vector2(0,-355),new Vector2(520,100),Back);
        }
        private void BuildPause()
        {
            var panel=Page(InterfaceScreen.Pause,"PAUSED",new Vector2(1000,940));
            pauseText=InterfaceLayout.Label(panel,"Remaining time","",new Vector2(0,300),new Vector2(880,60),31);
            InterfaceLayout.Button(panel,"Resume","RESUME",new Vector2(0,180),new Vector2(650,100),Resume).GetComponent<Image>().color=InterfaceLayout.Accent;
            InterfaceLayout.Button(panel,"Garage","GARAGE / NEXT MATCH",new Vector2(0,50),new Vector2(650,100),ShowGarage);
            InterfaceLayout.Button(panel,"Settings","SETTINGS",new Vector2(0,-80),new Vector2(650,100),ShowSettings);
            InterfaceLayout.Button(panel,"Help","HOW TO PLAY",new Vector2(0,-210),new Vector2(650,100),ShowHelp);
            InterfaceLayout.Button(panel,"Leave","MAIN MENU",new Vector2(0,-340),new Vector2(650,100),RequestLeave);
        }
        private void BuildResult()
        {
            var panel=Page(InterfaceScreen.Result,"MATCH RESULT",new Vector2(1320,900));
            resultTitle=InterfaceLayout.Label(panel,"Outcome","",new Vector2(0,230),new Vector2(1180,80),50);
            resultText=InterfaceLayout.Label(panel,"Reward","",new Vector2(0,45),new Vector2(1160,250),36);
            InterfaceLayout.Button(panel,"Again","PLAY AGAIN",new Vector2(0,-155),new Vector2(620,110),StartBattle).GetComponent<Image>().color=InterfaceLayout.Accent;
            InterfaceLayout.Button(panel,"Setup","BATTLE SETUP",new Vector2(-320,-305),new Vector2(570,100),ShowPreparation);
            InterfaceLayout.Button(panel,"Home","MAIN MENU",new Vector2(320,-305),new Vector2(570,100),Home);
        }
        private void BuildConfirmation()
        {
            var panel=Page(InterfaceScreen.ConfirmLeave,"LEAVE THIS MATCH?",new Vector2(1100,600));
            InterfaceLayout.Label(panel,"Consequence","Leaving counts as surrender.\nThis match gives no coins or progression.",new Vector2(0,30),new Vector2(990,160),35);
            InterfaceLayout.Button(panel,"Cancel","KEEP PLAYING",new Vector2(-260,-180),new Vector2(450,104),Back);
            InterfaceLayout.Button(panel,"Confirm","LEAVE MATCH",new Vector2(260,-180),new Vector2(450,104),ConfirmLeave);
        }
        private void BuildRotation()
        {
            var panel=Page(InterfaceScreen.Rotate,"ROTATE YOUR DEVICE",new Vector2(900,650));
            InterfaceLayout.Label(panel,"Hint","This battlefield uses landscape view.\nRotate your device to continue.\nAn active match stays paused until you resume.",Vector2.zero,new Vector2(800,330),36);
        }
        private void SetScreen(InterfaceScreen screen)
        {
            CurrentScreen=screen;input.SetUiBlocked(screen!=InterfaceScreen.Battle);
            foreach(var page in pages)page.Value.SetActive(page.Key==screen);
            battle.SetActive(screen==InterfaceScreen.Battle);
            if(screen!=InterfaceScreen.Garage)garage.Hide();
            transform.SetAsLastSibling();
            if(screen==InterfaceScreen.Garage)garage.Open();
        }
        public void Home(){if(match.IsRunning){RequestLeave();return;}SetScreen(InterfaceScreen.Home);}
        public void ShowPreparation(){if(match.IsRunning)return;SetScreen(InterfaceScreen.Preparation);}
        public void ShowGarage()
        {
            if(CurrentScreen==InterfaceScreen.Garage)return;
            if(CurrentScreen==InterfaceScreen.Battle)Pause();
            garageReturn=CurrentScreen;SetScreen(InterfaceScreen.Garage);
        }
        private void ReturnFromGarage()=>SetScreen(garageReturn);
        public void ShowSettings(){if(CurrentScreen==InterfaceScreen.Settings)return;if(CurrentScreen==InterfaceScreen.Battle)Pause();secondaryReturn=CurrentScreen;SetScreen(InterfaceScreen.Settings);}
        public void ShowHelp(){if(CurrentScreen==InterfaceScreen.Help)return;if(CurrentScreen==InterfaceScreen.Battle)Pause();secondaryReturn=CurrentScreen;SetScreen(InterfaceScreen.Help);}
        public void SelectTheme(int index){if(CurrentScreen==InterfaceScreen.Preparation)themes.SelectTheme(index);}
        public bool SelectStartingWeapon(int index)
        {
            if(CurrentScreen!=InterfaceScreen.Preparation||index<0||index>=input.AvailableWeapons.Length)return false;
            if(Progress!=null&&!Progress.Store.SetStartingWeapon(input.AvailableWeapons[index].WeaponId))return false;
            startingWeaponIndex=index;return true;
        }
        public void StartBattle()
        {
            if(CurrentScreen!=InterfaceScreen.Preparation&&CurrentScreen!=InterfaceScreen.Result)return;
            if(!bootstrap.CanStartConfiguredMatch)return;
            bootstrap.StartConfiguredMatch();
            if(!match.IsRunning)return;input.SelectWeapon(startingWeaponIndex);SetScreen(InterfaceScreen.Battle);
        }
        public void Pause(){if(!match.IsRunning)return;match.SetPaused(true);SetScreen(InterfaceScreen.Pause);}
        public void Resume(){if(!match.IsRunning)return;match.SetPaused(false);SetScreen(InterfaceScreen.Battle);}
        public void RequestLeave(){if(!match.IsRunning){Home();return;}match.SetPaused(true);SetScreen(InterfaceScreen.ConfirmLeave);}
        public void ConfirmLeave()
        {
            if(CurrentScreen!=InterfaceScreen.ConfirmLeave)return;
            leaving=true;try{match.Surrender(0);}finally{leaving=false;}SetScreen(InterfaceScreen.Home);
        }
        private void OnMatchEnded(MatchResult result){if(!leaving)SetScreen(InterfaceScreen.Result);}
        public void Back()
        {
            switch(CurrentScreen)
            {
                case InterfaceScreen.Battle:Pause();break;
                case InterfaceScreen.Pause:Resume();break;
                case InterfaceScreen.Preparation:case InterfaceScreen.Result:Home();break;
                case InterfaceScreen.Settings:case InterfaceScreen.Help:SetScreen(secondaryReturn);break;
                case InterfaceScreen.Garage:garage.Hide();ReturnFromGarage();break;
                case InterfaceScreen.ConfirmLeave:SetScreen(InterfaceScreen.Pause);break;
            }
        }
        public void HandleOrientation(bool portrait)
        {
            if(!built)return;
            if(portrait&&CurrentScreen!=InterfaceScreen.Rotate)
            {
                rotationReturn=CurrentScreen==InterfaceScreen.Battle?InterfaceScreen.Pause:CurrentScreen;
                if(match.IsRunning)match.SetPaused(true);SetScreen(InterfaceScreen.Rotate);
            }
            else if(!portrait&&CurrentScreen==InterfaceScreen.Rotate)SetScreen(rotationReturn);
        }
        private void Update()
        {
            if(built&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Back();
        }
        private void LateUpdate()
        {
            if(!built)return;
            if(Screen.width>0&&Screen.height>0)HandleOrientation(Screen.height>Screen.width);
            if(CurrentScreen==InterfaceScreen.Battle&&match.IsPaused)SetScreen(InterfaceScreen.Pause);
            var tank=bootstrap.NextTank;var shield=bootstrap.NextShield;var store=Progress!=null?Progress.Store:null;
            if(tank!=null)
            {
                homeTank.sprite=setupTank.sprite=tank.thumbnail;
                string summary=$"{tank.displayName.ToUpperInvariant()}\nSHIELD {(shield!=null?shield.displayName.ToUpperInvariant():"NONE")}";
                homeLoadout.text=summary;setupLoadout.text=summary;
            }
            wallet.text=store!=null?$"COINS {store.Coins}   /   WINS {store.Wins}\nBEST SCORE {store.BestScore}":"OFFLINE BATTLE";
            settingsText.text=store!=null?(string.IsNullOrEmpty(store.Status)?"Progress and loadout save automatically.\nYour data stays on this device.":store.Status):"Offline mode";
            soundText.text=store==null||store.SoundEnabled?"SOUND ON":"SOUND OFF";
            pauseText.text=$"TURN {match.CompletedTurns+1}   /   {Mathf.CeilToInt(match.RemainingTurnSeconds)} SECONDS LEFT";
            if(CurrentScreen==InterfaceScreen.Preparation)
            {
                for(int i=0;i<themeButtons.Length;i++)themeButtons[i].GetComponent<Image>().color=i==themes.SelectedTheme?themes.Themes[i].accentColor:InterfaceLayout.Ink;
                for(int i=0;i<startingWeapons.Length;i++)startingWeapons[i].GetComponent<Image>().color=i==startingWeaponIndex?InterfaceLayout.Accent:InterfaceLayout.Card;
                setupHint.text=store!=null&&!string.IsNullOrEmpty(store.Status)?store.Status:"One shot per turn.\nSpecial ammo refills each match.";
            }
            if(CurrentScreen==InterfaceScreen.Result)
            {
                var result=match.FinalResult;resultTitle.text=result==null?"MATCH CANCELLED":result.IsDraw?"DRAW":result.WinnerPlayerIndex==0?"VICTORY":"DEFEAT";
                resultText.text=$"SCORE {match.GetScore(0)}   /   COINS +{(Progress!=null?Progress.LastReward:0)}\n"+
                    $"WALLET {(store!=null?store.Coins:0)}   /   {match.EndReason}\n"+(Progress!=null?(Progress.RewardSaved?"PROGRESS SAVED":store.Status):"");
            }
        }
        private void OnEnable(){if(built){match.MatchEnded-=OnMatchEnded;match.MatchEnded+=OnMatchEnded;}}
        private void OnDisable()
        {
            if(match!=null){match.MatchEnded-=OnMatchEnded;if(match.IsRunning)match.SetPaused(true);}
            if(input!=null)input.SetUiBlocked(true);
        }
        private void OnDestroy(){if(garage!=null){garage.Closed-=ReturnFromGarage;garage.PreparationRequested-=ShowPreparation;}}
    }
}
