using System;
using UnityEngine;
using UnityEngine.UI;

namespace TankGame.Terrain
{
    [DefaultExecutionOrder(-100)]
    public sealed class TerrainThemeController : MonoBehaviour
    {
        [SerializeField] private DestructibleTerrain terrain;
        [SerializeField] private SpriteRenderer soilRenderer;
        [SerializeField] private SpriteRenderer surfaceRenderer;
        [SerializeField] private SpriteRenderer skyRenderer;
        [SerializeField] private SpriteRenderer farMountains;
        [SerializeField] private SpriteRenderer nearMountains;
        [SerializeField] private TerrainThemeDefinition[] themes = Array.Empty<TerrainThemeDefinition>();
        [SerializeField] private int selectedTheme;
        [SerializeField] private Button[] buttons = Array.Empty<Button>();
        private UnityEngine.Events.UnityAction[] handlers;
        public int SelectedTheme => selectedTheme;
        public event Action<int> ThemeChanged;
        public TerrainThemeDefinition[] Themes => (TerrainThemeDefinition[])themes.Clone();

        private void Awake() => SelectTheme(selectedTheme);

        private void OnEnable()
        {
            handlers = new UnityEngine.Events.UnityAction[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                handlers[i] = () => SelectTheme(index);
                if (buttons[i] != null) buttons[i].onClick.AddListener(handlers[i]);
            }
        }

        private void OnDisable()
        {
            if (handlers == null) return;
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] != null && i < handlers.Length) buttons[i].onClick.RemoveListener(handlers[i]);
        }

        public void SelectTheme(int index)
        {
            if (index < 0 || index >= themes.Length || themes[index] == null)
                throw new ArgumentOutOfRangeException(nameof(index));
            TerrainThemeDefinition theme = themes[index];
            if (theme.soil == null || theme.surface == null)
                throw new InvalidOperationException("Terrain theme requires soil and surface sprites.");
            if (Application.isPlaying && terrain != null)
                terrain.ApplyAppearance(theme.soil, theme.surface);
            else
            {
                soilRenderer.sprite = theme.soil;
                surfaceRenderer.sprite = theme.surface;
            }
            soilRenderer.color = surfaceRenderer.color = Color.white;
            if (skyRenderer != null) skyRenderer.color = theme.skyTint;
            if (farMountains != null) farMountains.color = theme.farMountainTint;
            if (nearMountains != null) nearMountains.color = theme.nearMountainTint;
            selectedTheme = index;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var colors = buttons[i].colors;
                colors.normalColor = i == index ? theme.accentColor : new Color(0.16f, 0.22f, 0.28f, 0.95f);
                colors.highlightedColor = i == index ? theme.accentColor * 1.12f : new Color(0.28f, 0.35f, 0.42f);
                colors.pressedColor = theme.accentColor * 0.8f;
                colors.selectedColor = colors.normalColor;
                colors.fadeDuration = 0.08f;
                buttons[i].colors = colors;
            }
            ThemeChanged?.Invoke(index);
        }
    }
}
