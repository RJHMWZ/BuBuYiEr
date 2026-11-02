using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    // Native uGUI geometry: no generated replacement artwork is used for rounded controls.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BearMenuShape : MaskableGraphic
    {
        public float radius = 36;
        public float border = 4;
        public Color borderColor = new Color32(102, 59, 22, 255);
        public Color bottomColor = new Color32(255, 230, 188, 255);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Rect inside = new Rect(rect.x + border, rect.y + border, Mathf.Max(0, rect.width - border * 2), Mathf.Max(0, rect.height - border * 2));
            if (border > 0) AddBorder(vh, rect, inside, radius, Mathf.Max(0,radius-border), borderColor);
            AddRounded(vh, inside, Mathf.Max(0, radius - border), bottomColor, color);
        }

        private static void AddBorder(VertexHelper vh, Rect outer, Rect inner, float outerRadius, float innerRadius, Color color)
        {
            int start=vh.currentVertCount;
            const int steps=12;
            for(int corner=0;corner<4;corner++)
            {
                Vector2 oc=new Vector2(corner==0||corner==3?outer.xMax-outerRadius:outer.xMin+outerRadius,corner<2?outer.yMax-outerRadius:outer.yMin+outerRadius);
                Vector2 ic=new Vector2(corner==0||corner==3?inner.xMax-innerRadius:inner.xMin+innerRadius,corner<2?inner.yMax-innerRadius:inner.yMin+innerRadius);
                for(int i=0;i<=steps;i++)
                {
                    float a=(corner*90+i*90f/steps)*Mathf.Deg2Rad;
                    Vector2 direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    vh.AddVert(oc+direction*outerRadius,color,Vector2.zero);
                    vh.AddVert(ic+direction*innerRadius,color,Vector2.zero);
                }
            }
            int count=4*(steps+1);
            for(int i=0;i<count;i++)
            {
                int a=start+i*2,b=start+((i+1)%count)*2;
                vh.AddTriangle(a,b,a+1);vh.AddTriangle(a+1,b,b+1);
            }
        }

        private static void AddRounded(VertexHelper vh, Rect rect, float radius, Color bottom, Color top)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            radius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * .5f);
            int start = vh.currentVertCount;
            vh.AddVert(rect.center, Color.Lerp(bottom, top, .5f), Vector2.zero);
            const int steps = 12;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (corner * 90 + i * 90f / steps) * Mathf.Deg2Rad;
                    Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    vh.AddVert(p, Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, p.y)), Vector2.zero);
                }
            }
            int count = 4 * (steps + 1);
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }
    }
}
