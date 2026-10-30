#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Tank;
using TankGame.Terrain;
using TankGame.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PresentationPolishSetup
{
    private const string Folder="Assets/_Game/Art/Terrain/Themes/";
    [MenuItem("Tools/Tank Game/Art/Polish Terrain and Ground Tanks %&F6")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().path!="Assets/_Game/Scenes/MVPIntegrated.unity")
            throw new InvalidOperationException("Open MVPIntegrated in Edit Mode.");
        var soil=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Terrain/RollingSoil.png");
        var cap=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Terrain/RollingGrass.png");
        var mask=Read(soil);var capMask=Read(cap);int w=(int)soil.rect.width,h=(int)soil.rect.height;
        string[] names={"Grassland","Snowfield","Rocklands"};
        for(int theme=0;theme<3;theme++)
        {
            var pixels=new Color32[mask.Length];var surface=new Color32[mask.Length];
            for(int x=0;x<w;x++)
            {
                int top=h-1;while(top>=0&&mask[top*w+x].a==0)top--;
                for(int y=0;y<h;y++)
                {
                    int i=y*w+x;if(mask[i].a==0)continue;
                    float depth=top-y;
                    float cloud=Mathf.PerlinNoise(x*.013f+17,y*.018f+31);
                    float grain=Hash(x,y,theme)-.5f;
                    float strata=Mathf.Sin(y*.12f+Mathf.Sin(x*.008f)*2.2f);
                    Color dark=theme==0?new Color(.22f,.155f,.10f):theme==1?new Color(.23f,.29f,.32f):new Color(.24f,.255f,.26f);
                    Color light=theme==0?new Color(.43f,.32f,.20f):theme==1?new Color(.43f,.48f,.49f):new Color(.49f,.47f,.41f);
                    Color ground=Color.Lerp(dark,light,cloud*.8f+.1f+strata*.08f);
                    ground*=1+grain*.18f;
                    // A shaded root layer gives the surface depth without changing occupancy.
                    ground*=Mathf.Lerp(.68f,1,Mathf.Clamp01(depth/18));
                    ground.a=mask[i].a/255f;pixels[i]=ground;
                    if(capMask[i].a>0)
                    {
                        Color bright=theme==0?new Color(.42f,.5f,.23f):theme==1?new Color(.94f,.96f,.94f):new Color(.57f,.55f,.46f);
                        Color shade=theme==0?new Color(.21f,.28f,.115f):theme==1?new Color(.58f,.71f,.75f):new Color(.34f,.35f,.32f);
                        float band=Mathf.Clamp01(depth/(theme==1?12:8));
                        Color topColor=Color.Lerp(bright,shade,band*.82f);
                        topColor*=1+grain*.12f+(cloud-.5f)*.18f;
                        topColor.a=capMask[i].a/255f;surface[i]=topColor;
                    }
                }
            }
            // Deterministic irregular pebbles: top light, underside shade, varying size and density.
            for(int gy=5;gy<h;gy+=15)for(int gx=5;gx<w;gx+=19)
            {
                if(Hash(gx,gy,8)>.59f)continue;
                int cx=gx+(int)(Hash(gx,gy,9)*12),cy=gy+(int)(Hash(gx,gy,10)*10);
                float rx=1.7f+Hash(gx,gy,11)*5.5f,ry=1.1f+Hash(gx,gy,12)*2.6f;
                Color stone=theme==0?new Color(.46f,.41f,.31f):theme==1?new Color(.48f,.55f,.57f):new Color(.56f,.55f,.5f);
                stone*=.75f+Hash(gx,gy,13)*.45f;
                for(int y=Mathf.Max(0,cy-5);y<=Mathf.Min(h-1,cy+5);y++)
                    for(int x=Mathf.Max(0,cx-8);x<=Mathf.Min(w-1,cx+8);x++)
                    {
                        float dx=(x-cx)/rx,dy=(y-cy)/ry;
                        float edge=1-dx*dx-dy*dy;if(edge<0)continue;
                        int i=y*w+x;if(mask[i].a==0||capMask[i].a>0)continue;
                        Color lit=stone*(.8f+dy*.24f);
                        Color mixed=Color.Lerp(pixels[i],lit,Mathf.Clamp01(edge*4)*.72f);mixed.a=mask[i].a/255f;pixels[i]=mixed;
                    }
            }
            var def=AssetDatabase.LoadAssetAtPath<TerrainThemeDefinition>(Folder+names[theme]+".asset");
            def.soil=Write(names[theme]+"Soil",pixels,soil);
            def.surface=Write(names[theme]+"Surface",surface,cap);
            def.accentColor=theme==0?new Color(.37f,.46f,.25f):theme==1?new Color(.27f,.48f,.57f):new Color(.54f,.43f,.29f);
            EditorUtility.SetDirty(def);
        }
        UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>().SelectTheme(0);
        AlignInitialCollider(soil,mask);
        SnapTanks();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Debug.Log("Presentation polish saved: textured terrain and grounded tank previews.");
    }
    private static void AlignInitialCollider(Sprite source,Color32[] mask)
    {
        var collider=UnityEngine.Object.FindAnyObjectByType<DestructibleTerrain>().GetComponent<PolygonCollider2D>();
        int w=(int)source.rect.width,h=(int)source.rect.height,samples=(w-1+2)/3+1;
        var points=new Vector2[samples+2];
        for(int i=0;i<samples;i++)
        {
            int x=Mathf.Min(i*3,w-1),y=h-1;while(y>=0&&mask[y*w+x].a==0)y--;
            float localX=i==0?-source.pivot.x/source.pixelsPerUnit:x==w-1?(w-source.pivot.x)/source.pixelsPerUnit:(x+.5f-source.pivot.x)/source.pixelsPerUnit;
            points[i]=new Vector2(localX,(y+1-source.pivot.y)/source.pixelsPerUnit)-collider.offset;
        }
        points[samples]=new Vector2((w-source.pivot.x)/source.pixelsPerUnit,-source.pivot.y/source.pixelsPerUnit)-collider.offset;
        points[samples+1]=new Vector2(-source.pivot.x/source.pixelsPerUnit,-source.pivot.y/source.pixelsPerUnit)-collider.offset;
        collider.SetPath(0,points);EditorUtility.SetDirty(collider);Physics2D.SyncTransforms();
    }
    public static void SnapTanks()
    {
        var ground=UnityEngine.Object.FindAnyObjectByType<DestructibleTerrain>().GetComponent<PolygonCollider2D>();
        var points=ground.GetPath(0);
        var preview=AssetDatabase.LoadAssetAtPath<TankGame.Data.TankDefinition>("Assets/_Game/Data/Tanks/Tank_Vanguard.asset");
        foreach(var player in UnityEngine.Object.FindObjectsByType<BackendTestPlayerController>())
        {
            float x=player.transform.position.x,height=float.NegativeInfinity;
            for(int i=0;i<points.Length;i++)
            {
                Vector2 a=ground.transform.TransformPoint(points[i]+ground.offset),b=ground.transform.TransformPoint(points[(i+1)%points.Length]+ground.offset);
                if(Mathf.Abs(b.x-a.x)<.00001f||x<Mathf.Min(a.x,b.x)||x>Mathf.Max(a.x,b.x))continue;
                height=Mathf.Max(height,Mathf.Lerp(a.y,b.y,(x-a.x)/(b.x-a.x)));
            }
            if(!float.IsFinite(height))throw new InvalidOperationException("Tank spawn has no terrain support.");
            var capsule=player.GetComponent<CapsuleCollider2D>();
            float bottom=(capsule.offset.y-capsule.size.y*.5f)*player.transform.lossyScale.y;
            var position=player.transform.position;position.y=height-bottom-.005f;player.transform.position=position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            TankLoadout.ApplyVisuals(player.gameObject,preview,player.name=="Player1");
            foreach(var renderer in player.GetComponentsInChildren<SpriteRenderer>())
            {PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);}
        }
    }
    private static float Hash(int x,int y,int seed)
    {unchecked{uint n=(uint)(x*374761393+y*668265263+seed*1274126177);n=(n^(n>>13))*1274126177;return (n&0xFFFFFF)/16777215f;}}
    private static Color32[] Read(Sprite sprite)
    {
        var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(sprite.texture.width,sprite.texture.height);
        var texture=new Texture2D((int)sprite.rect.width,(int)sprite.rect.height,TextureFormat.RGBA32,false);
        try{Graphics.Blit(sprite.texture,rt);RenderTexture.active=rt;texture.ReadPixels(sprite.rect,0,0);texture.Apply();return texture.GetPixels32();}
        finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    private static Sprite Write(string name,Color32[] pixels,Sprite source)
    {
        string path=Folder+name+".png";var texture=new Texture2D((int)source.rect.width,(int)source.rect.height,TextureFormat.RGBA32,false);
        try{texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=source.pixelsPerUnit;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;
        settings.spritePivot=new Vector2(source.pivot.x/source.rect.width,source.pivot.y/source.rect.height);importer.SetTextureSettings(settings);importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
