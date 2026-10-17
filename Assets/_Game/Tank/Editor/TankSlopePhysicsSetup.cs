using System;
using System.IO;
using System.Reflection;
using TankGame.Tank;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class TankSlopePhysicsSetup
{
    private const string PendingKey = "TankSlopePhysicsSetup.Pending";
    static TankSlopePhysicsSetup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            EditorApplication.delayCall += Verify;
        };
    }

    [MenuItem("Tools/Tank Game/Preview/Drive Player Right _F6")]
    public static void DriveRight() => Drive(1f);
    [MenuItem("Tools/Tank Game/Preview/Drive Player Left _F7")]
    public static void DriveLeft() => Drive(-1f);
    [MenuItem("Tools/Tank Game/Preview/Stop Player _F9")]
    public static void StopPlayer() => Drive(0f);
    private static void Drive(float direction)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode for the movement preview.");
        var tank = GameObject.Find("Player Tank");
        if (tank == null || !tank.TryGetComponent<TankMotor>(out var motor))
            throw new InvalidOperationException("Player Tank with TankMotor is required.");
        if (direction == 0f) motor.Stop(); else motor.Move(direction);
    }

    private const string PrefabPath = "Assets/_Game/Art/Tanks/TankVisual.prefab";

    [MenuItem("Tools/Tank Game/Apply Slope Physics and Verify")]
    public static void ApplyAndVerify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before configuring tank physics.");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var body = root.GetComponent<Rigidbody2D>() ?? root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = 4f;
            body.gravityScale = 2f;
            body.constraints = RigidbodyConstraints2D.None;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.angularDamping = 5f;
            var collider = root.GetComponent<CapsuleCollider2D>() ?? root.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(1.7f, 0.38f);
            collider.offset = new Vector2(0f, 0.19f);
            const string materialPath = "Assets/_Game/Art/Tanks/TankTracks.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("TankTracks");
                AssetDatabase.CreateAsset(material, materialPath);
            }
            // The motor provides traction and braking; avoid friction cancelling slow acceleration.
            material.friction = 0f;
            material.bounciness = 0f;
            EditorUtility.SetDirty(material);
            collider.sharedMaterial = material;
            if (root.GetComponent<TankMotor>() == null) root.AddComponent<TankMotor>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Verify();
    }

    [MenuItem("Tools/Tank Game/Verify Slope Physics _F8")]
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
            CheckSlope(0f, false, report);
            CheckSlope(20f, false, report);
            CheckSlope(-20f, false, report);
            CheckSlope(20f, true, report);
            CheckFall(report);
            report.AppendLine("PASS: all physics checks completed in isolated 2D physics scenes.");
            File.WriteAllText("Library/TankSlopePhysicsVerification.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        catch (Exception error)
        {
            report.AppendLine("FAIL: " + error);
            File.WriteAllText("Library/TankSlopePhysicsVerification.txt", report.ToString());
            Debug.LogException(error);
            throw;
        }
    }

    private static void CheckSlope(float angle, bool mirrored, System.Text.StringBuilder report)
    {
        Scene scene = SceneManager.CreateScene("TankSlopeCheck-" + Guid.NewGuid().ToString("N"), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        try
        {
            var ground = new GameObject("Test slope");
            SceneManager.MoveGameObjectToScene(ground, scene);
            var box = ground.AddComponent<BoxCollider2D>();
            box.size = new Vector2(30f, 1f);
            ground.transform.rotation = Quaternion.Euler(0, 0, angle);
            var tank = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            tank.transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            float rad = angle * Mathf.Deg2Rad;
            tank.transform.position = new Vector3(0, 0.5f / Mathf.Cos(rad) + 0.15f, 0);
            var motor = tank.GetComponent<TankMotor>();
            var body = tank.GetComponent<Rigidbody2D>();
            motor.SendMessage("Awake");
            Physics2D.SyncTransforms();
            var step = typeof(TankMotor).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            var physics = scene.GetPhysicsScene2D();
            for (int i = 0; i < 150; i++) { step.Invoke(motor, null); physics.Simulate(Time.fixedDeltaTime); }
            float startX = body.position.x;
            motor.Move(1f);
            float firstSpeed = 0f;
            for (int i = 0; i < 100; i++)
            {
                step.Invoke(motor, null);
                physics.Simulate(Time.fixedDeltaTime);
                if (i == 0) firstSpeed = body.linearVelocity.magnitude;
            }
            float distance = body.position.x - startX;
            if (distance < 2f) throw new Exception("Tank failed to traverse slope " + angle + "; distance=" + distance);
            if (!motor.IsGrounded) throw new Exception("Tank lost ground on slope " + angle);
            if (Mathf.Abs(Mathf.DeltaAngle(body.rotation, angle)) > 6f)
                throw new Exception("Incorrect body angle on slope " + angle + ": " + body.rotation);
            if (firstSpeed > 1f) throw new Exception("Movement accelerates too abruptly.");
            motor.Move(0f);
            for (int i = 0; i < 80; i++) { step.Invoke(motor, null); physics.Simulate(Time.fixedDeltaTime); }
            Vector2 tangent = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            if (Mathf.Abs(Vector2.Dot(body.linearVelocity, tangent)) > 0.2f)
                throw new Exception("Tank fails to brake on slope " + angle);
            motor.Stop();
            report.AppendLine($"PASS slope={angle}, mirrored={mirrored}, distance={distance:F2}, rotation={body.rotation:F2}, firstSpeed={firstSpeed:F2}");
        }
        finally { foreach (var item in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(item); SceneManager.UnloadSceneAsync(scene); }
    }

    private static void CheckFall(System.Text.StringBuilder report)
    {
        Scene scene = SceneManager.CreateScene("TankFallCheck", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        try
        {
            var tank = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            var motor = tank.GetComponent<TankMotor>();
            var body = tank.GetComponent<Rigidbody2D>();
            motor.SendMessage("Awake");
            var step = typeof(TankMotor).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 30; i++) { step.Invoke(motor, null); scene.GetPhysicsScene2D().Simulate(Time.fixedDeltaTime); }
            if (motor.IsGrounded || body.position.y > -1f) throw new Exception("Airborne tank does not fall naturally.");
            float before = body.linearVelocity.y;
            motor.Stop();
            if (Mathf.Abs(body.linearVelocity.y - before) > 0.001f) throw new Exception("Stop cancels gravity.");
            report.AppendLine("PASS airborne gravity and Stop preserves falling velocity.");
        }
        finally { foreach (var item in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(item); SceneManager.UnloadSceneAsync(scene); }
    }
}




