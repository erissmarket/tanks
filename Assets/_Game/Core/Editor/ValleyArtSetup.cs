#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Terrain;
using TankGame.Data;
using TankGame.Tank;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ValleyArtSetup
{
    const string Folder="Assets/_Game/Art/Battlefield/";
    [MenuItem("Tools/Tank Game/Art/Install Reference Battlefield %&F2")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        var bg=Import(Folder+"Valley.png");
        var controller=UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>();
        var valley=controller.GetComponent<ValleyEnvironment>();if(valley==null)valley=controller.gameObject.AddComponent<ValleyEnvironment>();
        valley.themes=controller;valley.panorama=bg;EditorUtility.SetDirty(valley);
        foreach(string name in new[]{"Sky","Mountains Far","Mountains Near","Clouds"})
        {var r=GameObject.Find(name).GetComponent<SpriteRenderer>();r.enabled=true;EditorUtility.SetDirty(r);}
        var atlas=Read(Folder+"TankParts.png");
        var parts=new Sprite[6];
        for(int row=0;row<2;row++)for(int col=0;col<3;col++)
        {
            // Unequal columns follow the atlas's actual generous spacing, then trim transparent padding.
            float[] edges={0,.365f,.725f,1};
            int x0=(int)(edges[col]*atlas.width),x1=(int)(edges[col+1]*atlas.width);
            int y0=row==0?atlas.height/2:0,y1=row==0?atlas.height:atlas.height/2;
            int minX=x1,minY=y1,maxX=x0,maxY=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(atlas.GetPixel(x,y).a>.1f)
            {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(maxX<=minX||maxY<=minY)throw new Exception("Missing tank part");
            var tex=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
            tex.SetPixels(atlas.GetPixels(minX,minY,tex.width,tex.height));tex.Apply();
            string path=Folder+(row==0?"Olive":"Navy")+new[]{"Hull","Tracks","Barrel"}[col]+".png";
            File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            parts[row*3+col]=Import(path);
        }
        UnityEngine.Object.DestroyImmediate(atlas);
        foreach(string name in new[]{"Vanguard","Scout","Bastion"})
        {
            var def=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_"+name+".asset");
            def.useSourceArtwork=def.detailedArtwork=true;def.bodySprite=parts[0];def.enemyBodySprite=parts[3];
            def.tracksSprite=parts[1];def.enemyTracksSprite=parts[4];def.barrelSprite=parts[2];def.enemyBarrelSprite=parts[5];
            def.thumbnail=parts[0];def.turretSprite=null;EditorUtility.SetDirty(def);
        }
        BuildGround();
        var preview=AssetDatabase.LoadAssetAtPath<TankDefinition>("Assets/_Game/Data/Tanks/Tank_Vanguard.asset");
        foreach(var loadout in UnityEngine.Object.FindObjectsByType<TankGame.Testing.BackendTestPlayerController>())
        {
            TankLoadout.ApplyVisuals(loadout.gameObject,preview,loadout.name=="Player1");
            foreach(var r in loadout.GetComponentsInChildren<SpriteRenderer>())
            {PrefabUtility.RecordPrefabInstancePropertyModifications(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r.transform);}
        }
        controller.SelectTheme(0);EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Debug.Log("Reference battlefield and articulated olive/navy tanks installed.");
    }
    static void BuildGround()
    {
        var mask=Read("Assets/_Game/Art/Terrain/RollingSoil.png");var cap=Read("Assets/_Game/Art/Terrain/RollingGrass.png");
        var pixels=mask.GetPixels32();var topPixels=cap.GetPixels32();int w=mask.width,h=mask.height;
        for(int x=0;x<w;x++)
        {
            int top=h-1;while(top>=0&&pixels[top*w+x].a==0)top--;
            for(int y=0;y<h;y++)
            {
                int i=y*w+x;if(pixels[i].a==0)continue;
                float depth=top-y,n=Mathf.PerlinNoise(x*.022f+3,y*.035f+17),large=Mathf.PerlinNoise(x*.008f,y*.012f);
                float strata=Mathf.Abs(Mathf.Sin(y*.065f+large*5+Mathf.Sin(x*.01f)));
                Color earth=Color.Lerp(new Color(.16f,.12f,.085f),new Color(.40f,.32f,.22f),n);
                float stone=Mathf.SmoothStep(.48f,.7f,n)*Mathf.SmoothStep(.18f,.32f,strata);
                earth=Color.Lerp(earth,Color.Lerp(new Color(.27f,.28f,.25f),new Color(.58f,.55f,.45f),large),stone*.85f);
                earth*=Mathf.Lerp(.56f,1,Mathf.SmoothStep(0,.14f,strata));
                float grain=(Mathf.Sin(x*127.1f+y*311.7f)*43758.54f);grain-=Mathf.Floor(grain);
                earth*=.91f+grain*.18f;
                if(grain>.989f&&depth>12)earth=Color.Lerp(earth,new Color(.64f,.59f,.46f),.65f);
                float roots=Mathf.Abs(Mathf.Sin(x*.075f+depth*.035f+large*4));
                if(depth<105&&roots<.027f)earth*=.56f;
                float moss=(1-Mathf.Clamp01(depth/18))*Mathf.SmoothStep(.3f,.7f,large);
                earth=Color.Lerp(earth,new Color(.27f,.32f,.10f),moss*.7f);
                earth.a=pixels[i].a/255f;pixels[i]=earth;
                if(topPixels[i].a>0)
                {
                    Color grass=Color.Lerp(new Color(.19f,.25f,.055f),new Color(.55f,.57f,.20f),n);
                    grass*=Mathf.Lerp(1,.65f,Mathf.Clamp01(depth/10));grass.a=topPixels[i].a/255f;topPixels[i]=grass;
                }
            }
        }
        // Embedded irregular boulders keep the original occupancy, and disappear with crater pixels.
        for(int gy=20;gy<h;gy+=47)for(int gx=12;gx<w;gx+=61)
        {
            float seed=Mathf.PerlinNoise(gx*.31f,gy*.29f);if(seed<.55f)continue;
            int cx=gx+(int)(Mathf.PerlinNoise(gx*.7f,gy*.4f)*48),cy=gy+(int)(seed*37);float rx=3+seed*16,ry=2+seed*7;
            for(int y=Mathf.Max(0,cy-16);y<Mathf.Min(h,cy+17);y++)for(int x=Mathf.Max(0,cx-31);x<Mathf.Min(w,cx+32);x++)
            {
                float dx=(x-cx)/rx,dy=(y-cy)/ry;
                float edge=1-Mathf.Abs(dx)*.7f-Mathf.Abs(dy)*.8f;if(edge<0)continue;
                int i=y*w+x;if(pixels[i].a==0||topPixels[i].a>0)continue;
                Color rock=Color.Lerp(new Color(.25f,.25f,.22f),new Color(.6f,.57f,.46f),Mathf.Clamp01(.45f+dy*.55f-dx*.18f));
                if(Mathf.Abs(dy+dx*.3f)<.045f)rock*=.58f;
                Color c=Color.Lerp(pixels[i],rock,Mathf.Clamp01(edge*7)*.85f);c.a=pixels[i].a/255f;pixels[i]=c;
            }
        }
        mask.SetPixels32(pixels);mask.Apply();cap.SetPixels32(topPixels);cap.Apply();
        foreach(string theme in new[]{"Grassland","Rocklands"})
        {
            string folder="Assets/_Game/Art/Terrain/Themes/";
            File.WriteAllBytes(folder+theme+"Soil.png",mask.EncodeToPNG());File.WriteAllBytes(folder+theme+"Surface.png",cap.EncodeToPNG());
            AssetDatabase.ImportAsset(folder+theme+"Soil.png",ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(folder+theme+"Surface.png",ImportAssetOptions.ForceSynchronousImport);
        }
        UnityEngine.Object.DestroyImmediate(mask);UnityEngine.Object.DestroyImmediate(cap);
    }
    internal static Texture2D Read(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var source=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height);
        var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
        Graphics.Blit(source,rt);RenderTexture.active=rt;copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();
        RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);return copy;
    }
    internal static Sprite Import(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
        importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
