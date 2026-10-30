#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using TankGame.AI;
using TankGame.Core;
using TankGame.Presentation;
using TankGame.Projectiles;
using TankGame.Tank;
using TankGame.Terrain;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TankMovementVerification
{
    private const string Pending="TankGame.VerifyMovement";
    private static MatchFlowController match;
    private static GameplayActionController actions;
    private static GameInterface flow;
    private static SimpleArtilleryAI ai;
    private static Rigidbody2D human,enemy;
    private static TankMotor humanMotor,enemyMotor;
    private static DestructibleTerrain terrain;
    private static int stage,shots;
    private static float humanStart,enemyStart,pausedX;
    private static double due,deadline;
    private static StringBuilder report;
    static TankMovementVerification()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Begin;
        };
    }
    [MenuItem("Tools/Tank Game/Movement/Verify Player and Enemy Movement %&F12")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start movement verification from Edit Mode.");
        EditorApplication.isPaused=false;
        SessionState.SetString("TankGame.Progression.TestPath",Path.GetFullPath("Library/MovementTests/"+Guid.NewGuid().ToString("N")+"/progress-v1.json"));
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    [MenuItem("Tools/Tank Game/Movement/Unify Tank Physics %&F8")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")
            throw new InvalidOperationException("Open MVPIntegrated in Edit Mode.");
        foreach(var player in UnityEngine.Object.FindObjectsByType<BackendTestPlayerController>())
        {
            var root=player.gameObject;var body=root.GetComponent<Rigidbody2D>();
            foreach(var motor in root.GetComponentsInChildren<TankMotor>())if(motor.gameObject!=root)UnityEngine.Object.DestroyImmediate(motor);
            foreach(var aim in root.GetComponentsInChildren<TankAim>())if(aim.gameObject!=root)UnityEngine.Object.DestroyImmediate(aim);
            foreach(var child in root.GetComponentsInChildren<Rigidbody2D>())
            {
                if(child==body)continue;
                foreach(var collider in child.GetComponents<Collider2D>())UnityEngine.Object.DestroyImmediate(collider);
                UnityEngine.Object.DestroyImmediate(child);
            }
            foreach(var box in root.GetComponents<BoxCollider2D>())UnityEngine.Object.DestroyImmediate(box);
            var hull=root.GetComponent<CapsuleCollider2D>();if(hull==null)hull=root.AddComponent<CapsuleCollider2D>();
            hull.direction=CapsuleDirection2D.Horizontal;hull.size=new Vector2(1.7f,.38f);hull.offset=new Vector2(0,-.31f);
            hull.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/_Game/Art/Tanks/TankTracks.physicsMaterial2D");
            body.bodyType=RigidbodyType2D.Dynamic;body.mass=4;body.gravityScale=2;body.constraints=RigidbodyConstraints2D.None;
            body.angularDamping=5;body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Debug.Log("Tank physics unified: one rigidbody, motor and capsule per player.");
    }
    private static void Begin()
    {
        report=new StringBuilder();stage=shots=0;
        match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();actions=UnityEngine.Object.FindAnyObjectByType<GameplayActionController>();
        flow=UnityEngine.Object.FindAnyObjectByType<GameInterface>();ai=UnityEngine.Object.FindAnyObjectByType<SimpleArtilleryAI>();
        terrain=UnityEngine.Object.FindAnyObjectByType<DestructibleTerrain>();
        foreach(var p in UnityEngine.Object.FindObjectsByType<BackendTestPlayerController>())
            if(p.PlayerIndex==0){human=p.GetComponent<Rigidbody2D>();humanMotor=p.GetComponent<TankMotor>();}
            else{enemy=p.GetComponent<Rigidbody2D>();enemyMotor=p.GetComponent<TankMotor>();}
        Projectile.Spawned+=Spawned;
        flow.ShowPreparation();flow.StartBattle();
        UnityEngine.Object.FindAnyObjectByType<IntegratedInput>().SetUiBlocked(true);
        due=EditorApplication.timeSinceStartup+.5;deadline=EditorApplication.timeSinceStartup+30;
        EditorApplication.update+=Tick;
    }
    private static void Spawned(Projectile shell){if(shell.OwnerPlayerIndex==1)shots++;}
    private static void Check(bool value,string text){if(!value)throw new Exception(text);}
    private static void Pass(string text){report.AppendLine("PASS "+text);File.WriteAllText("Library/MovementVerification.txt",report.ToString()+"RUNNING\n");}
    private static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying){Cleanup();return;}
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Movement verification timed out at stage "+stage);
            if(EditorApplication.timeSinceStartup<due)return;
            switch(stage)
            {
                case 0:
                    Check(human.GetComponentsInChildren<Rigidbody2D>().Length==1&&enemy.GetComponentsInChildren<Rigidbody2D>().Length==1,"Tank retained duplicate rigidbodies");
                    Pass("each visible tank uses one physics body");
                    foreach(var tankBody in new[]{human,enemy})
                    {
                        var tracks=tankBody.transform.Find("TankVisual/Tracks").GetComponent<SpriteRenderer>();
                        var hull=tankBody.GetComponent<CapsuleCollider2D>();
                        float visualGap=tracks.bounds.min.y-terrain.SurfaceHeightAt(tankBody.position.x);
                        Check(Mathf.Abs(tracks.bounds.min.y-hull.bounds.min.y)<.02f,"Track artwork does not match the physical hull bottom");
                        Check(Mathf.Abs(visualGap)<.04f,"Tank tracks float above or sink below visible ground: "+visualGap);
                        Pass($"{tankBody.name} visible track contact matches terrain: gap {visualGap:0.000} world units");
                    }
                    Check(!ai.IsMoving&&shots==0,"Enemy acted during player turn");
                    humanStart=human.position.x;actions.Move(0,1);stage=1;due=EditorApplication.timeSinceStartup+.6;break;
                case 1:
                    actions.Stop(0);
                    Check(human.position.x-humanStart>.25f&&humanMotor.RemainingMovement<3&&humanMotor.RemainingMovement>=0,$"Human did not move within its budget: dx={human.position.x-humanStart}, remaining={humanMotor.RemainingMovement}, grounded={humanMotor.IsGrounded}");
                    Pass($"player moved {human.position.x-humanStart:0.00} units through the real motor and spent movement budget");
                    bool rejected=false;try{actions.Move(1,1);}catch(InvalidOperationException){rejected=true;}
                    Check(rejected&&!ai.IsMoving&&shots==0,"Inactive enemy moved or fired");Pass("inactive tank cannot move or shoot");
                    enemyStart=enemy.position.x;match.AdvanceSimulation(31);match.AdvanceSimulation(1.6f);stage=2;break;
                case 2:
                    if(!ai.IsMoving)break;
                    pausedX=enemy.position.x;flow.Pause();stage=3;due=EditorApplication.timeSinceStartup+.35;break;
                case 3:
                    Check(Mathf.Abs(enemy.position.x-pausedX)<.01f&&shots==0,"Paused enemy moved or fired");
                    Pass("pause freezes enemy repositioning and prevents its shot");flow.Resume();stage=4;break;
                case 4:
                    if(shots==0)break;
                    Check(!ai.IsMoving&&Mathf.Abs(enemy.position.x-enemyStart)>.3f&&Mathf.Abs(enemy.position.x-enemyStart)<1.6f,"AI did not reposition before firing");
                    Check(enemyMotor.RemainingMovement>=0&&enemyMotor.RemainingMovement<3,"AI ignored movement budget");
                    Pass($"enemy moved {Mathf.Abs(enemy.position.x-enemyStart):0.00} units, stopped and fired once");stage=5;break;
                case 5:
                    if(match.State==MatchState.ProjectileFlying||match.State==MatchState.Resolving)break;
                    Check(shots==1&&match.CurrentPlayerIndex==0&&!ai.IsMoving,"Enemy duplicated shot or retained control");
                    Pass("shot finishes and returns control to the player without duplicate enemy fire");
                    Check(!ai.CanTraverseTo(float.NaN)&&!ai.CanTraverseTo(match.Rules.maximumX+1),"AI accepted invalid or out-of-bounds path");
                    float pitX=(enemy.position.x+human.position.x)*.5f;
                    terrain.Carve(new Vector2(pitX,terrain.SurfaceHeightAt(pitX)),1.4f);
                    Check(!ai.CanTraverseTo(pitX),"AI accepted a steep crater route");
                    Pass("path planning rejects steep holes, invalid destinations and map boundaries");
                    terrain.ResetSurface();flow.Pause();flow.RequestLeave();flow.ConfirmLeave();
                    report.AppendLine("PASS all movement checks.");File.WriteAllText("Library/MovementVerification.txt",report.ToString());
                    Debug.Log(report.ToString());Cleanup();break;
            }
        }
        catch(Exception error)
        {report.AppendLine("FAIL "+error);File.WriteAllText("Library/MovementVerification.txt",report.ToString());Debug.LogException(error);Cleanup();}
    }
    private static void Cleanup(){EditorApplication.update-=Tick;Projectile.Spawned-=Spawned;}
}
#endif
