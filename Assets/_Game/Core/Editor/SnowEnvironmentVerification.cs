#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using TankGame.Core;
using TankGame.Presentation;
using TankGame.Terrain;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SnowEnvironmentVerification
{
    private const string Pending = "TankGame.VerifySnow";
    private static SnowEnvironment snow;
    private static TerrainThemeController themes;
    private static GameInterface flow;
    private static StringBuilder report;
    private static int stage;
    private static float time;
    private static double due;
    static SnowEnvironmentVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode) EditorApplication.update -= Tick;
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false); EditorApplication.delayCall += Begin;
        };
    }
    [MenuItem("Tools/Tank Game/Snow/Verify Alpine Environment %&F4")]
    public static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Verify from Edit Mode.");
        SessionState.SetString("TankGame.Progression.TestPath", Path.GetFullPath("Library/SnowTests/" + Guid.NewGuid().ToString("N") + "/progress-v1.json"));
        SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); report.AppendLine("PASS " + message); }
    private static void Begin()
    {
        report = new StringBuilder();
        try
        {
            snow = UnityEngine.Object.FindAnyObjectByType<SnowEnvironment>();
            themes = UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>();
            flow = UnityEngine.Object.FindAnyObjectByType<GameInterface>();
            var ai = UnityEngine.Object.FindAnyObjectByType<TankGame.AI.SimpleArtilleryAI>();
            if (ai != null) ai.enabled = false;
            themes.SelectTheme(0);
            var originals = new SpriteRenderer[4];
            string[] names = { "Sky", "Mountains Far", "Mountains Near", "Clouds" };
            for (int i = 0; i < names.Length; i++) originals[i] = GameObject.Find(names[i]).GetComponent<SpriteRenderer>();
            originals[3].enabled = false;
            var terrain = themes.GetComponentInChildren<DestructibleTerrain>();
            var collider = terrain.GetComponent<PolygonCollider2D>();
            var path = collider.GetPath(0);
            themes.SelectTheme(1);
            Check(snow.IsVisible, "Snow scenery appears only on SNOW");
            foreach (var renderer in originals) if (renderer.enabled) throw new Exception("Original backdrop overlaps snow");
            Check(snow.WeatherVertexCount == 1008, "112 soft flakes use one bounded 1008-vertex mesh");
            var root = snow.transform.Find("Snow scenery (visual only)");
            Check(root.GetComponentsInChildren<Collider2D>(true).Length == 0, "Scenery adds no gameplay colliders");
            int children = snow.transform.childCount;
            for (int cycle = 0; cycle < 12; cycle++)
            {
                themes.SelectTheme(cycle % 3);
                if (snow.IsVisible != (cycle % 3 == 1)) throw new Exception("Snow leaked into another theme");
            }
            themes.SelectTheme(2);
            Check(!snow.IsVisible && originals[0].enabled && originals[1].enabled && originals[2].enabled && !originals[3].enabled,
                "Grass/rock restore the exact original background visibility");
            originals[3].enabled = true;
            Check(snow.transform.childCount == children, "Repeated theme switches do not duplicate scenery");
            var current = collider.GetPath(0);
            bool same = current.Length == path.Length;
            for (int i = 0; same && i < path.Length; i++) same &= current[i] == path[i];
            Check(same, "Scenery and theme switches preserve exact collision geometry");
            themes.SelectTheme(1);
            snow.enabled = false;
            Check(!snow.IsVisible && originals[0].enabled, "Disabling snow restores the normal scene");
            snow.enabled = true;
            Check(snow.IsVisible && !originals[0].enabled, "Re-enabling restores snow without duplicate objects");
            flow.ShowPreparation(); flow.StartBattle();
            time = snow.AnimationTime; stage = 0; due = EditorApplication.timeSinceStartup + .6;
            EditorApplication.update += Tick;
        }
        catch (Exception error) { Fail(error); }
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < due) return;
        try
        {
            if (stage == 0)
            {
                Check(snow.AnimationTime > time, "Snowfall animates during active play");
                flow.Pause(); time = snow.AnimationTime; stage++; due = EditorApplication.timeSinceStartup + .6;
            }
            else if (stage == 1)
            {
                Check(snow.AnimationTime == time, "Game pause freezes snowfall");
                flow.Resume(); themes.SelectTheme(0); time = snow.AnimationTime;
                stage++; due = EditorApplication.timeSinceStartup + .6;
            }
            else
            {
                Check(snow.AnimationTime == time, "Other themes do not update snowfall");
                themes.SelectTheme(1);
                TerrainThemeSetup.Verify();
                Check(!File.ReadAllText("Library/TerrainThemeVerification.txt").Contains("FAIL"), "All terrain silhouette, crater and scorch regression checks pass");
                report.AppendLine("PASS all snow environment checks. Test profile isolated; normal save untouched.");
                Finish();
            }
        }
        catch (Exception error) { Fail(error); }
    }
    private static void Fail(Exception error) { report.AppendLine("FAIL " + error); Debug.LogException(error); Finish(); }
    private static void Finish()
    {
        EditorApplication.update -= Tick;
        File.WriteAllText("Library/SnowEnvironmentVerification.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
#endif
