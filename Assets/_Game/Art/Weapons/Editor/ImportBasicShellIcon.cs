using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class ImportBasicShellIcon
{
    const string Path="Assets/_Game/Art/Weapons/BasicShell_Icon.png";
    [MenuItem("Tools/Tank Game/Import Basic Shell Icon")]
    public static void Import()
    {
        AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceUpdate);
        var importer=AssetImporter.GetAtPath(Path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Shell PNG missing.");
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.sRGBTexture=true;
        importer.maxTextureSize=256;
        importer.filterMode=FilterMode.Bilinear;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Path);
        if(sprite==null || sprite.rect.width!=256 || sprite.rect.height!=256)
            throw new InvalidOperationException("Expected imported 256 x 256 Sprite.");
        // A hidden temporary object verifies UI assignment without adding a scene UI.
        var test=new GameObject("Shell UI validation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        test.hideFlags=HideFlags.HideAndDontSave;
        try
        {
            var image=test.GetComponent<Image>();
            image.sprite=sprite;
            image.preserveAspect=true;
            if(image.sprite!=sprite)throw new InvalidOperationException("UI Image assignment failed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(test); }
        AssetDatabase.SaveAssets();
        Selection.activeObject=sprite;
        EditorGUIUtility.PingObject(sprite);
        Debug.Log("BasicShell_Icon validated: 256x256 single Sprite, alpha enabled, mipmaps off, UI Image assignment passed. Scene unchanged.");
    }
}
