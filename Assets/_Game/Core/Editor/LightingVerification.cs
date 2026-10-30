#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Core;
using TankGame.Presentation;
using TankGame.Projectiles;
using TankGame.Data;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class LightingVerification
{
    static BattleLighting lighting;static GameInterface flow;static HealthComponent enemy;static float health,time;static double due,deadline;static int stage;static string report;
    static LightingVerification(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VerifyLighting",false)){SessionState.SetBool("VerifyLighting",false);EditorApplication.delayCall+=Begin;}};}
    [MenuItem("Tools/Tank Game/Art/Verify Lighting and Real Tank Impact %#&F2")]
    public static void Verify(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Start in Edit Mode");SessionState.SetString("TankGame.Progression.TestPath",Path.GetFullPath("Library/LightingTests/"+Guid.NewGuid()+"/progress.json"));SessionState.SetBool("VerifyLighting",true);EditorApplication.EnterPlaymode();}
    static void Check(bool value,string text){if(!value)throw new Exception(text);report+="PASS "+text+"\n";}
    static void Begin()
    {
        report="";stage=0;
        try{
            flow=UnityEngine.Object.FindAnyObjectByType<GameInterface>();var ai=UnityEngine.Object.FindAnyObjectByType<TankGame.AI.SimpleArtilleryAI>();if(ai!=null)ai.enabled=false;
            flow.ShowPreparation();UnityEngine.Object.FindAnyObjectByType<TankGame.Terrain.TerrainThemeController>().SelectTheme(2);flow.StartBattle();
            lighting=UnityEngine.Object.FindAnyObjectByType<BattleLighting>();Check(lighting!=null&&lighting.ShadowCount==2,"Both tanks have contact shadows");
            foreach(var p in UnityEngine.Object.FindObjectsByType<TankGame.Testing.BackendTestPlayerController>())if(p.PlayerIndex==1)enemy=p.GetComponent<HealthComponent>();
            health=enemy.CurrentHealth+enemy.CurrentShield;
            var data=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Weapon_Rocket.asset");
            var shell=UnityEngine.Object.Instantiate(data.ProjectilePrefab,enemy.transform.position+Vector3.up*1.6f,Quaternion.Euler(0,0,-90));shell.Initialize(0,32,.8f,Vector2.down*8);
            deadline=EditorApplication.timeSinceStartup+25;EditorApplication.update+=Tick;
        }catch(Exception e){Finish(e);}
    }
    static void Tick()
    {
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Impact test timeout");
            if(stage==0){if(lighting.ActiveFlashes==0)return;Check(enemy.CurrentHealth+enemy.CurrentShield<health,"Real falling rocket damages the tank and creates explosion light");Check(lighting.PresentedImpacts==1,"One detonation produces one light");flow.Pause();time=lighting.EffectTime;due=EditorApplication.timeSinceStartup+.4;stage=1;}
            else if(stage==1&&EditorApplication.timeSinceStartup>due){Check(lighting.EffectTime==time,"Pause freezes explosion animation");flow.Resume();ScreenCapture.CaptureScreenshot("Library/LightingImpact.png");stage=2;due=EditorApplication.timeSinceStartup+1;}
            else if(stage==2&&lighting.EffectTime-time>1){Check(lighting.ActiveFlashes==0,"Explosion light expires without leftover objects");Finish(null);}
        }catch(Exception e){Finish(e);}
    }
    static void Finish(Exception e){EditorApplication.update-=Tick;if(e!=null){report+="FAIL "+e;Debug.LogException(e);}File.WriteAllText("Library/LightingVerification.txt",report);Debug.Log(report);}
}
#endif
