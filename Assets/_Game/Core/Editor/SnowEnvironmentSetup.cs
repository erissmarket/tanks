#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SnowEnvironmentSetup
{
    private const string Themes = "Assets/_Game/Art/Terrain/Themes/";
    private const string Backdrop = "Assets/_Game/Art/Terrain/Snow/AlpineBackdrop.png";
    [MenuItem("Tools/Tank Game/Snow/Install Alpine Environment %&F5")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != "Assets/_Game/Scenes/MVPIntegrated.unity")
            throw new InvalidOperationException("Open MVPIntegrated in Edit Mode first.");
        AssetDatabase.ImportAsset(Backdrop, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Backdrop);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        BuildSnowTextures();
        var controller = UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>();
        var environment = controller.GetComponent<SnowEnvironment>();
        if (environment == null) environment = controller.gameObject.AddComponent<SnowEnvironment>();
        var serialized = new SerializedObject(environment);
        serialized.FindProperty("themes").objectReferenceValue = controller;
        serialized.FindProperty("snowTheme").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TerrainThemeDefinition>(Themes + "Snowfield.asset");
        serialized.FindProperty("terrain").objectReferenceValue = controller.GetComponentInChildren<DestructibleTerrain>();
        serialized.FindProperty("backdrop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Backdrop);
        serialized.FindProperty("weatherMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Presentation/Assets/BattleEffects.mat");
        var backgrounds = serialized.FindProperty("originalBackgrounds");
        string[] names = { "Sky", "Mountains Far", "Mountains Near", "Clouds" };
        backgrounds.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            backgrounds.GetArrayElementAtIndex(i).objectReferenceValue = GameObject.Find(names[i]).GetComponent<SpriteRenderer>();
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(environment);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Snow environment saved: alpine panorama, compact snow, glacial rock and bounded animated snowfall. Other themes unchanged.");
    }
    private static void BuildSnowTextures()
    {
        var soil = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Terrain/RollingSoil.png");
        var cap = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Terrain/RollingGrass.png");
        var mask = Read(soil); var capMask = Read(cap);
        int w = (int)soil.rect.width, h = (int)soil.rect.height;
        var ground = new Color32[mask.Length]; var surface = new Color32[mask.Length];
        for (int x = 0; x < w; x++)
        {
            int top = h - 1; while (top >= 0 && mask[top * w + x].a == 0) top--;
            float snowDepth = 12 + Mathf.PerlinNoise(x * .022f, 4) * 18;
            for (int y = 0; y < h; y++)
            {
                int p = y * w + x; if (mask[p].a == 0) continue;
                float depth = top - y;
                float fine = Noise(x * .19f, y * .23f);
                float rock = Noise(x * .032f, y * .044f);
                float cloud = Noise(x * .009f, y * .012f);
                float grain = Hash(x, y, 9) - .5f;
                Cell(x, y, out float cell, out float seam);
                Color stone = Color.Lerp(new Color(.17f,.25f,.31f), new Color(.43f,.52f,.56f), rock * .5f + cell * .5f);
                stone *= .79f + fine * .30f + grain * .13f;
                // Broken mineral strata with pale edges and dark recessed seams.
                float layer = Mathf.Abs(Mathf.Sin((y + cloud * 31 + Mathf.Sin(x * .012f) * 9) * .048f));
                stone *= Mathf.Lerp(.68f, 1, Mathf.SmoothStep(0, .2f, layer));
                stone *= Mathf.Lerp(.54f, 1, Mathf.SmoothStep(0, 2.6f, seam));
                float frost = Mathf.Clamp01((cloud - .43f) * 3f) * Mathf.Clamp01(1 - depth / 220f);
                Color ice = Color.Lerp(new Color(.26f,.47f,.58f), new Color(.52f,.72f,.77f), rock);
                Color c = Color.Lerp(stone, ice, frost * .65f);
                float vein = Mathf.Abs(Mathf.Sin(y * .047f + Mathf.Sin(x * .017f) * .8f + cloud * 3));
                if (vein < .05f && seam > 1) c = Color.Lerp(c, new Color(.64f,.79f,.82f), (1 - vein / .05f) * .32f);
                // A variable compacted-snow overhang is painted INSIDE the unchanged solid mask.
                float snow = 1 - Mathf.SmoothStep(snowDepth - 3, snowDepth + 6, depth);
                Color packed = Color.Lerp(new Color(.96f,.98f,.97f), new Color(.53f,.70f,.77f), Mathf.Clamp01(depth / snowDepth));
                packed *= 1 + grain * .055f + (fine - .5f) * .05f;
                float ripple = Mathf.Sin(depth * .75f + cloud * 9) * .5f + .5f;
                packed *= 1 - ripple * .05f * Mathf.Clamp01(depth / 8);
                c = Color.Lerp(c * (depth < snowDepth + 8 ? .76f : 1), packed, snow);
                c.a = mask[p].a / 255f; ground[p] = c;
                if (capMask[p].a > 0)
                {
                    Color powder = Color.Lerp(new Color(.99f,1f,.98f), new Color(.72f,.85f,.9f), Mathf.Clamp01(depth / 12f));
                    powder *= 1 + grain * .045f;
                    powder.a = capMask[p].a / 255f; surface[p] = powder;
                }
            }
        }
        var definition = AssetDatabase.LoadAssetAtPath<TerrainThemeDefinition>(Themes + "Snowfield.asset");
        definition.soil = Write("SnowfieldSoil", ground, soil);
        definition.surface = Write("SnowfieldSurface", surface, cap);
        EditorUtility.SetDirty(definition);
    }
    private static float Noise(float x, float y) => Mathf.PerlinNoise(x + 13.7f, y + 27.9f);
    private static float Hash(int x, int y, int seed)
    { unchecked { uint n = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177); n = (n ^ (n >> 13)) * 1274126177; return (n & 0xFFFFFF) / 16777215f; } }
    private static void Cell(int x, int y, out float cell, out float seam)
    {
        const float size = 34;
        int cx = Mathf.FloorToInt(x / size), cy = Mathf.FloorToInt(y / (size * .62f));
        float first = float.MaxValue, second = float.MaxValue; cell = 0;
        for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++)
        {
            int gx = cx + a, gy = cy + b;
            float px = (gx + .2f + Hash(gx, gy, 1) * .6f) * size;
            float py = (gy + .2f + Hash(gx, gy, 2) * .6f) * size * .62f;
            float d = (x - px) * (x - px) + (y - py) * (y - py);
            if (d < first) { second = first; first = d; cell = Hash(gx, gy, 3); }
            else if (d < second) second = d;
        }
        seam = Mathf.Sqrt(second) - Mathf.Sqrt(first);
    }
    private static Color32[] Read(Sprite sprite)
    {
        var previous = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height);
        var texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
        try { Graphics.Blit(sprite.texture, rt); RenderTexture.active = rt; texture.ReadPixels(sprite.rect, 0, 0); texture.Apply(); return texture.GetPixels32(); }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(texture); }
    }
    private static Sprite Write(string name, Color32[] pixels, Sprite source)
    {
        string path = Themes + name + ".png";
        var texture = new Texture2D((int)source.rect.width, (int)source.rect.height, TextureFormat.RGBA32, false);
        try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        // Preserve the existing dimensions, pivot, pixel scale and import settings.
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
