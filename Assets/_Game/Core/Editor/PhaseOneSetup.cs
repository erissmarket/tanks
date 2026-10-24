using System;
using System.IO;
using System.Reflection;
using System.Text;
using TankGame.Core;
using TankGame.Player;
using TankGame.Projectiles;
using TankGame.Presentation;
using TankGame.Tank;
using TankGame.Testing;
using TankGame.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class PhaseOneSetup
{
    private const string PendingKey = "PhaseOneSetup.Pending";
    static PhaseOneSetup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false); EditorApplication.delayCall += Verify;
        };
    }
    [MenuItem("Tools/Tank Game/Phase One/Install Rules and HUD _F1")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (SceneManager.GetActiveScene().path != "Assets/_Game/Scenes/MVPIntegrated.unity") throw new InvalidOperationException("Open MVPIntegrated.");
        const string rulesPath = "Assets/_Game/Data/DefaultMatchRules.asset";
        var rules = AssetDatabase.LoadAssetAtPath<MatchRules>(rulesPath);
        if (rules == null) { rules = ScriptableObject.CreateInstance<MatchRules>(); AssetDatabase.CreateAsset(rules, rulesPath); }
        rules.Validate();
        var match = UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();
        var matchSettings = new SerializedObject(match); matchSettings.FindProperty("rules").objectReferenceValue = rules; matchSettings.ApplyModifiedProperties();
        var bootstrap = UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();
        var canvas = GameObject.Find("MVP HUD").GetComponent<Canvas>();
        Transform parent = canvas.transform.Find("SafeArea"); if (parent == null) parent = canvas.transform.Find("Safe Area"); if (parent == null) parent = canvas.transform;
        Transform existing = parent.Find("Phase One HUD");
        var root = existing != null ? existing.gameObject : new GameObject("Phase One HUD", typeof(RectTransform));
        if (existing == null) { Undo.RegisterCreatedObjectUndo(root, "Install phase one HUD"); root.transform.SetParent(parent, false); }
        root.SetActive(false);
        var rootRect = (RectTransform)root.transform; rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one; rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var strip = Panel(root.transform, "Turn Status", new Vector2(.5f,0), new Vector2(0,224), new Vector2(720,54), new Color(.06f,.10f,.14f,.9f));
        var status = Text(strip, "Status", "TURN 1/40    TIME 30s    MOVE 3.0", 24, font); Stretch(status.rectTransform);
        var pause = Button(root.transform, "Pause", "PAUSE", new Vector2(1,1), new Vector2(-122,-174), new Vector2(168,64), font);
        var paused = Panel(root.transform, "Pause Panel", new Vector2(.5f,.5f), Vector2.zero, new Vector2(840,350), new Color(.06f,.10f,.15f,.98f));
        var pausedTitle = Text(paused, "Title", "PAUSED", 36, font); Position(pausedTitle.rectTransform,new Vector2(.5f,.5f),new Vector2(0,105),new Vector2(790,60));
        var resume = Button(paused,"Resume","RESUME",new Vector2(.5f,.5f),new Vector2(-170,-70),new Vector2(280,78),font);
        var surrender = Button(paused,"Surrender","SURRENDER",new Vector2(.5f,.5f),new Vector2(170,-70),new Vector2(280,78),font);
        var confirm = Button(paused,"Confirm","YES, SURRENDER",new Vector2(.5f,.5f),new Vector2(170,-70),new Vector2(300,78),font);
        var back = Button(paused,"Back","BACK",new Vector2(.5f,.5f),new Vector2(-170,-70),new Vector2(280,78),font);
        var result = Panel(root.transform,"Result Panel",new Vector2(.5f,.5f),Vector2.zero,new Vector2(840,400),new Color(.06f,.10f,.15f,.98f));
        var title = Text(result,"Title","RESULT",40,font); Position(title.rectTransform,new Vector2(.5f,.5f),new Vector2(0,137),new Vector2(790,64));
        var details=Text(result,"Details","SCORE 0",27,font); Position(details.rectTransform,new Vector2(.5f,.5f),new Vector2(0,10),new Vector2(790,154));
        var restart=Button(result,"Restart","PLAY AGAIN",new Vector2(.5f,.5f),new Vector2(0,-132),new Vector2(340,76),font);
        var hud=root.GetComponent<PhaseOneHud>(); if(hud==null) hud=root.AddComponent<PhaseOneHud>();
        var settings=new SerializedObject(hud);
        Link(settings,"match",match); Link(settings,"bootstrap",bootstrap); Link(settings,"status",status);
        Link(settings,"pausePanel",paused.gameObject); Link(settings,"pauseTitle",pausedTitle); Link(settings,"pauseButton",pause);
        Link(settings,"resumeButton",resume); Link(settings,"surrenderButton",surrender); Link(settings,"confirmButton",confirm); Link(settings,"backButton",back);
        Link(settings,"resultPanel",result.gameObject); Link(settings,"resultTitle",title); Link(settings,"resultDetails",details); Link(settings,"restartButton",restart);
        settings.ApplyModifiedProperties();
        paused.gameObject.SetActive(false); result.gameObject.SetActive(false); root.SetActive(true);
        EditorUtility.SetDirty(match); EditorUtility.SetDirty(hud); EditorUtility.SetDirty(rules);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Scene save failed.");
        Debug.Log("Phase one installed: turn timer, movement budget, grouped shots, results, score, pause and restart.");
    }
    private static void Link(SerializedObject settings,string name,UnityEngine.Object value)=>settings.FindProperty(name).objectReferenceValue=value;
    private static void Position(RectTransform rect,Vector2 anchor,Vector2 offset,Vector2 size){rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=offset;rect.sizeDelta=size;}
    private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    private static RectTransform Rect(Transform parent,string name)
    {
        var found=parent.Find(name);if(found!=null)return(RectTransform)found;
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return(RectTransform)go.transform;
    }
    private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size,Color color)
    {
        var rect=Rect(parent,name);Position(rect,anchor,offset,size);var image=rect.GetComponent<Image>();if(image==null)image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=true;return rect;
    }
    private static Text Text(Transform parent,string name,string value,int size,Font font)
    {
        var rect=Rect(parent,name);var text=rect.GetComponent<Text>();if(text==null)text=rect.gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
    }
    private static Button Button(Transform parent,string name,string value,Vector2 anchor,Vector2 offset,Vector2 size,Font font)
    {
        var rect=Panel(parent,name,anchor,offset,size,new Color(.18f,.30f,.39f,1));var button=rect.GetComponent<Button>();if(button==null)button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.navigation=new Navigation{mode=Navigation.Mode.None};var label=Text(rect,"Label",value,26,font);Stretch(label.rectTransform);return button;
    }

    internal sealed class Harness : IDisposable
    {
        public readonly GameObject Root;
        public readonly GameManager Game;
        public readonly TurnManager Turns;
        public readonly MatchFlowController Match;
        public readonly MatchRules Rules;
        public readonly HealthComponent[] Health=new HealthComponent[2];
        public readonly BackendTestPlayerController[] Players=new BackendTestPlayerController[2];
        public Harness(int maxTurns=40)
        {
            Root=new GameObject("PhaseOneTest");Game=Root.AddComponent<GameManager>();Turns=Root.AddComponent<TurnManager>();Match=Root.AddComponent<MatchFlowController>();
            Rules=ScriptableObject.CreateInstance<MatchRules>();Rules.maximumTurns=maxTurns;Match.ConfigureRules(Rules);
            for(int i=0;i<2;i++)
            {
                var tank=new GameObject("Test Tank "+i);tank.transform.SetParent(Root.transform);tank.transform.position=new Vector3(100+i*3,100,0);
                var body=tank.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
                tank.AddComponent<BoxCollider2D>();tank.AddComponent<TankMotor>();tank.AddComponent<TankAim>();tank.AddComponent<TankController>();tank.AddComponent<WeaponController>();
                Health[i]=tank.AddComponent<HealthComponent>();Players[i]=tank.AddComponent<BackendTestPlayerController>();Players[i].Initialize(i);
            }
            Match.Initialize(Game,Turns,Players[0],Players[1]);Match.RegisterHealth(0,Health[0]);Match.RegisterHealth(1,Health[1]);
            Physics2D.SyncTransforms();Match.StartMatch();
        }
        public Projectile Shell(ShotContext shot=null,int owner=0,float damage=10f)
        {
            var go=new GameObject("Test Shell");go.transform.SetParent(Root.transform);go.transform.position=new Vector3(100,120,0);
            var body=go.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            var projectile=go.AddComponent<Projectile>();projectile.Initialize(owner,damage,.6f,Vector2.zero,shot);return projectile;
        }
        public void Pass(){Match.AdvanceSimulation(Rules.turnSeconds+.01f);Match.AdvanceSimulation(2f);}
        public void Resolve(){Match.AdvanceSimulation(.01f);Match.AdvanceSimulation(2f);}
        public void Dispose(){if(Root!=null)UnityEngine.Object.DestroyImmediate(Root);UnityEngine.Object.DestroyImmediate(Rules);}
    }
    private static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
    private static void Case(StringBuilder report,string name,Action test){test();report.AppendLine("PASS "+name);}
    [MenuItem("Tools/Tank Game/Phase One/Verify Rules _F2")]
    public static void Verify()
    {
        if(!EditorApplication.isPlaying){SessionState.SetBool(PendingKey,true);EditorApplication.EnterPlaymode();return;}
        var report=new StringBuilder();
        try
        {
            Case(report,"30-second timeout passes turn and replenishes movement",()=>{using(var h=new Harness()){Check(h.Turns.CurrentPlayerIndex==0,"Wrong starting player");h.Pass();Check(h.Turns.CurrentPlayerIndex==1&&h.Match.CompletedTurns==1,"Timeout failed");Check(h.Match.RemainingTurnSeconds==30f&&h.Match.RemainingMovement==3f,"Turn budget not reset");}});
            Case(report,"one root shot; children block turn advancement; completion idempotent",()=>{using(var h=new Harness()){var root=h.Shell();h.Match.NotifyProjectileFired(root);var child=h.Shell(root.Shot);int count=0;root.Completed+=_=>count++;root.CompleteAndDestroy();root.CompleteAndDestroy();Check(count==1,"Duplicate completion");h.Match.AdvanceSimulation(1f);Check(h.Game.CurrentState==MatchState.ProjectileFlying,"Child did not block turn");bool rejected=false;try{h.Match.NotifyProjectileFired(h.Shell());}catch(InvalidOperationException){rejected=true;}Check(rejected,"Second shot accepted");child.CompleteAndDestroy();h.Resolve();Check(h.Turns.CurrentPlayerIndex==1,"Turn did not advance after children");}});
            Case(report,"15-second watchdog cancels every member without extra damage",()=>{using(var h=new Harness()){var root=h.Shell();h.Match.NotifyProjectileFired(root);var child=h.Shell(root.Shot);h.Match.AdvanceSimulation(16f);Check(root.IsFinished&&child.IsFinished&&root.Shot.IsCancelled,"Watchdog left a child alive");root.TriggerExplosion(h.Health[1].transform.position,100,.6f);Check(h.Health[1].CurrentHealth==100,"Cancelled projectile inflicted damage");h.Match.AdvanceSimulation(2f);Check(h.Turns.CurrentPlayerIndex==1,"Watchdog left match stuck");}});
            Case(report,"two deaths in different ticks of one shot produce draw",()=>{using(var h=new Harness()){var root=h.Shell(null,0,100);h.Match.NotifyProjectileFired(root);var child=h.Shell(root.Shot,0,100);root.TriggerExplosion(h.Health[1].transform.position);h.Match.AdvanceSimulation(.1f);Check(h.Match.IsRunning,"Ended before remaining submunitions");child.TriggerExplosion(h.Health[0].transform.position);root.CompleteAndDestroy();child.CompleteAndDestroy();h.Resolve();Check(h.Match.FinalResult!=null&&h.Match.FinalResult.IsDraw,"Simultaneous elimination was not a draw");}});
            Case(report,"score example: 70 effective damage + win + 60% survival = 200",()=>{using(var h=new Harness()){var root=h.Shell(null,0,70);h.Match.NotifyProjectileFired(root);root.TriggerExplosion(h.Health[1].transform.position);h.Health[0].TakeDamage(40);h.Health[1].TakeDamage(30);root.CompleteAndDestroy();h.Resolve();Check(h.Match.FinalResult.WinnerPlayerIndex==0&&h.Match.GetScore(0)==200,"Incorrect score or damage attribution: "+h.Match.GetScore(0));}});
            Case(report,"self damage earns no score; overkill awards only actual health removed",()=>{using(var h=new Harness()){var root=h.Shell(null,0,200);h.Match.NotifyProjectileFired(root);root.TriggerExplosion(h.Health[0].transform.position,10,.6f);Check(h.Match.GetScore(0)==0,"Self-damage credited");root.TriggerExplosion(h.Health[1].transform.position);root.CompleteAndDestroy();h.Resolve();Check(h.Match.GetScore(0)==245,"Overkill or survival score wrong: "+h.Match.GetScore(0));}});
            Case(report,"turn cap compares normalized health",()=>{using(var h=new Harness(2)){h.Health[0].TakeDamage(20);h.Health[1].TakeDamage(40);h.Pass();h.Pass();Check(h.Match.EndReason==MatchEndReason.TurnLimit&&h.Match.FinalResult.WinnerPlayerIndex==0,"Turn limit result wrong");}});
            Case(report,"equal health at turn limit draws",()=>{using(var h=new Harness(2)){h.Pass();h.Pass();Check(h.Match.FinalResult.IsDraw&&h.Match.GetScore(0)==25,"Turn-limit draw failed");}});
            Case(report,"fall below -8 eliminates tank",()=>{using(var h=new Harness()){h.Health[0].transform.position=new Vector3(100,-9,0);h.Resolve();Check(h.Match.EndReason==MatchEndReason.Fall&&h.Match.FinalResult.WinnerPlayerIndex==1,"Fall rule failed");}});
            Case(report,"pause freezes timer and controls; resume preserves remaining time",()=>{using(var h=new Harness()){h.Match.AdvanceSimulation(4f);float before=h.Match.RemainingTurnSeconds;h.Match.SetPaused(true);h.Match.AdvanceSimulation(100);Check(h.Match.RemainingTurnSeconds==before&&!h.Players[0].CanAct&&Time.timeScale==0f,"Pause did not freeze match");h.Match.SetPaused(false);Check(h.Players[0].CanAct&&h.Match.RemainingTurnSeconds==before&&Time.timeScale>0f,"Resume reset timer/control");}});
            Case(report,"surrender gives loss and zero score; result emitted once",()=>{using(var h=new Harness()){int emitted=0;h.Match.MatchEnded+=_=>emitted++;h.Match.Surrender();h.Match.Surrender();h.Match.EndMatch();Check(emitted==1&&h.Match.FinalResult.WinnerPlayerIndex==1&&h.Match.GetScore(0)==0,"Surrender or duplicate result failed");}});
            Case(report,"technical cancellation has no win or score",()=>{using(var h=new Harness()){h.Match.EndMatch();Check(h.Match.FinalResult==null&&h.Match.EndReason==MatchEndReason.Cancelled&&h.Match.GetScore(0)==0,"Cancel result failed");}});
            Case(report,"rematch resets score, timer, turn and movement",()=>{using(var h=new Harness()){h.Match.Surrender();h.Health[0].ResetHealth();h.Health[1].ResetHealth();h.Match.StartMatch();Check(h.Match.EndReason==MatchEndReason.None&&h.Match.FinalResult==null&&h.Match.GetScore(0)==0&&h.Match.CompletedTurns==0&&h.Match.RemainingMovement==3,"Rematch did not reset state");}});
            Case(report,"physics movement budget stops at 3 units and does not refill on reversing",()=>VerifyMovement(false));
            Case(report,"falling without movement does not consume budget",()=>VerifyMovement(true));
            Case(report,"terrain reset restores geometry, crater count and current theme",VerifyTerrainReset);
            Case(report,"final scores stay frozen after later health changes",()=>{using(var h=new Harness()){var root=h.Shell(null,0,70);h.Match.NotifyProjectileFired(root);root.TriggerExplosion(h.Health[1].transform.position);h.Health[0].TakeDamage(40);h.Health[1].TakeDamage(30);root.CompleteAndDestroy();h.Resolve();int score=h.Match.GetScore(0);h.Health[0].ResetHealth();Check(score==200&&h.Match.GetScore(0)==score,"Final score was recomputed after health changed");}});
            Case(report,"projectile leaving shot bounds completes without explosion",()=>{using(var h=new Harness()){var shell=h.Shell(null,0,100);h.Match.NotifyProjectileFired(shell);Check(!shell.Shot.Contains(new Vector2(1000,1000)),"Shot boundary not applied");shell.transform.position=new Vector3(1000,1000,0);typeof(Projectile).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(shell,null);Check(shell.IsFinished&&h.Health[1].CurrentHealth==100,"Out-of-bounds projectile failed safe removal");}});
            Case(report,"real cluster prefab children inherit action; real digger completes same action",()=>{using(var h=new Harness())
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Projectiles/Projectile_Cluster.prefab");
                var instance=UnityEngine.Object.Instantiate(prefab,new Vector3(0,10,0),Quaternion.identity,h.Root.transform);
                var root=instance.GetComponent<Projectile>();root.Initialize(0,10,.5f,Vector2.down);h.Match.NotifyProjectileFired(root);
                var cluster=instance.GetComponent<ClusterImpactHandler>();cluster.TriggerSplit(new Vector2(0,10),Vector2.up);
                Check(root.Shot.ActiveCount==cluster.ClusterCount+1,"Cluster prefab did not register children on parent action");
                root.CompleteAndDestroy();h.Match.AdvanceSimulation(.2f);Check(h.Game.CurrentState==MatchState.ProjectileFlying,"Real cluster ended its turn too early");
                root.Shot.Cancel();h.Resolve();Check(h.Turns.CurrentPlayerIndex==1,"Real cluster could not resolve");
            }
            using(var h=new Harness())
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Projectiles/Projectile_Digger.prefab");
                var instance=UnityEngine.Object.Instantiate(prefab,new Vector3(0,10,0),Quaternion.identity,h.Root.transform);
                var root=instance.GetComponent<Projectile>();root.Initialize(0,10,.5f,Vector2.down);h.Match.NotifyProjectileFired(root);
                var digger=instance.GetComponent<DiggerImpactHandler>();digger.DigStepInterval=0;digger.StartBurrowing(root,Vector2.down);
                Check(root.IsFinished&&root.Shot.ActiveCount==0,"Real digger did not finish its action");h.Resolve();Check(h.Turns.CurrentPlayerIndex==1,"Digger left a stuck turn");
            }});
            report.AppendLine("PASS all phase-one checks.");File.WriteAllText("Library/PhaseOneVerification.txt",report.ToString());Debug.Log(report.ToString());
        }
        catch(Exception error){report.AppendLine("FAIL "+error);File.WriteAllText("Library/PhaseOneVerification.txt",report.ToString());Debug.LogException(error);}
    }
    private static void VerifyMovement(bool falling)
    {
        var scene=SceneManager.CreateScene("MovementBudget-"+Guid.NewGuid().ToString("N"),new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        try
        {
            if(!falling){var floor=new GameObject("Floor");SceneManager.MoveGameObjectToScene(floor,scene);floor.AddComponent<BoxCollider2D>().size=new Vector2(30,1);}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Tanks/TankVisual.prefab");
            var tank=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);tank.transform.position=new Vector3(-5,falling?5:.6f,0);
            var motor=tank.GetComponent<TankMotor>();var body=tank.GetComponent<Rigidbody2D>();Physics2D.SyncTransforms();
            var step=typeof(TankMotor).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);var physics=scene.GetPhysicsScene2D();
            if(!falling)for(int i=0;i<100;i++){step.Invoke(motor,null);physics.Simulate(Time.fixedDeltaTime);}
            motor.BeginTurnBudget(3,-8,8);float startX=body.position.x;float previous=startX,total=0;
            for(int i=0;i<(falling?30:260);i++)
            {
                if(!falling)motor.Move(i<65?1:-1);
                step.Invoke(motor,null);physics.Simulate(Time.fixedDeltaTime);total+=Mathf.Abs(body.position.x-previous);previous=body.position.x;
            }
            if(falling)Check(Mathf.Abs(motor.RemainingMovement-3)<.001f&&body.position.y<5,"Gravity consumed fuel or did not fall");
            else{Check(motor.RemainingMovement<.01f,"Budget never exhausted");Check(total<=3.10f,"Movement exceeded budget: "+total);Check(body.position.x>=-8.01f&&body.position.x<=8.01f,"Horizontal boundary failed");}
        }
        finally{foreach(var go in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(go);SceneManager.UnloadSceneAsync(scene);}
    }
    private static void VerifyTerrainReset()
    {
        var source=GameObject.Find("MVP Battlefield");var holder=new GameObject("ResetTerrainTest");holder.SetActive(false);
        try
        {
            var copy=UnityEngine.Object.Instantiate(source,holder.transform,false);var controller=copy.GetComponent<TankGame.Terrain.TerrainThemeController>();if(controller!=null)UnityEngine.Object.DestroyImmediate(controller);
            holder.transform.position=new Vector3(100,0,0);holder.SetActive(true);
            var terrain=copy.GetComponentInChildren<TankGame.Terrain.DestructibleTerrain>();float initial=terrain.SurfaceHeightAt(100);
            terrain.Carve(new Vector2(100,initial),.8f);Check(terrain.SurfaceHeightAt(100)<initial-.7f,"No crater to reset");
            var themes=UnityEngine.Object.FindAnyObjectByType<TankGame.Terrain.TerrainThemeController>();
            if(themes!=null){var snow=themes.Themes[1];terrain.ApplyAppearance(snow.soil,snow.surface);}
            terrain.ResetSurface();Check(terrain.CraterCount==0&&Mathf.Abs(terrain.SurfaceHeightAt(100)-initial)<.001f,"Terrain reset failed");
        }
        finally{UnityEngine.Object.DestroyImmediate(holder);}
    }
}
