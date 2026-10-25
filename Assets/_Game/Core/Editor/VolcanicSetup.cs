#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using TankGame.Core;
using TankGame.Presentation;
using TankGame.Terrain;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VolcanicSetup
{
    private const string Pending="TankGame.VolcanicTest";
    static VolcanicSetup()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=VerifyRuntime;
        };
    }
    [MenuItem("Tools/Tank Game/Volcanic Update/Install Lava and Balanced Craters %&F9")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Install from Edit Mode.");
        if(SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")throw new InvalidOperationException("Open MVPIntegrated first.");
        var match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();
        var lava=UnityEngine.Object.FindAnyObjectByType<LavaHazard>();
        if(lava==null)lava=new GameObject("Underground Lava").AddComponent<LavaHazard>();
        var settings=new SerializedObject(lava);
        settings.FindProperty("match").objectReferenceValue=match;
        settings.FindProperty("lavaMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Presentation/Assets/BattleEffects.mat");
        settings.ApplyModifiedProperties();
        var rules=new SerializedObject(match);rules.FindProperty("lava").objectReferenceValue=lava;rules.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Debug.Log("Volcanic update installed: animated lava and restrained explosion craters.");
    }
    [MenuItem("Tools/Tank Game/Volcanic Update/Verify Lava and Craters %&F10")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Verify from Edit Mode.");
        SessionState.SetString("TankGame.Progression.TestPath",Path.GetFullPath("Library/VolcanicTests/"+Guid.NewGuid().ToString("N")+"/progress-v1.json"));
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    private static void Check(bool value,string text){if(!value)throw new Exception(text);}
    private static void VerifyRuntime()
    {
        var report=new StringBuilder();
        var terrain=UnityEngine.Object.FindAnyObjectByType<DestructibleTerrain>();
        var lava=UnityEngine.Object.FindAnyObjectByType<LavaHazard>();
        var flow=UnityEngine.Object.FindAnyObjectByType<GameInterface>();
        var match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();
        var ai=UnityEngine.Object.FindAnyObjectByType<TankGame.AI.SimpleArtilleryAI>();if(ai!=null)ai.enabled=false;
        GameObject target=null;
        try
        {
            Check(lava!=null&&lava.VertexCount<1000,"Bounded lava mesh missing");report.AppendLine("PASS animated lava uses fewer than 1000 vertices");
            float y=terrain.SurfaceHeightAt(0), far=terrain.SurfaceHeightAt(1);
            Check(terrain.CarveExplosion(new Vector2(0,y),1.5f),"Rocket crater missing");
            float depth=y-terrain.SurfaceHeightAt(0);
            Check(depth>.20f&&depth<.35f,"Rocket crater depth outside compact budget: "+depth);
            Check(Mathf.Abs(terrain.SurfaceHeightAt(1)-far)<.001f,"Rocket crater changed distant terrain");report.AppendLine("PASS rocket crater is about 0.30 units deep and leaves distant ground intact");
            terrain.ResetSurface();y=terrain.SurfaceHeightAt(0);
            terrain.CarveExplosion(new Vector2(0,y),20);
            Check(y-terrain.SurfaceHeightAt(0)<.50f,"Large blast bypassed crater cap");report.AppendLine("PASS even oversized explosion terrain depth stays below 0.50 units");
            Check(!terrain.CarveExplosion(Vector2.zero,0)&&!terrain.CarveExplosion(Vector2.zero,float.NaN),"Invalid blast accepted");
            terrain.ResetSurface();Check(Mathf.Abs(terrain.SurfaceHeightAt(0)-y)<.001f&&terrain.CraterCount==0,"Reset failed");report.AppendLine("PASS reset restores crater geometry and invalid sizes do not modify ground");
            target=new GameObject("Isolated blast damage target");target.transform.position=new Vector3(100,0);target.AddComponent<BoxCollider2D>();
            var health=target.AddComponent<HealthComponent>();health.ConfigureTank(100,0);health.EquipShield(null);Physics2D.SyncTransforms();
            TankGame.Projectiles.ExplosionDamage.Apply(new Vector2(100,0),32,1.5f);
            Check(Mathf.Abs(health.CurrentHealth-68)<.01f,"Terrain tuning changed tank damage");report.AppendLine("PASS original 32-point tank damage is unchanged");
            flow.ShowPreparation();flow.StartBattle();
            var player=Player(0);var body=player.GetComponent<Rigidbody2D>();var hp=player.GetComponent<HealthComponent>();
            body.position=new Vector2(0,-2.8f);Physics2D.SyncTransforms();Check(!lava.Contains(hp),"Lava reaches normal tank area");
            body.position=new Vector2(0,lava.SurfaceY-.2f);Physics2D.SyncTransforms();
            Check(lava.Contains(hp),"Tank entered lava without contact");
            flow.Pause();match.AdvanceSimulation(.1f);Check(hp.CurrentHealth>0,"Paused lava eliminated tank");flow.Resume();
            match.AdvanceSimulation(.01f);match.AdvanceSimulation(.5f);
            Check(hp.CurrentHealth==0&&match.EndReason==MatchEndReason.Fall&&match.FinalResult.WinnerPlayerIndex==1,"Lava did not resolve a fall defeat");
            Check(lava.Splashes==1,"Lava splash duplicated");report.AppendLine("PASS lava respects pause, defeats immersed tank as Fall and emits one splash");
            flow.Home();flow.ShowPreparation();flow.StartBattle();lava.ResetPresentation();
            Check(hp.CurrentHealth>0&&!lava.Contains(hp)&&terrain.CraterCount==0,"Restart retained lava defeat");
            foreach(var sprite in hp.GetComponentsInChildren<SpriteRenderer>())if(sprite.name=="Body")Check(sprite.enabled,"Tank stayed invisible after restart");
            report.AppendLine("PASS restart restores tank visuals, health and terrain");
            flow.Pause();flow.RequestLeave();flow.ConfirmLeave();
            report.AppendLine("PASS all volcanic update checks.");
        }
        catch(Exception error){report.AppendLine("FAIL "+error);Debug.LogException(error);}
        finally
        {
            if(target!=null)UnityEngine.Object.DestroyImmediate(target);
            File.WriteAllText("Library/VolcanicVerification.txt",report.ToString());Debug.Log(report.ToString());
        }
    }
    private static BackendTestPlayerController Player(int index)
    {foreach(var p in UnityEngine.Object.FindObjectsByType<BackendTestPlayerController>())if(p.PlayerIndex==index)return p;throw new Exception("Player missing");}
}
#endif
