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
            rect.anchorMin = new Vector2(previous.xMin / Screen.width, previous.yMin / Screen.height);
            rect.anchorMax = new Vector2(previous.xMax / Screen.width, previous.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
