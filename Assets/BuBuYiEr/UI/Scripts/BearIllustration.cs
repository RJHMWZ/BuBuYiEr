using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public enum BearDrawing { Jar, Slot, Magnet, Hammer, Key, Cooler, Lock, Warm }

    // Small illustrations share the menu's chocolate ink, soft fills and rounded silhouettes.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BearIllustration : MaskableGraphic
    {
        public BearDrawing drawing;
        public Color candyColor = new Color32(255, 184, 187, 255);
        public float fill;
        public bool highlighted;
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

        public static BearDrawing Tool(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return BearDrawing.Magnet;
                case PowerUpType.Hammer: return BearDrawing.Hammer;
                case PowerUpType.Key: return BearDrawing.Key;
                case PowerUpType.Cooler: return BearDrawing.Cooler;
                default: return BearDrawing.Slot;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            mesh = vh;
            drawingRect = GetPixelAdjustedRect();
            switch (drawing)
            {
                case BearDrawing.Jar: Jar(); break;
                case BearDrawing.Hammer: Hammer(); break;
                case BearDrawing.Magnet: Magnet(); break;
                case BearDrawing.Key: Key(); break;
                case BearDrawing.Cooler: Snowflake(); break;
                case BearDrawing.Lock: Lock(); break;
                case BearDrawing.Warm:
                    Round(32, 17, 64, 70, 25, pink);
                    Line(45, 93, 40, 113, 7, pink);
                    Line(64, 96, 64, 119, 7, pink);
                    Line(83, 93, 88, 113, 7, pink);
                    Heart(64, 49, 14, paper);
                    break;
                default:
                    Round(21, 27, 68, 72, 20, mint);
                    Round(34, 39, 43, 47, 12, paper);
                    Round(79, 35, 20, 68, 8, pink);
                    Round(29, 92, 54, 18, 7, honey);
                    Line(99, 77, 121, 77, 6, ink);
                    Line(110, 66, 110, 88, 6, ink);
                    break;
            }
        }

        private void Jar()
        {
            if (highlighted) Oval(64, 65, 60, 60, new Color(1, .82f, .28f, .42f));
            Oval(64, 15, 44, 8, new Color(.32f, .16f, .04f, .15f));
            Round(22, 17, 84, 91, 25, Color.Lerp(paper, candyColor, .10f));
            Round(34, 94, 60, 22, 8, paper);
            int count = Mathf.Clamp(Mathf.CeilToInt(fill * 9), 0, 9);
            for (int i = 0; i < count; i++)
            {
                float x = 34 + i % 3 * 21;
                float y = 29 + i / 3 * 18;
                Round(x, y, 19, 17, 6, Color.Lerp(candyColor, paper, (i % 3) * .09f), 2.5f);
                Line(x + 5, y + 12, x + 9, y + 13, 2, new Color(1, 1, 1, .75f));
            }
            Oval(45, 80, 3, 3.4f, ink);
            Oval(83, 80, 3, 3.4f, ink);
            Oval(36, 73, 6, 3, pink);
            Oval(92, 73, 6, 3, pink);
            Line(59, 76, 64, 73, 2.5f, ink);
            Line(64, 73, 69, 76, 2.5f, ink);
            if (count == 0) Heart(64, 49, 14, candyColor);
            Line(32, 87, 32, 96, 4, Color.white);
            Round(27, 105, 74, 18, 8, honey, 4);
            Line(38, 116, 59, 116, 3, paper);
            Round(52, 104, 24, 20, 6, candyColor, 3);
        }

        private void Hammer()
        {
            Round(54, 12, 22, 77, 9, new Color32(226, 178, 125, 255));
            Line(62, 23, 62, 67, 3, paper);
            Round(25, 78, 81, 36, 12, pink);
            Round(20, 77, 20, 38, 8, honey, 4);
            Round(92, 77, 20, 38, 8, honey, 4);
            Heart(66, 96, 10, paper);
        }

        private void Magnet()
        {
            Round(23, 18, 82, 94, 36, pink);
            // An open center makes the curved horseshoe silhouette.
            RoundedFill(45, 54, 38, 68, 18, paper);
            Round(20, 87, 29, 29, 7, honey, 4);
            Round(79, 87, 29, 29, 7, mint, 4);
            Line(34, 34, 43, 28, 3, paper);
            Heart(64, 35, 9, paper);
        }

        private void Key()
        {
            Round(55, 15, 18, 64, 7, honey, 4);
            Round(66, 19, 24, 13, 4, honey, 3);
            Round(66, 39, 20, 13, 4, honey, 3);
            Oval(64, 91, 29, 29, ink);
            Oval(64, 91, 24, 24, honey);
            Heart(64, 93, 15, ink);
            Heart(64, 94, 10, paper);
            Line(47, 106, 53, 109, 3, paper);
        }

        private void Lock()
        {
            Round(39, 60, 50, 58, 24, honey);
            RoundedFill(50, 70, 28, 36, 13, paper);
            Round(24, 19, 80, 63, 17, honey);
            Heart(64, 53, 13, pink);
            Line(64, 46, 64, 36, 5, ink);
        }

        private void Snowflake()
        {
            Oval(64, 64, 49, 49, paper);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 n = new Vector2(-d.y, d.x);
                Vector2 c = new Vector2(64, 64);
                Line(c.x, c.y, c.x + d.x * 44, c.y + d.y * 44, 9, ink);
                Line(c.x, c.y, c.x + d.x * 43, c.y + d.y * 43, 5, mint);
                Vector2 fork = c + d * 27;
                Vector2 left = c + d * 34 + n * 12;
                Vector2 right = c + d * 34 - n * 12;
                Line(fork.x, fork.y, left.x, left.y, 5, ink);
                Line(fork.x, fork.y, right.x, right.y, 5, ink);
            }
            Heart(64, 64, 13, pink);
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
