using System.Collections.Generic;
using TankGame.Core;
using TankGame.Presentation;
using UnityEngine;

namespace TankGame.Terrain
{
    // One small dynamic mesh: no per-frame textures or particle GameObjects.
    public sealed class LavaHazard : MonoBehaviour
    {
        [SerializeField] private Material lavaMaterial;
        [SerializeField] private MatchFlowController match;
        [SerializeField] private float surfaceY = -3.85f;
        [SerializeField] private float left = -10f, right = 10f, bottom = -5.8f;
        private const int Columns = 96, Rows = 8, EmberCount = 22;
        private Mesh mesh;
        private Material material;
        private Vector3[] vertices;
        private Color[] colors;
        private Vector2[] uv;
        private float clock;
        private int matchNumber = -1;
        private readonly List<HiddenSprite> hidden = new List<HiddenSprite>();
        private readonly List<SinkingSprite> sinking = new List<SinkingSprite>();
        private sealed class HiddenSprite { public SpriteRenderer Renderer; public bool WasEnabled; }
        private sealed class SinkingSprite { public SpriteRenderer Renderer; public Vector3 Start; public float Age; }
        public float SurfaceY => surfaceY;
        public float AnimationTime => clock;
        public int Splashes { get; private set; }
        public int VertexCount => vertices != null ? vertices.Length : 0;
        private void Awake()
        {
            material = lavaMaterial != null ? new Material(lavaMaterial) : new Material(Shader.Find("Sprites/Default"));
            var visual = new GameObject("Animated magma"); visual.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Bounded lava surface" }; mesh.MarkDynamic();
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = visual.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = 8;
            int grid = (Columns + 1) * Rows;
            vertices = new Vector3[grid + EmberCount * 4]; colors = new Color[vertices.Length]; uv = new Vector2[vertices.Length];
            var indices = new int[Columns * (Rows - 1) * 6 + EmberCount * 6]; int k = 0;
            for (int y = 0; y < Rows - 1; y++) for (int x = 0; x < Columns; x++)
            {
                int a = y * (Columns + 1) + x, b = a + Columns + 1;
                indices[k++] = a; indices[k++] = b; indices[k++] = a + 1;
                indices[k++] = a + 1; indices[k++] = b; indices[k++] = b + 1;
            }
            for (int e = 0; e < EmberCount; e++)
            {
                int a = grid + e * 4;
                indices[k++] = a; indices[k++] = a + 2; indices[k++] = a + 1;
                indices[k++] = a + 1; indices[k++] = a + 2; indices[k++] = a + 3;
            }
            Draw(); mesh.uv = uv; mesh.triangles = indices;
        }
        private float Wave(float x) => Mathf.Sin(x * 2.6f + clock * 1.5f) * .024f + Mathf.Sin(x * 5.2f - clock * 2f) * .012f;
        private void Draw()
        {
            for (int y = 0; y < Rows; y++) for (int x = 0; x <= Columns; x++)
            {
                int i = y * (Columns + 1) + x;
                float px = Mathf.Lerp(left, right, (float)x / Columns), t = (float)y / (Rows - 1);
                float py = y == Rows - 1 ? surfaceY + Wave(px) : y == Rows - 2 ? surfaceY - .075f + Wave(px) : Mathf.Lerp(bottom, surfaceY - .11f, (float)y / (Rows - 2));
                vertices[i] = new Vector3(px, py, 0);
                float flow = Mathf.Sin(px * 2.4f + py * 7f - clock * 1.4f) * Mathf.Sin(px * 1.2f - py * 4f + clock * .7f);
                Color deep = new Color(.28f, .025f, .018f), hot = new Color(1f, .32f, .018f);
                colors[i] = y == Rows - 1 ? new Color(1f, .78f, .15f) : y == Rows - 2 ? new Color(1f, .42f, .025f) : Color.Lerp(deep, hot, Mathf.Clamp01(.16f + t * .36f + flow * .44f));
            }
            int first = (Columns + 1) * Rows;
            for (int e = 0; e < EmberCount; e++)
            {
                float p = Mathf.Repeat(clock * (.16f + e % 3 * .025f) + e * .618034f, 1f);
                float x = Mathf.Lerp(left, right, Mathf.Repeat(e * .381966f + Mathf.Sin(clock + e) * .009f, 1f));
                float y = surfaceY + Wave(x) + Mathf.Sin(p * Mathf.PI) * (.08f + e % 4 * .05f);
                float size = .014f + Mathf.Sin(p * Mathf.PI) * .018f;
                Color color = new Color(1f, .57f, .08f, Mathf.Sin(p * Mathf.PI) * .8f);
                int i = first + e * 4;
                vertices[i] = new Vector3(x - size, y - size); vertices[i + 1] = new Vector3(x + size, y - size);
                vertices[i + 2] = new Vector3(x - size, y + size); vertices[i + 3] = new Vector3(x + size, y + size);
                for (int j = 0; j < 4; j++) colors[i + j] = color;
            }
            mesh.vertices = vertices; mesh.colors = colors; mesh.RecalculateBounds();
        }
        private void LateUpdate()
        {
            if (match != null && match.MatchNumber != matchNumber)
            { ResetPresentation(); matchNumber = match.MatchNumber; }
            if (match != null && match.IsPaused) return;
            clock += Time.deltaTime; Draw();
            for (int i = sinking.Count - 1; i >= 0; i--)
            {
                var part = sinking[i]; part.Age += Time.deltaTime;
                if (part.Age >= .9f) { Destroy(part.Renderer.gameObject); sinking.RemoveAt(i); continue; }
                part.Renderer.transform.position = part.Start + Vector3.down * part.Age * 1.25f;
                part.Renderer.transform.Rotate(0, 0, Time.deltaTime * 26f);
                part.Renderer.color = Color.Lerp(Color.white, new Color(.35f, .06f, .015f, 0), part.Age / .9f);
            }
        }
        public bool Contains(HealthComponent tank)
        {
            if (tank == null || !isActiveAndEnabled) return false;
            var collider = tank.GetComponent<Collider2D>();
            Vector3 point = collider != null && collider.enabled ? collider.bounds.center : tank.transform.position;
            float foot = collider != null && collider.enabled ? collider.bounds.min.y : point.y;
            return point.x >= left && point.x <= right && foot <= surfaceY + Wave(point.x);
        }
        public void Engulf(HealthComponent tank)
        {
            Splashes++;
            var point = new Vector2(Mathf.Clamp(tank.transform.position.x, left, right), surfaceY);
            if (BattleFeedback.Active != null) BattleFeedback.Active.Burst(point, new Color(1f, .42f, .035f), 1.15f, 14, .75f);
            foreach (var sprite in tank.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!sprite.enabled || sprite.sprite == null) continue;
                hidden.Add(new HiddenSprite { Renderer = sprite, WasEnabled = sprite.enabled });
                var clone = new GameObject("Sinking tank fragment").AddComponent<SpriteRenderer>(); clone.transform.SetParent(transform);
                clone.sprite = sprite.sprite; clone.sharedMaterial = sprite.sharedMaterial; clone.flipX = sprite.flipX; clone.flipY = sprite.flipY;
                clone.transform.SetPositionAndRotation(sprite.transform.position, sprite.transform.rotation); clone.transform.localScale = sprite.transform.lossyScale;
                clone.sortingLayerID = sprite.sortingLayerID; clone.sortingOrder = 7;
                sinking.Add(new SinkingSprite { Renderer = clone, Start = clone.transform.position }); sprite.enabled = false;
            }
        }
        public void ResetPresentation()
        {
            foreach (var sprite in hidden) if (sprite.Renderer != null) sprite.Renderer.enabled = sprite.WasEnabled;
            hidden.Clear();
            foreach (var part in sinking) if (part.Renderer != null) Destroy(part.Renderer.gameObject);
            sinking.Clear();
        }
        private void OnDisable() => ResetPresentation();
        private void OnDestroy() { if (mesh != null) Destroy(mesh); if (material != null) Destroy(material); }
    }
}
