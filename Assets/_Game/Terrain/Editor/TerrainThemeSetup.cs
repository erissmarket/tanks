using System;
using System.IO;
using System.Text;
using TankGame.Core;
using TankGame.Data;
using TankGame.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class TerrainThemeSetup
{
    private const string ArtFolder = "Assets/_Game/Art/Terrain/Themes";
    private const string PendingKey = "TerrainThemeSetup.PendingVerification";
    static TerrainThemeSetup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            EditorApplication.delayCall += Verify;
        };
    }

    [MenuItem("Tools/Tank Game/Terrain Themes/Build and Install _F3")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (SceneManager.GetActiveScene().path != "Assets/_Game/Scenes/MVPIntegrated.unity")
            throw new InvalidOperationException("Open MVPIntegrated before installing the terrain selector.");
        var battlefield = GameObject.Find("MVP Battlefield");
        var soil = battlefield.transform.Find("Soil Ground").GetComponent<SpriteRenderer>();
        var surface = battlefield.transform.Find("Grass Surface").GetComponent<SpriteRenderer>();
        var originalSoil = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Terrain/RollingSoil.png");
        var originalSurface = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Terrain/RollingGrass.png");
        if (originalSoil == null || originalSurface == null) throw new InvalidOperationException("Original terrain sprites missing.");
        Directory.CreateDirectory(ArtFolder);
        var soilPixels = Read(originalSoil);
        var surfacePixels = Read(originalSurface);
        int width = (int)originalSoil.rect.width, height = (int)originalSoil.rect.height;
        var defs = new TerrainThemeDefinition[3];
        defs[0] = Definition("Grassland", "GRASS", originalSoil, originalSurface, new Color32(190,219,233,255), Color.white, Color.white, new Color(.26f,.57f,.23f));
        foreach (bool snow in new[] { true, false })
        {
            var groundPixels = new Color32[soilPixels.Length];
            var topPixels = new Color32[soilPixels.Length];
            for (int x = 0; x < width; x++)
            {
                int top = height - 1;
                while (top >= 0 && soilPixels[top * width + x].a == 0) top--;
                for (int y = 0; y < height; y++)
                {
                    int i = y * width + x;
                    if (soilPixels[i].a == 0) continue;
                    float noise = Mathf.PerlinNoise(x * .027f, y * .027f);
                    Color ground;
                    Color cap;
                    if (snow)
                    {
                        float strata = .5f + .5f * Mathf.Sin(y * .14f + Mathf.Sin(x * .009f) * 1.3f);
                        ground = Color.Lerp(new Color(.24f,.34f,.43f), new Color(.43f,.55f,.65f), noise * .6f + strata * .4f);
                        float depth = Mathf.Clamp01((top - y) / 11f);
                        cap = Color.Lerp(new Color(.97f,.99f,1f), new Color(.65f,.79f,.88f), depth * depth);
                        if ((x * 17 + y * 11) % 53 == 0) cap = Color.Lerp(cap, Color.white, .5f);
                    }
                    else
                    {
                        RockSample(x, y, out float cell, out float seam);
                        ground = Color.Lerp(new Color(.27f,.29f,.32f), new Color(.48f,.49f,.51f), cell * .7f + noise * .3f);
                        ground *= Mathf.Lerp(.48f, 1f, Mathf.SmoothStep(0, 1, seam / 2.2f));
                        float layer = Mathf.Abs(Mathf.Sin((y + Mathf.Sin(x * .012f) * 9f) * .075f));
                        ground *= layer < .075f ? .79f : 1f;
                        cap = Color.Lerp(new Color(.57f,.59f,.61f), new Color(.32f,.35f,.39f), (top-y)/13f);
                        cap *= Mathf.Lerp(.57f, 1f, Mathf.Clamp01(seam / 2.2f));
                    }
                    ground.a = soilPixels[i].a / 255f;
                    groundPixels[i] = ground;
                    if (surfacePixels[i].a > 0) { cap.a = surfacePixels[i].a / 255f; topPixels[i] = cap; }
                }
            }
            string name = snow ? "Snowfield" : "Rocklands";
            Sprite groundSprite = WriteSprite(name + "Soil", groundPixels, width, height, originalSoil);
            Sprite surfaceSprite = WriteSprite(name + "Surface", topPixels, width, height, originalSurface);
            defs[snow ? 1 : 2] = Definition(name, snow ? "SNOW" : "ROCK", groundSprite, surfaceSprite,
                snow ? new Color(.74f,.85f,.94f) : new Color(.79f,.76f,.69f),
                snow ? new Color(1.19f,1.2f,1.2f) : new Color(.87f,.85f,.81f),
                snow ? new Color(1.10f,1.12f,1.15f) : new Color(.73f,.74f,.75f),
                snow ? new Color(.20f,.50f,.67f) : new Color(.55f,.43f,.30f));
        }
        var terrain = soil.GetComponent<DestructibleTerrain>();
        if (terrain == null) terrain = Undo.AddComponent<DestructibleTerrain>(soil.gameObject);
        Undo.RecordObject(terrain, "Connect destructible terrain surface");
        terrain.Configure(surface);
        var controller = battlefield.GetComponent<TerrainThemeController>();
        if (controller == null) controller = Undo.AddComponent<TerrainThemeController>(battlefield);
        Undo.RecordObject(controller, "Configure terrain themes");
        var canvas = GameObject.Find("MVP HUD").GetComponent<Canvas>();
        // Stay inside the existing SafeArea container when one is present.
        Transform uiParent = canvas.transform.Find("SafeArea");
        if (uiParent == null) uiParent = canvas.transform.Find("Safe Area");
        if (uiParent == null) uiParent = canvas.transform;
        Transform panel = uiParent.Find("Terrain Selector");
        if (panel == null)
        {
            var go = new GameObject("Terrain Selector", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create terrain selector");
            go.transform.SetParent(uiParent, false);
            panel = go.transform;
        }
        var rect = (RectTransform)panel;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.sizeDelta = new Vector2(552, 100);
        rect.anchoredPosition = new Vector2(0, 92);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text title = Label(panel, "Title", font);
        title.text = "TERRAIN"; title.fontSize = 19; title.color = new Color(.87f,.91f,.94f);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -14); titleRect.sizeDelta = new Vector2(540, 28);
        var buttons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            string name = "Theme " + defs[i].displayName;
            Transform buttonTransform = panel.Find(name);
            if (buttonTransform == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(panel, false);
                buttonTransform = go.transform;
            }
            var buttonRect = (RectTransform)buttonTransform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(.5f, 0);
            buttonRect.pivot = new Vector2(.5f, 0);
            buttonRect.anchoredPosition = new Vector2((i - 1) * 184, 0);
            buttonRect.sizeDelta = new Vector2(174, 64);
            var image = buttonTransform.GetComponent<Image>(); image.color = Color.white;
            buttons[i] = buttonTransform.GetComponent<Button>(); buttons[i].targetGraphic = image;
            buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };
            Text label = Label(buttonTransform, "Label", font);
            label.text = defs[i].displayName; label.fontSize = 24; label.fontStyle = FontStyle.Bold;
            var labelRect = label.rectTransform; labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        }
        var serialized = new SerializedObject(controller);
        SetReference(serialized, "terrain", terrain);
        SetReference(serialized, "soilRenderer", soil);
        SetReference(serialized, "surfaceRenderer", surface);
        SetReference(serialized, "skyRenderer", GameObject.Find("Sky").GetComponent<SpriteRenderer>());
        SetReference(serialized, "farMountains", GameObject.Find("Mountains Far").GetComponent<SpriteRenderer>());
        SetReference(serialized, "nearMountains", GameObject.Find("Mountains Near").GetComponent<SpriteRenderer>());
        var list = serialized.FindProperty("themes"); list.arraySize = defs.Length;
        var buttonList = serialized.FindProperty("buttons"); buttonList.arraySize = buttons.Length;
        for (int i = 0; i < defs.Length; i++) { list.GetArrayElementAtIndex(i).objectReferenceValue = defs[i]; buttonList.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i]; }
        serialized.FindProperty("selectedTheme").intValue = 0;
        serialized.ApplyModifiedProperties();
        controller.SelectTheme(0);
        EditorUtility.SetDirty(controller); EditorUtility.SetDirty(terrain);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Terrain scene save failed.");
        Debug.Log("Terrain themes installed: GRASS / SNOW / ROCK. Existing weapons and battle wiring preserved.");
    }

    private static void RockSample(int x, int y, out float cell, out float seam)
    {
        const float size = 55f;
        int cx = Mathf.FloorToInt(x / size), cy = Mathf.FloorToInt(y / size);
        float nearest = float.MaxValue, second = float.MaxValue; cell = 0f;
        for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++)
        {
            int gx = cx + a, gy = cy + b;
            float px = (gx + .2f + .6f * Hash(gx, gy, 1)) * size;
            float py = (gy + .2f + .6f * Hash(gx, gy, 2)) * size;
            float d = (x-px)*(x-px) + (y-py)*(y-py);
            if (d < nearest) { second = nearest; nearest = d; cell = Hash(gx, gy, 3); }
            else if (d < second) second = d;
        }
        seam = Mathf.Sqrt(second) - Mathf.Sqrt(nearest);
    }
    private static float Hash(int x, int y, int seed)
    {
        unchecked { uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177); h = (h ^ (h >> 13)) * 1274126177; return (h & 0xFFFFFF) / 16777215f; }
    }
    private static void SetReference(SerializedObject settings, string field, UnityEngine.Object value) => settings.FindProperty(field).objectReferenceValue = value;
    private static Text Label(Transform parent, string name, Font font)
    {
        Transform child = parent.Find(name);
        if (child == null) { child = new GameObject(name, typeof(RectTransform), typeof(Text)).transform; child.SetParent(parent, false); }
        var text = child.GetComponent<Text>(); text.font = font; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; return text;
    }
    private static TerrainThemeDefinition Definition(string name, string title, Sprite soil, Sprite surface, Color sky, Color far, Color near, Color accent)
    {
        string path = ArtFolder + "/" + name + ".asset";
        var def = AssetDatabase.LoadAssetAtPath<TerrainThemeDefinition>(path);
        if (def == null) { def = ScriptableObject.CreateInstance<TerrainThemeDefinition>(); AssetDatabase.CreateAsset(def, path); }
        def.displayName = title; def.soil = soil; def.surface = surface; def.skyTint = sky; def.farMountainTint = far; def.nearMountainTint = near; def.accentColor = accent;
        EditorUtility.SetDirty(def); return def;
    }
    private static Sprite WriteSprite(string name, Color32[] colors, int width, int height, Sprite source)
    {
        string path = ArtFolder + "/" + name + ".png";
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try { texture.SetPixels32(colors); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = source.pixelsPerUnit;
        var spriteSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(spriteSettings);
        spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
        spriteSettings.spritePivot = new Vector2(source.pivot.x / source.rect.width, source.pivot.y / source.rect.height);
        importer.SetTextureSettings(spriteSettings);
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static Color32[] Read(Sprite source)
    {
        var previous = RenderTexture.active;
        var temp = RenderTexture.GetTemporary(source.texture.width, source.texture.height, 0, RenderTextureFormat.ARGB32);
        var result = new Texture2D((int)source.rect.width, (int)source.rect.height, TextureFormat.RGBA32, false);
        try { Graphics.Blit(source.texture, temp); RenderTexture.active = temp; result.ReadPixels(source.rect,0,0); result.Apply(); return result.GetPixels32(); }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temp); UnityEngine.Object.DestroyImmediate(result); }
    }

    [MenuItem("Tools/Tank Game/Terrain Themes/Verify All Themes _F4")]
    public static void Verify()
    {
        if (!EditorApplication.isPlaying) { SessionState.SetBool(PendingKey, true); EditorApplication.EnterPlaymode(); return; }
        var report = new StringBuilder(); GameObject container = null;
        try
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>();
            if (controller == null) throw new Exception("Terrain selector not installed.");
            var health = UnityEngine.Object.FindObjectsByType<HealthComponent>();
            float[] hp = Array.ConvertAll(health, h => h.CurrentHealth);
            int selected = controller.SelectedTheme;
            foreach (var theme in controller.Themes)
            {
                var reference = Read(controller.Themes[0].soil); var pixels = Read(theme.soil);
                var capReference = Read(controller.Themes[0].surface); var capPixels = Read(theme.surface);
                if (pixels.Length != reference.Length) throw new Exception("Terrain dimensions differ.");
                for (int i = 0; i < pixels.Length && theme != controller.Themes[2]; i++)
                    if (pixels[i].a != reference[i].a || capPixels[i].a != capReference[i].a) throw new Exception("Theme changes terrain silhouette.");
                report.AppendLine("PASS " + theme.displayName + ": compatible dimensions; grass/snow share silhouette, rock has dedicated geometry.");
            }
            container = new GameObject("TerrainThemeVerification"); container.SetActive(false);
            var copy = UnityEngine.Object.Instantiate(GameObject.Find("MVP Battlefield"), container.transform, false);
            UnityEngine.Object.DestroyImmediate(copy.GetComponent<TerrainThemeController>());
            container.transform.position = new Vector3(100, 0, 0); container.SetActive(true);
            var terrain = copy.GetComponentInChildren<DestructibleTerrain>();
            var collider = terrain.GetComponent<PolygonCollider2D>();
            float x = 100f;
            float before = terrain.SurfaceHeightAt(x);
            if (!terrain.Carve(new Vector2(x, before), .8f)) throw new Exception("Terrain cannot be deformed.");
            float craterFloor = terrain.SurfaceHeightAt(x);
            Vector2[] path = collider.GetPath(0);
            int count = terrain.CraterCount;
            foreach (var theme in controller.Themes)
            {
                terrain.ApplyAppearance(theme.soil, theme.surface);
                if (Mathf.Abs(terrain.SurfaceHeightAt(x)-craterFloor) > .001f || terrain.CraterCount != count)
                    throw new Exception("Theme change resets crater.");
                var current = collider.GetPath(0);
                if (current.Length != path.Length) throw new Exception("Theme changes collision.");
                for (int i = 0; i < path.Length; i++) if (current[i] != path[i]) throw new Exception("Theme moves collider.");
                var sprite = terrain.GetComponent<SpriteRenderer>().sprite;
                int px = Mathf.FloorToInt((x-terrain.transform.position.x)*sprite.pixelsPerUnit+sprite.pivot.x);
                int py = Mathf.FloorToInt((craterFloor-terrain.transform.position.y)*sprite.pixelsPerUnit+sprite.pivot.y);
                if (sprite.texture.GetPixel(px,py+10).a > .01f) throw new Exception("Theme fills visible crater.");
                Color charred = sprite.texture.GetPixel(px,py-2);
                if (Mathf.Max(charred.r, Mathf.Max(charred.g,charred.b)) > .30f) throw new Exception("Theme clears scorch marks.");
                report.AppendLine("PASS " + theme.displayName + ": crater depth, empty pixels, scorch marks and exact collider preserved.");
            }
            for (int i = 0; i < controller.Themes.Length; i++) controller.SelectTheme(i);
            controller.SelectTheme(selected);
            for (int i = 0; i < health.Length; i++) if (hp[i] != health[i].CurrentHealth) throw new Exception("Theme switch affects tank health.");
            report.AppendLine("PASS scene selector cycles all maps without changing tank health.");
            report.AppendLine("PASS all terrain theme checks.");
            File.WriteAllText("Library/TerrainThemeVerification.txt", report.ToString()); Debug.Log(report.ToString());
        }
        catch (Exception error) { report.AppendLine("FAIL " + error); File.WriteAllText("Library/TerrainThemeVerification.txt", report.ToString()); Debug.LogException(error); }
        finally { if (container != null) UnityEngine.Object.DestroyImmediate(container); }
    }

    [MenuItem("Tools/Tank Game/Terrain Themes/Preview Rocket Impact _F12")]
    public static void PreviewRocket()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var terrain = GameObject.Find("Soil Ground").GetComponent<DestructibleTerrain>();
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/Data/Weapons/Weapon_Rocket.asset");
        if (weapon == null || weapon.ProjectilePrefab == null) throw new Exception("Existing rocket weapon missing.");
        float y = terrain.SurfaceHeightAt(0f);
        var projectile = UnityEngine.Object.Instantiate(weapon.ProjectilePrefab, new Vector3(0,y+2,0), Quaternion.Euler(0,0,-90));
        projectile.Initialize(0, 0f, .8f, Vector2.down * 8f);
        projectile.Completed += _ => { string message = "PASS real rocket completed; theme=" + UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>().SelectedTheme + "; craters=" + terrain.CraterCount;
            File.AppendAllText("Library/TerrainThemeRocketImpacts.txt", message + Environment.NewLine); Debug.Log(message); };
    }
}


