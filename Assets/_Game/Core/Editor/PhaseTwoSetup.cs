using System;
using System.IO;
using System.Text;
using TankGame.Core;
using TankGame.Data;
using TankGame.Presentation;
using TankGame.Projectiles;
using TankGame.Testing;
using TankGame.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class PhaseTwoSetup
{
    private const string Pending = "PhaseTwo.Verify";
    static PhaseTwoSetup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Verify;
        };
    }
    [MenuItem("Tools/Tank Game/Phase Two/Install Balance and Shields %#F9")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        if(SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")throw new InvalidOperationException("Open MVPIntegrated first.");
        string[] names={"Rocket","HeavyMortar","Railgun","Floater","Bouncy","Cluster","Digger"};
        float[] damage={32,48,38,40,34,0,42};
        float[] radii={1.5f,2.4f,.65f,2f,1.4f,1.2f,1.8f};
        int[] ammo={-1,3,3,2,3,2,2};
        for(int i=0;i<names.Length;i++)
        {
            var weapon=AssetDatabase.LoadAssetAtPath<WeaponData>($"Assets/_Game/Data/Weapons/Weapon_{names[i]}.asset");
            var settings=new SerializedObject(weapon);
            settings.FindProperty("baseDamage").floatValue=damage[i];settings.FindProperty("explosionRadius").floatValue=radii[i];
            settings.FindProperty("ammunition").intValue=ammo[i];settings.FindProperty("minimumDamageFraction").floatValue=.35f;
            settings.FindProperty("shieldPenetration").floatValue=i==2?.5f:0;
            settings.ApplyModifiedProperties();EditorUtility.SetDirty(weapon);
        }
        // Cluster's damage budget belongs entirely to its four children.
        string clusterPath="Assets/_Game/Prefabs/Projectiles/Projectile_Cluster.prefab";
        var clusterRoot=PrefabUtility.LoadPrefabContents(clusterPath);
        try
        {
            var cluster=clusterRoot.GetComponent<ClusterImpactHandler>();cluster.SubmunitionDamage=14;cluster.SubmunitionExplosionRadius=.9f;
            PrefabUtility.SaveAsPrefabAsset(clusterRoot,clusterPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(clusterRoot);}
        var shields=new ShieldData[3];
        string[] shieldNames={"None","Light","Heavy"};float[] capacity={0,30,60};
        for(int i=0;i<3;i++)
        {
            string path=$"Assets/_Game/Data/Shield_{shieldNames[i]}.asset";
            shields[i]=AssetDatabase.LoadAssetAtPath<ShieldData>(path);
            if(shields[i]==null){shields[i]=ScriptableObject.CreateInstance<ShieldData>();AssetDatabase.CreateAsset(shields[i],path);}
            shields[i].displayName=shieldNames[i];shields[i].capacity=capacity[i];shields[i].movementMultiplier=i==2?.75f:1;
            EditorUtility.SetDirty(shields[i]);
        }
        var bootstrap=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();
        var bootSettings=new SerializedObject(bootstrap);var options=bootSettings.FindProperty("shieldOptions");options.arraySize=3;
        for(int i=0;i<3;i++)options.GetArrayElementAtIndex(i).objectReferenceValue=shields[i];
        bootSettings.FindProperty("nextShieldIndex").intValue=1;bootSettings.ApplyModifiedProperties();
        var input=UnityEngine.Object.FindAnyObjectByType<IntegratedInput>();
        var inputSettings=new SerializedObject(input);
        var player=(BackendTestPlayerController)inputSettings.FindProperty("player").objectReferenceValue;
        var health=player.GetComponent<HealthComponent>();
        var hud=UnityEngine.Object.FindAnyObjectByType<IntegratedHud>();var oldHud=new SerializedObject(hud);
        var enemy=(HealthComponent)oldHud.FindProperty("enemyHealth").objectReferenceValue;
        var ai=enemy.GetComponent<TankGame.AI.SimpleArtilleryAI>();
        var basic=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Presentation/Assets/IntegratedBasicWeapon.asset");
        var basicSettings=new SerializedObject(basic);basicSettings.FindProperty("baseDamage").floatValue=32;
        basicSettings.FindProperty("explosionRadius").floatValue=1.5f;basicSettings.FindProperty("minimumDamageFraction").floatValue=.35f;
        basicSettings.FindProperty("ammunition").intValue=-1;basicSettings.ApplyModifiedProperties();
        if(ai!=null){var settings=new SerializedObject(ai);settings.FindProperty("weaponData").objectReferenceValue=basic;settings.ApplyModifiedProperties();}
        var enemyWeapon=new SerializedObject(enemy.GetComponent<WeaponController>());enemyWeapon.FindProperty("currentWeapon").objectReferenceValue=basic;enemyWeapon.ApplyModifiedProperties();
        foreach(var target in new[]{health,enemy})
        {var settings=new SerializedObject(target);settings.FindProperty("shield").objectReferenceValue=shields[1];settings.ApplyModifiedProperties();}
        var canvas=GameObject.Find("MVP HUD").transform;
        var parent=canvas.Find("SafeArea");if(parent==null)parent=canvas.Find("Safe Area");if(parent==null)parent=canvas;
        var existing=parent.Find("Phase Two HUD");var root=existing!=null?existing.gameObject:new GameObject("Phase Two HUD",typeof(RectTransform));
        root.transform.SetParent(parent,false);var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var presentation=root.GetComponent<PhaseTwoHud>();if(presentation==null)presentation=root.AddComponent<PhaseTwoHud>();
        var links=new SerializedObject(presentation);
        Link(links,"input",input);Link(links,"playerWeapon",player.GetComponent<WeaponController>());Link(links,"playerHealth",health);Link(links,"enemyHealth",enemy);
        Link(links,"match",UnityEngine.Object.FindAnyObjectByType<MatchFlowController>());Link(links,"bootstrap",bootstrap);links.ApplyModifiedProperties();
        EditorUtility.SetDirty(bootstrap);EditorUtility.SetDirty(presentation);AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Phase two installed: seven weapon budgets, falloff, ammunition, shields and loadout HUD.");
    }
    private static void Link(SerializedObject settings,string name,UnityEngine.Object value)=>settings.FindProperty(name).objectReferenceValue=value;
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void Near(float actual,float expected,string message)=>Check(Mathf.Abs(actual-expected)<.02f,message+": "+actual+" expected "+expected);
    [MenuItem("Tools/Tank Game/Phase Two/Verify Balance %#F10")]
    public static void Verify()
    {
        if(!EditorApplication.isPlaying){SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();return;}
        var report=new StringBuilder();var root=new GameObject("PhaseTwoTests");var light=ScriptableObject.CreateInstance<ShieldData>();light.capacity=30;
        try
        {
            var target=new GameObject("Target");target.transform.SetParent(root.transform);target.transform.position=new Vector3(100,100,0);
            var body=target.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            target.AddComponent<BoxCollider2D>().size=Vector2.one;
            var health=target.AddComponent<HealthComponent>();health.EquipShield(light);
            health.TakeDamage(20);Near(health.CurrentShield,10,"Shield absorption");Near(health.CurrentHealth,100,"Shield must protect HP");report.AppendLine("PASS shield absorbs damage before health");
            health.TakeDamage(25);Near(health.CurrentHealth,85,"Overflow");Near(health.CurrentShield,0,"Shield break");report.AppendLine("PASS shield breaks and overflow hits health");
            health.ResetHealth();health.ApplyDamage(40,.5f);Near(health.CurrentHealth,80,"Rail penetration HP");Near(health.CurrentShield,10,"Rail penetration shield");report.AppendLine("PASS railgun bypasses half of shield absorption");
            health.ResetHealth();int deaths=0;health.Died+=()=>deaths++;health.Eliminate();health.Eliminate();Check(deaths==1&&health.CurrentHealth==0,"Shield prevented fall death or duplicated death");report.AppendLine("PASS falling bypasses shield and emits death once");
            health.ResetHealth();Near(health.CurrentShield,30,"Shield reset");Near(health.CurrentHealth,100,"HP reset");report.AppendLine("PASS rematch restores shield and health");
            var weaponObject=new GameObject("Weapon");weaponObject.transform.SetParent(root.transform);var controller=weaponObject.AddComponent<WeaponController>();
            var mortar=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Weapon_HeavyMortar.asset");
            controller.AssignWeapon(mortar);
            for(int i=0;i<mortar.Ammunition;i++){var shell=controller.Fire(0,new Vector2(100,120),Vector2.up);Check(shell!=null,"Ammo rejected early");shell.CancelWithoutExplosion();}
            Check(!controller.CanFire&&controller.Fire(0,new Vector2(100,120),Vector2.up)==null,"Empty weapon fired");report.AppendLine("PASS finite ammo exhausts exactly; empty fire creates no shell");
            var rocket=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Weapon_Rocket.asset");controller.AssignWeapon(rocket);controller.AssignWeapon(mortar);Check(controller.RemainingAmmo(mortar)==0,"Switch refilled ammo");
            controller.ResetAmmunition();Check(controller.RemainingAmmo(mortar)==3,"Reset did not refill");report.AppendLine("PASS switching cannot refill ammo; rematch can");
            controller.AssignWeapon(rocket);for(int i=0;i<8;i++)controller.Fire(0,new Vector2(100,120),Vector2.up).CancelWithoutExplosion();Check(controller.RemainingAmmo(rocket)==-1,"Rocket depleted");report.AppendLine("PASS base rocket remains unlimited");
            health.EquipShield(null);health.ResetHealth();Physics2D.SyncTransforms();
            var sample=new GameObject("Falloff Shell");sample.transform.SetParent(root.transform);sample.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
            var projectile=sample.AddComponent<Projectile>();projectile.Initialize(0,40,2,Vector2.zero);projectile.ConfigureDamage(.35f,0);
            projectile.TriggerExplosion(new Vector2(100,100));Near(health.CurrentHealth,60,"Center damage");
            health.ResetHealth();projectile.TriggerExplosion(new Vector2(98,100));Check(Mathf.Abs(health.CurrentHealth-79.5f)<.2f,"Radial falloff outside collider contact tolerance");report.AppendLine("PASS blast falloff uses distance to target collider");
            target.AddComponent<CircleCollider2D>().radius=.25f;health.ResetHealth();projectile.TriggerExplosion(new Vector2(100,100));Near(health.CurrentHealth,60,"Duplicate collider damage");report.AppendLine("PASS multiple colliders receive one damage event");
            health.EquipShield(light);health.ResetHealth();float credit=0;Action<ShotContext,HealthComponent,float> listener=(shot,hp,amount)=>credit+=amount;
            ExplosionDamage.DamageApplied+=listener;
            try{projectile.TriggerExplosion(new Vector2(100,100),20,2);Near(credit,0,"Shield damage credited");projectile.TriggerExplosion(new Vector2(100,100),20,2);Near(credit,10,"Effective HP damage credit");}
            finally{ExplosionDamage.DamageApplied-=listener;}
            report.AppendLine("PASS absorbed shield damage gives no score; actual HP loss does");
            var clusterAsset=AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Weapon_Cluster.asset");controller.AssignWeapon(clusterAsset);
            var clusterShell=controller.Fire(0,new Vector2(100,120),Vector2.down);var cluster=clusterShell.GetComponent<ClusterImpactHandler>();
            cluster.TriggerSplit(new Vector2(100,120),Vector2.up);Check(clusterShell.Damage==0&&cluster.ClusterCount*cluster.SubmunitionDamage==56,"Cluster budget");
            foreach(var child in UnityEngine.Object.FindObjectsByType<Projectile>())if(child.Shot==clusterShell.Shot)Near(child.MinimumDamageFraction,.35f,"Child falloff");
            clusterShell.Shot.Cancel();report.AppendLine("PASS cluster damage capped at four times fourteen; children inherit falloff");
            foreach(string name in new[]{"Rocket","HeavyMortar","Railgun","Floater","Bouncy","Cluster","Digger"})
            {
                var data=AssetDatabase.LoadAssetAtPath<WeaponData>($"Assets/_Game/Data/Weapons/Weapon_{name}.asset");controller.AssignWeapon(data);
                var shell=controller.Fire(0,new Vector2(100,120),Vector2.up);Check(shell!=null&&shell.IsInitialized,"Weapon failed "+name);Near(shell.Damage,data.BaseDamage,"Weapon data transfer");shell.CancelWithoutExplosion();
            }
            report.AppendLine("PASS all seven actual weapon prefabs launch with balanced damage");
            var heavy=AssetDatabase.LoadAssetAtPath<ShieldData>("Assets/_Game/Data/Shield_Heavy.asset");Near(heavy.capacity,60,"Heavy capacity");Near(3*heavy.movementMultiplier,2.25f,"Heavy movement");report.AppendLine("PASS heavy shield trades sixty capacity for 2.25 movement units");
            using(var h=new PhaseOneSetup.Harness())
            {
                h.Match.EndMatch();h.Health[0].EquipShield(heavy);h.Match.StartMatch();Near(h.Match.RemainingMovement,2.25f,"Actual heavy movement budget");
                h.Health[0].TakeDamage(15);h.Pass();h.Pass();Near(h.Health[0].CurrentShield,45,"Shield recharged between turns");
                h.Health[0].transform.position=new Vector3(100,-9,0);h.Resolve();Check(h.Match.EndReason==MatchEndReason.Fall&&h.Match.FinalResult.WinnerPlayerIndex==1,"Shield blocked match fall rule");
            }
            report.AppendLine("PASS actual match applies heavy movement, no turn regeneration, and shield-proof fall elimination");
            using(var h=new PhaseOneSetup.Harness())
            {
                var w=h.Players[0].GetComponent<WeaponController>();w.AssignWeapon(mortar);
                for(int i=0;i<mortar.Ammunition;i++)w.Fire(0,new Vector2(100,120),Vector2.up).CancelWithoutExplosion();
                var actions=h.Root.AddComponent<GameplayActionController>();
                h.Match.EndMatch();actions.Initialize(h.Game,h.Turns,h.Match);
                for(int i=0;i<2;i++)actions.RegisterPlayer(i,h.Players[i],h.Players[i].GetComponent<TankGame.Tank.TankMotor>(),h.Players[i].GetComponent<TankGame.Tank.TankAim>(),h.Players[i].GetComponent<WeaponController>(),h.Players[i].transform);
                h.Match.StartMatch();h.Match.AdvanceSimulation(4);float timer=h.Match.RemainingTurnSeconds;
                Check(actions.Fire(0,Vector2.up)==null&&h.Game.CurrentState==MatchState.TurnActive&&h.Match.RemainingTurnSeconds==timer,"Empty ammo consumed turn or reset timer");
            }
            report.AppendLine("PASS empty-ammo action preserves active turn and remaining timer");
            report.AppendLine("PASS all phase-two checks.");
        }
        catch(Exception error){report.AppendLine("FAIL "+error);Debug.LogException(error);}
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(light);File.WriteAllText("Library/PhaseTwoVerification.txt",report.ToString());Debug.Log(report.ToString());}
    }
}
