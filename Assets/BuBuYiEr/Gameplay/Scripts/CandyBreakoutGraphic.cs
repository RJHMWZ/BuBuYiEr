using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClawbearGames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CandyBreakoutGraphic : MaskableGraphic, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler, IPointerExitHandler
    {
        public CandyBreakoutSimulation Board;
        public static readonly Color32[] CandyColors = {
            new Color32(247,211,124,255), new Color32(239,166,180,255),
            new Color32(166,192,234,255), new Color32(164,212,183,255),
            new Color32(201,163,140,255), new Color32(203,180,226,255) };
        private int pressedPointer = int.MinValue;
        private Vector2 pressPosition;
        private CandyBreakoutSimulation pressBoard;
        private CandyBreakoutSimulation.Brick pressedBrick;
        private float paintOpacity=1;
        private bool painting;
        private float paintScale;
        private Rect paintBounds;
        private static readonly Vector2[] roundedDirections = CreateRoundedDirections();
        private static readonly Vector2[] circleDirections = CreateCircleDirections();
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
            if(departing.Count>0)SetVerticesDirty();
            for(int i=departing.Count-1;i>=0;i--)
            {
                var item=departing[i];item.Remaining-=delta;
                if(item.Remaining<=0)departing.RemoveAt(i);else departing[i]=item;
            }
        }
        public void ReleaseBoard()
        {
            CancelPress();
            Board=feedbackBoard=null;
            departing.Clear();
            SetVerticesDirty();
        }
        protected override void OnDisable()
        {
            CancelPress();
            departing.Clear();
            paintOpacity=1;
            base.OnDisable();
        }

        public void FitPresentation()
        {
            if(feedbackBoard!=Board){feedbackBoard=Board;departing.Clear();CancelPress();}
            SetVerticesDirty();
        }
        public float BoardScale => Mathf.Max(.001f, Mathf.Min(rectTransform.rect.width / CandyBreakoutSimulation.Width,
            rectTransform.rect.height / CandyBreakoutSimulation.Height));
        public Rect PlayBounds
        {
            get
            {
                Vector2 size = new Vector2(CandyBreakoutSimulation.Width, CandyBreakoutSimulation.Height) * BoardScale;
                return new Rect(rectTransform.rect.center - size * .5f, size);
            }
        }
        // A single uniform transform preserves angles, circular contacts and speed everywhere.
        public Vector2 ToLocalPoint(Vector2 p)
        {
            Rect bounds=painting?paintBounds:PlayBounds;
            float scale=painting?paintScale:BoardScale;
            return new Vector2(bounds.xMin+p.x*scale,bounds.yMax-p.y*scale);
        }
        public Vector2 ToBoardPoint(Vector2 local)
        {
            Rect r = PlayBounds;
            return new Vector2((local.x - r.xMin) / BoardScale, (r.yMax - local.y) / BoardScale);
        }
        public Rect VisualRect(Rect r)
        {
            Vector2 top = ToLocalPoint(new Vector2(r.xMin, r.yMin));
            Vector2 bottom = ToLocalPoint(new Vector2(r.xMax, r.yMax));
            return new Rect(top.x, bottom.y, bottom.x - top.x, top.y - bottom.y);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (pressedPointer != int.MinValue || e.button != PointerEventData.InputButton.Left || !TryBoardPoint(e, out Vector2 point)) return;
            foreach (var brick in Board.Bricks)
            {
                if (!brick.Alive || !brick.Bounds.Contains(point)) continue;
                pressedPointer = e.pointerId;
                pressPosition = e.position;
                pressBoard = Board;
                pressedBrick = brick;
                break;
            }
        }
        public void OnBeginDrag(PointerEventData e) => OnDrag(e);
        public void OnDrag(PointerEventData e)
        {
            if (pressedPointer == e.pointerId) CancelPress();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pressedPointer != e.pointerId) return;
            var brick = pressedBrick;
            var source = pressBoard;
            Vector2 start = pressPosition;
            CancelPress();
            float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10;
            if (e.button != PointerEventData.InputButton.Left || e.dragging || (e.position - start).sqrMagnitude > threshold * threshold ||
                source != Board || brick == null || !brick.Alive || !TryBoardPoint(e, out Vector2 point) || !brick.Bounds.Contains(point)) return;
            Board.Click(point);
        }
        public void OnPointerExit(PointerEventData e) { if (pressedPointer == e.pointerId) CancelPress(); }
        private void CancelPress()
        {
            pressedPointer = int.MinValue;
            pressedBrick = null;
            pressBoard = null;
        }
        private void Update() { if (pressedBrick != null && !CanAcceptInput()) CancelPress(); }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelPress(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelPress(); }
        private bool CanAcceptInput()
        {
            IngameManager game = IngameManager.Instance;
            if (game == null || !game.CanInteract || Board == null || Board != game.Board) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.IsBlockingInput) return false;
