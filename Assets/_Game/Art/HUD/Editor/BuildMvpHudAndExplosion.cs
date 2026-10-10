using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class BuildMvpHudAndExplosion
{
    const string FX="Assets/_Game/Art/FX/BasicExplosion";
    const string HUD="Assets/_Game/Art/HUD";
    static Sprite white, circle;
    static Font font;
    [MenuItem("Tools/Tank Game/Finalize HUD Readability")]
    static void FinalizeHUD()
    {
        var turn=GameObject.Find("MVP HUD/Turn Indicator");
        if(turn==null) throw new InvalidOperationException("HUD missing.");
        var image=turn.GetComponent<Image>() ?? turn.AddComponent<Image>();
        image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Background/SkySquare.png");
        image.color=new Color(.12f,.17f,.21f,.94f);image.raycastTarget=false;
        var text=turn.GetComponentInChildren<Text>();
        text.resizeTextForBestFit=true;text.resizeTextMinSize=14;text.resizeTextMaxSize=24;
        text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;
        EditorSceneManager.MarkSceneDirty(turn.scene);
        if(!EditorSceneManager.SaveScene(turn.scene)) throw new IOException("Save failed.");
        Debug.Log("HUD readability finalized and SampleScene saved.");
    }
    [MenuItem("Tools/Tank Game/Build Minimal HUD And Explosion")]
    static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.name!="SampleScene" || GameObject.Find("MVP HUD")!=null || Directory.Exists(FX))
            throw new InvalidOperationException("Review existing scene/HUD/FX before rebuilding.");
        Directory.CreateDirectory(FX);
        Directory.CreateDirectory(HUD);
        var frames=new Sprite[6];
        float[] radii={22,45,70,91,104,110};
        float[] opacity={1,1,.95f,.78f,.43f,.10f};
        for(int f=0;f<6;f++)
        {
            var tex=new Texture2D(256,256,TextureFormat.RGBA32,false);
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++) for(int x=0;x<256;x++)
            {
                float dx=x-127.5f,dy=y-127.5f,d=Mathf.Sqrt(dx*dx+dy*dy),a=Mathf.Atan2(dy,dx);
                float edge=radii[f]*(.83f+.12f*Mathf.Cos(10*a)+.05f*Mathf.Cos(7*a+.4f));
                float coverage=Mathf.Clamp01(edge-d+.5f);
                Color c=d<edge*.47f ? new Color(1,.93f,.38f) : d<edge*.76f ? new Color(1,.68f,.14f) : new Color(.96f,.32f,.08f);
                c.a=coverage*opacity[f]; pixels[y*256+x]=c;
            }
            tex.SetPixels(pixels); tex.Apply();
            string path=FX+"/Explosion_"+f.ToString("00")+".png";
            File.WriteAllBytes(path,tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            frames[f]=Import(path,100,256);
        }
        var clip=new AnimationClip {name="BasicExplosion",frameRate=12};
        var keys=new ObjectReferenceKeyframe[7];
        for(int i=0;i<6;i++) keys[i]=new ObjectReferenceKeyframe {time=i/12f,value=frames[i]};
        keys[6]=new ObjectReferenceKeyframe {time=.5f,value=null};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding {path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=false;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,FX+"/BasicExplosion.anim");
        var controller=AnimatorController.CreateAnimatorControllerAtPath(FX+"/BasicExplosion.controller");
        var sm=controller.layers[0].stateMachine;
        sm.defaultState=sm.AddState("Idle");
        var explode=sm.AddState("Explode"); explode.motion=clip;
        var preview=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("BasicExplosion_Visual"); SceneManager.MoveGameObjectToScene(root,preview);
            var sr=root.AddComponent<SpriteRenderer>(); sr.sortingOrder=30;
            root.AddComponent<Animator>().runtimeAnimatorController=controller;
            clip.SampleAnimation(root,.2f);
            if(sr.sprite==null) throw new InvalidOperationException("Explosion sampling failed.");
            clip.SampleAnimation(root,.5f);
            if(sr.sprite!=null) throw new InvalidOperationException("Explosion must finish invisible.");
            PrefabUtility.SaveAsPrefabAsset(root,FX+"/BasicExplosion_Visual.prefab");
        }
        finally {EditorSceneManager.ClosePreviewScene(preview);}
        var ct=new Texture2D(128,128,TextureFormat.RGBA32,false);
        var cp=new Color[128*128];
        for(int y=0;y<128;y++) for(int x=0;x<128;x++)
            cp[y*128+x]=new Color(1,1,1,Mathf.Clamp01(63-Mathf.Sqrt((x-63.5f)*(x-63.5f)+(y-63.5f)*(y-63.5f))));
        ct.SetPixels(cp);ct.Apply();File.WriteAllBytes(HUD+"/HUD_Circle.png",ct.EncodeToPNG());UnityEngine.Object.DestroyImmediate(ct);
        circle=Import(HUD+"/HUD_Circle.png",100,128);
        white=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Background/SkySquare.png");
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(white==null || font==null) throw new InvalidOperationException("Required built-in art/font unavailable.");
        var canvasGO=new GameObject("MVP HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasGO,"Create visual MVP HUD");
        var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
        Bar(canvasGO.transform,"Player Health Bar",new Vector2(0,1),new Vector2(1,1),new Vector2(60,-55),new Color(.25f,.75f,.29f),.85f,"PLAYER 85%");
        Bar(canvasGO.transform,"Enemy Health Bar",new Vector2(1,1),new Vector2(1,1),new Vector2(-60,-55),new Color(.87f,.25f,.25f),.70f,"ENEMY 70%");
        var turn=Rect("Turn Indicator",canvasGO.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-55),new Vector2(300,56));
        Label(turn,"PLAYER TURN",24);
        var fire=Rect("Fire Button",canvasGO.transform,new Vector2(1,0),new Vector2(1,0),new Vector2(-60,60),new Vector2(156,156));
        Image(fire,circle,new Color(.12f,.17f,.21f));
        var button=fire.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.interactable=false;
        var disc=Stretch("Disc",fire,8);Image(disc,circle,new Color(.91f,.34f,.16f));
        Label(fire,"FIRE",30);
        if(canvasGO.GetComponentsInChildren<Button>().Length!=1 || button.onClick.GetPersistentEventCount()!=0)
            throw new InvalidOperationException("Unexpected HUD bindings.");
        AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
        Selection.activeGameObject=canvasGO;
        Debug.Log("MVP HUD and explosion saved. Six 256px frames / 12fps / 0.5s, non-looping, default Idle invisible. HUD 85%/70%, disabled unbound Fire button, no EventSystem or gameplay scripts. SampleScene saved.");
    }
    static Sprite Import(string path,float ppu,int max)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;
        i.spritePixelsPerUnit=ppu;i.alphaIsTransparency=true;i.alphaSource=TextureImporterAlphaSource.FromInput;i.mipmapEnabled=false;
        i.filterMode=FilterMode.Bilinear;i.wrapMode=TextureWrapMode.Clamp;i.maxTextureSize=max;i.textureCompression=TextureImporterCompression.CompressedHQ;
        var s=new TextureImporterSettings();i.ReadTextureSettings(s);s.spriteAlignment=(int)SpriteAlignment.Center;s.spritePivot=new Vector2(.5f,.5f);s.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(s);
        i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    static RectTransform Stretch(string name,Transform parent,float inset)
    {
        var r=Rect(name,parent,Vector2.zero,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);r.anchorMax=Vector2.one;
        r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;return r;
    }
    static Image Image(RectTransform r,Sprite sprite,Color color)
    {var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;i.raycastTarget=false;return i;}
    static void Label(Transform parent,string value,int size)
    {
        var t=Stretch("Label",parent,3).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.fontStyle=FontStyle.Bold;
        t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;
    }
    static void Bar(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 pos,Color color,float amount,string text)
    {
        // Left bar uses a left pivot; right bar uses a right pivot.
        pivot.x=anchor.x;
        var r=Rect(name,parent,anchor,pivot,pos,new Vector2(340,56));Image(r,white,new Color(.12f,.17f,.21f));
        var fill=Image(Stretch("Fill",r,6),white,color);fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillAmount=amount;
        Label(r,text,24);
    }
}
