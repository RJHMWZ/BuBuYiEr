using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CandyBallPortrait : MaskableGraphic
    {
        public int kind;
        public string skin;
        public bool useEquippedSkin;
        public bool showTrail;
        public bool locked;
        private string shownSkin;
        private void LateUpdate()
        {
            if(!useEquippedSkin || !Application.isPlaying)return;
            string current=PlayerDataHandler.GetEquipment(BearShopEffect.BallSkin);
            if(shownSkin==current)return;
            shownSkin=current;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect=rectTransform.rect;
            string selected=useEquippedSkin && Application.isPlaying?PlayerDataHandler.GetEquipment(BearShopEffect.BallSkin):skin;
            float radius=Mathf.Min(rect.width,rect.height)*(showTrail ? .25f : .46f);
            Vector2 center=rect.center+(showTrail?new Vector2(rect.width*.19f,0):Vector2.zero);
            if(showTrail)
                for(int i=5;i>=1;i--)
                {
                    Vector2 p=center-new Vector2(i*radius*.58f,Mathf.Sin(i*.9f)*radius*.12f);
                    float t=1-i/7f;
                    if(selected=="skin_starry")CandyBallArt.Star(vh,p,radius*.25f*t,new Color(.65f,.49f,1,.8f*t),i*18);
                    else if(selected=="skin_sunset")
                    {
                        CandyBallArt.Line(vh,p-Vector2.right*radius*.30f,p+Vector2.right*radius*.25f,radius*.38f*t,new Color(1,.46f,.16f,.65f*t));
                        CandyBallArt.Line(vh,p-Vector2.right*radius*.30f,p+Vector2.right*radius*.25f,radius*.15f*t,new Color(1,.93f,.56f,.8f*t));
                    }
                    else if(selected=="skin_jelly")
                    {
                        CandyBallArt.Disc(vh,p,radius*.37f*t,new Color(.16f,.68f,.66f,.65f*t));
                        CandyBallArt.Disc(vh,p,radius*.25f*t,new Color(.73f,1,.92f,.8f*t));
                    }
                    else CandyBallArt.Disc(vh,p,radius*.26f*t,new Color(.72f,.41f,.16f,.65f*t));
                }
            CandyBallArt.Draw(vh,center,radius,kind,selected,locked ? .38f : 1);
        }
    }
}
