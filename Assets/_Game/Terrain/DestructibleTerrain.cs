using System;
using UnityEngine;

namespace TankGame.Terrain
{
    [RequireComponent(typeof(PolygonCollider2D), typeof(SpriteRenderer))]
    public sealed class DestructibleTerrain : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer grassRenderer;
        [SerializeField, Min(0.01f)] private float scorchWidth = 0.16f;
        [SerializeField, Range(.1f, 1f)] private float blastCraterScale = .36f;
        [SerializeField, Range(.1f, 1f)] private float blastDepthFraction = .55f;
        [SerializeField, Min(.1f)] private float maximumBlastCraterRadius = .85f;
        private SpriteRenderer soilRenderer;
        private PolygonCollider2D groundCollider;
        private Texture2D soilTexture, grassTexture;
        private Sprite soilSprite, grassSprite;
        private Color32[] soilPixels, grassPixels;
        private float[] heights;
        private float[] scorchAmounts;
        private Color32[] cleanSoilPixels, cleanGrassPixels;
        private float[] startingHeights;
        private Vector2[] startingCollider;
        private int width, height;
        private float ppu;
        private Vector2 pivot;
        public int CraterCount { get; private set; }

        private void Awake() => InitializeSurface();

        public void Configure(SpriteRenderer grass) { grassRenderer = grass; }

        // Blast reach is a damage rule; terrain erosion has its own smaller budget.
        public bool CarveExplosion(Vector2 center, float blastRadius)
        {
            if (!float.IsFinite(blastRadius) || blastRadius <= 0f) return false;
            float radius = Mathf.Min(blastRadius * blastCraterScale, maximumBlastCraterRadius);
            return Carve(center + Vector2.up * radius * (1f - blastDepthFraction), radius);
        }

        private void InitializeSurface()
        {
            if (heights != null) return;
            soilRenderer = GetComponent<SpriteRenderer>();
            groundCollider = GetComponent<PolygonCollider2D>();
            Sprite source = soilRenderer.sprite;
            if (source == null) throw new InvalidOperationException("Terrain requires a soil sprite.");
            width = Mathf.RoundToInt(source.rect.width);
            height = Mathf.RoundToInt(source.rect.height);
            pivot = source.pivot;
            ppu = source.pixelsPerUnit;
            soilTexture = CopySpriteTexture(source);
            soilPixels = soilTexture.GetPixels32();
            cleanSoilPixels = (Color32[])soilPixels.Clone();
            soilSprite = MakeSprite(soilTexture);
            soilRenderer.sprite = soilSprite;
            if (grassRenderer != null && grassRenderer.sprite != null)
            {
                if (grassRenderer.sprite.rect.size != source.rect.size)
                    throw new InvalidOperationException("Soil and grass must have matching texture dimensions.");
                grassTexture = CopySpriteTexture(grassRenderer.sprite);
                grassPixels = grassTexture.GetPixels32();
                cleanGrassPixels = (Color32[])grassPixels.Clone();
                grassSprite = MakeSprite(grassTexture);
                grassRenderer.sprite = grassSprite;
            }
            heights = new float[width];
            scorchAmounts = new float[width * height];
            for (int x = 0; x < width; x++)
            {
                int y = height - 1;
                while (y >= 0 && soilPixels[y * width + x].a == 0) y--;
                heights[x] = (y + 1 - pivot.y) / ppu;
            }
        }

        public void ResetSurface()
        {
            InitializeSurface();
            if (startingHeights == null) CaptureInitialGeometry();
            soilPixels = (Color32[])cleanSoilPixels.Clone();
            if (cleanGrassPixels != null) grassPixels = (Color32[])cleanGrassPixels.Clone();
            heights = (float[])startingHeights.Clone();
            Array.Clear(scorchAmounts, 0, scorchAmounts.Length); CraterCount = 0;
            soilTexture.SetPixels32(soilPixels); soilTexture.Apply(false);
            if (grassTexture != null) { grassTexture.SetPixels32(grassPixels); grassTexture.Apply(false); }
            groundCollider.SetPath(0, startingCollider); Physics2D.SyncTransforms();
        }
        private void CaptureInitialGeometry()
        {
            startingHeights = (float[])heights.Clone(); startingCollider = groundCollider.GetPath(0);
        }

