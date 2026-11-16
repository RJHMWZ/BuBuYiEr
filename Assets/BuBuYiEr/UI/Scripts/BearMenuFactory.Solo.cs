using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static partial class BearMenuFactory
    {
        public static void EnsureSoloActions(BearHomeMenuController menu)
        {
            Transform main = menu.pages[0].transform;
            Transform play = main.Find("PlayAction");
            if (play == null) return;
            Transform solo = play.Find("SoloPlay");
            if (solo == null)
            {
                Transform artwork = play.Find("ButtonArtwork");
                Transform legacy = play.Find("StartGame");
                if (artwork != null) artwork.gameObject.SetActive(false);
                if (legacy != null) legacy.gameObject.SetActive(false);
                RectTransform hit=Node(play,"SoloPlay",187,0,650,190);
                BearMenuShape face=Shape(hit,"Face",0,0,650,190,90,Gold,new Color32(255,204,62,255),6);
                face.rectTransform.pivot=new Vector2(.5f,.5f);
                face.rectTransform.anchoredPosition=new Vector2(325,-95);
                Shadow shadow=face.gameObject.AddComponent<Shadow>();
                shadow.effectColor=new Color(.32f,.17f,.07f,.12f);shadow.effectDistance=new Vector2(0,-6);
                Shape(face.transform,"SoftLight",48,22,554,8,4,new Color(1,1,1,.3f),new Color(1,1,1,.3f),0);
                menu.soloPlayLabel=Label(menu,face.transform,"Caption","开始打糖砖",20,0,610,190,61);
                Button button=Action(hit,"play");button.targetGraphic=face;
                BearButtonMotion motion=hit.gameObject.AddComponent<BearButtonMotion>();
                motion.artwork=face.rectTransform;motion.primary=true;
            }
            else menu.soloPlayLabel = solo.GetComponentInChildren<Text>();
        }
    }
}
