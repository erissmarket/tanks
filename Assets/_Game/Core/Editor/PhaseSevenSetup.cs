#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Presentation;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PhaseSevenSetup
{
    private const string Scene="Assets/_Game/Scenes/MVPIntegrated.unity",Pending="PhaseSeven.Verify";
    static PhaseSevenSetup()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);
            EditorApplication.delayCall+=()=>UnityEngine.Object.FindAnyObjectByType<ReleaseQualification>().Begin("Library/PhaseSevenQualification");
        };
    }
    [MenuItem("Tools/Tank Game/Phase Seven/Prepare Delivery Scene %#&F4")]
    public static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Prepare from Edit Mode.");
        if(SceneManager.GetActiveScene().path!=Scene)throw new InvalidOperationException("Open MVPIntegrated before preparing delivery.");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
        var feedback=UnityEngine.Object.FindAnyObjectByType<BattleFeedback>();
        const string materialPath="Assets/_Game/Presentation/Assets/BattleEffects.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,materialPath);}
        var link=new SerializedObject(feedback);link.FindProperty("effectMaterial").objectReferenceValue=material;link.ApplyModifiedProperties();
        if(UnityEngine.Object.FindAnyObjectByType<ReleaseQualification>()==null)new GameObject("Release Qualification").AddComponent<ReleaseQualification>();
        foreach(var diagnostic in UnityEngine.Object.FindObjectsByType<BackendTestDiagnostics>(FindObjectsInactive.Include))diagnostic.enabled=false;
        foreach(var prototype in UnityEngine.Object.FindObjectsByType<BackendTestInput>(FindObjectsInactive.Include))prototype.enabled=false;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.allowedAutorotateToLandscapeLeft=PlayerSettings.allowedAutorotateToLandscapeRight=true;
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        // Keep company/product names so existing local progress retains its original path.
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Directory.CreateDirectory("Builds/Reports");
        File.WriteAllText("Builds/Reports/Platforms.txt",
            "Windows x64 support: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64)+
            "\nAndroid support: "+BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android)+
            "\nAndroid requires Android Build Support and SDK/NDK/JDK; device testing requires an attached authorized device.\n");
        Debug.Log("Phase seven: delivery scene and explicit effect material saved.");
    }
    [MenuItem("Tools/Tank Game/Phase Seven/Verify 168 Combinations %#&F11")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start qualification from Edit Mode.");
        SessionState.SetString("TankGame.Progression.TestPath",Path.GetFullPath("Library/PhaseSevenTests/"+Guid.NewGuid().ToString("N")+"/progress-v1.json"));
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
    [MenuItem("Tools/Tank Game/Phase Seven/Build Windows Test Version %#&F1")]
    public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Windows/TankBattle.exe");
    [MenuItem("Tools/Tank Game/Phase Seven/Build Android Test APK")]
    public static void BuildAndroid()=>Build(BuildTarget.Android,"Builds/Android/TankBattle.apk");
    private static void Build(BuildTarget target,string output)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Build from Edit Mode.");
        Directory.CreateDirectory("Builds/Reports");
        var group=target==BuildTarget.Android?BuildTargetGroup.Android:BuildTargetGroup.Standalone;
        if(!BuildPipeline.IsBuildTargetSupported(group,target))
        {
            File.WriteAllText("Builds/Reports/"+target+".txt","BLOCKED: Unity platform support module is not installed. No output was generated.\n");
            Debug.LogWarning("Build blocked: platform support is not installed for "+target);return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=output,target=target,options=BuildOptions.None});
        var summary=result.summary;
        File.WriteAllText("Builds/Reports/"+target+".txt",$"{summary.result}\nOutput: {summary.outputPath}\nSize: {summary.totalSize}\nDuration: {summary.totalTime}\nErrors: {summary.totalErrors}\nWarnings: {summary.totalWarnings}\n");
        if(summary.result!=BuildResult.Succeeded)throw new Exception("Build failed; see Builds/Reports/"+target+".txt");
        Debug.Log("Built "+summary.outputPath);
    }
}
#endif
