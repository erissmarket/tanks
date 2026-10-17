using System;
using System.IO;
using TankGame.Tank;
using TankGame.Terrain;
using TankGame.Projectiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class CraterAndAimSetup
{
    private const string PendingKey = "CraterAndAimSetup.Pending";
    private const string TankPath = "Assets/_Game/Art/Tanks/TankVisual.prefab";
    private const string ShellPath = "Assets/_Game/Art/Projectiles/BasicShell_Visual.prefab";
    static CraterAndAimSetup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            EditorApplication.delayCall += Verify;
        };
    }

    [MenuItem("Tools/Tank Game/Apply Craters and 90 Degree Aim _F5")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene before applying terrain integration.");
        var root = PrefabUtility.LoadPrefabContents(TankPath);
        try
        {
            var aim = root.GetComponent<TankAim>(); if (aim == null) aim = root.AddComponent<TankAim>();
            var settings = new SerializedObject(aim);
            settings.FindProperty("aimPivot").objectReferenceValue = root.transform.Find("TurretPivot/BarrelPivot");
            settings.FindProperty("minAimAngle").floatValue = 0f;
            settings.FindProperty("maxAimAngle").floatValue = 90f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, TankPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var shell = PrefabUtility.LoadPrefabContents(ShellPath);
        try
        {
            var body = shell.GetComponent<Rigidbody2D>(); if (body == null) body = shell.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 2f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = shell.GetComponent<CircleCollider2D>(); if (collider == null) collider = shell.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f; // Prefab scale is 0.25: 5 cm world radius.
            if (shell.GetComponent<Projectile>() == null) shell.AddComponent<Projectile>();
            PrefabUtility.SaveAsPrefabAsset(shell, ShellPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(shell); }
        var soil = GameObject.Find("Soil Ground");
        var grass = GameObject.Find("Grass Surface");
        if (soil == null || grass == null) throw new InvalidOperationException("Soil and grass objects are required.");
        var terrain = soil.GetComponent<DestructibleTerrain>();
        if (terrain == null) terrain = Undo.AddComponent<DestructibleTerrain>(soil);
        Undo.RecordObject(terrain, "Configure destructible terrain");
        terrain.Configure(grass.GetComponent<SpriteRenderer>());
        EditorUtility.SetDirty(terrain);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Scene save failed.");
        AssetDatabase.SaveAssets();
        Debug.Log("Craters, scorch marks, shell collision and 0-90 degree aim configured and saved.");
    }

    [MenuItem("Tools/Tank Game/Preview/Fire Player Shell _F10")]
    public static void FirePreview()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var tank = GameObject.Find("Player Tank");
        var aim = tank.GetComponent<TankAim>();
        aim.SetAimAngle(45f);
        var pivot = tank.transform.Find("TurretPivot/BarrelPivot");
        Vector2 direction = aim.WorldAimDirection;
        var shell = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ShellPath),
            pivot.position + (Vector3)(direction * 1.05f), Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        var projectile = shell.GetComponent<Projectile>();
        projectile.Initialize(0, 10f, 0.9f, direction * 8f);
        projectile.Completed += _ =>
        {
            var terrain = GameObject.Find("Soil Ground").GetComponent<DestructibleTerrain>();
            File.WriteAllText("Library/CraterProjectilePreview.txt", "Projectile completed; terrain crater count=" + terrain.CraterCount);
            Debug.Log("Projectile impact completed. Craters: " + terrain.CraterCount);
        };
    }

    [MenuItem("Tools/Tank Game/Verify Craters and Aim _F11")]
    public static void Verify()
    {
        if (!EditorApplication.isPlaying)
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
            return;
        }
        var report = new System.Text.StringBuilder();
        try
        {
            foreach (float sign in new[] { 1f, -1f })
            {
                var tank = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TankPath));
                try
                {
                    tank.GetComponent<Rigidbody2D>().simulated = false;
                    tank.transform.position = new Vector3(100, 100, 0);
                    tank.transform.localScale = new Vector3(sign, 1, 1);
                    var aim = tank.GetComponent<TankAim>();
                    aim.SetAimAngle(180f);
                    if (aim.CurrentAimAngle != 90f) throw new Exception("Aim exceeds 90 degrees.");
                    aim.SetAimAngle(-45f);
                    if (aim.CurrentAimAngle != 0f) throw new Exception("Aim goes below forward horizontal.");
                    aim.SetAimAngle(45f);
                    if (aim.WorldAimDirection.x * sign <= 0f || aim.WorldAimDirection.y <= 0f)
                        throw new Exception("Aim points away from the opponent.");
                    aim.SetWorldAimDirection(new Vector2(-sign, 0.2f));
                    if (aim.WorldAimDirection.x * sign < -0.001f) throw new Exception("Backward shot bypasses limits.");
                    report.AppendLine("PASS 0-90 degree aim, upward direction and backward-shot clamp; facing=" + sign);
                }
                finally { UnityEngine.Object.DestroyImmediate(tank); }
            }
            Scene testScene = SceneManager.CreateScene("CraterCheck-" + Guid.NewGuid().ToString("N"), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            try
            {
                var battlefield = UnityEngine.Object.Instantiate(GameObject.Find("MVP Battlefield"));
                SceneManager.MoveGameObjectToScene(battlefield, testScene);
                var terrain = battlefield.GetComponentInChildren<DestructibleTerrain>();
                float before = terrain.SurfaceHeightAt(-2f);
                if (!terrain.Carve(new Vector2(-2f, before), 0.9f)) throw new Exception("No crater created.");
                float after = terrain.SurfaceHeightAt(-2f);
                if (before - after < 0.8f) throw new Exception("Crater is too shallow.");
                var ray = testScene.GetPhysicsScene2D().Raycast(new Vector2(-2f, before + 1f), Vector2.down, 5f);
                if (ray.collider == null || Mathf.Abs(ray.point.y - after) > 0.04f)
                    throw new Exception("Crater collider does not follow the visible surface.");
                var sprite = terrain.GetComponent<SpriteRenderer>().sprite;
                int pixelX = Mathf.FloorToInt((-2f - terrain.transform.position.x) * sprite.pixelsPerUnit + sprite.pivot.x);
                int pixelY = Mathf.Clamp(Mathf.FloorToInt((after - terrain.transform.position.y) * sprite.pixelsPerUnit + sprite.pivot.y) - 3, 0, sprite.texture.height - 1);
                Color dark = sprite.texture.GetPixel(pixelX, pixelY);
                if (dark.a < 0.9f || Mathf.Max(dark.r, Mathf.Max(dark.g, dark.b)) > 0.24f)
                    throw new Exception("Exposed soil is not visibly scorched.");
                if (terrain.Carve(new Vector2(-2f, before), 0f)) throw new Exception("Zero radius changed terrain.");
                float firstFloor = after;
                terrain.Carve(new Vector2(-1.7f, after), 0.65f);
                if (terrain.SurfaceHeightAt(-2f) >= firstFloor) throw new Exception("Overlapping impacts do not deepen terrain.");
                report.AppendLine($"PASS crater depth={before-after:F3}, collider error={Mathf.Abs(ray.point.y-after):F3}, dark soil and overlapping impacts.");
            }
            finally
            {
                foreach (var item in testScene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(item);
                SceneManager.UnloadSceneAsync(testScene);
            }
            report.AppendLine("PASS all crater and aim checks.");
            File.WriteAllText("Library/CraterAndAimVerification.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        catch (Exception error)
        {
            report.AppendLine("FAIL: " + error);
            File.WriteAllText("Library/CraterAndAimVerification.txt", report.ToString());
            Debug.LogException(error);
        }
    }
}
