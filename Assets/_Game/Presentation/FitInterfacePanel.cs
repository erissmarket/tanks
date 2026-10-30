using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TankGame.Presentation
{
    public sealed class FitInterfacePanel : MonoBehaviour
    {
        public Vector2 designSize=new Vector2(1680,980);
        public float margin=32;
        public static float ScaleFor(Vector2 available,Vector2 design,float margin)
        {
            if(available.x<=0||available.y<=0||design.x<=0||design.y<=0)return 1;
            return Mathf.Max(.05f,Mathf.Min(1,(available.x-margin*2)/design.x,(available.y-margin*2)/design.y));
        }
        public void Refresh()
        {
            var parent=transform.parent as RectTransform;if(parent==null)return;
            transform.localScale=Vector3.one*ScaleFor(parent.rect.size,designSize,margin);
        }
        private void LateUpdate()=>Refresh();
        private void OnEnable()=>Refresh();
        private void OnRectTransformDimensionsChange()=>Refresh();
    }
    public static class InterfaceLayout
    {
        public static event System.Action Clicked;
        public static void NotifyClick()=>Clicked?.Invoke();
        public static readonly Color Ink=new Color(.055f,.075f,.09f,.96f);
        public static readonly Color Card=new Color(.12f,.16f,.19f,1);
        public static readonly Color Accent=new Color(.64f,.43f,.18f,1);
        private static Font font;
        private static Sprite plate;
        public static Font Font => font!=null?font:(font=Resources.Load<Font>("Interface/Barlow-SemiBold")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        // One shared nine-slice keeps corners and edge highlights sharp at every panel size.
        public static void StylePlate(Image image,bool shadow=false)
        {
            if(plate==null)
            {
                const int n=64;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Interface Plate",filterMode=FilterMode.Bilinear};
                var pixels=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    float dx=Mathf.Max(Mathf.Abs(x-31.5f)-19.5f,0),dy=Mathf.Max(Mathf.Abs(y-31.5f)-19.5f,0);
                    float edge=12-Mathf.Sqrt(dx*dx+dy*dy);
                    float light=Mathf.Lerp(.84f,1,y/63f)+(edge<1.7f?.14f:0);
                    pixels[y*n+x]=new Color(light,light,light,Mathf.Clamp01(edge+.5f));
                }
                texture.SetPixels(pixels);texture.Apply(false,true);
                plate=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));
            }
            image.sprite=plate;image.type=Image.Type.Sliced;
            if(shadow)
            {
                var shade=image.gameObject.AddComponent<Shadow>();shade.effectColor=new Color(0,0,0,.23f);shade.effectDistance=new Vector2(0,-5);shade.useGraphicAlpha=true;
            }
        }
        public static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        public static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        public static RectTransform Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {
            var rect=Rect(parent,name,new Vector2(.5f,.5f),position,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;
            if(size.x>0&&size.y>0)StylePlate(image,size.y>60);return rect;
        }
        public static RectTransform Page(Transform parent,string name,Vector2 size)
        {
            var cover=Panel(parent,name,Vector2.zero,Vector2.zero,new Color(.015f,.025f,.035f,.78f));Stretch(cover);
            var panel=Panel(cover,"Content",Vector2.zero,size,Ink);var fit=panel.gameObject.AddComponent<FitInterfacePanel>();fit.designSize=size;fit.Refresh();return panel;
        }
        public static Text Label(Transform parent,string name,string text,Vector2 position,Vector2 size,int fontSize=32)
        {
            var rect=Rect(parent,name,new Vector2(.5f,.5f),position,size);var label=rect.gameObject.AddComponent<Text>();label.font=Font;label.text=text;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.91f,.94f,.94f);label.raycastTarget=false;return label;
        }
        public static Button Button(Transform parent,string name,string title,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var rect=Panel(parent,name,position,size,Card);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.navigation=new Navigation{mode=Navigation.Mode.None};
            var colors=button.colors;colors.highlightedColor=new Color(1.18f,1.18f,1.18f);colors.pressedColor=new Color(.72f,.76f,.8f);colors.disabledColor=new Color(.48f,.53f,.57f,.7f);colors.fadeDuration=.12f;button.colors=colors;
            Label(rect,"Label",title,Vector2.zero,size-Vector2.one*12,32);
            button.onClick.AddListener(()=>{Clicked?.Invoke();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);});
            if(action!=null)button.onClick.AddListener(action);return button;
        }
        public static Image Picture(Transform parent,string name,Sprite sprite,Vector2 position,Vector2 size)
        {
            var rect=Rect(parent,name,new Vector2(.5f,.5f),position,size);var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
    }
}
