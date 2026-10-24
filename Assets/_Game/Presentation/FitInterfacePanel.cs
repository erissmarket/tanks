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
        public static readonly Color Ink=new Color(.04f,.075f,.10f,.98f);
        public static readonly Color Card=new Color(.12f,.20f,.27f,1);
        public static readonly Color Accent=new Color(.25f,.52f,.34f,1);
        private static Font font;
        public static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        public static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        public static RectTransform Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {
            var rect=Rect(parent,name,new Vector2(.5f,.5f),position,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;return rect;
        }
        public static RectTransform Page(Transform parent,string name,Vector2 size)
        {
            var cover=Panel(parent,name,Vector2.zero,Vector2.zero,new Color(.02f,.04f,.06f,.94f));Stretch(cover);
            var panel=Panel(cover,"Content",Vector2.zero,size,Ink);var fit=panel.gameObject.AddComponent<FitInterfacePanel>();fit.designSize=size;fit.Refresh();return panel;
        }
        public static Text Label(Transform parent,string name,string text,Vector2 position,Vector2 size,int fontSize=32)
        {
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rect=Rect(parent,name,new Vector2(.5f,.5f),position,size);var label=rect.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.raycastTarget=false;return label;
        }
        public static Button Button(Transform parent,string name,string title,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var rect=Panel(parent,name,position,size,Card);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.navigation=new Navigation{mode=Navigation.Mode.None};
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
