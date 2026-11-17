using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    // One painter for the moving balls, shop swatches and companion portraits.
    public static class CandyBallArt
    {
        private static readonly Color Ink = new Color32(94, 53, 32, 255);
        private static readonly Vector2[] Ring = BuildRing();
        private static Vector2[] BuildRing()
        {
            var result=new Vector2[33];
            for(int i=0;i<result.Length;i++)result[i]=new Vector2(Mathf.Cos(i*Mathf.PI/16),Mathf.Sin(i*Mathf.PI/16));
            return result;
        }
        public static void Disc(VertexHelper vh,Vector2 center,float radius,Color color)
        {
            if(radius<=0 || color.a<=0)return;
            int n=vh.currentVertCount;vh.AddVert(center,color,Vector2.zero);
            for(int i=0;i<Ring.Length;i++)vh.AddVert(center+Ring[i]*radius,color,Vector2.zero);
            for(int i=0;i<32;i++)vh.AddTriangle(n,n+i+1,n+i+2);
        }
        public static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            Vector2 normal=Vector2.Perpendicular(b-a).normalized*width*.5f;
            int n=vh.currentVertCount;
            vh.AddVert(a+normal,color,Vector2.zero);vh.AddVert(b+normal,color,Vector2.zero);
            vh.AddVert(b-normal,color,Vector2.zero);vh.AddVert(a-normal,color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        public static void Star(VertexHelper vh,Vector2 center,float radius,Color color,float rotation=0)
        {
            int n=vh.currentVertCount;vh.AddVert(center,color,Vector2.zero);
            for(int i=0;i<10;i++)
            {
                float angle=(90+i*36+rotation)*Mathf.Deg2Rad;
                vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*(i%2==0?1:.45f),color,Vector2.zero);
            }
            for(int i=0;i<10;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%10);
        }
        public static void Draw(VertexHelper vh,Vector2 center,float radius,int kind,string skin,float opacity=1)
        {
            Color candy=CandyCosmetics.SkinPreview(skin,kind);
            CandyBallFinish finish=CandyBallSkin.Resolve(skin)?.finish ?? CandyBallFinish.Plain;
            Color ink=WithOpacity(Color.Lerp(candy,Ink,.68f),opacity);
            Color cream=WithOpacity(Color.Lerp(candy,Color.white,.58f),opacity);
            Color shade=WithOpacity(Color.Lerp(candy,Ink,.20f),opacity);
            candy.a=opacity;
            Disc(vh,center,radius,ink);
            float r=radius*.84f;
            if(finish==CandyBallFinish.Cookie)
            {
                Disc(vh,center,r,candy);
                Disc(vh,center,r*.81f,WithOpacity(Color.Lerp(candy,Color.white,.30f),opacity));
                for(int i=0;i<5;i++)
                {
                    float a=i*Mathf.PI*2/5+.3f;
                    Disc(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.70f,r*.095f,shade);
                }
            }
            else if(finish==CandyBallFinish.Jelly)
            {
                Disc(vh,center,r,candy);
                Disc(vh,center+Vector2.up*r*.06f,r*.79f,WithOpacity(Color.Lerp(candy,Color.white,.30f),opacity));
                Disc(vh,center-Vector2.up*r*.24f,r*.53f,candy);
                Disc(vh,center+new Vector2(.43f,-.29f)*r,r*.12f,new Color(1,1,1,.85f*opacity));
            }
            else if(finish==CandyBallFinish.Comet)
            {
                Disc(vh,center,r,candy);
                for(int i=-1;i<=1;i+=2)
                {
                    float y=i*r*.46f;float span=r*.73f;
                    Line(vh,center+new Vector2(-span,y-r*.12f),center+new Vector2(span,y+r*.12f),r*.17f,cream);
                }
            }
            else if(finish==CandyBallFinish.Crystal)
            {
                Disc(vh,center,r,shade);
                int n=vh.currentVertCount;
                vh.AddVert(center,cream,Vector2.zero);
                for(int i=0;i<6;i++)
                {
                    float a=(i*60+30)*Mathf.Deg2Rad;
                    vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.93f,
                        i%2==0?candy:shade,Vector2.zero);
                }
                for(int i=0;i<6;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%6);
            }
            else Disc(vh,center,r,candy);

            Disc(vh,center+new Vector2(-.34f,.40f)*r,r*(finish==CandyBallFinish.Jelly ? .23f : .17f),new Color(1,1,1,.78f*opacity));
            DrawKindMark(vh,center,r,kind,candy,opacity);
        }

        // Theme controls material; the six colour families and identity marks remain visible.
        private static void DrawKindMark(VertexHelper vh,Vector2 center,float r,int kind,Color candy,float opacity)
        {
            Color mark=WithOpacity(Color.Lerp(candy,Ink,.60f),opacity);
            Color milk=new Color(1,.98f,.91f,opacity);
            if(kind==2)Star(vh,center,r*.43f,milk);
            else if(kind==1)
                for(int i=0;i<3;i++)Disc(vh,center+new Vector2((i-1)*.30f,-.22f+(i%2)*.23f)*r,r*.09f,milk);
            else if(kind==3)
            {
                Disc(vh,center+new Vector2(.16f,.12f)*r,r*.22f,mark);
                Line(vh,center+new Vector2(-.30f,-.30f)*r,center+new Vector2(.28f,.22f)*r,r*.12f,milk);
            }
            else if(kind==4)
                for(int i=0;i<2;i++)Line(vh,center+new Vector2(-.43f,-.28f+i*.4f)*r,center+new Vector2(.43f,-.08f+i*.4f)*r,r*.14f,mark);
            else if(kind==5)
            {
                Star(vh,center,r*.43f,milk);
                Disc(vh,center+new Vector2(.40f,-.30f)*r,r*.15f,new Color(1,.57f,.69f,opacity));
            }
            else Disc(vh,center+new Vector2(.14f,-.17f)*r,r*.17f,mark);
        }

        public static void DrawTrail(VertexHelper vh,Vector2 center,Vector2 next,float radius,int kind,string skin,float opacity,int step)
        {
            CandyBallFinish finish=CandyBallSkin.Resolve(skin)?.finish ?? CandyBallFinish.Plain;
            if((finish==CandyBallFinish.Jelly || finish==CandyBallFinish.Crystal) && step%2!=0)return;
            Color body=CandyCosmetics.SkinPreview(skin,kind);
            Color light=WithOpacity(Color.Lerp(body,Color.white,.58f),opacity);
            Color tint=WithOpacity(body,opacity);
            if(finish==CandyBallFinish.Crystal)Star(vh,center,radius*.72f,tint,step*19);
            else if(finish==CandyBallFinish.Comet)
            {
                if((next-center).sqrMagnitude<.001f)return;
                Line(vh,center,next,radius*1.7f,tint);
                Line(vh,center,next,radius*.68f,light);
            }
            else if(finish==CandyBallFinish.Jelly)
            {
                Disc(vh,center,radius,tint);
                Disc(vh,center,radius*.65f,light);
            }
            else if(finish==CandyBallFinish.Cookie)
                Disc(vh,center,radius*.5f,WithOpacity(Color.Lerp(body,Ink,.28f),opacity));
            else Disc(vh,center,radius,tint);
        }

        private static Color WithOpacity(Color color,float opacity) { color.a=opacity;return color; }
    }
}
