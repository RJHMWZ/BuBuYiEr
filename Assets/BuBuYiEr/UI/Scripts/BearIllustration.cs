using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public enum BearDrawing { Lock }

    // Small illustrations share the menu's chocolate ink, soft fills and rounded silhouettes.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BearIllustration : MaskableGraphic
    {
        public BearDrawing drawing;
        private VertexHelper mesh;
        private Rect drawingRect;
        private static readonly Vector2[] unitCircle = BuildUnitCircle();

        private static Vector2[] BuildUnitCircle()
        {
            var points = new Vector2[24];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / points.Length;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            return points;
        }
        private readonly Color ink = BearGameTheme.Brown;
        private readonly Color paper = BearGameTheme.Cream;
        private readonly Color pink = new Color32(255, 184, 187, 255);
        private readonly Color mint = new Color32(183, 215, 188, 255);
        private readonly Color honey = new Color32(255, 222, 113, 255);

        public static BearIllustration Create(Transform parent, string name, BearDrawing kind,
            float x, float y, float width, float height)
        {
            GameObject node = BearMenuFactory.Node(parent, name, x, y, width, height).gameObject;
            node.AddComponent<CanvasRenderer>();
            BearIllustration icon = node.AddComponent<BearIllustration>();
            icon.drawing = kind;
            icon.raycastTarget = false;
            return icon;
        }

        protected override void Awake()
        {
            EnsureCanvasRenderer();
            base.Awake();
        }

        protected override void OnEnable()
        {
            // Existing objects can survive a script reload without gaining new requirements.
            EnsureCanvasRenderer();
            base.OnEnable();
        }

        private void EnsureCanvasRenderer()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            mesh = vh;
            drawingRect = GetPixelAdjustedRect();
            Lock();
        }

        private void Lock()
        {
            Round(39, 60, 50, 58, 24, honey);
            RoundedFill(50, 70, 28, 36, 13, paper);
            Round(24, 19, 80, 63, 17, honey);
            Heart(64, 53, 13, pink);
            Line(64, 46, 64, 36, 5, ink);
        }

        private void Heart(float x, float y, float size, Color tint)
        {
            Oval(x - size * .38f, y + size * .24f, size * .54f, size * .52f, tint);
            Oval(x + size * .38f, y + size * .24f, size * .54f, size * .52f, tint);
            int start = mesh.currentVertCount;
            AddPoint(new Vector2(x - size * .88f, y + size * .15f), tint);
            AddPoint(new Vector2(x, y - size), tint);
            AddPoint(new Vector2(x + size * .88f, y + size * .15f), tint);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private void Round(float x, float y, float w, float h, float radius, Color tint, float stroke = 4)
        {
            RoundedFill(x, y, w, h, radius, ink);
            RoundedFill(x + stroke, y + stroke, w - stroke * 2, h - stroke * 2, Mathf.Max(1, radius - stroke), tint);
        }

        private void RoundedFill(float x, float y, float w, float h, float radius, Color tint)
        {
            if (w <= 0 || h <= 0) return;
            radius = Mathf.Min(radius, Mathf.Min(w, h) * .5f);
            int start = mesh.currentVertCount;
            AddPoint(new Vector2(x + w * .5f, y + h * .5f), tint);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? x + w - radius : x + radius,
                    corner < 2 ? y + h - radius : y + radius);
                for (int j = 0; j < 7; j++) AddPoint(center + unitCircle[(corner * 6 + j) % 24] * radius, tint);
            }
            CloseFan(start, 28);
        }

        private void Oval(float x, float y, float rx, float ry, Color tint)
        {
            int start = mesh.currentVertCount;
            Vector2 center = new Vector2(x, y);
            AddPoint(center, tint);
            for (int i = 0; i < 24; i++)
                AddPoint(center + new Vector2(unitCircle[i].x * rx, unitCircle[i].y * ry), tint);
            CloseFan(start, 24);
        }

        private void Line(float x1, float y1, float x2, float y2, float width, Color tint)
        {
            Vector2 a = new Vector2(x1, y1), b = new Vector2(x2, y2);
            Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int start = mesh.currentVertCount;
            AddPoint(a + n, tint);
            AddPoint(b + n, tint);
            AddPoint(b - n, tint);
            AddPoint(a - n, tint);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
            Oval(x1, y1, width * .5f, width * .5f, tint);
            Oval(x2, y2, width * .5f, width * .5f, tint);
        }

        private void AddPoint(Vector2 point, Color tint)
        {
            mesh.AddVert(new Vector2(drawingRect.xMin + point.x * (drawingRect.width / 128),
                drawingRect.yMin + point.y * (drawingRect.height / 128)), tint, Vector2.zero);
        }

        private void CloseFan(int start, int count)
        {
            for (int i = 0; i < count; i++)
                mesh.AddTriangle(start, start + i + 1, start + (i + 1) % count + 1);
        }
    }
}
