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
            Color ink=Ink;ink.a=opacity;candy.a=opacity;
            Disc(vh,center,radius,ink);
            float r=radius*.84f;
            if(skin=="skin_milk")
            {
                Disc(vh,center,r,new Color(1,.82f,.49f,opacity));
                Disc(vh,center,r*.81f,new Color(1,.96f,.80f,opacity));
                for(int i=0;i<5;i++)
                {
                    float a=i*Mathf.PI*2/5+.3f;
                    Disc(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.66f,r*.115f,new Color(.47f,.25f,.14f,opacity));
                }
                Disc(vh,center,r*.39f,candy);
                Disc(vh,center+new Vector2(-.1f,.12f)*r,r*.17f,new Color(1,1,1,.72f*opacity));
            }
            else if(skin=="skin_jelly")
            {
                Disc(vh,center,r,new Color(.20f,.76f,.76f,opacity));
                Disc(vh,center+Vector2.up*r*.04f,r*.78f,new Color(.62f,.97f,.89f,opacity));
                Disc(vh,center-Vector2.up*r*.19f,r*.49f,candy);
                Disc(vh,center+new Vector2(-.35f,.38f)*r,r*.24f,new Color(1,1,1,.94f*opacity));
                Disc(vh,center+new Vector2(.43f,-.29f)*r,r*.12f,new Color(1,1,1,.85f*opacity));
            }
            else if(skin=="skin_sunset")
            {
                Disc(vh,center,r,new Color(1,.46f,.21f,opacity));
                for(int i=-1;i<=1;i++)
                {
                    float y=i*r*.48f;float span=Mathf.Sqrt(r*r-y*y)*.82f;
                    Line(vh,center+new Vector2(-span,y-r*.13f),center+new Vector2(span,y+r*.13f),r*.19f,new Color(1,.91f,.56f,opacity));
                }
                Disc(vh,center+new Vector2(.36f,-.38f)*r,r*.22f,candy);
                Disc(vh,center+new Vector2(-.32f,.48f)*r,r*.16f,new Color(1,1,1,.85f*opacity));
            }
            else if(skin=="skin_starry")
            {
                Disc(vh,center,r,new Color(.29f,.23f,.57f,opacity));
                int n=vh.currentVertCount;
                vh.AddVert(center,new Color(.74f,.67f,1,opacity),Vector2.zero);
                for(int i=0;i<6;i++)
                {
                    float a=(i*60+30)*Mathf.Deg2Rad;
                    vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.93f,
                        i%2==0?new Color(.64f,.44f,.93f,opacity):new Color(.39f,.71f,.96f,opacity),Vector2.zero);
                }
                for(int i=0;i<6;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%6);
                Star(vh,center,r*.53f,new Color(1,.96f,.71f,opacity));
            }
            else
            {
                Disc(vh,center,r,candy);
                Disc(vh,center+new Vector2(-.32f,.38f)*r,r*.27f,new Color(1,1,1,.76f*opacity));
                if(kind==2)Star(vh,center,r*.43f,new Color(1,.98f,.76f,opacity));
                else if(kind==1)
                    for(int i=0;i<3;i++)Disc(vh,center+new Vector2((i-1)*.30f,-.22f+(i%2)*.23f)*r,r*.085f,new Color(1,.97f,.79f,opacity));
                else if(kind==3)
                {
                    Line(vh,center+new Vector2(-.35f,-.35f)*r,center+new Vector2(.35f,.26f)*r,r*.16f,new Color(.19f,.49f,.33f,opacity));
                    Disc(vh,center+new Vector2(.20f,.15f)*r,r*.21f,new Color(.31f,.65f,.43f,opacity));
                }
                else if(kind==4)
                    for(int i=0;i<2;i++)Line(vh,center+new Vector2(-.5f,-.30f+i*.4f)*r,center+new Vector2(.5f,-.10f+i*.4f)*r,r*.14f,new Color(.48f,.26f,.16f,opacity));
                else if(kind==5)
                {
                    Star(vh,center,r*.5f,new Color(1,.91f,.52f,opacity));
                    Disc(vh,center+new Vector2(.4f,-.3f)*r,r*.15f,new Color(.98f,.48f,.65f,opacity));
                }
                else Disc(vh,center+new Vector2(.16f,-.2f)*r,r*.13f,new Color(.55f,.36f,.20f,opacity));
            }
        }
    }
}
