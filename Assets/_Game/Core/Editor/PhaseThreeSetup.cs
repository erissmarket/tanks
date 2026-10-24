using System;
using System.IO;
using System.Text;
using TankGame.Core;
using TankGame.Data;
using TankGame.Presentation;
using TankGame.Tank;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class PhaseThreeSetup
{
    private const string Pending="PhaseThree.Verify";
    static PhaseThreeSetup()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Verify;
        };
    }
    [MenuItem("Tools/Tank Game/Phase Three/Install Tanks and Garage %#F11")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        if(SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")throw new InvalidOperationException("Open MVPIntegrated first.");
        if(!AssetDatabase.IsValidFolder("Assets/_Game/Data/Tanks"))AssetDatabase.CreateFolder("Assets/_Game/Data","Tanks");
        if(!AssetDatabase.IsValidFolder("Assets/_Game/Art/Tanks/Variants"))AssetDatabase.CreateFolder("Assets/_Game/Art/Tanks","Variants");
        var definitions=new TankDefinition[3];string[] names={"Scout","Vanguard","Bastion"};
        float[] health={80,100,140},armor={0,.1f,.2f},speed={3.8f,3,2.2f},movement={1.4f,1,2.2f/3};
        Color[] colors={new Color(.22f,.65f,.63f),new Color(.32f,.64f,.25f),new Color(.76f,.56f,.27f)};
        string[] descriptions={"Fast / light protection","Balanced / flexible equipment","Armored / slower movement"};
        for(int i=0;i<3;i++)
        {
            string path=$"Assets/_Game/Data/Tanks/Tank_{names[i]}.asset";
            var tank=AssetDatabase.LoadAssetAtPath<TankDefinition>(path);
            if(tank==null){tank=ScriptableObject.CreateInstance<TankDefinition>();AssetDatabase.CreateAsset(tank,path);}
            tank.tankId=names[i].ToLowerInvariant();tank.displayName=names[i];tank.description=descriptions[i];
            tank.maxHealth=health[i];tank.armorReduction=armor[i];tank.speed=speed[i];tank.movementMultiplier=movement[i];tank.shieldCapacity=i==0?30:60;tank.color=colors[i];
            if(i==1)
            {
                tank.bodySprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Tanks/TankBody.png");
                tank.turretSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Tanks/TankTurret.png");
            }
            else
            {
                int variant=i;
                tank.bodySprite=Sprite(names[i]+"Body",150,45,(x,y)=>
                {
                    float inset=variant==0?Mathf.Max(0,y-16)*1.35f:Mathf.Max(0,y-33)*.35f;
                    bool inside=x>=inset&&x<150-inset;
                    return inside?((x<inset+2||x>147-inset||y<2||y>42)?new Color32(125,145,125,255):new Color32(255,255,255,255)):new Color32();
                });
                tank.turretSprite=Sprite(names[i]+"Turret",76,34,(x,y)=>
                {
                    float inset=variant==0?Mathf.Max(0,y-10)*1.4f:Mathf.Max(0,y-27)*.4f;
                    bool inside=x>=inset&&x<76-inset;
                    return inside?((x<inset+2||x>73-inset||y<2||y>31)?new Color32(125,145,125,255):new Color32(255,255,255,255)):new Color32();
                });
            }
            int style=i;Color tint=colors[i];
            tank.thumbnail=Sprite(names[i]+"Thumbnail",320,150,(x,y)=>
            {
                Color32 clear=new Color32();
                float wheelX=Mathf.Clamp(x,47,254);
                if(y>=15&&y<=53&&(new Vector2(x-wheelX,y-34)).sqrMagnitude<=19*19)
                {
                    foreach(float wheel in new[]{57f,111f,166f,220f})if(new Vector2(x-wheel,y-34).sqrMagnitude<12*12)return new Color32(84,96,99,255);
                    return new Color32(42,49,52,255);
                }
                float inset=style==0?Mathf.Max(0,y-61)*1.1f:style==1?Mathf.Max(0,y-77)*.8f:Mathf.Max(0,y-87)*.3f;
                if(y>=49&&y<99&&x>=38+inset&&x<274-inset)return tint;
                if(y>=95&&y<130&&x>=92+(style==0?(y-95)*.7f:0)&&x<207-(style==0?(y-95)*.7f:0))return tint*new Color(.9f,.9f,.9f,1);
                if(x>=196&&x<302&&y>=108&&y<120)return tint;
                return clear;
            });
            tank.Validate();EditorUtility.SetDirty(tank);definitions[i]=tank;
        }
        var bootstrap=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();var settings=new SerializedObject(bootstrap);
        var options=settings.FindProperty("tankOptions");options.arraySize=3;for(int i=0;i<3;i++)options.GetArrayElementAtIndex(i).objectReferenceValue=definitions[i];
        settings.FindProperty("nextTankIndex").intValue=1;settings.FindProperty("opponentTank").objectReferenceValue=definitions[1];settings.FindProperty("startInGarage").boolValue=true;settings.ApplyModifiedProperties();
        var parent=GameObject.Find("MVP HUD").transform;var safe=parent.Find("SafeArea");if(safe==null)safe=parent.Find("Safe Area");if(safe!=null)parent=safe;
        var found=parent.Find("Garage HUD");var root=found!=null?found.gameObject:new GameObject("Garage HUD",typeof(RectTransform));root.transform.SetParent(parent,false);
        var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var garage=root.GetComponent<GarageHud>();if(garage==null)garage=root.AddComponent<GarageHud>();
        var links=new SerializedObject(garage);links.FindProperty("bootstrap").objectReferenceValue=bootstrap;links.FindProperty("match").objectReferenceValue=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();links.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Phase three installed: three tank definitions, armor, class silhouettes/thumbnails, and pre-match garage.");
    }
    private static Sprite Sprite(string name,int width,int height,Func<int,int,Color32> color)
    {
        string path=$"Assets/_Game/Art/Tanks/Variants/{name}.png";var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);var pixels=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=color(x,y);
        texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Near(float value,float expected,string message)=>Check(Mathf.Abs(value-expected)<.02f,message+": "+value+" expected "+expected);
    [MenuItem("Tools/Tank Game/Phase Three/Verify Tanks and Garage %#&F12")]
    public static void Verify()
    {
        if(!EditorApplication.isPlaying){SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();return;}
        var report=new StringBuilder();
        try
        {
            var scout=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Scout.asset");
            var balanced=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Vanguard.asset");
            var heavy=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Bastion.asset");
            var light=AssetDatabase.LoadAssetAtPath<ShieldData>("Assets/_Game/Data/Shield_Light.asset");var shield=AssetDatabase.LoadAssetAtPath<ShieldData>("Assets/_Game/Data/Shield_Heavy.asset");
            foreach(var tank in new[]{scout,balanced,heavy})Check(tank!=null&&tank.thumbnail!=null&&tank.bodySprite!=null&&tank.turretSprite!=null,"Missing tank art/data");
            Check(scout.bodySprite!=heavy.bodySprite&&scout.thumbnail!=heavy.thumbnail,"Tank silhouettes identical");report.AppendLine("PASS all three tank definitions have distinct thumbnails and class art");
            Check(!scout.Supports(shield)&&scout.Supports(light)&&heavy.Supports(shield),"Equipment capacity");report.AppendLine("PASS scout rejects heavy shield; other classes support it");
            using(var h=new PhaseOneSetup.Harness())
            {
                h.Match.EndMatch();var loadout=h.Players[0].gameObject.AddComponent<TankLoadout>();loadout.Apply(scout,light,false);h.Match.StartMatch();
                Near(h.Health[0].MaxHealth,80,"Scout HP");Near(h.Match.RemainingMovement,4.2f,"Scout movement");Near(h.Players[0].GetComponent<TankMotor>().MovementSpeed,3.8f,"Scout speed");report.AppendLine("PASS scout applies 80 HP, 3.8 speed and 4.2 movement in actual match");
                bool rejected=false;try{loadout.Apply(scout,shield,false);}catch(InvalidOperationException){rejected=true;}Check(rejected&&h.Health[0].MaxShield==30,"Invalid equipment changed loadout");report.AppendLine("PASS incompatible shield is rejected without partial loadout changes");
                h.Match.EndMatch();loadout.Apply(heavy,shield,false);h.Match.StartMatch();Near(h.Health[0].MaxHealth,140,"Heavy HP");Near(h.Match.RemainingMovement,1.65f,"Combined heavy tank and shield movement");report.AppendLine("PASS heavy tank and heavy shield multipliers compose to 1.65 movement");
                h.Health[0].ApplyDamage(80,0);Near(h.Health[0].CurrentHealth,124,"Shield then armor");Near(h.Health[0].CurrentShield,0,"Shield exhausted");report.AppendLine("PASS sixty shield absorbs first, then twenty-percent armor reduces overflow");
                h.Health[0].ResetHealth();h.Health[0].ApplyDamage(40,.5f);Near(h.Health[0].CurrentHealth,124,"Rail versus armor");Near(h.Health[0].CurrentShield,40,"Rail shield");report.AppendLine("PASS rail bypasses half shield while armor still protects hull");
                h.Health[0].transform.position=new Vector3(100,-9,0);h.Resolve();Check(h.Match.EndReason==MatchEndReason.Fall&&h.Health[0].CurrentHealth==0,"Armor prevented fall");report.AppendLine("PASS falling eliminates armored shielded tank");
                h.Health[0].transform.position=new Vector3(100,100,0);h.Health[0].ResetHealth();loadout.Apply(scout,light,false);h.Match.StartMatch();Near(h.Health[0].ArmorReduction,0,"Stale armor");Near(h.Health[0].CurrentHealth,80,"Stale HP");report.AppendLine("PASS heavy-to-light rematch clears old armor and health capacity");
            }
            using(var h=new PhaseOneSetup.Harness())
            {
                h.Health[1].ConfigureTank(100,.1f);h.Health[1].EquipShield(light);var shell=h.Shell(null,0,50);h.Match.NotifyProjectileFired(shell);shell.TriggerExplosion(h.Health[1].transform.position);
                Near(h.Health[1].CurrentHealth,82,"Armor HP");Check(h.Match.GetScore(0)==18,"Score includes blocked damage");shell.CompleteAndDestroy();report.AppendLine("PASS score counts eighteen actual HP after shield and armor");
            }
            using(var h=new PhaseOneSetup.Harness(2))
            {
                h.Match.EndMatch();h.Health[0].ConfigureTank(80,0);h.Health[1].ConfigureTank(140,0);h.Match.StartMatch();h.Health[0].TakeDamage(16);h.Health[1].TakeDamage(42);h.Pass();h.Pass();
                Check(h.Match.FinalResult.WinnerPlayerIndex==0,"Turn cap compared raw HP");report.AppendLine("PASS turn cap compares HP percentage across different tank capacities");
            }
            var sourceBootstrap=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();
            var selectionRoot=new GameObject("IsolatedGarageSelection");selectionRoot.SetActive(false);
            var bootstrap=selectionRoot.AddComponent<BackendTestBootstrap>();var selectionSettings=new SerializedObject(bootstrap);
            var tankOptions=selectionSettings.FindProperty("tankOptions");tankOptions.arraySize=sourceBootstrap.TankOptions.Length;
            for(int i=0;i<tankOptions.arraySize;i++)tankOptions.GetArrayElementAtIndex(i).objectReferenceValue=sourceBootstrap.TankOptions[i];
            var shieldOptions=selectionSettings.FindProperty("shieldOptions");shieldOptions.arraySize=sourceBootstrap.ShieldOptions.Length;
            for(int i=0;i<shieldOptions.arraySize;i++)shieldOptions.GetArrayElementAtIndex(i).objectReferenceValue=sourceBootstrap.ShieldOptions[i];
            selectionSettings.ApplyModifiedProperties();
            try
            {
                bootstrap.SelectNextTank(2);Check(bootstrap.SelectNextShield(2),"Heavy selection rejected");bootstrap.SelectNextTank(0);
                Check(bootstrap.NextTank==scout&&bootstrap.NextShield==light&&!bootstrap.SelectNextShield(2),"Garage allowed incompatible equipment");report.AppendLine("PASS garage downgrades incompatible shield and rejects impossible selection");
            }
            finally{UnityEngine.Object.DestroyImmediate(selectionRoot);}
            VerifySpeed(scout,heavy);report.AppendLine("PASS local physics simulation confirms scout travels farther than heavy at equal input time");
            report.AppendLine("PASS all phase-three checks.");
        }
        catch(Exception error){report.AppendLine("FAIL "+error);Debug.LogException(error);}
        finally{File.WriteAllText("Library/PhaseThreeVerification.txt",report.ToString());Debug.Log(report.ToString());}
    }
    private static void VerifySpeed(TankDefinition scout,TankDefinition heavy)
    {
        var scene=SceneManager.CreateScene("TankClassPhysics-"+Guid.NewGuid().ToString("N"),new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        try
        {
            var floor=new GameObject("Floor");SceneManager.MoveGameObjectToScene(floor,scene);floor.AddComponent<BoxCollider2D>().size=new Vector2(40,1);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Tanks/TankVisual.prefab");var motors=new TankMotor[2];var bodies=new Rigidbody2D[2];
            for(int i=0;i<2;i++)
            {
                var tank=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);tank.transform.position=new Vector3(i==0?-10:0,.6f,0);motors[i]=tank.GetComponent<TankMotor>();bodies[i]=tank.GetComponent<Rigidbody2D>();
                motors[i].ConfigureSpeed(i==0?scout.speed:heavy.speed);
            }
            Physics2D.SyncTransforms();var step=typeof(TankMotor).GetMethod("FixedUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var physics=scene.GetPhysicsScene2D();
            for(int frame=0;frame<100;frame++){foreach(var motor in motors)step.Invoke(motor,null);physics.Simulate(Time.fixedDeltaTime);}
            float x0=bodies[0].position.x,x1=bodies[1].position.x;
            for(int frame=0;frame<55;frame++){foreach(var motor in motors){motor.Move(1);step.Invoke(motor,null);}physics.Simulate(Time.fixedDeltaTime);}
            Check(bodies[0].position.x-x0>bodies[1].position.x-x1+.5f,"Class speeds did not affect actual movement");
        }
        finally{foreach(var go in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(go);SceneManager.UnloadSceneAsync(scene);}
    }
}
