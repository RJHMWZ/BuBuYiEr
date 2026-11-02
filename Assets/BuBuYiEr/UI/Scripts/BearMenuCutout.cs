using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    // Clips the ORIGINAL reference sprite with a native mesh; texture pixels are never repainted.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BearMenuCutout : MaskableGraphic
    {
        public Sprite sprite;
        public string silhouette = "rectangle";
        public override Texture mainTexture => sprite != null ? sprite.texture : base.mainTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null) return;
            Rect r = GetPixelAdjustedRect();
            Rect uv = sprite.textureRect;
            Vector2[] points;
            if (silhouette == "circle")
            {
                points = new Vector2[64];
                for (int i = 0; i < points.Length; i++)
                {
                    float a = i * Mathf.PI * 2 / points.Length;
                    points[i] = new Vector2(.5f + .485f * Mathf.Cos(a), .5f + .485f * Mathf.Sin(a));
                }
            }
            else if (silhouette == "capsule")
            {
                points = new Vector2[48];
                float rx = Mathf.Min(.5f, r.height / r.width * .5f);
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 c = new Vector2(corner == 0 || corner == 3 ? 1-rx : rx, corner < 2 ? 1-.5f : .5f);
                    for (int step = 0; step < 12; step++)
                    {
                        float angle = (corner * 90 + step * 90f / 11) * Mathf.Deg2Rad;
                        points[corner * 12 + step] = c + new Vector2(rx * Mathf.Cos(angle), .5f * Mathf.Sin(angle));
                    }
                }
            }
            else if (silhouette == "heart")
                points = new[] { new Vector2(.5f,.02f),new Vector2(.14f,.30f),new Vector2(.02f,.57f),new Vector2(.05f,.82f),new Vector2(.23f,.97f),new Vector2(.40f,.94f),new Vector2(.5f,.83f),new Vector2(.64f,.96f),new Vector2(.83f,.95f),new Vector2(.98f,.77f),new Vector2(.96f,.53f),new Vector2(.84f,.30f) };
            else if (silhouette == "jar")
                points = new[] { new Vector2(.17f,.02f),new Vector2(.03f,.12f),new Vector2(.02f,.62f),new Vector2(.15f,.76f),new Vector2(.16f,.84f),new Vector2(.12f,.88f),new Vector2(.12f,.97f),new Vector2(.86f,.97f),new Vector2(.88f,.88f),new Vector2(.82f,.82f),new Vector2(.85f,.75f),new Vector2(.98f,.62f),new Vector2(.98f,.13f),new Vector2(.86f,.02f) };
            else points = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            // All supplied outlines are star-shaped around their interior center.
            Vector2 center = new Vector2(.5f, .5f);
            Add(vh, center, r, uv);
            for (int i = 0; i < points.Length; i++) Add(vh, points[i], r, uv);
            for (int i = 0; i < points.Length; i++) vh.AddTriangle(0, i + 1, (i + 1) % points.Length + 1);
        }

        private void Add(VertexHelper vh, Vector2 p, Rect r, Rect uv)
        {
            vh.AddVert(new Vector3(r.xMin + r.width * p.x, r.yMin + r.height * p.y), color,
                new Vector2((uv.x + uv.width * p.x) / sprite.texture.width, (uv.y + uv.height * p.y) / sprite.texture.height));
        }
    }
}
