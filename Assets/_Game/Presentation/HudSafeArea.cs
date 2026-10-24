using UnityEngine;

namespace TankGame.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class HudSafeArea : MonoBehaviour
    {
        private Rect previous;
        private Vector2Int resolution;
        private ScreenOrientation orientation;

        private void OnEnable() => Apply();
        private void Update()
        {
            if (previous != Screen.safeArea || resolution.x != Screen.width || resolution.y != Screen.height
                || orientation != Screen.orientation) Apply();
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            previous = Screen.safeArea;
            resolution = new Vector2Int(Screen.width, Screen.height);
            orientation = Screen.orientation;
            RectTransform rect = (RectTransform)transform;
            Rect normalized=Normalize(previous,new Vector2(Screen.width,Screen.height));
            rect.anchorMin = normalized.min;
            rect.anchorMax = normalized.max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        public static Rect Normalize(Rect safe,Vector2 size)
        {
            if(!float.IsFinite(size.x)||!float.IsFinite(size.y)||size.x<=0||size.y<=0||!float.IsFinite(safe.x)||!float.IsFinite(safe.y)||!float.IsFinite(safe.width)||!float.IsFinite(safe.height))return new Rect(0,0,1,1);
            float x=Mathf.Clamp01(safe.xMin/size.x),y=Mathf.Clamp01(safe.yMin/size.y);
            float right=Mathf.Clamp01(safe.xMax/size.x),top=Mathf.Clamp01(safe.yMax/size.y);
            return right>x&&top>y?Rect.MinMaxRect(x,y,right,top):new Rect(0,0,1,1);
        }
    }
}
