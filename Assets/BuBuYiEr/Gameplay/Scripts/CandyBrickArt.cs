using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    // Shared by every brick and the shop preview; never varies with level, HP or brick colour.
    public static class CandyBrickArt
    {
        private const int CornerSteps = 7;
        private static readonly Vector2[] corners = BuildCorners();
        private static readonly Vector2[] ring = BuildRing();
        private static readonly Vector2[] heart = {
            new Vector2(0, -.48f), new Vector2(-.42f, -.08f),
            new Vector2(-.5f, .16f), new Vector2(-.43f, .36f),
            new Vector2(-.24f, .43f), new Vector2(-.08f, .35f),
            new Vector2(0, .23f), new Vector2(.08f, .35f),
            new Vector2(.24f, .43f), new Vector2(.43f, .36f),
            new Vector2(.5f, .16f), new Vector2(.42f, -.08f) };

        public static void Draw(VertexHelper vh, Rect rect, CandyBrickSkin skin, float opacity = 1, float flash = 0)
        {
            if (rect.width <= 0 || rect.height <= 0 || opacity <= 0) return;
            float unit = Mathf.Min(rect.height / 41f, rect.width / 76f);
            float radius = 11 * unit;
            float shine = Mathf.Clamp01(flash) * .6f;
            Color rim = Tint(skin.rim, opacity, shine);
            Color top = Tint(skin.top, opacity, shine);
            Color bottom = Tint(skin.bottom, opacity, shine);
            Color accent = Tint(skin.accent, opacity, shine);

            Rounded(vh, new Rect(rect.x, rect.y - 2 * unit, rect.width, rect.height), radius,
                new Color(.72f, .34f, .47f, .10f * opacity), new Color(.72f, .34f, .47f, .10f * opacity));
            Rounded(vh, rect, radius, rim, rim);
            Rect inside = Inset(rect, 1.4f * unit);
            Color baseColor = Tint(skin.baseColor, opacity, shine);
            Rounded(vh, inside, radius - 1.4f * unit, baseColor, baseColor);
            Rect face = new Rect(inside.x, inside.y + 3 * unit, inside.width, inside.height - 3 * unit);
            Rounded(vh, face, radius - 1.4f * unit, top, bottom);

            // A soft milk glaze and a short glint leave the entire centre clear for the number.
            Rounded(vh, new Rect(face.x + 5 * unit, face.yMax - 11 * unit, face.width - 10 * unit, 8 * unit),
                4 * unit, new Color(1, 1, 1, .54f * opacity), new Color(1, 1, 1, .02f * opacity));
            Rounded(vh, new Rect(face.x + 10 * unit, face.yMax - 5.3f * unit, face.width * .27f, 2 * unit),
                unit, new Color(1, 1, 1, .9f * opacity), new Color(1, 1, 1, .65f * opacity));
            Rounded(vh, new Rect(inside.x + 9 * unit, inside.y + unit, inside.width - 18 * unit, 1.2f * unit),
                .6f * unit, new Color(1, 1, 1, .32f * opacity), new Color(1, 1, 1, .32f * opacity));
            Motif(vh, new Vector2(face.x + face.width * .17f, face.center.y - unit), 9 * unit, accent, skin.motif);
            Motif(vh, new Vector2(face.xMax - face.width * .17f, face.center.y - unit), 9 * unit, accent, skin.motif);
        }

        private static void Motif(VertexHelper vh, Vector2 center, float size, Color color, CandyBrickMotif motif)
        {
            if (motif == CandyBrickMotif.Star) CandyBallArt.Star(vh, center, size * .60f, color);
            else if (motif == CandyBrickMotif.Cloud)
            {
                Disc(vh, center + new Vector2(-.26f, -.03f) * size, size * .27f, color);
                Disc(vh, center + new Vector2(.23f, -.03f) * size, size * .29f, color);
                Disc(vh, center + Vector2.up * size * .17f, size * .33f, color);
                Rounded(vh, new Rect(center.x - size * .53f, center.y - size * .34f, size * 1.06f, size * .48f),
                    size * .20f, color, color);
            }
            else if (motif == CandyBrickMotif.Flower)
            {
                for (int i = 0; i < 5; i++)
                {
                    float angle = (90 + i * 72) * Mathf.Deg2Rad;
                    Disc(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size * .29f, size * .23f, color);
                }
                Color cream = Color.Lerp(color, Color.white, .70f); cream.a = color.a;
                Disc(vh, center, size * .17f, cream);
            }
            else Heart(vh, center, size, color);
        }

        private static void Disc(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i < ring.Length; i++) vh.AddVert(center + ring[i] * radius, color, Vector2.zero);
            for (int i = 0; i < ring.Length; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % ring.Length);
        }

        private static Vector2[] BuildRing()
        {
            var directions = new Vector2[12];
            for (int i = 0; i < directions.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / directions.Length;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            return directions;
        }

        private static Color Tint(Color color, float opacity, float flash)
        {
            color = Color.Lerp(color, Color.white, flash);
            color.a *= opacity;
            return color;
        }

        private static Rect Inset(Rect rect, float amount) =>
            new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2, rect.height - amount * 2);

        private static void Heart(VertexHelper vh, Vector2 center, float size, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i < heart.Length; i++) vh.AddVert(center + heart[i] * size, color, Vector2.zero);
            for (int i = 0; i < heart.Length; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % heart.Length);
        }

        private static void Rounded(VertexHelper vh, Rect rect, float radius, Color top, Color bottom)
        {
            radius = Mathf.Clamp(radius, 0, Mathf.Min(rect.width, rect.height) * .5f);
            int start = vh.currentVertCount;
            vh.AddVert(rect.center, Color.Lerp(top, bottom, .5f), Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int i = 0; i <= CornerSteps; i++)
                {
                    Vector2 p = center + corners[corner * (CornerSteps + 1) + i] * radius;
                    vh.AddVert(p, Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, p.y)), Vector2.zero);
                }
            }
            const int count = 4 * (CornerSteps + 1);
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }

        private static Vector2[] BuildCorners()
        {
            var directions = new Vector2[4 * (CornerSteps + 1)];
            for (int corner = 0; corner < 4; corner++)
                for (int i = 0; i <= CornerSteps; i++)
                {
                    float angle = (corner * 90 + i * 90f / CornerSteps) * Mathf.Deg2Rad;
                    directions[corner * (CornerSteps + 1) + i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }
            return directions;
        }
    }
}
