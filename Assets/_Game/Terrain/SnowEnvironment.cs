using UnityEngine;

namespace TankGame.Terrain
{
    /// <summary>Snow-only scenery. Never participates in collision or terrain occupancy.</summary>
    public sealed class SnowEnvironment : MonoBehaviour
    {
        [SerializeField] private TerrainThemeController themes;
        [SerializeField] private TerrainThemeDefinition snowTheme;
        [SerializeField] private DestructibleTerrain terrain;
        [SerializeField] private Sprite backdrop;
        [SerializeField] private Material weatherMaterial;
        [SerializeField] private SpriteRenderer[] originalBackgrounds;
        private GameObject visuals;
        private Mesh weather;
        private bool[] backgroundEnabled;
        private Vector3[] vertices;
        private Color[] colors;
        private float animationTime;
        private const int Flakes = 112, VerticesPerFlake = 9;
        public bool IsVisible => visuals != null && visuals.activeSelf;
        public float AnimationTime => animationTime;
        public int WeatherVertexCount => weather != null ? weather.vertexCount : 0;

        private void OnEnable()
        {
            if (themes == null || backdrop == null || weatherMaterial == null) return;
            Build();
            themes.ThemeChanged += OnThemeChanged;
            OnThemeChanged(themes.SelectedTheme);
        }
        private void OnThemeChanged(int index)
        {
            var definitions = themes.Themes;
            bool snow = index >= 0 && index < definitions.Length && definitions[index] == snowTheme;
            if (snow && !IsVisible)
            {
                backgroundEnabled = new bool[originalBackgrounds.Length];
                for (int i = 0; i < originalBackgrounds.Length; i++)
                {
                    if (originalBackgrounds[i] == null) continue;
                    backgroundEnabled[i] = originalBackgrounds[i].enabled;
                    originalBackgrounds[i].enabled = false;
                }
            }
            else if (!snow && IsVisible) RestoreBackgrounds();
            visuals.SetActive(snow);
        }
        private void RestoreBackgrounds()
        {
            if (backgroundEnabled == null) return;
            for (int i = 0; i < originalBackgrounds.Length; i++)
                if (originalBackgrounds[i] != null) originalBackgrounds[i].enabled = backgroundEnabled[i];
            backgroundEnabled = null;
        }
        private void Build()
        {
            if (visuals != null) return;
            visuals = new GameObject("Snow scenery (visual only)");
            visuals.transform.SetParent(transform, false);
            var plate = new GameObject("Alpine panorama").AddComponent<SpriteRenderer>();
            plate.transform.SetParent(visuals.transform, false);
            plate.sprite = backdrop;
            // Cover the fixed 16:9 arena without stretching the mountain illustration.
            float scale = Mathf.Max(18.2f / backdrop.bounds.size.x, 10.25f / backdrop.bounds.size.y);
            plate.transform.localScale = new Vector3(scale, scale, 1);
            plate.sortingOrder = -35;

            var snow = new GameObject("Fine drifting snowfall");
            snow.transform.SetParent(visuals.transform, false);
            weather = new Mesh { name = "Snowfall 112 soft flakes" };
            weather.MarkDynamic();
            snow.AddComponent<MeshFilter>().sharedMesh = weather;
            var renderer = snow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = weatherMaterial;
            renderer.sortingOrder = 3;
            vertices = new Vector3[Flakes * VerticesPerFlake];
            colors = new Color[vertices.Length];
            var triangles = new int[Flakes * 8 * 3];
            for (int i = 0; i < Flakes; i++)
                for (int j = 0; j < 8; j++)
                {
                    int t = (i * 8 + j) * 3, v = i * VerticesPerFlake;
                    triangles[t] = v; triangles[t + 1] = v + 1 + j; triangles[t + 2] = v + 1 + (j + 1) % 8;
                }
            DrawSnow();
            weather.uv = new Vector2[vertices.Length];
            weather.triangles = triangles;
            visuals.SetActive(false);
        }
        private void Update()
        {
            if (!IsVisible || Time.deltaTime <= 0) return;
            animationTime += Time.deltaTime;
            DrawSnow();
        }
        private void DrawSnow()
        {
            for (int i = 0; i < Flakes; i++)
            {
                float seed = i * .6180339887f;
                float near = i % 4 / 3f;
                float x = -9.5f + Mathf.Repeat(seed * 19f + animationTime * (.08f + near * .16f)
                    + Mathf.Sin(animationTime * .43f + i * 2.1f) * .18f, 19f);
                float y = 5.4f - Mathf.Repeat(i * .731f + animationTime * (.16f + near * .30f), 10.5f);
                Vector3 world = transform.TransformPoint(new Vector3(x, y));
                float ground = terrain != null ? terrain.SurfaceHeightAt(world.x) : -2f;
                // Flakes melt into the real surface, including newly carved craters.
                float fade = Mathf.Clamp01((world.y - ground) * 5f) * Mathf.Clamp01((5.3f - y) * 2f);
                int v = i * VerticesPerFlake;
                float radius = .014f + near * .024f;
                vertices[v] = new Vector3(x, y);
                colors[v] = new Color(.92f, .98f, 1f, (.35f + near * .4f) * fade);
                for (int j = 0; j < 8; j++)
                {
                    float angle = j * Mathf.PI / 4f;
                    vertices[v + 1 + j] = new Vector3(x + Mathf.Cos(angle) * radius, y + Mathf.Sin(angle) * radius);
                    colors[v + 1 + j] = new Color(.92f, .98f, 1f, 0);
                }
            }
            weather.vertices = vertices;
            weather.colors = colors;
            weather.RecalculateBounds();
        }
        private void OnDisable()
        {
            if (themes != null) themes.ThemeChanged -= OnThemeChanged;
            RestoreBackgrounds();
            if (visuals != null) visuals.SetActive(false);
        }
        private void OnDestroy()
        {
            if (weather != null) Destroy(weather);
            if (visuals != null) Destroy(visuals);
        }
    }
}
