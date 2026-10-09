using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class BuildMvpBackground
{
    const string Folder="Assets/_Game/Art/Background";
    const int W=900,H=300,PPU=25;
    static readonly float[] X={-18,-14,-11,-8,-4,-1,3,6,10,13,16,18};
    static readonly float[] Far={0,1.4f,.5f,2,1,1.5f,.6f,1.8f,.9f,1.6f,.7f,.2f};
    static readonly float[] Near={-.9f,.4f,-.4f,.8f,-.6f,.3f,-.7f,.5f,-.3f,.6f,-.7f,-.9f};

    [MenuItem("Tools/Tank Game/Build MVP Background")]
    public static void Build()
    {
        if(SceneManager.GetActiveScene().path!="Assets/Scenes/SampleScene.unity" || GameObject.Find("MVP Background")!=null)
            throw new InvalidOperationException("Open SampleScene. An existing MVP Background must be reviewed before rebuilding.");
        if(Camera.main==null || !Camera.main.orthographic)
            throw new InvalidOperationException("Expected the existing orthographic main camera.");
        Directory.CreateDirectory(Folder);
        var far=new Color32[W*H];
        var near=new Color32[W*H];
        var clouds=new Color32[W*H];
        for(int y=0;y<H;y++) for(int x=0;x<W;x++)
        {
            float wx=(x+.5f)/PPU-18,wy=(y+.5f)/PPU-6;
            int index=y*W+x;
            if(wy<Profile(wx,Far)) far[index]=new Color32(154,185,198,255);
            if(wy<Profile(wx,Near)) near[index]=new Color32(116,157,174,255);
            if(Cloud(wx+6.8f,wy-3.3f,1f) || Cloud(wx-.1f,wy-4.15f,.8f) || Cloud(wx-7.1f,wy-2.9f,1.1f))
                clouds[index]=new Color32(246,250,250,245);
        }
        var sky=Write("SkySquare",new[]{new Color32(255,255,255,255)},1,1,1);
        var farSprite=Write("MountainsFar",far,W,H,PPU);
        var nearSprite=Write("MountainsNear",near,W,H,PPU);
        var cloudSprite=Write("Clouds",clouds,W,H,PPU);
        var root=new GameObject("MVP Background");
        root.transform.position=new Vector3(Camera.main.transform.position.x,Camera.main.transform.position.y,5);
        Add(root.transform,"Sky",sky,-40,new Color32(190,219,233,255),new Vector3(36,12,1));
        Add(root.transform,"Mountains Far",farSprite,-30,Color.white,Vector3.one);
        Add(root.transform,"Mountains Near",nearSprite,-20,Color.white,Vector3.one);
        Add(root.transform,"Clouds",cloudSprite,-10,Color.white,Vector3.one);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Scene save failed.");
        Debug.Log("MVP background built and saved: four visual layers, no physics. Coverage 36 x 12 world units.");
    }
    static float Profile(float x,float[] heights)
    {
        for(int i=0;i<X.Length-1;i++)
        {
            if(x>X[i+1])continue;
            float t=Mathf.InverseLerp(X[i],X[i+1],x);
            // A gently rounded silhouette with broad peaks.
            t=t*t*(3-2*t);
            return Mathf.Lerp(heights[i],heights[i+1],t);
        }
        return heights[heights.Length-1];
    }
    static bool Ellipse(float x,float y,float rx,float ry)=>x*x/(rx*rx)+y*y/(ry*ry)<=1;
    static bool Cloud(float x,float y,float scale)
    {
        x/=scale;y/=scale;
        return Ellipse(x,y,.95f,.24f)||Ellipse(x+.35f,y-.12f,.43f,.35f)||Ellipse(x-.22f,y-.23f,.50f,.39f);
    }
    static Sprite Write(string name,Color32[] pixels,int w,int h,int ppu)
    {
        string path=Folder+"/"+name+".png";
        if(File.Exists(path))throw new IOException("Refusing to overwrite existing background asset: "+path);
        var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
        texture.SetPixels32(pixels);texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=ppu;
        importer.mipmapEnabled=false;
        importer.alphaIsTransparency=true;
        importer.filterMode=FilterMode.Bilinear;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite==null)throw new IOException("Sprite import failed: "+path);
        return sprite;
    }
    static void Add(Transform parent,string name,Sprite sprite,int order,Color color,Vector3 scale)
    {
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);go.transform.localScale=scale;
        var renderer=go.AddComponent<SpriteRenderer>();
        renderer.sprite=sprite;renderer.color=color;renderer.sortingLayerName="Default";renderer.sortingOrder=order;
    }
}
