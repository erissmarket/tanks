using System;
using System.IO;
using System.Reflection;
using System.Text;
using TankGame.Core;
using TankGame.Data;
using TankGame.Presentation;
using TankGame.Terrain;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class PhaseFiveSetup
{
    private const string Pending="PhaseFive.Verify";
    static PhaseFiveSetup()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);EditorApplication.delayCall+=Run;
        };
    }
    [MenuItem("Tools/Tank Game/Phase Five/Install Menus and Battle UI %#&F5")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        if(SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")throw new InvalidOperationException("Open MVPIntegrated first.");
        var bootstrap=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();var match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();var input=UnityEngine.Object.FindAnyObjectByType<IntegratedInput>();
        var oldHud=UnityEngine.Object.FindAnyObjectByType<IntegratedHud>();var oldSettings=new SerializedObject(oldHud);
        var player=(HealthComponent)oldSettings.FindProperty("playerHealth").objectReferenceValue;var enemy=(HealthComponent)oldSettings.FindProperty("enemyHealth").objectReferenceValue;
        var garage=UnityEngine.Object.FindAnyObjectByType<GarageHud>();var garageSettings=new SerializedObject(garage);garageSettings.FindProperty("externalNavigation").boolValue=true;garageSettings.ApplyModifiedProperties();
        var canvas=GameObject.Find("MVP HUD").transform;var parent=canvas.Find("SafeArea");if(parent==null)throw new InvalidOperationException("Safe area root missing.");
        var found=parent.Find("Game Interface");var root=found!=null?found.gameObject:new GameObject("Game Interface",typeof(RectTransform));root.SetActive(false);root.transform.SetParent(parent,false);InterfaceLayout.Stretch((RectTransform)root.transform);
        foreach(Transform child in parent)if(child!=root.transform&&child!=garage.transform)child.gameObject.SetActive(false);
        oldHud.enabled=false;
        var flow=root.GetComponent<GameInterface>();if(flow==null)flow=root.AddComponent<GameInterface>();var links=new SerializedObject(flow);
        Link(links,"bootstrap",bootstrap);Link(links,"match",match);Link(links,"input",input);Link(links,"garage",garage);Link(links,"themes",UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>());
        Link(links,"playerHealth",player);Link(links,"enemyHealth",enemy);Link(links,"weapon",player.GetComponent<TankGame.Weapons.WeaponController>());links.ApplyModifiedProperties();
        string[] names={"Rocket","HeavyMortar","Railgun","Floater","Bouncy","Cluster","Digger"};
        string[] descriptions={"Reliable, unlimited shots.","Wide blast. Limited ammunition.","Precise. Half bypasses shields.","Slow flight. Large blast.","Bounces before exploding.","Four fragments cover the ground.","Drills before detonating."};
        for(int i=0;i<names.Length;i++)
        {
            var data=AssetDatabase.LoadAssetAtPath<WeaponData>($"Assets/_Game/Data/Weapons/Weapon_{names[i]}.asset");var settings=new SerializedObject(data);settings.FindProperty("description").stringValue=descriptions[i];settings.ApplyModifiedProperties();EditorUtility.SetDirty(data);
        }
        var bootSettings=new SerializedObject(bootstrap);bootSettings.FindProperty("startInGarage").boolValue=true;bootSettings.ApplyModifiedProperties();root.SetActive(true);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Phase five installed: home, battle setup, settings, guide, touch battle HUD, pause, leave confirmation and result.");
    }
    private static void Link(SerializedObject settings,string name,UnityEngine.Object value)=>settings.FindProperty(name).objectReferenceValue=value;
    [MenuItem("Tools/Tank Game/Phase Five/Verify Navigation and Layout %#&F6")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run from Edit Mode to use a fresh isolated profile.");
        string folder=Path.GetFullPath("Library/PhaseFiveTests/"+Guid.NewGuid().ToString("N"));
        SessionState.SetString("TankGame.Progression.TestPath",Path.Combine(folder,"progress-v1.json"));SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Near(float value,float expected,string message)=>Check(Mathf.Abs(value-expected)<.02f,message+": "+value+" expected "+expected);
    private static void Case(StringBuilder report,string title,Action test){test();report.AppendLine("PASS "+title);}
    private static void Run()
    {
        var report=new StringBuilder();var flow=UnityEngine.Object.FindAnyObjectByType<GameInterface>();var match=UnityEngine.Object.FindAnyObjectByType<MatchFlowController>();var input=UnityEngine.Object.FindAnyObjectByType<IntegratedInput>();var bootstrap=UnityEngine.Object.FindAnyObjectByType<BackendTestBootstrap>();var garage=UnityEngine.Object.FindAnyObjectByType<GarageHud>();var progress=bootstrap.Progression;
        try
        {
            Case(report,"launch shows home with controls blocked and no match reserved",()=>{Check(flow.IsBuilt&&flow.CurrentScreen==InterfaceScreen.Home&&!match.IsRunning&&input.UiBlocked&&string.IsNullOrEmpty(progress.Store.Snapshot.pendingMatchId),"Launch route started a match");});
            Case(report,"battle setup and back never spend a turn or start a match",()=>{flow.ShowPreparation();Check(flow.CurrentScreen==InterfaceScreen.Preparation&&!match.IsRunning,"Setup started battle");flow.Back();Check(flow.CurrentScreen==InterfaceScreen.Home&&match.MatchNumber==0,"Back changed match");});
            Case(report,"garage from home returns home before the first match",()=>{flow.ShowGarage();Check(garage.IsOpen,"Garage did not open");garage.Close();Check(flow.CurrentScreen==InterfaceScreen.Home&&!garage.IsOpen&&!match.IsRunning,"Garage back lost route");});
            Case(report,"garage setup action routes through preparation rather than firing",()=>{flow.ShowGarage();garage.Begin();Check(flow.CurrentScreen==InterfaceScreen.Preparation&&!match.IsRunning,"Garage bypassed setup");});
            Case(report,"all seven starting weapons can be selected and invalid index is rejected",()=>{for(int i=0;i<input.AvailableWeapons.Length;i++)Check(flow.SelectStartingWeapon(i)&&flow.StartingWeaponIndex==i,"Weapon selection failed");Check(!flow.SelectStartingWeapon(-1)&&!flow.SelectStartingWeapon(7),"Invalid weapon index");flow.SelectStartingWeapon(5);});
            Case(report,"preparation environment and starting weapon persist in the profile",()=>{flow.SelectTheme(1);Check(progress.Store.Snapshot.selectedTheme==1&&progress.Store.Snapshot.startingWeapon==input.AvailableWeapons[5].WeaponId&&!match.IsRunning,"Setup preference failed");});
            Case(report,"settings sound toggle and help return to home",()=>{flow.Home();flow.ShowSettings();bool sound=progress.Store.SoundEnabled;Check(progress.ToggleSound()&&progress.Store.SoundEnabled!=sound,"Sound preference failed");flow.Back();Check(flow.CurrentScreen==InterfaceScreen.Home,"Settings return");flow.ShowHelp();flow.Back();Check(flow.CurrentScreen==InterfaceScreen.Home,"Guide return");});
            Case(report,"start uses chosen weapon and double activation cannot reserve another match",()=>{flow.ShowPreparation();flow.StartBattle();string id=progress.Store.Snapshot.pendingMatchId;int count=match.MatchNumber;flow.StartBattle();Check(match.IsRunning&&!input.UiBlocked&&input.CanAct&&input.CurrentWeapon==input.AvailableWeapons[5]&&match.MatchNumber==count&&progress.Store.Snapshot.pendingMatchId==id,"Start or duplicate activation failed");});
            Case(report,"pause freezes timer and blocks fire; resume preserves remaining time",()=>{match.AdvanceSimulation(3);float time=match.RemainingTurnSeconds;flow.Pause();input.Fire();match.AdvanceSimulation(100);Check(match.IsPaused&&input.UiBlocked&&match.ActiveShotProjectileCount==0,"Pause accepted input");Near(match.RemainingTurnSeconds,time,"Pause timer");flow.Resume();Check(flow.CurrentScreen==InterfaceScreen.Battle&&input.CanAct&&!match.IsPaused,"Resume failed");});
            Case(report,"garage and settings opened during pause cannot resume gameplay on back",()=>{flow.Pause();flow.ShowGarage();garage.Close();Check(flow.CurrentScreen==InterfaceScreen.Pause&&match.IsPaused&&input.UiBlocked,"Garage resumed match");flow.ShowSettings();flow.Back();Check(flow.CurrentScreen==InterfaceScreen.Pause&&match.IsPaused,"Settings resumed match");});
            Case(report,"cancel leaving keeps the current match paused and unrewarded",()=>{flow.RequestLeave();flow.Back();Check(flow.CurrentScreen==InterfaceScreen.Pause&&match.IsRunning&&match.IsPaused&&progress.Store.Coins==0,"Cancel leave ended match");});
            Case(report,"confirm leaving returns home as surrender with zero reward",()=>{flow.RequestLeave();flow.ConfirmLeave();Check(flow.CurrentScreen==InterfaceScreen.Home&&!match.IsRunning&&match.EndReason==MatchEndReason.Surrender&&progress.LastReward==0&&progress.Store.Coins==0,"Leave reward or route wrong");});
            Case(report,"portrait orientation pauses a battle and returning landscape requires resume",()=>{flow.ShowPreparation();flow.StartBattle();float time=match.RemainingTurnSeconds;flow.HandleOrientation(true);Check(flow.CurrentScreen==InterfaceScreen.Rotate&&match.IsPaused&&input.UiBlocked,"Portrait did not guard battle");flow.Back();Check(flow.CurrentScreen==InterfaceScreen.Rotate,"Back bypassed rotation");flow.HandleOrientation(false);Check(flow.CurrentScreen==InterfaceScreen.Pause&&match.IsPaused,"Rotation auto-resumed");Near(match.RemainingTurnSeconds,time,"Rotation timer");flow.Resume();});
            Case(report,"UI blocking rejects held touch and fire even during an authorized turn",()=>{input.SetUiBlocked(true);input.Hold(0,true);input.Fire();Check(!input.CanAct&&!input.CanFire&&match.ActiveShotProjectileCount==0&&match.State==MatchState.TurnActive,"Overlay input leaked");input.SetUiBlocked(false);});
            Case(report,"multitouch hold releases only after all pointers lift and clears on disable",()=>
            {
                var holder=new GameObject("TouchHoldTest");var button=holder.AddComponent<IntegratedHoldButton>();button.Configure(input,0);
                var first=new PointerEventData(EventSystem.current){pointerId=10,button=PointerEventData.InputButton.Left};var second=new PointerEventData(EventSystem.current){pointerId=11,button=PointerEventData.InputButton.Left};
                var field=typeof(IntegratedInput).GetField("held",BindingFlags.NonPublic|BindingFlags.Instance);
                try{button.OnPointerDown(first);button.OnPointerDown(second);button.OnPointerUp(first);Check(((bool[])field.GetValue(input))[0],"First touch released the second");button.OnPointerUp(second);Check(!((bool[])field.GetValue(input))[0],"Last touch did not release");button.OnPointerDown(first);holder.SetActive(false);Check(!((bool[])field.GetValue(input))[0],"Disabled hold stuck");}
                finally{UnityEngine.Object.DestroyImmediate(holder);}
            });
            Case(report,"actual match result opens result screen and persists one reward",()=>
            {
                var fields=new SerializedObject(flow);var enemy=(HealthComponent)fields.FindProperty("enemyHealth").objectReferenceValue;enemy.Eliminate();match.AdvanceSimulation(.01f);match.AdvanceSimulation(2);
                Check(flow.CurrentScreen==InterfaceScreen.Result&&!match.IsRunning&&input.UiBlocked&&progress.Store.Wins==1&&progress.Store.Coins==110,"Result screen or reward missing");
            });
            Case(report,"garage return after result and play again do not duplicate prior rewards",()=>{flow.ShowGarage();garage.Close();Check(flow.CurrentScreen==InterfaceScreen.Result,"Garage lost result route");flow.StartBattle();Check(flow.CurrentScreen==InterfaceScreen.Battle&&match.IsRunning&&match.CompletedTurns==0&&progress.Store.Coins==110,"Play again duplicated reward");Near(match.RemainingTurnSeconds,30,"Rematch timer");flow.Pause();flow.RequestLeave();flow.ConfirmLeave();});
            Case(report,"cluster presentation shows children damage and radius",()=>{var cluster=input.AvailableWeapons[5];Check(WeaponPresentation.Damage(cluster)=="4 x 14", "Cluster display damage");Near(WeaponPresentation.Radius(cluster),.9f,"Cluster display radius");foreach(var data in input.AvailableWeapons)Check(data.Icon!=null&&!string.IsNullOrEmpty(data.Description),"Missing weapon explanation/icon");});
            Case(report,"safe area normalizes notch insets and rejects zero or invalid dimensions",()=>{var rect=HudSafeArea.Normalize(new Rect(96,32,2200,1000),new Vector2(2400,1080));Near(rect.xMin,.04f,"Notch inset");Near(rect.yMin,32f/1080,"Bottom inset");Check(rect.xMax<1&&rect.yMax<1,"Safe area escaped screen");Check(HudSafeArea.Normalize(Rect.zero,Vector2.zero)==new Rect(0,0,1,1),"Zero screen safe area");});
            Case(report,"menus and touch docks fit landscape phones and tablet aspect ratios",()=>
            {
                foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080),new Vector2(2340,1080),new Vector2(1024,768)})
                {
                    float canvasScale=Mathf.Sqrt(size.x/1920f*size.y/1080f);Vector2 logical=size/canvasScale;
                    foreach(var design in new[]{new Vector2(1680,980),new Vector2(1540,960),new Vector2(1824,230),new Vector2(1824,218)})
                    {float fit=FitInterfacePanel.ScaleFor(logical,design,32);Check(design.x*fit<=logical.x-63.9f&&design.y*fit<=logical.y-63.9f,"Panel clipped at "+size);}
                    float dock=FitInterfacePanel.ScaleFor(logical,new Vector2(1824,218),32);Check(120*dock*canvasScale>=48,"Touch target too small at "+size);
                }
            });
            report.AppendLine("PASS all phase-five checks.");
        }
        catch(Exception error){report.AppendLine("FAIL "+error);Debug.LogException(error);}
        finally{File.WriteAllText("Library/PhaseFiveVerification.txt",report.ToString());Debug.Log(report.ToString());}
    }
}
