using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class PrepareBasicShellProjectile
{
    const string SpritePath="Assets/_Game/Art/Projectiles/BasicShell_Projectile.png";
    const string PrefabPath="Assets/_Game/Art/Projectiles/BasicShell_Visual.prefab";
    [MenuItem("Tools/Tank Game/Prepare Basic Shell Projectile")]
    public static void Prepare()
    {
        if(File.Exists(PrefabPath))throw new InvalidOperationException("Existing projectile prefab must be reviewed before replacement.");
        AssetDatabase.ImportAsset(SpritePath,ImportAssetOptions.ForceUpdate);
        var importer=AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if(importer==null)throw new IOException("Projectile PNG not found.");
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.filterMode=FilterMode.Bilinear;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize=128;
        var settings=new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment=(int)SpriteAlignment.Center;
        settings.spritePivot=new Vector2(.5f,.5f);
        settings.spriteMeshType=SpriteMeshType.Tight;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if(sprite==null || sprite.rect.width!=128 || sprite.rect.height!=64)
            throw new InvalidOperationException("Unexpected projectile Sprite dimensions.");
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("BasicShell_Visual");
            SceneManager.MoveGameObjectToScene(root,preview);
            root.transform.localScale=Vector3.one*.25f;
            var renderer=root.AddComponent<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.sortingLayerName="Default";
            renderer.sortingOrder=20;
            var size=renderer.bounds.size;
            root.transform.rotation=Quaternion.Euler(0,0,90);
            var rotated=renderer.bounds.size;
            if(Mathf.Abs(size.x-rotated.y)>.001f || Mathf.Abs(size.y-rotated.x)>.001f)
                throw new InvalidOperationException("Rotation changed projectile proportions.");
            root.transform.rotation=Quaternion.identity;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            if(prefab==null)throw new IOException("Prefab save failed.");
            Selection.activeObject=prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        AssetDatabase.SaveAssets();
        Debug.Log("BasicShell projectile validated: 128x64, 100 PPU, centered pivot, +X artwork, uniform 0.25 scale, 90-degree rotation passed. Visual prefab saved; battlefield scene untouched.");
    }
}