#endif
            return true;
        }
        private bool TryBoardPoint(PointerEventData e, out Vector2 point)
        {
            point = Vector2.zero;
            if (!CanAcceptInput() || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out Vector2 local) || !PlayBounds.Contains(local)) return false;
            point = ToBoardPoint(local);
            return true;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Board == null) return;
            paintScale=BoardScale;paintBounds=PlayBounds;painting=true;
            try { DrawBoard(vh); }
            finally { painting=false;paintOpacity=1; }
        }
        private void DrawBoard(VertexHelper vh)
        {
            paintOpacity=1;
            Rect play = paintBounds;
            Rounded(vh, new Rect(play.x - 3, play.y - 3, play.width + 6, play.height + 6), 8,
                new Color32(180,139,100,255), new Color32(180,139,100,255));
            Rounded(vh, play, 6, new Color32(255,251,241,255), new Color32(255,243,220,255));
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
                Color c = CandyColors[ball.Kind];
                for (int i = ball.TrailCount - 1; i >= 0; i--)
                    Circle(vh, ball.Trail[i], 8 - i * .6f, new Color(c.r,c.g,c.b,.22f * (1 - i / 8f)));
                if (ball.Impact > 0) Circle(vh, ball.Position, 16, new Color(1,.75f,.25f,ball.Impact*.22f));
                Circle(vh, ball.Position, CandyBreakoutSimulation.Radius, BearGameTheme.Brown);
                Circle(vh, ball.Position, 8.5f, c);
                Circle(vh, ball.Position + new Vector2(-3,-3), 2.7f, new Color(1,1,1,.9f));
            }
            foreach (var spark in Board.Sparks)
            {
                Color c = spark.Color; c.a = Mathf.Clamp01(spark.Life * 4);
                Circle(vh, spark.Position, 3.5f * Mathf.Clamp01(spark.Life * 5), c);
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
                    Vector2 p = center + roundedDirections[corner*(segments+1)+i] * radius;
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
                vh.AddVert(center+circleDirections[i]*radius,color,Vector2.zero);
                if(i>0) vh.AddTriangle(n,n+i,n+i+1);
            }
        }
        private void Circle(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            Dot(vh, ToLocalPoint(center), radius * (painting?paintScale:BoardScale), color);
        }
        private static Vector2[] CreateRoundedDirections()
        {
            const int segments=5;
            var directions=new Vector2[4*(segments+1)];
            for(int corner=0;corner<4;corner++)for(int i=0;i<=segments;i++)
            {
                float a=(corner*90+i*90f/segments)*Mathf.Deg2Rad;
                directions[corner*(segments+1)+i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            }
            return directions;
        }
        private static Vector2[] CreateCircleDirections()
        {
            var directions=new Vector2[21];
            for(int i=0;i<=20;i++)
            {
                float a=i*Mathf.PI/10;
                directions[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
            }
            return directions;
        }
    }
}
