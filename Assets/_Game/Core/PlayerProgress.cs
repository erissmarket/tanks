using System;
using System.Collections.Generic;

namespace TankGame.Core
{
    public enum UpgradeKind { Hull, Armor, Engine }
    [Serializable]
    public sealed class TankUpgrades
    {
        public string tankId;
        public int hull,armor,engine;
        public int Level(UpgradeKind kind)=>kind==UpgradeKind.Hull?hull:kind==UpgradeKind.Armor?armor:engine;
        public void Increment(UpgradeKind kind)
        {if(kind==UpgradeKind.Hull)hull++;else if(kind==UpgradeKind.Armor)armor++;else engine++;}
    }
    [Serializable]
    public sealed class PlayerProgress
    {
        public int version=1;
        public int coins,wins,draws,losses,totalScore,bestScore;
        public string selectedTank="vanguard",selectedShield="light";
        public string startingWeapon="weapon-rocket";
        public int selectedTheme;
        public bool soundEnabled=true;
        public string pendingMatchId="";
        public List<string> ownedTanks=new List<string>{"vanguard"};
        public List<string> ownedShields=new List<string>{"none","light"};
        public List<TankUpgrades> upgrades=new List<TankUpgrades>();
    }
}
