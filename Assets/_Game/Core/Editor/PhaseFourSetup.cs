using System;
using System.IO;
using System.Text;
using TankGame.Core;
using TankGame.Data;
using TankGame.Tank;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class PhaseFourSetup
{
    [Serializable] private sealed class TestEnvelope { public string payload,checksum; }
    private const string Pending="PhaseFour.Verify";
    private const string PreviewKey="TankGame.Progression.TestPath";
    static PhaseFourSetup()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseString(PreviewKey);
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Verify;
        };
    }
    private static EconomyRules Rules=>AssetDatabase.LoadAssetAtPath<EconomyRules>("Assets/_Game/Data/DefaultEconomyRules.asset");
    [MenuItem("Tools/Tank Game/Phase Four/Install Progression %#&F7")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        if(SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")throw new InvalidOperationException("Open MVPIntegrated first.");
        var rules=Rules;if(rules==null){rules=ScriptableObject.CreateInstance<EconomyRules>();AssetDatabase.CreateAsset(rules,"Assets/_Game/Data/DefaultEconomyRules.asset");}
        string[] names={"Scout","Vanguard","Bastion"};int[] prices={100,0,250},wins={1,0,3};
        for(int i=0;i<3;i++)
        {
            var tank=AssetDatabase.LoadAssetAtPath<TankDefinition>($"Assets/_Game/Data/Tanks/Tank_{names[i]}.asset");tank.unlockCost=prices[i];tank.requiredWins=wins[i];EditorUtility.SetDirty(tank);
            var shield=AssetDatabase.LoadAssetAtPath<ShieldData>($"Assets/_Game/Data/Shield_{new[]{"None","Light","Heavy"}[i]}.asset");
            shield.shieldId=new[]{"none","light","heavy"}[i];shield.unlockCost=i==2?100:0;shield.requiredWins=i==2?2:0;EditorUtility.SetDirty(shield);
        }
        var match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();var bootstrap=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();
        var existing=GameObject.Find("Progression");var root=existing!=null?existing:new GameObject("Progression");root.SetActive(false);root.transform.SetParent(match.transform.parent,false);
        var controller=root.GetComponent<ProgressionController>();if(controller==null)controller=root.AddComponent<ProgressionController>();
        var settings=new SerializedObject(controller);settings.FindProperty("rules").objectReferenceValue=rules;settings.FindProperty("match").objectReferenceValue=match;
        settings.FindProperty("themes").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<TankGame.Terrain.TerrainThemeController>();settings.FindProperty("savePathOverride").stringValue="";settings.ApplyModifiedProperties();
        var bootSettings=new SerializedObject(bootstrap);bootSettings.FindProperty("progression").objectReferenceValue=controller;bootSettings.ApplyModifiedProperties();root.SetActive(true);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Phase four installed: persistent coins, unlocks, capped tank upgrades, saved loadout/theme/sound and once-only rewards.");
    }
    [MenuItem("Tools/Tank Game/Phase Four/Preview Economy (isolated save) %#&F9")]
    public static void Preview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        string path=Path.GetFullPath("Library/PhaseFourPreview/progress-v1.json");
        if(!File.Exists(path))
        {
            var store=new ProgressStore(path,Rules);for(int i=0;i<6;i++)Reward(store,new MatchResult(0,1),MatchEndReason.Elimination,200);
        }
        SessionState.SetString(PreviewKey,path);EditorApplication.EnterPlaymode();
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Near(float actual,float expected,string message)=>Check(Mathf.Abs(actual-expected)<.02f,message+": "+actual+" expected "+expected);
    private static int Reward(ProgressStore store,MatchResult result,MatchEndReason reason,int score)
    {string id=store.BeginMatch();Check(store.CompleteMatch(id,result,reason,score,out int coins),"Could not settle test match");return coins;}
    private static void Case(StringBuilder report,string title,Action action){action();report.AppendLine("PASS "+title);}
    [MenuItem("Tools/Tank Game/Phase Four/Verify Progression %#&F8")]
    public static void Verify()
    {
        if(!EditorApplication.isPlaying){SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();return;}
        string folder=Path.GetFullPath("Library/PhaseFourTests/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var report=new StringBuilder();int sequence=0;
        ProgressStore New()=>new ProgressStore(Path.Combine(folder,"profile-"+(sequence++)+".json"),Rules);
        ProgressStore Funded(){var store=New();for(int i=0;i<15;i++)Reward(store,new MatchResult(0,1),MatchEndReason.Elimination,200);return store;}
        var scout=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Scout.asset");var balanced=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Vanguard.asset");
        var heavy=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Bastion.asset");var light=AssetDatabase.LoadAssetAtPath<ShieldData>("Assets/_Game/Data/Shield_Light.asset");var shield=AssetDatabase.LoadAssetAtPath<ShieldData>("Assets/_Game/Data/Shield_Heavy.asset");
        try
        {
            Case(report,"new profile starts at zero coins with Vanguard and light shield",()=>{var s=New();Check(s.Coins==0&&s.Wins==0&&s.OwnsTank(balanced)&&!s.OwnsTank(scout)&&s.OwnsShield(light)&&!s.OwnsShield(shield),"Starter grants wrong");});
            Case(report,"win pays base plus capped performance; score remains separate",()=>{var s=New();Check(Reward(s,new MatchResult(0,1),MatchEndReason.Elimination,200)==110&&s.Coins==110&&s.TotalScore==200&&s.BestScore==200&&s.Wins==1,"Win reward wrong");});
            Case(report,"draw and defeat use separate rewards and counters",()=>{var s=New();Check(Reward(s,new MatchResult(null,null),MatchEndReason.TurnLimit,50)==45,"Draw reward");Check(Reward(s,new MatchResult(1,0),MatchEndReason.Fall,20)==19,"Loss reward");Check(s.Snapshot.draws==1&&s.Snapshot.losses==1&&s.Wins==0,"Counters wrong");});
            Case(report,"surrender and technical cancellation give no coins or progress",()=>{var s=New();Reward(s,new MatchResult(1,0),MatchEndReason.Surrender,999);Reward(s,null,MatchEndReason.Cancelled,999);Check(s.Coins==0&&s.TotalScore==0&&s.Snapshot.losses==0,"Invalid match farmed progress");});
            Case(report,"unreserved and repeated results cannot issue rewards",()=>{var s=New();Check(!s.CompleteMatch("invented",new MatchResult(0,1),MatchEndReason.Elimination,200,out _),"Unreserved reward");string id=s.BeginMatch();Check(s.CompleteMatch(id,new MatchResult(0,1),MatchEndReason.Elimination,200,out _),"First reward failed");Check(!s.CompleteMatch(id,new MatchResult(0,1),MatchEndReason.Elimination,200,out _)&&s.Coins==110,"Duplicate reward");});
            Case(report,"reward stays settled after loading save again",()=>{string path=Path.Combine(folder,"reload-reward.json");var s=new ProgressStore(path,Rules);string id=s.BeginMatch();s.CompleteMatch(id,new MatchResult(0,1),MatchEndReason.Elimination,200,out _);var reload=new ProgressStore(path,Rules);Check(reload.Coins==110&&!reload.CompleteMatch(id,new MatchResult(0,1),MatchEndReason.Elimination,200,out _),"Reload reissued reward");});
            Case(report,"tank unlock checks wins and funds then charges once",()=>{var s=New();Check(!s.Unlock(scout),"Free unlock");Reward(s,new MatchResult(0,1),MatchEndReason.Elimination,200);Check(s.Unlock(scout)&&s.Coins==10&&s.OwnsTank(scout),"Unlock cost wrong");Check(!s.Unlock(scout)&&s.Coins==10&&!s.Unlock(heavy),"Repeated or unaffordable unlock");});
            Case(report,"heavy shield unlock requires two wins and deducts exact cost",()=>{var s=New();Reward(s,new MatchResult(0,1),MatchEndReason.Elimination,200);Check(!s.Unlock(shield),"Wins gate ignored");Reward(s,new MatchResult(0,1),MatchEndReason.Elimination,200);Check(s.Unlock(shield)&&s.Coins==120&&s.OwnsShield(shield),"Shield unlock cost wrong");});
            Case(report,"saved loadout must be owned and shield-compatible",()=>{var s=Funded();Check(!s.Select(scout,light),"Selected locked tank");s.Unlock(scout);s.Unlock(shield);Check(s.Select(scout,light)&&!s.Select(scout,shield)&&s.Snapshot.selectedShield=="light","Selected incompatible shield");});
            Case(report,"upgrade cost increases and fourth level is blocked",()=>{var s=Funded();int before=s.Coins;Check(s.UpgradeCost(balanced.tankId,UpgradeKind.Hull)==60,"Level one price");Check(s.Upgrade(balanced,UpgradeKind.Hull)&&s.UpgradeCost(balanced.tankId,UpgradeKind.Hull)==120,"Level two price");Check(s.Upgrade(balanced,UpgradeKind.Hull)&&s.Upgrade(balanced,UpgradeKind.Hull),"Upgrade failed");Check(!s.Upgrade(balanced,UpgradeKind.Hull)&&s.Upgrades(balanced.tankId).hull==3&&s.Coins==before-360,"Upgrade cap/cost");});
            Case(report,"locked or unaffordable upgrades do not change level or balance",()=>{var s=New();Check(!s.Upgrade(balanced,UpgradeKind.Engine)&&!s.Upgrade(scout,UpgradeKind.Hull)&&s.Coins==0&&s.Upgrades(balanced.tankId).engine==0,"Invalid upgrade changed profile");});
            Case(report,"upgrade levels are independent for tanks and attributes",()=>{var s=Funded();s.Unlock(scout);s.Upgrade(scout,UpgradeKind.Armor);s.Upgrade(balanced,UpgradeKind.Engine);Check(s.Upgrades(scout.tankId).armor==1&&s.Upgrades(scout.tankId).engine==0&&s.Upgrades(balanced.tankId).armor==0,"Levels leaked across tanks");});
            Case(report,"upgrade maxima modify stats without mutating base tank assets",()=>{var stats=new TankStats(balanced,new TankUpgrades{hull=3,armor=3,engine=3},Rules);Near(stats.Health,115,"Max hull");Near(stats.Armor,.16f,"Max armor");Near(stats.Speed,3.45f,"Max engine");Near(stats.MovementMultiplier,1.15f,"Movement gain");Near(balanced.maxHealth,100,"Asset mutated");});
            Case(report,"purchased upgrades apply next match and do not heal active tank",()=>
            {
                var s=Funded();s.Upgrade(balanced,UpgradeKind.Engine);
                using(var h=new PhaseOneSetup.Harness())
                {
                    h.Match.EndMatch();var loadout=h.Players[0].gameObject.AddComponent<TankLoadout>();loadout.Apply(balanced,light,false,s.Upgrades(balanced.tankId),Rules);h.Match.StartMatch();Near(h.Match.RemainingMovement,3.15f,"Initial upgraded movement");h.Health[0].TakeDamage(50);float hp=h.Health[0].CurrentHealth;
                    s.Upgrade(balanced,UpgradeKind.Engine);s.Upgrade(balanced,UpgradeKind.Hull);Near(h.Health[0].CurrentHealth,hp,"Upgrade healed active tank");Near(h.Players[0].GetComponent<TankMotor>().MovementSpeed,3.15f,"Upgrade changed active engine");
                    h.Match.EndMatch();loadout.Apply(balanced,shield,false,s.Upgrades(balanced.tankId),Rules);h.Match.StartMatch();Near(h.Health[0].MaxHealth,105,"Next hull");Near(h.Match.RemainingMovement,2.475f,"Next engine with heavy shield");
                }
            });
            Case(report,"loadout, unlocks, upgrades, theme and sound survive reload",()=>{string path=Path.Combine(folder,"preferences.json");var s=new ProgressStore(path,Rules);for(int i=0;i<5;i++)Reward(s,new MatchResult(0,1),MatchEndReason.Elimination,200);s.Unlock(scout);s.Upgrade(scout,UpgradeKind.Hull);s.Select(scout,light);s.SetTheme(2);s.SetSound(false);var reload=new ProgressStore(path,Rules);Check(reload.Snapshot.selectedTank==scout.tankId&&reload.OwnsTank(scout)&&reload.Upgrades(scout.tankId).hull==1&&reload.Snapshot.selectedTheme==2&&!reload.SoundEnabled&&reload.Coins==s.Coins,"Preferences lost");});
            Case(report,"corrupted primary recovers valid backup and preserves damaged file",()=>{string path=Path.Combine(folder,"recover.json");var s=new ProgressStore(path,Rules);Reward(s,new MatchResult(0,1),MatchEndReason.Elimination,200);s.SetSound(false);File.WriteAllText(path,"broken");var reload=new ProgressStore(path,Rules);Check(reload.Coins==110&&reload.CanWrite&&Directory.GetFiles(folder,"recover.json.damaged-*").Length==1,"Backup recovery failed");});
            Case(report,"unreadable primary and backup stay read-only without overwriting files",()=>{string path=Path.Combine(folder,"broken.json");File.WriteAllText(path,"broken-one");File.WriteAllText(path+".bak","broken-two");var s=new ProgressStore(path,Rules);Check(!s.CanWrite&&s.BeginMatch()==null&&File.ReadAllText(path)=="broken-one"&&File.ReadAllText(path+".bak")=="broken-two","Damaged progress was overwritten");});
            Case(report,"future save version stays intact and read-only",()=>
            {
                string path=Path.Combine(folder,"future.json");var s=new ProgressStore(path,Rules);s.BeginMatch();var envelope=JsonUtility.FromJson<TestEnvelope>(File.ReadAllText(path));
                envelope.payload=envelope.payload.Replace("\"version\":1","\"version\":99");
                using(var hash=System.Security.Cryptography.SHA256.Create())envelope.checksum=Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(envelope.payload)));
                string future=JsonUtility.ToJson(envelope);File.WriteAllText(path,future);var reload=new ProgressStore(path,Rules);Check(!reload.CanWrite&&reload.BeginMatch()==null&&File.ReadAllText(path)==future,"Future save overwritten");
            });
            Case(report,"failed disk write cannot spend coins or upgrade in memory",()=>{string path=Path.Combine(folder,"locked.json");var s=new ProgressStore(path,Rules);Reward(s,new MatchResult(0,1),MatchEndReason.Elimination,200);int before=s.Coins;using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))Check(!s.Upgrade(balanced,UpgradeKind.Hull)&&s.Coins==before&&s.Upgrades(balanced.tankId).hull==0,"Failed save applied purchase");var reload=new ProgressStore(path,Rules);Check(reload.Coins==before&&reload.Upgrades(balanced.tankId).hull==0,"Failed save modified old file");});
            Case(report,"interrupted battle pays nothing and old reservation cannot replay",()=>{string path=Path.Combine(folder,"interrupted.json");var s=new ProgressStore(path,Rules);string old=s.BeginMatch();var reload=new ProgressStore(path,Rules);Check(reload.Coins==0,"Interrupted match paid reward");string current=reload.BeginMatch();Check(old!=current&&!reload.CompleteMatch(old,new MatchResult(0,1),MatchEndReason.Elimination,200,out _),"Old match replay accepted");});
            Case(report,"real match-end event deposits coins exactly once",()=>VerifyBridge(folder,false));
            Case(report,"failed result save can retry once without losing or duplicating reward",()=>VerifyBridge(folder,true));
            report.AppendLine("PASS all phase-four checks.");
        }
        catch(Exception error){report.AppendLine("FAIL "+error);Debug.LogException(error);}
        finally
        {
            File.WriteAllText("Library/PhaseFourVerification.txt",report.ToString());Debug.Log(report.ToString());
            string safeRoot=Path.GetFullPath("Library/PhaseFourTests")+Path.DirectorySeparatorChar;
            if(folder.StartsWith(safeRoot,StringComparison.OrdinalIgnoreCase))Directory.Delete(folder,true);
        }
    }
    private static void VerifyBridge(string folder,bool blocked)
    {
        using(var h=new PhaseOneSetup.Harness())
        {
            var root=new GameObject("IsolatedProgressionBridge");root.SetActive(false);
            try
            {
                var controller=root.AddComponent<ProgressionController>();var settings=new SerializedObject(controller);string path=Path.Combine(folder,blocked?"bridge-retry.json":"bridge.json");
                settings.FindProperty("rules").objectReferenceValue=Rules;settings.FindProperty("match").objectReferenceValue=h.Match;settings.FindProperty("savePathOverride").stringValue=path;settings.ApplyModifiedProperties();root.SetActive(true);
                Check(controller.BeginBattle(),"Could not reserve match");
                if(blocked)
                {
                    using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None)){h.Health[1].Eliminate();h.Resolve();Check(!controller.RewardSaved&&controller.LastReward==0&&controller.Store.Coins==0,"Uncommitted reward displayed");}
                    Check(controller.RetrySettlement(),"Retry failed");
                }
                else{h.Health[1].Eliminate();h.Resolve();}
                Check(controller.RewardSaved&&controller.LastReward==110&&controller.Store.Coins==110&&controller.Store.Wins==1,"Result event did not settle reward");
                controller.RetrySettlement();h.Match.EndMatch();Check(controller.Store.Coins==110,"Result retry duplicated coins");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
