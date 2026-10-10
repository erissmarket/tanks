using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class BuildTankArt
{
    const string Folder = "Assets/_Game/Art/Tanks";
    const string PrefabPath = Folder + "/TankVisual.prefab";
    public static void BuildFromCommandLine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Build();
    }

    [MenuItem("Tools/Tank Game/Build Tank Art")]
    public static void Build()
    {
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity" ||
            GameObject.Find("Player Tank") != null || GameObject.Find("Enemy Tank") != null ||
            File.Exists(PrefabPath))
            throw new InvalidOperationException("Open SampleScene; existing tank instances or prefab must be reviewed before rebuilding.");
        var terrain = GameObject.Find("Soil Ground").GetComponent<PolygonCollider2D>();
        if (terrain == null) throw new InvalidOperationException("Terrain polygon missing.");
        var leftY = TerrainHeight(terrain, -5f);
        var rightY = TerrainHeight(terrain, 5f);
        Directory.CreateDirectory(Folder);
        var body = MakeSprite("Body", 150, 45, (x,y) => {
            float inset = y > 27 ? (y - 27) * 0.85f : 0;
            return x >= inset && x < 150 - inset;
        });
        var turret = MakeSprite("Turret", 76, 34, (x,y) => {
            float inset = y > 18 ? (y - 18) * 0.6f : 0;
            return x >= inset && x < 76-inset;
        });
        var barrel = MakeSprite("Barrel", 90, 13, (x,y) => true);
        var tracks = MakeSprite("Tracks", 170, 38, (x,y) => {
            float cx = Mathf.Clamp(x + 0.5f, 19, 151);
            return (new Vector2(x + 0.5f - cx, y + 0.5f - 19)).sqrMagnitude <= 19*19;
        }, true);

        var root = new GameObject("TankVisual");
        var green = new Color(0.32f, 0.64f, 0.25f);
        Part(root.transform, "Body", body, new Vector3(0, 0.48f, 0), green, 12);
        Part(root.transform, "Tracks", tracks, new Vector3(0, 0.19f, 0), Color.white, 11);
        var pivot = new GameObject("TurretPivot").transform;
        pivot.SetParent(root.transform, false);
        pivot.localPosition = new Vector3(0, 0.79f, 0);
        Part(pivot, "Turret", turret, Vector3.zero, green * new Color(0.9f,0.9f,0.9f,1), 14);
        var barrelPivot = new GameObject("BarrelPivot").transform;
        barrelPivot.SetParent(pivot, false);
        barrelPivot.localPosition = new Vector3(0.20f, 0.03f, 0);
        barrelPivot.localRotation = Quaternion.Euler(0,0,12);
        Part(barrelPivot, "Barrel", barrel, new Vector3(0.45f,0,0), green, 13);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        Place(prefab, "Player Tank", -5, leftY, false, green);
        Place(prefab, "Enemy Tank", 5, rightY, true, new Color(0.82f,0.25f,0.22f));
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
            throw new IOException("Scene save failed.");
        Debug.Log($"Tank art built and saved. Player (-5,{leftY}); Enemy (5,{rightY}). Prefab verified: {prefab != null}");
    }

    static float TerrainHeight(PolygonCollider2D terrain, float x)
    {
        var points = terrain.GetPath(0);
        float highest = float.NegativeInfinity;
        for(int i=0;i<points.Length;i++)
        {
            var a=terrain.transform.TransformPoint(points[i]+terrain.offset);
            var b=terrain.transform.TransformPoint(points[(i+1)%points.Length]+terrain.offset);
            if(Mathf.Abs(a.x-b.x)<0.00001f || x<Mathf.Min(a.x,b.x) || x>Mathf.Max(a.x,b.x)) continue;
            highest=Mathf.Max(highest,Mathf.Lerp(a.y,b.y,(x-a.x)/(b.x-a.x)));
        }
        if(float.IsNegativeInfinity(highest)) throw new InvalidOperationException("Spawn is outside terrain.");
        return highest;
    }

    static void Place(GameObject prefab, string name, float x, float y, bool mirror, Color color)
    {
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name=name;
        instance.transform.position=new Vector3(x,y,0);
        instance.transform.localScale=new Vector3(mirror?-1:1,1,1);
        foreach(var r in instance.GetComponentsInChildren<SpriteRenderer>())
        {
            if(r.name=="Tracks") continue;
            r.color=r.name=="Turret" ? color * new Color(.9f,.9f,.9f,1) : color;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
    }

    static void Part(Transform parent, string name, Sprite sprite, Vector3 position, Color color, int order)
    {
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);
        go.transform.localPosition=position;
        var r=go.AddComponent<SpriteRenderer>();
        r.sprite=sprite; r.color=color; r.sortingOrder=order;
    }

    static Sprite MakeSprite(string name,int w,int h,Func<int,int,bool> inside,bool track=false)
    {
        string path=Folder+"/Tank"+name+".png";
        var pixels=new Color32[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            if(!inside(x,y)) continue;
            bool edge=x<2||y<2||x>=w-2||y>=h-2||!inside(Mathf.Max(0,x-2),y)||!inside(Mathf.Min(w-1,x+2),y)||!inside(x,Mathf.Min(h-1,y+2));
            var c=edge ? new Color32(125,145,125,255) : new Color32(255,255,255,255);
            if(track)
            {
                c=new Color32(42,49,52,255);
                foreach(float wheel in new[]{29f,66f,103f,140f})
                {
                    float d=(new Vector2(x+.5f-wheel,y+.5f-19)).sqrMagnitude;
                    if(d<12*12) c=new Color32(84,96,99,255);
                    if(d<4*4) c=new Color32(40,47,50,255);
                }
            }
            pixels[y*w+x]=c;
        }
        var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);
        tex.SetPixels32(pixels); tex.Apply();
        File.WriteAllBytes(path,tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.filterMode=FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
