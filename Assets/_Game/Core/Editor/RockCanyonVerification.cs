#if UNITY_EDITOR
using System;
using System.IO;
using TankGame.Terrain;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class RockCanyonVerification
{
    static RockCanyonVerification(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("VerifyCanyon",false)){SessionState.SetBool("VerifyCanyon",false);EditorApplication.delayCall+=Verify;}};}
    [MenuItem("Tools/Tank Game/Art/Verify Rugged Canyon %&F3")]
    public static void Verify()
    {
        if(!EditorApplication.isPlaying){SessionState.SetBool("VerifyCanyon",true);EditorApplication.EnterPlaymode();return;}
        try
        {
            var themes=UnityEngine.Object.FindAnyObjectByType<TerrainThemeController>();var terrain=themes.GetComponentInChildren<DestructibleTerrain>();
            terrain.ResetSurface();themes.SelectTheme(0);float center=terrain.SurfaceHeightAt(1),spawn=terrain.SurfaceHeightAt(-5);
            themes.SelectTheme(2);float rock=terrain.SurfaceHeightAt(1);
            if(Mathf.Abs(rock-center)<.15f||Mathf.Abs(terrain.SurfaceHeightAt(-5)-spawn)>.02f)throw new Exception("Geometry variation or safe spawn failed");
            for(float x=-8;x<8;x+=.31f)
            {
                bool hit=false;foreach(var r in Physics2D.RaycastAll(new Vector2(x,4),Vector2.down,10))if(r.collider==terrain.GetComponent<PolygonCollider2D>())
                {if(Mathf.Abs(r.point.y-terrain.SurfaceHeightAt(x))>.06f)throw new Exception("Collider mismatch");hit=true;}
                if(!hit)throw new Exception("Missing ground collision");
            }
            float before=terrain.SurfaceHeightAt(1);terrain.CarveExplosion(new Vector2(1,before),1.5f);float after=terrain.SurfaceHeightAt(1);
            if(after>=before)throw new Exception("Rock crater missing");
            terrain.ResetSurface();if(Mathf.Abs(terrain.SurfaceHeightAt(1)-rock)>.001f)throw new Exception("Reset lost rock geometry");
            themes.SelectTheme(0);if(Mathf.Abs(terrain.SurfaceHeightAt(1)-center)>.001f)throw new Exception("Grass shape not restored");
            themes.SelectTheme(2);
            File.WriteAllText("Library/RockCanyonVerification.txt","PASS distinct rock elevations; safe tank spawn shelves; 52 collider probes; real terrain explosion; rock reset; return to original grass geometry.\n");
            Debug.Log("PASS rugged canyon geometry and collider checks");
        }
        catch(Exception e){File.WriteAllText("Library/RockCanyonVerification.txt","FAIL "+e);Debug.LogException(e);}
    }
}
#endif
