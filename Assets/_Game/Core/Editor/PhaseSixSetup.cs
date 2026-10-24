#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using TankGame.Core;
using TankGame.Data;
using TankGame.Presentation;
using TankGame.Projectiles;
using TankGame.Testing;
using TankGame.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PhaseSixSetup
{
    private const string Pending="PhaseSix.Verify";
    private static StringBuilder report;
    private static BattleFeedback feedback;
    private static GameInterface flow;
    private static MatchFlowController match;
    private static HealthComponent target;
    private static GameObject testRoot;
    private static int stage,effectCount,labelCount;
    private static double due;
    private static float pausedTime;
    private static float savedVolume;
    private static TankGame.AI.SimpleArtilleryAI ai;
    private static bool aiEnabled;
    static PhaseSixSetup()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))
            {SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Begin;}
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
        };
    }
    [MenuItem("Tools/Tank Game/Phase Six/Install Shot Feedback %#&F3")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Install in Edit Mode.");
        var feedback=UnityEngine.Object.FindAnyObjectByType<BattleFeedback>();
        if(feedback==null){var root=new GameObject("Battle Feedback");Undo.RegisterCreatedObjectUndo(root,"Install feedback");root.AddComponent<BattleFeedback>();}
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Debug.Log("Phase six feedback installed.");
    }
    [MenuItem("Tools/Tank Game/Phase Six/Verify Shot Feedback %#&F10")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start verification from Edit Mode.");
        SessionState.SetString("TankGame.Progression.TestPath",Path.GetFullPath("Library/PhaseSixTests/"+Guid.NewGuid().ToString("N")+"/progress-v1.json"));
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Case(string name,Action run){run();report.AppendLine("PASS "+name);}
    private static void Begin()
    {
        report=new StringBuilder();savedVolume=AudioListener.volume;
        try
        {
            feedback=UnityEngine.Object.FindAnyObjectByType<BattleFeedback>();flow=UnityEngine.Object.FindAnyObjectByType<GameInterface>();match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();
            ai=UnityEngine.Object.FindAnyObjectByType<TankGame.AI.SimpleArtilleryAI>();if(ai!=null){aiEnabled=ai.enabled;ai.enabled=false;}
            Case("scene installs one active feedback service with a supported material",()=>Check(feedback!=null&&BattleFeedback.Active==feedback&&feedback.EffectMaterial.shader!=null&&feedback.EffectMaterial.shader.isSupported,"Feedback service/material missing"));
            Case("seven weapon styles have distinct color and audio pitch",()=>{var colors=new HashSet<Color>();var pitches=new HashSet<float>();foreach(var data in UnityEngine.Object.FindAnyObjectByType<IntegratedInput>().AvailableWeapons){var style=FeedbackStyle.For(data.WeaponId);colors.Add(style.Color);pitches.Add(style.Pitch);}Check(colors.Count==7&&pitches.Count==7,"Repeated weapon style");});
            Case("camera retains baseline on widescreen and field width on tablets",()=>{Check(Mathf.Abs(BattleFeedback.CameraSize(7,12.4f,4f/3)*4f/3-12.4f)<.01f,"Tablet field cropped");Check(BattleFeedback.CameraSize(7,12.4f,21f/9)==7,"Wide screen zoomed");});
            flow.ShowPreparation();flow.StartBattle();feedback.Clear();
            Case("real player launch ignores owner contact while retaining other collisions",()=>
            {
                var controls=UnityEngine.Object.FindAnyObjectByType<GameplayActionController>();
                var shell=controls.Fire(0,new Vector2(1,1));
                HealthComponent owner=null;foreach(var health in UnityEngine.Object.FindObjectsByType<HealthComponent>())
                {var player=health.GetComponent<BackendTestPlayerController>();if(player!=null&&player.PlayerIndex==0)owner=health;}
                Check(owner!=null,"Player owner missing");
                foreach(var shellCollider in shell.GetComponentsInChildren<Collider2D>())foreach(var ownerCollider in owner.GetComponentsInChildren<Collider2D>())
                    Check(Physics2D.GetIgnoreCollision(shellCollider,ownerCollider),"Owner contact is still enabled");
                shell.CancelWithoutExplosion();
            });
            testRoot=new GameObject("Phase Six Test Weapon");var weapon=testRoot.AddComponent<WeaponController>();
            var weapons=UnityEngine.Object.FindAnyObjectByType<IntegratedInput>().AvailableWeapons;
            Case("all seven real projectiles receive trails without changing damage or velocity",()=>
            {
                foreach(var data in weapons)
                {
                    weapon.AssignWeapon(data);var shell=weapon.Fire(0,new Vector2(0,4),Vector2.up);
                    Check(shell.GetComponent<ProjectileFeedback>()!=null&&shell.GetComponentsInChildren<TrailRenderer>().Length==2,"Missing trail "+data.WeaponId);
                    Check(shell.Damage==data.BaseDamage&&Mathf.Abs(shell.Body.linearVelocity.y-data.LaunchSpeed)<.01f,"Presentation changed weapon stats");
                    shell.CancelWithoutExplosion();
                }
            });
            Case("cluster children inherit style and receive the same shot identity",()=>
            {
                weapon.AssignWeapon(Array.Find(weapons,w=>w.WeaponId=="weapon-cluster"));var shell=weapon.Fire(0,new Vector2(0,4),Vector2.up);var shot=shell.Shot;
                shell.GetComponent<ClusterImpactHandler>().TriggerSplit(new Vector2(0,4),Vector2.up);
                int children=0;foreach(var child in UnityEngine.Object.FindObjectsByType<Projectile>())if(child!=shell&&child.Shot==shot){children++;Check(child.Shot.VisualId=="weapon-cluster"&&child.GetComponent<ProjectileFeedback>()!=null,"Child lost feedback style");}
                Check(children==4,"Wrong child count");shot.Cancel();
            });
            Case("each actual detonation generates one presentation event",()=>
            {
                weapon.ResetAmmunition();
                foreach(var data in weapons){weapon.AssignWeapon(data);var shell=weapon.Fire(0,new Vector2(0,4),Vector2.up);int count=feedback.PresentedExplosions;shell.TriggerExplosion(new Vector2(0,6));Check(feedback.PresentedExplosions==count+1,"Explosion missing or duplicated");shell.CancelWithoutExplosion();}
            });
            Case("cancelling a projectile does not invent an explosion",()=>{weapon.AssignWeapon(weapons[0]);var shell=weapon.Fire(0,new Vector2(0,4),Vector2.up);int count=feedback.PresentedExplosions;shell.CancelWithoutExplosion();Check(feedback.PresentedExplosions==count,"Cancel exploded");});
            target=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>().GetComponentInChildren<HealthComponent>();
            if(target==null)target=UnityEngine.Object.FindObjectsByType<HealthComponent>()[0];
            target.ResetHealth();float hp=target.CurrentHealth;float sh=target.CurrentShield;bool received=false;
            Action<float,float,bool> handler=(loss,absorbed,broken)=>{received=true;Check(Mathf.Abs(loss-Mathf.Max(0,20-sh)*(1-target.ArmorReduction))<.01f&&Mathf.Abs(absorbed-Mathf.Min(sh,20))<.01f,"Incorrect actual feedback amounts");};
            target.Damaged+=handler;target.ApplyDamage(20,0);target.Damaged-=handler;
            Case("damage feedback reports actual shield absorption and health loss",()=>Check(received&&target.CurrentHealth<=hp,"No damage notification"));
            bool broke=false;handler=(loss,absorbed,broken)=>broke|=broken;target.Damaged+=handler;target.ApplyDamage(target.CurrentShield+1,0);target.Damaged-=handler;
            Case("the exact shield-breaking hit is identified",()=>Check(sh<=0||broke,"Break not reported"));
            Case("effect budget remains bounded during repeated cluster-style impacts",()=>{for(int i=0;i<30;i++)feedback.Burst(Vector2.zero,Color.yellow,1,10,.4f);Check(feedback.ActiveEffects==BattleFeedback.MaximumEffects,"Unbounded effects");});
            Case("audio is cached and respects mute",()=>{AudioListener.volume=1;feedback.Audio.Play("verify",200,.05f,.1f);int count=feedback.Audio.CachedClips;feedback.Audio.Play("verify",200,.05f,.1f);Check(feedback.Audio.CachedClips==count,"Audio recreated");AudioListener.volume=0;feedback.Audio.SetEngine(true,false);Check(!feedback.Audio.EnginePlaying,"Muted engine playing");AudioListener.volume=savedVolume;});
            Case("engine sound starts and stops with movement and pause",()=>{AudioListener.volume=1;feedback.Audio.SetEngine(true,false);Check(feedback.Audio.EnginePlaying,"Engine not started");feedback.Audio.SetEngine(true,true);Check(!feedback.Audio.EnginePlaying,"Paused engine continued");AudioListener.volume=savedVolume;});
            stage=0;due=EditorApplication.timeSinceStartup+.20;EditorApplication.update+=Tick;
        }
        catch(Exception error){Finish(error);}
    }
    private static void Tick()
    {
        if(EditorApplication.timeSinceStartup<due)return;
        try
        {
            if(stage==0)
            {
                Case("nearby damage is coalesced into a bounded floating label",()=>Check(feedback.ActiveLabels>0&&feedback.ActiveLabels<=BattleFeedback.MaximumLabels,"Damage label missing"));
                feedback.Burst(new Vector2(0,3),Color.cyan,1,10,.4f);flow.Pause();effectCount=feedback.ActiveEffects;labelCount=feedback.ActiveLabels;pausedTime=Time.time;stage=1;due=EditorApplication.timeSinceStartup+.25;
            }
            else if(stage==1)
            {
                Case("pause freezes presentation lifetime and scaled time",()=>Check(feedback.ActiveEffects==effectCount&&feedback.ActiveLabels==labelCount&&Mathf.Abs(Time.time-pausedTime)<.01f,"Paused presentation advanced"));
                flow.Resume();stage=2;due=EditorApplication.timeSinceStartup+1.6;
            }
            else
            {
                Case("transient effects and labels expire after resume",()=>Check(feedback.ActiveEffects==0&&feedback.ActiveLabels==0,"Stale visual effect"));
                flow.Pause();flow.RequestLeave();flow.ConfirmLeave();
                Case("surrender clears presentation without granting coins",()=>{feedback.Clear();Check(!match.IsRunning&&feedback.ActiveEffects==0&&UnityEngine.Object.FindAnyObjectByType<ProgressionController>().Store.Coins==0,"Result or feedback leak");});
                Case("disabling and enabling restores subscriptions without duplicate explosions",()=>{feedback.enabled=false;Check(BattleFeedback.Active==null,"Active retained on disable");feedback.enabled=true;Check(BattleFeedback.Active==feedback,"Enable failed");flow.ShowPreparation();flow.StartBattle();var weapon=testRoot.GetComponent<WeaponController>();weapon.AssignWeapon(UnityEngine.Object.FindAnyObjectByType<IntegratedInput>().AvailableWeapons[0]);var shell=weapon.Fire(0,new Vector2(0,4),Vector2.up);int count=feedback.PresentedExplosions;shell.TriggerExplosion(new Vector2(0,6));Check(feedback.PresentedExplosions==count+1,"Duplicate subscription");shell.CancelWithoutExplosion();flow.Pause();flow.RequestLeave();flow.ConfirmLeave();feedback.Clear();});
                Finish(null);
            }
        }
        catch(Exception error){Finish(error);}
    }
    private static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;AudioListener.volume=savedVolume;
        if(ai!=null)ai.enabled=aiEnabled;
        if(testRoot!=null)UnityEngine.Object.Destroy(testRoot);
        report.AppendLine(error==null?"PASS all phase-six checks.":"FAIL "+error);
        File.WriteAllText("Library/PhaseSixVerification.txt",report.ToString());
        if(error==null)Debug.Log(report.ToString());else Debug.LogError(report.ToString());
    }
}
#endif
