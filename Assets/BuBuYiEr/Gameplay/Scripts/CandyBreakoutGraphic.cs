using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClawbearGames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CandyBreakoutGraphic : MaskableGraphic, IPointerDownHandler, IDragHandler
    {
        public CandyBreakoutSimulation Board;
        public static readonly Color32[] CandyColors = {
            new Color32(247,211,124,255), new Color32(239,166,180,255),
            new Color32(166,192,234,255), new Color32(164,212,183,255),
            new Color32(201,163,140,255), new Color32(203,180,226,255) };
        private float contentBottom = 440;
        private float paintOpacity=1;
        private CandyBreakoutSimulation feedbackBoard;
        private struct DepartingBrick
        {
            public CandyBreakoutSimulation.Brick Brick;
            public float Remaining;
        }
        private readonly List<DepartingBrick> departing=new List<DepartingBrick>();
        public void PlayBreak(CandyBreakoutSimulation.Brick brick)
        {
            if(brick==null)return;
            for(int i=0;i<departing.Count;i++) if(departing[i].Brick==brick)return;
            departing.Add(new DepartingBrick { Brick=brick,Remaining=.18f });
            SetVerticesDirty();
        }
        public void AdvanceFeedback(float delta)
        {
            if(delta<=0)return;
            for(int i=departing.Count-1;i>=0;i--)
            {
                var item=departing[i];item.Remaining-=delta;
                if(item.Remaining<=0)departing.RemoveAt(i);else departing[i]=item;
            }
        }
        protected override void OnDisable()
        {
            departing.Clear();
            paintOpacity=1;
            base.OnDisable();
        }

        public void FitPresentation()
        {
            if(feedbackBoard!=Board){feedbackBoard=Board;departing.Clear();}
            float bottom = 0;
            if (Board != null)
                foreach (var brick in Board.Bricks) bottom = Mathf.Max(bottom, brick.Bounds.yMax);
            // One reversible presentation transform for bricks, balls, labels and input.
            // Logical bounds and all simulation/collision data remain untouched.
            contentBottom = Mathf.Clamp(bottom + 26, 240, 665);
            SetVerticesDirty();
        }
        private float BandHeight
        {
            get
            {
                float height=rectTransform.rect.height;
                bool boss=contentBottom>600;
                return Mathf.Max(96,Mathf.Min(height * (boss ? .83f : .72f), height - (boss ? 172 : 212)));
            }
        }
        private float VisualY(float y)
        {
            float h = rectTransform.rect.height;
            if (y <= 55) return y / 55 * 32;
            return y <= contentBottom ? 32 + (y-55) / (contentBottom-55) * (BandHeight-32)
                : BandHeight + (y - contentBottom) / (760 - contentBottom) * (h - BandHeight);
        }
        public Vector2 ToLocalPoint(Vector2 p) => new Vector2(
            rectTransform.rect.xMin + p.x / 900 * rectTransform.rect.width,
            rectTransform.rect.yMax - VisualY(p.y));
        public Vector2 ToBoardPoint(Vector2 local)
        {
            Rect r = rectTransform.rect;
            float y = r.yMax - local.y;
            float logicalY = y <= 32 ? y / 32 * 55 : y <= BandHeight ? 55 + (y-32) / (BandHeight-32) * (contentBottom-55)
                : contentBottom + (y - BandHeight) / (r.height - BandHeight) * (760 - contentBottom);
            return new Vector2((local.x - r.xMin) / r.width * 900, logicalY);
        }
        public Rect VisualRect(Rect r)
        {
            Vector2 top = ToLocalPoint(new Vector2(r.xMin, r.yMin));
            Vector2 bottom = ToLocalPoint(new Vector2(r.xMax, r.yMax));
            return new Rect(top.x, bottom.y, bottom.x - top.x, top.y - bottom.y);
        }
        public void OnPointerDown(PointerEventData e) => Tap(e);
        public void OnDrag(PointerEventData e) => Tap(e);
        private void Tap(PointerEventData e)
        {
            IngameManager game = IngameManager.Instance;
            if (game == null || !game.CanInteract || Board != game.Board) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.IsBlockingInput) return;
#endif
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out Vector2 local))
                Board.Click(ToBoardPoint(local));
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Board == null) return;
            paintOpacity=1;
            foreach(var brick in Board.Bricks)
                if(brick.Alive)DrawBrick(vh,brick,1,1);
            foreach(var item in departing)
            {
                float t=1-Mathf.Clamp01(item.Remaining/.18f);
                DrawBrick(vh,item.Brick,Mathf.Lerp(1,.76f,t),1-t*t);
            }
            paintOpacity=1;
            foreach (var ball in Board.Balls)
            {
                Vector2 direction = ball.Velocity.normalized;
                Color c = CandyColors[ball.Kind];
                for (int i = 4; i > 0; i--)
                    Circle(vh, ball.Position - direction * i * 9, 10 - i * 1.4f, new Color(c.r,c.g,c.b,.17f));
                Circle(vh, ball.Position, 12.5f, BearGameTheme.Brown);
                Circle(vh, ball.Position, 9.5f, c);
                Circle(vh, ball.Position + new Vector2(-3,-3), 2.7f, new Color(1,1,1,.9f));
            }
            foreach (var spark in Board.Sparks)
            {
                Color c = spark.Color; c.a = spark.Life * 1.6f;
                Circle(vh, spark.Position, 3.5f * spark.Life * 2, c);
            }
        }
        private void DrawBrick(VertexHelper vh,CandyBreakoutSimulation.Brick brick,float scale,float opacity)
        {
            paintOpacity=opacity;
            Rect r=VisualRect(brick.Bounds);
            if(scale!=1)r=new Rect(r.center-r.size*scale*.5f,r.size*scale);
            float radius=brick.Boss?18:8;
            Rounded(vh,new Rect(r.x-1,r.y-5,r.width+2,r.height+1),radius+1,
                new Color32(104,65,32,12),new Color32(104,65,32,12));
            Rounded(vh,new Rect(r.x,r.y-3,r.width,r.height),radius,
                new Color32(104,65,32,23),new Color32(104,65,32,23));
            Rounded(vh,r,radius,BearGameTheme.Brown,BearGameTheme.Brown);
            Color c=brick.PoisonTime>0?(Color)new Color32(167,222,176,255):CandyColors[brick.Color];
            if(brick.Alive&&brick.Flash>0)c=Color.Lerp(c,Color.white,.6f);
            Rect inner=new Rect(r.x+2.8f,r.y+2.8f,r.width-5.6f,r.height-5.6f);
            Rounded(vh,inner,radius-2.8f,Color.Lerp(c,Color.white,.18f),c);
            Rounded(vh,new Rect(inner.x+5,inner.yMax-6,inner.width-10,3),1.5f,
                new Color(1,1,1,.50f),new Color(1,1,1,.32f));
            Rounded(vh,new Rect(inner.x+4,inner.y+1.5f,inner.width-8,2.5f),1.25f,
                new Color(.32f,.17f,.07f,.12f),new Color(.32f,.17f,.07f,.18f));
            if(brick.Boss)
            {
                Dot(vh,r.center+new Vector2(-56,12)*scale,5*scale,BearGameTheme.Brown);
                Dot(vh,r.center+new Vector2(56,12)*scale,5*scale,BearGameTheme.Brown);
            }
            if(brick.Alive&&brick.Health<brick.MaxHealth)
                Rounded(vh,new Rect(r.x+8,r.y+5,(r.width-16)*brick.Health/brick.MaxHealth,3),1.5f,
                    new Color(.32f,.17f,.07f,.55f),new Color(.32f,.17f,.07f,.55f));
            paintOpacity=1;
        }
        private void Rounded(VertexHelper vh, Rect r, float radius, Color top, Color bottom)
        {
            if (r.width <= 0 || r.height <= 0) return;
            top.a*=paintOpacity;bottom.a*=paintOpacity;
            radius = Mathf.Clamp(radius,0, Mathf.Min(r.width,r.height) * .5f);
            int n = vh.currentVertCount;
            vh.AddVert(r.center, Color.Lerp(top,bottom,.5f), Vector2.zero);
            const int segments = 5;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? r.xMax-radius : r.xMin+radius,
                    corner < 2 ? r.yMax-radius : r.yMin+radius);
                for (int i = 0; i <= segments; i++)
                {
                    float a = (corner * 90 + i * 90f / segments) * Mathf.Deg2Rad;
                    Vector2 p = center + new Vector2(Mathf.Cos(a),Mathf.Sin(a)) * radius;
                    vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);
                }
            }
            const int count = 4 * (segments+1);
            for(int i=0;i<count;i++) vh.AddTriangle(n,n+1+i,n+1+(i+1)%count);
        }
        private void Dot(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            color.a*=paintOpacity;
            int n = vh.currentVertCount;
            vh.AddVert(center,color,Vector2.zero);
            for(int i=0;i<=20;i++)
            {
                float a=i*Mathf.PI/10;
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);
                if(i>0) vh.AddTriangle(n,n+i,n+i+1);
            }
        }
        private void Circle(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            // Preserve a round candy silhouette while its centre follows the reversible board transform.
            Dot(vh, ToLocalPoint(center), radius * rectTransform.rect.width / 900, color);
        }
    }
}
