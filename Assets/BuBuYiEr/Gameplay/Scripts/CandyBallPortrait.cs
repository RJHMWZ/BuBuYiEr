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
                    Vector2 next=center-new Vector2((i-1)*radius*.58f,Mathf.Sin((i-1)*.9f)*radius*.12f);
                    CandyBallArt.DrawTrail(vh,p,next,radius*.38f*t,kind,selected,(locked ? .38f : 1)*.70f*t,i);
                }
            CandyBallArt.Draw(vh,center,radius,kind,selected,locked ? .38f : 1);
        }
    }
}
