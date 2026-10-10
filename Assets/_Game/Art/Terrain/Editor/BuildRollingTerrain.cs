using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class BuildRollingTerrain
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string AssetFolder = "Assets/_Game/Art/Terrain";
    private const string SoilPath = AssetFolder + "/RollingSoil.png";
    private const string GrassPath = AssetFolder + "/RollingGrass.png";
    private const int PixelsPerUnit = 100;
    private const int Width = 1800;
    private const int Height = 350;
    private const float Bottom = -4.5f;
    private const float SpriteCenterY = -2.75f;

    private static readonly float[] KeysX = { -9, -7, -6, -4, -2, 0, 2, 4, 6, 8, 9 };
    private static readonly float[] KeysY = { -2.05f, -2.02f, -1.95f, -1.95f, -1.55f,
        -1.95f, -1.55f, -1.95f, -1.95f, -2.02f, -2.05f };

    [MenuItem("Tools/Tank Game/Build Rolling Terrain")]
    public static void Build()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("Open SampleScene before building the rolling terrain.");
            return;
        }

        var battlefield = GameObject.Find("MVP Battlefield");
        var soil = battlefield != null ? battlefield.transform.Find("Soil Ground") : null;
        var grass = battlefield != null ? battlefield.transform.Find("Grass Surface") : null;
        if (soil == null || grass == null || soil.GetComponent<SpriteRenderer>() == null ||
            grass.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogError("The existing MVP terrain hierarchy is missing or incompatible.");
            return;
        }

        var soilPixels = new Color32[Width * Height];
        var grassPixels = new Color32[Width * Height];
        var brown = new Color32(93, 55, 34, 255);
        var green = new Color32(89, 177, 69, 255);
        var clear = new Color32(0, 0, 0, 0);
        for (var x = 0; x < Width; x++)
        {
            var worldX = -9f + (x + 0.5f) / PixelsPerUnit;
            var surfacePixel = Mathf.RoundToInt((SurfaceY(worldX) - Bottom) * PixelsPerUnit);
            for (var y = 0; y < Height; y++)
            {
                var index = y * Width + x;
                soilPixels[index] = y < surfacePixel ? brown : clear;
                grassPixels[index] = y >= surfacePixel - 12 && y < surfacePixel ? green : clear;
            }
        }

        WriteSprite(SoilPath, soilPixels);
        WriteSprite(GrassPath, grassPixels);

        var soilRenderer = soil.GetComponent<SpriteRenderer>();
        var grassRenderer = grass.GetComponent<SpriteRenderer>();
        soilRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SoilPath);
        grassRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GrassPath);
        if (soilRenderer.sprite == null || grassRenderer.sprite == null)
        {
            Debug.LogError("Generated terrain sprites could not be imported.");
            return;
        }
        soilRenderer.color = Color.white;
        grassRenderer.color = Color.white;
        soilRenderer.sortingOrder = 0;
        grassRenderer.sortingOrder = 1;
        soil.localPosition = new Vector3(0, SpriteCenterY, 0);
        grass.localPosition = new Vector3(0, SpriteCenterY, 0);
        soil.localScale = Vector3.one;
        grass.localScale = Vector3.one;

        var oldBox = soil.GetComponent<BoxCollider2D>();
        if (oldBox != null) UnityEngine.Object.DestroyImmediate(oldBox);
        var polygon = soil.GetComponent<PolygonCollider2D>();
        if (polygon == null) polygon = soil.gameObject.AddComponent<PolygonCollider2D>();
        var points = new Vector2[75];
        for (var i = 0; i <= 72; i++)
        {
            var x = -9f + i * 0.25f;
            points[i] = new Vector2(x, SurfaceY(x) - SpriteCenterY);
        }
        points[73] = new Vector2(9, Bottom - SpriteCenterY);
        points[74] = new Vector2(-9, Bottom - SpriteCenterY);
        polygon.pathCount = 1;
        polygon.SetPath(0, points);
        polygon.isTrigger = false;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Rolling terrain built and SampleScene saved.");
    }

    private static float SurfaceY(float x)
    {
        for (var i = 0; i < KeysX.Length - 1; i++)
        {
            if (x > KeysX[i + 1]) continue;
            var t = Mathf.InverseLerp(KeysX[i], KeysX[i + 1], x);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(KeysY[i], KeysY[i + 1], t);
        }
        return KeysY[KeysY.Length - 1];
    }

    private static void WriteSprite(string assetPath, Color32[] pixels)
    {
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        var fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