        /// <summary>Removes a bowl from the surface and darkens the exposed soil, only in this match.</summary>
        public bool Carve(Vector2 worldCenter, float worldRadius)
        {
            if (float.IsNaN(worldRadius) || float.IsInfinity(worldRadius) || worldRadius <= 0f) return false;
            if (!float.IsFinite(worldCenter.x) || !float.IsFinite(worldCenter.y)) return false;
            InitializeSurface();
            if (startingHeights == null) CaptureInitialGeometry();
            Vector2 center = transform.InverseTransformPoint(worldCenter);
            Vector3 scale = transform.lossyScale;
            float rx = worldRadius / Mathf.Max(0.001f, Mathf.Abs(scale.x));
            float ry = worldRadius / Mathf.Max(0.001f, Mathf.Abs(scale.y));
            int left = Mathf.Clamp(Mathf.FloorToInt((center.x - rx - scorchWidth) * ppu + pivot.x), 0, width - 1);
            int right = Mathf.Clamp(Mathf.CeilToInt((center.x + rx + scorchWidth) * ppu + pivot.x), 0, width - 1);
            bool changed = false;
            float bottom = -pivot.y / ppu;
            for (int x = left; x <= right; x++)
            {
                float localX = (x + 0.5f - pivot.x) / ppu;
                float dx = (localX - center.x) / rx;
                if (Mathf.Abs(dx) > 1f) continue;
                float halfHeight = ry * Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx));
                if (center.y + halfHeight < heights[x]) continue;
                float cut = Mathf.Max(bottom + 0.02f, center.y - halfHeight);
                if (cut >= heights[x]) continue;
                heights[x] = cut;
                changed = true;
                int firstEmpty = Mathf.Clamp(Mathf.FloorToInt(cut * ppu + pivot.y), 0, height);
                for (int y = firstEmpty; y < height; y++)
                {
                    soilPixels[y * width + x] = new Color32(0, 0, 0, 0);
                    if (grassPixels != null) grassPixels[y * width + x] = new Color32(0, 0, 0, 0);
                }
            }
            if (!changed) return false;
            // A dark band follows the bowl; intact soil outside the blast is preserved.
            for (int x = left; x <= right; x++)
                for (int y = 0; y < height; y++)
                {
                    int index = y * width + x;
                    if (soilPixels[index].a == 0) continue;
                    Vector2 point = new Vector2((x + 0.5f - pivot.x) / ppu, (y + 0.5f - pivot.y) / ppu);
                    float distance = new Vector2((point.x - center.x) / rx, (point.y - center.y) / ry).magnitude;
                    float band = scorchWidth / Mathf.Min(rx, ry);
                    if (distance > 1f + band) continue;
                    float amount = 0.85f * Mathf.Clamp01((1f + band - distance) / Mathf.Max(band, 0.001f));
                    scorchAmounts[index] = 1f - (1f - scorchAmounts[index]) * (1f - amount);
                    soilPixels[index] = Color32.Lerp(soilPixels[index], new Color32(12, 10, 9, 255), amount);
                    if (grassPixels != null && grassPixels[index].a > 0)
                        grassPixels[index] = Color32.Lerp(grassPixels[index], new Color32(18, 15, 10, 255), amount);
                }
            soilTexture.SetPixels32(soilPixels);
            soilTexture.Apply(false);
            if (grassTexture != null) { grassTexture.SetPixels32(grassPixels); grassTexture.Apply(false); }
            RebuildCollider();
            CraterCount++;
            return true;
        }

        public float SurfaceHeightAt(float worldX)
        {
            InitializeSurface();
            Vector2 local = transform.InverseTransformPoint(new Vector2(worldX, transform.position.y));
            int x = Mathf.Clamp(Mathf.FloorToInt(local.x * ppu + pivot.x), 0, width - 1);
            return transform.TransformPoint(new Vector3(local.x, heights[x], 0f)).y;
        }

        /// <summary>Changes the map palette without resetting craters, scorch marks or collision.</summary>
        public void ApplyAppearance(Sprite soil, Sprite surface)
        {
            if (soil == null || surface == null) throw new ArgumentNullException(nameof(soil));
            InitializeSurface();
            if ((int)soil.rect.width != width || (int)soil.rect.height != height
                || (int)surface.rect.width != width || (int)surface.rect.height != height
                || Mathf.Abs(soil.pixelsPerUnit - ppu) > 0.001f || Mathf.Abs(surface.pixelsPerUnit - ppu) > 0.001f
                || (soil.pivot - pivot).sqrMagnitude > 0.001f || (surface.pivot - pivot).sqrMagnitude > 0.001f)
                throw new InvalidOperationException("Terrain themes must share dimensions, PPU and pivots.");
            var sourceSoil = CopySpriteTexture(soil);
            var sourceSurface = CopySpriteTexture(surface);
            try
            {
                var newSoil = sourceSoil.GetPixels32();
                var newSurface = sourceSurface.GetPixels32();
                cleanSoilPixels = (Color32[])newSoil.Clone(); cleanGrassPixels = (Color32[])newSurface.Clone();
                // Keep exactly the same occupancy mask. Theme changes cannot refill a hole.
                for (int i = 0; i < soilPixels.Length; i++)
                {
                    byte alpha = soilPixels[i].a;
                    soilPixels[i] = Color32.Lerp(newSoil[i], new Color32(12, 10, 9, 255), scorchAmounts[i]);
                    soilPixels[i].a = alpha;
                    if (grassPixels != null)
                    {
                        byte surfaceAlpha = grassPixels[i].a;
                        grassPixels[i] = Color32.Lerp(newSurface[i], new Color32(18, 15, 10, 255), scorchAmounts[i]);
                        grassPixels[i].a = surfaceAlpha;
                    }
                }
                soilTexture.SetPixels32(soilPixels);
                soilTexture.Apply(false);
                if (grassTexture != null) { grassTexture.SetPixels32(grassPixels); grassTexture.Apply(false); }
            }
            finally { Destroy(sourceSoil); Destroy(sourceSurface); }
        }

        private void RebuildCollider()
        {
            const int stride = 3; // 3 cm at 100 PPU; collision follows the visible crater closely.
            int samples = (width - 1 + stride - 1) / stride + 1;
            var points = new Vector2[samples + 2];
            for (int i = 0; i < samples; i++)
            {
                int x = Mathf.Min(i * stride, width - 1);
                float localX = i == 0 ? -pivot.x / ppu : (x == width - 1 ? (width - pivot.x) / ppu : (x + 0.5f - pivot.x) / ppu);
                points[i] = new Vector2(localX, heights[x]) - groundCollider.offset;
            }
            float bottom = -pivot.y / ppu;
            points[samples] = new Vector2((width - pivot.x) / ppu, bottom) - groundCollider.offset;
            points[samples + 1] = new Vector2(-pivot.x / ppu, bottom) - groundCollider.offset;
            groundCollider.pathCount = 1;
            groundCollider.SetPath(0, points);
            Physics2D.SyncTransforms();
        }

        private Sprite MakeSprite(Texture2D texture) => Sprite.Create(texture,
            new Rect(0, 0, width, height), new Vector2(pivot.x / width, pivot.y / height), ppu, 0, SpriteMeshType.FullRect);

        private static Texture2D CopySpriteTexture(Sprite sprite)
        {
            Texture2D source = sprite.texture;
            var result = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.RGBA32, false);
            result.filterMode = source.filterMode;
            result.wrapMode = TextureWrapMode.Clamp;
            RenderTexture previous = RenderTexture.active;
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            try
            {
                Graphics.Blit(source, temp);
                RenderTexture.active = temp;
                result.ReadPixels(sprite.rect, 0, 0);
                result.Apply(false);
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temp); }
            return result;
        }

        private void OnDestroy()
        {
            if (soilSprite != null) Destroy(soilSprite);
            if (grassSprite != null) Destroy(grassSprite);
            if (soilTexture != null) Destroy(soilTexture);
            if (grassTexture != null) Destroy(grassTexture);
        }
    }
}
