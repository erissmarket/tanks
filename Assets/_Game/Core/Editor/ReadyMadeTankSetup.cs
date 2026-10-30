#if UNITY_EDITOR
using System;
using TankGame.Data;
using TankGame.Tank;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReadyMadeTankSetup
{
    private const string Art="Assets/ThirdParty/KenneyTanks/";
    [MenuItem("Tools/Tank Game/Art/Apply Ready-made Tanks %&F7")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")
            throw new InvalidOperationException("Open MVPIntegrated in Edit Mode.");
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Art.TrimEnd('/')}))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        string[] names={"Vanguard","Scout","Bastion"};
        for(int i=0;i<names.Length;i++)
        {
            var definition=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_"+names[i]+".asset");
            int variant=i+1;
            definition.useSourceArtwork=true;
            definition.bodySprite=Sprite("tanks_tankGreen_body"+variant);
            definition.enemyBodySprite=Sprite("tanks_tankNavy_body"+variant);
            definition.barrelSprite=Sprite("tanks_turret2");
            definition.tracksSprite=Sprite("tanks_tankTracks3");
            definition.turretSprite=null;
            definition.thumbnail=Sprite("tanks_tankGreen"+variant);
            EditorUtility.SetDirty(definition);
        }
        var preview=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Vanguard.asset");
        foreach(var player in UnityEngine.Object.FindObjectsByType<BackendTestPlayerController>())
        {
            player.transform.localScale=Vector3.one*.7f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            TankLoadout.ApplyVisuals(player.gameObject,preview,player.name=="Player1");
            foreach(var child in player.GetComponentsInChildren<Transform>())
                if(child.name=="BarrelPivot"||child.name=="Muzzle")
                    PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            foreach(var renderer in player.GetComponentsInChildren<SpriteRenderer>())
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
            }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Debug.Log("Ready-made Kenney artwork configured for all three tanks; player roots use 70% scale.");
    }
    private static Sprite Sprite(string name)
        =>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png")??throw new InvalidOperationException("Missing tank sprite: "+name);
}
#endif
