#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RockCanyonSetup
{
    [MenuItem("Tools/Tank Game/Art/Install Rugged Canyon %&F1")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
        var controller=UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>();
        var valley=controller.GetComponent<ValleyEnvironment>();
        valley.rockPanorama=ValleyArtSetup.Import("Assets/_Game/Art/Battlefield/RockCanyon.png");EditorUtility.SetDirty(valley);
        var source=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Terrain/RollingSoil.png");
        var texture=ValleyArtSetup.Read("Assets/_Game/Art/Terrain/RollingSoil.png");
        int w=texture.width,h=texture.height;var mask=texture.GetPixels32();var soil=new Color32[mask.Length];var cap=new Color32[mask.Length];
        for(int x=0;x<w;x++)
        {
            int old=h-1;while(old>=0&&mask[old*w+x].a==0)old--;
            float wx=(x-source.pivot.x)/source.pixelsPerUnit;
            float spawn=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(Mathf.Abs(wx)-5)-.85f)/.8f));
            float edge=Mathf.Clamp01((8.8f-Mathf.Abs(wx))/.8f);
            float wave=(Mathf.Sin(wx*2.25f)*.43f+Mathf.Sin(wx*4.1f+1)*.14f)*spawn*edge;
            int top=Mathf.Clamp(old+Mathf.RoundToInt(wave*source.pixelsPerUnit),8,h-1);
            for(int y=0;y<=top;y++)
            {
                int i=y*w+x;float depth=top-y;
                float cloud=Mathf.PerlinNoise(x*.018f+4,y*.025f+15);
                float strata=Mathf.Abs(Mathf.Sin(y*.062f+Mathf.Sin(x*.012f)*2));
                float crack=Mathf.Abs(Mathf.Sin(x*.047f+Mathf.Sin(y*.013f)*3));
                float shard=Mathf.PerlinNoise(x*.085f+23,y*.12f+9);
                Color c=Color.Lerp(new Color(.31f,.29f,.26f),new Color(.70f,.58f,.43f),cloud*.55f+shard*.45f);
                c*=Mathf.Lerp(.65f,1,Mathf.SmoothStep(0,.14f,strata))*Mathf.Lerp(.70f,1,Mathf.SmoothStep(0,.08f,crack));
                float grain=Mathf.PerlinNoise(x*.5f,y*.5f);c*=.86f+grain*.28f;
                float speck=Mathf.Sin(x*127.1f+y*311.7f)*43758.54f;speck-=Mathf.Floor(speck);
                if(speck>.975f)c=Color.Lerp(c,new Color(.78f,.72f,.61f),.5f);
                c.a=1;soil[i]=c;
                if(depth<7){Color rim=Color.Lerp(new Color(.67f,.59f,.43f),new Color(.35f,.32f,.26f),depth/7);rim*=.85f+grain*.25f;rim.a=1;cap[i]=rim;}
            }
        }
        string folder="Assets/_Game/Art/Terrain/Themes/";
        // Larger angular inclusions break up the fine-grain soil without changing its mask.
        for(int gy=12;gy<h;gy+=37)for(int gx=8;gx<w;gx+=49)
        {
            float n=Mathf.PerlinNoise(gx*.7f,gy*.4f);if(n<.48f)continue;
            int cx=gx+(int)(n*37),cy=gy+(int)(Mathf.PerlinNoise(gy,gx)*29);float rx=5+n*14,ry=3+n*8;
            for(int y=Mathf.Max(0,cy-12);y<Mathf.Min(h,cy+13);y++)for(int x=Mathf.Max(0,cx-20);x<Mathf.Min(w,cx+21);x++)
            {int i=y*w+x;if(soil[i].a==0||cap[i].a>0)continue;float dx=(x-cx)/rx,dy=(y-cy)/ry;
                float edge=1-Mathf.Abs(dx)*.7f-Mathf.Abs(dy);if(edge<=0)continue;
                Color rock=Color.Lerp(new Color(.22f,.22f,.21f),new Color(.72f,.65f,.52f),Mathf.Clamp01(.55f+dy*.5f-dx*.2f));
                if(Mathf.Abs(dy+dx*.4f)<.06f)rock*=.65f;
                Color c=Color.Lerp(soil[i],rock,Mathf.Clamp01(edge*6)*.85f);c.a=1;soil[i]=c;
            }
        }
        texture.SetPixels32(soil);texture.Apply();File.WriteAllBytes(folder+"RocklandsSoil.png",texture.EncodeToPNG());
        texture.SetPixels32(cap);texture.Apply();File.WriteAllBytes(folder+"RocklandsSurface.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(folder+"RocklandsSoil.png",ImportAssetOptions.ForceSynchronousImport);AssetDatabase.ImportAsset(folder+"RocklandsSurface.png",ImportAssetOptions.ForceSynchronousImport);
        controller.SelectTheme(0);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Debug.Log("Rugged canyon installed: unique rock geometry, safe spawn shelves, stratified rock and dedicated panorama.");
    }
}
#endif
