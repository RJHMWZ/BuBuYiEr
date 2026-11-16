using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CandyRoomBackdrop : MaskableGraphic
    {
        public int chapter;
        public static Color Accent(int chapter)
        {
            switch(chapter)
            {
                case 1:return new Color32(244,173,177,255);
                case 2:return new Color32(175,217,188,255);
                case 3:return new Color32(215,181,153,255);
                case 4:return new Color32(190,184,228,255);
                case 5:return new Color32(189,216,226,255);
                default:return new Color32(244,194,164,255);
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect rect=rectTransform.rect;
            Quad(vh,rect,new Color32(255,248,234,255));
            Color tint=Accent(chapter);tint.a=.16f;
            const float step=84;
            for(float x=rect.xMin;x<rect.xMax;x+=step*2)
                Quad(vh,new Rect(x,rect.yMin,Mathf.Min(step,rect.xMax-x),rect.height),tint);
            for(float y=rect.yMin;y<rect.yMax;y+=step*2)
                Quad(vh,new Rect(rect.xMin,y,rect.width,Mathf.Min(step,rect.yMax-y)),tint);
        }
        private static void Quad(VertexHelper vh,Rect r,Color c)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector2(r.xMin,r.yMax),c,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMin),c,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
