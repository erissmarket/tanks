using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TankGame.Data;
using UnityEngine;

namespace TankGame.Core
{
    // Every mutation is committed to disk before the in-memory balance changes.
    public sealed class ProgressStore
    {
        [Serializable] private sealed class Envelope { public string payload,checksum; }
        private const int Limit=1000000000;
        private PlayerProgress data;
        private readonly string path;
        public EconomyRules Rules { get; }
        public string Status { get; private set; }="";
        public bool CanWrite { get; private set; }=true;
        public int Coins=>data.coins;
        public int Wins=>data.wins;
        public int TotalScore=>data.totalScore;
        public int BestScore=>data.bestScore;
        public bool SoundEnabled=>data.soundEnabled;
        public PlayerProgress Snapshot=>Clone(data);
        public ProgressStore(string filePath,EconomyRules rules)
        {
            path=filePath;Rules=rules!=null?rules:throw new ArgumentNullException(nameof(rules));
            data=new PlayerProgress();Load();
        }
        private static PlayerProgress Clone(PlayerProgress value)=>JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(value));
        private static string Hash(string value)
        {using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(value)));}
        private PlayerProgress Read(string file)
        {
            var envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(file));
            if(envelope==null||envelope.payload==null||envelope.checksum!=Hash(envelope.payload))throw new InvalidDataException("Save checksum mismatch.");
            var value=JsonUtility.FromJson<PlayerProgress>(envelope.payload);
            if(value==null||value.version!=1)throw new NotSupportedException("Unsupported save version.");
            Validate(value);return value;
        }
        private static void Validate(PlayerProgress value)
        {
            if(value.coins<0||value.coins>Limit||value.wins<0||value.wins>1000000||value.draws<0||value.draws>1000000||value.losses<0||value.losses>1000000||value.totalScore<0||value.totalScore>Limit||value.bestScore<0||value.bestScore>Limit||value.ownedTanks==null||value.ownedShields==null||value.upgrades==null)
                throw new InvalidDataException("Invalid progress values.");
            if(!value.ownedTanks.Contains("vanguard")||!value.ownedShields.Contains("light")||!value.ownedShields.Contains("none"))throw new InvalidDataException("Starter equipment is missing.");
            if(!value.ownedTanks.Contains(value.selectedTank))value.selectedTank="vanguard";
            if(!value.ownedShields.Contains(value.selectedShield))value.selectedShield="light";
            value.selectedTheme=Mathf.Clamp(value.selectedTheme,0,2);
            if(value.upgrades.Any(x=>x==null||string.IsNullOrEmpty(x.tankId)||x.hull<0||x.hull>5||x.armor<0||x.armor>5||x.engine<0||x.engine>5)
                ||value.upgrades.Select(x=>x.tankId).Distinct().Count()!=value.upgrades.Count)throw new InvalidDataException("Invalid upgrades.");
        }
        private void Load()
        {
            if(!File.Exists(path))return;
            try{data=Read(path);return;}
            catch(NotSupportedException){CanWrite=false;Status="Save version unsupported; progress is read-only.";return;}
            catch(Exception error) when(error is IOException||error is ArgumentException||error is InvalidDataException||error is UnauthorizedAccessException)
            {
                try
                {
                    data=Read(path+".bak");
                    // Preserve the damaged primary before replacing it with the valid backup.
                    File.Copy(path,path+".damaged-"+DateTime.UtcNow.Ticks,false);
                    File.Copy(path+".bak",path,true);Status="Recovered progress from backup.";return;
                }
                catch(Exception)
                {
                    CanWrite=false;Status="Save could not be read. Existing files were preserved.";
                }
            }
        }
        private bool Commit(PlayerProgress next)
        {
            if(!CanWrite)return false;
            try
            {
                Validate(next);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                string payload=JsonUtility.ToJson(next);string json=JsonUtility.ToJson(new Envelope{payload=payload,checksum=Hash(payload)},true);
                using(var stream=new FileStream(path+".tmp",FileMode.Create,FileAccess.Write,FileShare.None))
                {byte[] bytes=Encoding.UTF8.GetBytes(json);stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
                data=next;Status="";return true;
            }
            catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is InvalidDataException)
            {Status="Could not save progress; change was not applied.";return false;}
        }
        public bool OwnsTank(TankDefinition tank)=>tank!=null&&data.ownedTanks.Contains(tank.tankId);
        public bool OwnsShield(ShieldData shield)=>shield!=null&&data.ownedShields.Contains(shield.shieldId);
        public bool CanUnlock(TankDefinition tank)=>tank!=null&&tank.unlockCost>=0&&tank.requiredWins>=0&&!OwnsTank(tank)&&data.wins>=tank.requiredWins&&data.coins>=tank.unlockCost&&CanWrite;
        public bool CanUnlock(ShieldData shield)=>shield!=null&&shield.unlockCost>=0&&shield.requiredWins>=0&&!OwnsShield(shield)&&data.wins>=shield.requiredWins&&data.coins>=shield.unlockCost&&CanWrite;
        public bool Unlock(TankDefinition tank)
        {if(!CanUnlock(tank))return false;var next=Clone(data);next.coins-=tank.unlockCost;next.ownedTanks.Add(tank.tankId);return Commit(next);}
        public bool Unlock(ShieldData shield)
        {if(!CanUnlock(shield))return false;var next=Clone(data);next.coins-=shield.unlockCost;next.ownedShields.Add(shield.shieldId);return Commit(next);}
        public TankUpgrades Upgrades(string tankId)
        {
            var value=data.upgrades.Find(x=>x.tankId==tankId);
            return value==null?new TankUpgrades{tankId=tankId}:new TankUpgrades{tankId=tankId,hull=value.hull,armor=value.armor,engine=value.engine};
        }
        public int UpgradeCost(string tankId,UpgradeKind kind)
        {int basis=kind==UpgradeKind.Hull?Rules.hullBaseCost:kind==UpgradeKind.Armor?Rules.armorBaseCost:Rules.engineBaseCost;return basis*(Upgrades(tankId).Level(kind)+1);}
        public bool CanUpgrade(TankDefinition tank,UpgradeKind kind)=>OwnsTank(tank)&&Enum.IsDefined(typeof(UpgradeKind),kind)&&Upgrades(tank.tankId).Level(kind)<Rules.maximumUpgradeLevel&&data.coins>=UpgradeCost(tank.tankId,kind)&&CanWrite;
        public bool Upgrade(TankDefinition tank,UpgradeKind kind)
        {
            if(!CanUpgrade(tank,kind))return false;var next=Clone(data);next.coins-=UpgradeCost(tank.tankId,kind);
            var value=next.upgrades.Find(x=>x.tankId==tank.tankId);if(value==null){value=new TankUpgrades{tankId=tank.tankId};next.upgrades.Add(value);}value.Increment(kind);return Commit(next);
        }
        public bool Select(TankDefinition tank,ShieldData shield)
        {
            if(!OwnsTank(tank)||!OwnsShield(shield)||!tank.Supports(shield))return false;
            if(data.selectedTank==tank.tankId&&data.selectedShield==shield.shieldId)return true;
            var next=Clone(data);next.selectedTank=tank.tankId;next.selectedShield=shield.shieldId;return Commit(next);
        }
        public bool SetTheme(int index)
        {
            if(index<0||index>2)return false;if(data.selectedTheme==index)return true;var next=Clone(data);next.selectedTheme=index;return Commit(next);
        }
        public bool SetSound(bool enabled)
        {if(data.soundEnabled==enabled)return true;var next=Clone(data);next.soundEnabled=enabled;return Commit(next);}
        public bool SetStartingWeapon(string id)
        {
            if(string.IsNullOrEmpty(id)||id.Length>64)return false;
            if(data.startingWeapon==id)return true;var next=Clone(data);next.startingWeapon=id;return Commit(next);
        }
        public string BeginMatch()
        {var next=Clone(data);next.pendingMatchId=Guid.NewGuid().ToString("N");return Commit(next)?next.pendingMatchId:null;}
        public bool CompleteMatch(string id,MatchResult result,MatchEndReason reason,int score,out int reward)
        {
            reward=0;
            if(string.IsNullOrEmpty(id)||id!=data.pendingMatchId)return false;
            var next=Clone(data);next.pendingMatchId="";
            bool valid=result!=null&&(reason==MatchEndReason.Elimination||reason==MatchEndReason.Fall||reason==MatchEndReason.TurnLimit);
            if(valid)
            {
                score=Mathf.Clamp(score,0,Limit);int basis=result.IsDraw?Rules.drawCoins:result.WinnerPlayerIndex==0?Rules.winCoins:Rules.lossCoins;
                reward=basis+Mathf.Min(Rules.performanceCap,Mathf.FloorToInt(score*Rules.coinsPerScore));
                next.coins=(int)Math.Min(Limit,(long)next.coins+reward);next.totalScore=(int)Math.Min(Limit,(long)next.totalScore+score);next.bestScore=Mathf.Max(next.bestScore,score);
                if(result.IsDraw)next.draws=Mathf.Min(1000000,next.draws+1);else if(result.WinnerPlayerIndex==0)next.wins=Mathf.Min(1000000,next.wins+1);else next.losses=Mathf.Min(1000000,next.losses+1);
            }
            if(Commit(next))return true;reward=0;return false;
        }
    }
}
