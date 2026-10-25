#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Core;
using TankGame.Presentation;
using TankGame.Terrain;
using TankGame.Testing;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NaturalLavaFallVerification
{
    private const string Pending="TankGame.NaturalLavaFall";
    private static MatchFlowController match;
    private static GameInterface flow;
    private static HealthComponent tank;
    private static DestructibleTerrain terrain;
    private static LavaHazard lava;
    private static Rigidbody2D body;
    private static double due;
    private static int stage,blasts;
    private static float initialY;
    static NaturalLavaFallVerification()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Begin;
        };
    }
    [MenuItem("Tools/Tank Game/Volcanic Update/Verify Natural Gravity Fall %&F11")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start from Edit Mode.");
        EditorApplication.isPaused=false;
        SessionState.SetString("TankGame.Progression.TestPath",Path.GetFullPath("Library/NaturalLavaTests/"+Guid.NewGuid().ToString("N")+"/progress-v1.json"));
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    private static void Begin()
    {
        flow=UnityEngine.Object.FindAnyObjectByType<GameInterface>();match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();
        terrain=UnityEngine.Object.FindAnyObjectByType<DestructibleTerrain>();lava=UnityEngine.Object.FindAnyObjectByType<LavaHazard>();
        foreach(var p in UnityEngine.Object.FindObjectsByType<BackendTestPlayerController>())if(p.PlayerIndex==0)tank=p.GetComponent<HealthComponent>();
        body=tank.GetComponent<Rigidbody2D>();
        var ai=UnityEngine.Object.FindAnyObjectByType<TankGame.AI.SimpleArtilleryAI>();if(ai!=null)ai.enabled=false;
        flow.ShowPreparation();flow.StartBattle();
        UnityEngine.Object.FindAnyObjectByType<IntegratedInput>().SetUiBlocked(true);
        stage=0;blasts=0;due=EditorApplication.timeSinceStartup+1;
        EditorApplication.update+=Tick;
    }
    private static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying||tank==null){EditorApplication.update-=Tick;return;}
            if(match.IsPaused)flow.Resume();
            if(EditorApplication.timeSinceStartup<due)return;
            if(stage==0)
            {
                initialY=body.position.y;
                if(lava.Contains(tank)||body.bodyType!=RigidbodyType2D.Dynamic)throw new Exception("Tank not standing above lava with dynamic physics");
                stage=1;
            }
            if(stage==1)
            {
                // Ground-only fixture: ordinary rocket-sized erosion, never reposition the tank
                // or modify its health. This tests physical reachability, not combat frequency.
                for(int i=-2;i<=2;i++)
                {
                    float x=body.position.x+i*.6f;
                    terrain.CarveExplosion(new Vector2(x,terrain.SurfaceHeightAt(x)),1.5f);blasts++;
                }
                if(terrain.SurfaceHeightAt(body.position.x)<lava.SurfaceY-.25f||blasts>=60)
                {stage=2;due=EditorApplication.timeSinceStartup+3;}
                else due=EditorApplication.timeSinceStartup+.18;
                return;
            }
            if(stage==2)
            {
                if(body.position.y>initialY-1||tank.CurrentHealth>0||match.EndReason!=MatchEndReason.Fall||lava.Splashes!=1)
                    throw new Exception($"Natural fall failed: initial={initialY}, final={body.position.y}, hp={tank.CurrentHealth}, reason={match.EndReason}, splashes={lava.Splashes}");
                File.WriteAllText("Library/NaturalLavaFallVerification.txt",$"PASS tank started on intact ground at y={initialY:0.000}\nPASS support was removed with {blasts} rocket-sized ground-only crater operations\nPASS untouched dynamic Rigidbody fell naturally to y={body.position.y:0.000}\nPASS lava contact produced Fall defeat and exactly one splash\nNOTE ground-only fixture; no tank teleport, health edits, or projectile combat damage. Does not measure frequency in normal matches.\n");
                Debug.Log("Natural gravity fall PASS; report Library/NaturalLavaFallVerification.txt");EditorApplication.update-=Tick;
            }
        }
        catch(Exception error)
        {File.WriteAllText("Library/NaturalLavaFallVerification.txt","FAIL "+error);Debug.LogException(error);EditorApplication.update-=Tick;}
    }
}
#endif
