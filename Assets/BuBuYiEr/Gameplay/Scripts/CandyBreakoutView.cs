using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class CandyBreakoutView : MonoBehaviour
    {
        private static readonly Color SecondaryInk = new Color32(121,88,62,255);
        private static readonly Color PaperWhite = new Color32(255,252,244,255);
        private static readonly Color Mint = new Color32(190,220,189,255);
        private BearMenuArt art;
        private IngameManager game;
        private CandyBreakoutGraphic arena;
        private RectTransform frame, board, arenaLabels, progress, progressGleam, shop, mainAction, hintRect, characters, shopHeading;
        private BearMenuLayout roomLayout;
        private RectTransform whiteBear, brownBear, bearHeart;
        private Vector2 whiteRest, brownRest, heartRest;
        private readonly RectTransform[] ballIcons = new RectTransform[6];
        private readonly BearMenuShape[] cardFaces = new BearMenuShape[6];
        private readonly float[] purchasePulse = new float[6], upgradePulse = new float[6];
        private float animationTime, happyTime, celebrateTime;
        private Text level, honey, goal, time, hint, introTitle, introBody, clickPower, ballCount;
        private Image[] stars;
        private GameObject intro;
        private Button clickUpgrade;
        private CandyBreakoutButton clickVisual;
        private readonly Button[] buy = new Button[6], upgrade = new Button[6];
        private readonly CandyBreakoutButton[] buyVisual = new CandyBreakoutButton[6], upgradeVisual = new CandyBreakoutButton[6];
        private readonly Text[] info = new Text[6], owned = new Text[6], ranks = new Text[6];
        private readonly List<Text> health = new List<Text>();
        private float hintUntil, displayedProgress, targetProgress, lastHeight = -1, honeyPop;
        private int lastHoney = -1, lastDestroyed = -1, lastSeconds = -1, lastStars = -1, lastShopHoney = -1;
        private bool lastReady;
        private int shownClickLevel=-1;
        private readonly int[] shownCounts=new int[6], shownUpgrades=new int[6];
        private CanvasScaler scaler;
        private Vector2 previousReference;
        private float previousMatch;
        private CanvasScaler.ScreenMatchMode previousMode;
        private bool scalerApplied;

        public void Build(IngameViewController owner, BearMenuArt theme)
        {
            art = theme;
            foreach (Transform child in owner.transform) child.gameObject.SetActive(false);
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            if (canvas != null) scaler = canvas.rootCanvas.GetComponent<CanvasScaler>();
            ApplyCanvasScale();
            RectTransform root = BearMenuFactory.Node(owner.transform, "CandyBreakoutUI", 0, 0, 1080, 1920);
            BearMenuFactory.Stretch(root);
            root.gameObject.AddComponent<Image>().color = BearGameTheme.Cream;
            frame = BearMenuFactory.Node(root, "CandyRoom", 0, 0, 1080, 1920);
            roomLayout = frame.gameObject.AddComponent<BearMenuLayout>();
            roomLayout.designSize = new Vector2(1080,1760);
            roomLayout.flexibleMain = true;
            roomLayout.Refresh();
            BuildHeader();
            board = Paper(frame,"CandyBoard",36,344,1008,656,new Color32(255,237,211,255),34).rectTransform;
            board.gameObject.AddComponent<RectMask2D>();
            characters = BearMenuFactory.Node(board,"BearCompanions",0,0,1008,190);
            BearGameTheme.Bears(art,characters,298,30,.75f);
            whiteBear=(RectTransform)characters.Find("WhiteBear");
            brownBear=(RectTransform)characters.Find("BrownBear");
            bearHeart=(RectTransform)characters.Find("BearHeart");
            // Companions sit behind the field; every moving ball remains visible above them.
            RectTransform field = BearMenuFactory.Node(board,"Arena",16,12,976,632);
            arena = field.gameObject.AddComponent<CandyBreakoutGraphic>();
            arena.color = Color.white;
            arenaLabels = BearMenuFactory.Node(field,"BrickHealthLabels",0,0,976,632);
            BearMenuFactory.Stretch(arenaLabels);
            hint = Label(frame,"Hint","点糖砖赚蜂蜜，让小球帮你收集甜蜜",36,1016,1008,40,27);
            hint.color = SecondaryInk;
            hintRect = hint.rectTransform;
            BuildMainAction();
            shopHeading = BearMenuFactory.Node(frame,"ShopHeading",36,1210,1008,36);
            Label(shopHeading,"Title","糖果小队",0,0,300,36,31,TextAnchor.MiddleLeft);
            ballCount = Label(shopHeading,"BallCount","",620,0,388,36,25,TextAnchor.MiddleRight);
            ballCount.color = SecondaryInk;
            shop = BearMenuFactory.Node(frame,"BallShop",36,1258,1008,638);
            GridLayoutGroup grid = shop.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(324,310);
            grid.spacing = new Vector2(18,18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            for (int i=0;i<6;i++) BuildCard(shop,i);
            BuildIntro(root);
            RefreshLayout(true);
        }
        private void BuildHeader()
        {
            RectTransform header = BearMenuFactory.Node(frame,"Header",0,28,1080,104);
            Paper(header,"LevelPaper",36,0,280,104,PaperWhite,30);
            Label(header,"LevelCaption","糖果旅行",58,13,236,28,23).color=SecondaryInk;
            level=Label(header,"Level","",50,40,252,52,37);
            Paper(header,"HoneyPaper",340,0,576,104,new Color32(255,239,194,255),30);
            BearGameTheme.Picture(art,header,"HoneyIcon","Daily/IconBottle",366,19,64,66);
            Label(header,"HoneyCaption","本关蜂蜜",451,15,220,28,24,TextAnchor.MiddleLeft).color=SecondaryInk;
            honey=Label(header,"Honey","",449,40,440,52,43,TextAnchor.MiddleLeft,true);
            Label(frame,"Mode","布布一二 · 甜蜜弹弹乐",36,153,1008,66,50);
            goal=Label(frame,"Goal","",38,238,510,44,30,TextAnchor.MiddleLeft);
            stars=new Image[3];
            for(int i=0;i<3;i++) stars[i]=BearGameTheme.Picture(art,frame,"RatingStar"+i,"IconStar",625+i*46,242,37,35);
            Label(frame,"TimeCaption","用时",808,239,74,40,24).color=SecondaryInk;
            time=Label(frame,"TimeAndStars","0:00",896,235,148,46,34,TextAnchor.MiddleRight,true);
            BearMenuShape track=Paper(frame,"ProgressTrack",36,298,1008,26,new Color32(238,218,190,255),13,false);
            track.border=2; track.borderColor=new Color32(188,153,117,255);
            progress=Paper(track.transform,"Progress",3,3,0,20,Mint,10,false).rectTransform;
            progress.GetComponent<BearMenuShape>().border=0;
            progressGleam=Paper(progress,"ProgressGleam",0,3,0,4,new Color(1,1,1,.43f),2,false).rectTransform;
            progressGleam.GetComponent<BearMenuShape>().border=0;
        }
        private void BuildMainAction()
        {
            mainAction=BearMenuFactory.Node(frame,"MainAction",36,1072,1008,132);
            clickVisual=Action(mainAction,"ClickUpgrade",0,0,806,132,BearGameTheme.Gold,UpgradeClick,true);
            clickUpgrade=clickVisual.button;
            BearGameTheme.Place(clickVisual.caption.rectTransform,clickVisual.face.transform,107,14,450,54);
            clickVisual.caption.fontSize=41;clickVisual.caption.alignment=TextAnchor.MiddleLeft;
            BearGameTheme.Place(clickVisual.price.rectTransform,clickVisual.face.transform,586,18,192,54);
            clickVisual.price.fontSize=44;
            clickPower=Label(clickVisual.face.transform,"ClickPower","",108,70,464,36,26,TextAnchor.MiddleLeft);
            Label(clickVisual.face.transform,"HoneyUnit","蜂蜜",611,75,142,30,24).color=SecondaryInk;
            BearMenuShape icon=Paper(clickVisual.face.transform,"UpgradeIcon",23,24,68,68,new Color32(255,245,202,255),23,false);
            icon.border=0;
            Paper(icon.transform,"PlusHorizontal",17,30,34,8,BearGameTheme.Brown,4,false).border=0;
            Paper(icon.transform,"PlusVertical",30,17,8,34,BearGameTheme.Brown,4,false).border=0;
            Label(mainAction,"RoundCurrencyTip","本关蜂蜜\n仅用于强化",824,17,184,94,25).color=SecondaryInk;
        }
        private void BuildCard(Transform parent,int kind)
        {
            Color tint=CandyBreakoutGraphic.CandyColors[kind];
            RectTransform card=Paper(parent,"BallShop"+kind,0,0,324,310,PaperWhite,28).rectTransform;
            BearMenuShape icon=Paper(card,"CandyBall",20,20,84,84,tint,42,false);
            icon.border=3;
            cardFaces[kind]=card.GetComponent<BearMenuShape>();
            ballIcons[kind]=icon.rectTransform;
            ballIcons[kind].pivot=new Vector2(.5f,.5f);
            ballIcons[kind].anchoredPosition=new Vector2(62,-62);
            BearMenuShape shine=Paper(icon.transform,"Shine",18,15,24,10,new Color(1,1,1,.65f),5,false);shine.border=0;
            if(kind==2) BearGameTheme.Picture(art,icon.transform,"StarMark","IconStar",24,25,36,34);
            else
            {
                BearMenuShape dot=Paper(icon.transform,"CandyMark",kind==5?36:37,39,kind==4?20:10,kind==4?8:10,
                    new Color(.32f,.17f,.07f,.5f),5,false);dot.border=0;
            }
            Label(card,"Name",CandyBreakoutSimulation.Names[kind],117,18,191,46,37,TextAnchor.MiddleLeft);
            Label(card,"Description",CandyBreakoutSimulation.Descriptions[kind],117,67,191,36,27,TextAnchor.MiddleLeft).color=SecondaryInk;
            Paper(card,"OwnedPill",24,111,78,31,Color.Lerp(tint,PaperWhite,.65f),15,false).border=0;
            owned[kind]=Label(card,"Owned","",24,109,78,34,26);
            info[kind]=Label(card,"Stats","",116,111,126,32,25,TextAnchor.MiddleLeft);
            ranks[kind]=Label(card,"Rank","",242,111,66,32,24,TextAnchor.MiddleRight);
            ranks[kind].color=SecondaryInk;
            buyVisual[kind]=Action(card,"Buy",12,160,144,132,tint,()=>Buy(kind));
            upgradeVisual[kind]=Action(card,"Upgrade",168,160,144,132,Mint,()=>Upgrade(kind));
            buy[kind]=buyVisual[kind].button;upgrade[kind]=upgradeVisual[kind].button;
        }
        private CandyBreakoutButton Action(Transform parent,string name,float x,float y,float w,float h,Color tint,UnityAction action,bool primary=false)
        {
            RectTransform root=BearMenuFactory.Node(parent,name,x,y,w,h);
            Image hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            Button button=root.gameObject.AddComponent<Button>();
            button.transition=Selectable.Transition.None;
            button.navigation=new Navigation { mode=Navigation.Mode.None };
            button.onClick.AddListener(action);
            BearMenuShape face=Paper(root,"Face",2,6,w-4,h-12,tint,primary?30:23);
            face.border=primary?4:2.5f;
            // Keep the visual pivot centred for press and breathing feedback.
            face.rectTransform.anchorMin=face.rectTransform.anchorMax=face.rectTransform.pivot=new Vector2(.5f,.5f);
            face.rectTransform.anchoredPosition=Vector2.zero;
            button.targetGraphic=face;
            CandyBreakoutButton visual=root.gameObject.AddComponent<CandyBreakoutButton>();
            visual.button=button;visual.face=face;visual.primary=primary;
            visual.caption=Label(face.transform,"Label","",5,11,w-14,34,25);
            visual.price=Label(face.transform,"Price","",8,48,w-20,52,primary?39:35,TextAnchor.MiddleCenter,true);
            visual.sheen=Paper(face.transform,"SoftHighlight",13,9,w-30,5,new Color(1,1,1,.26f),2.5f,false);
            visual.sheen.border=0;
            return visual;
        }
        private BearMenuShape Paper(Transform parent,string name,float x,float y,float w,float h,Color color,float radius,bool shadow=true)
        {
            BearMenuShape shape=BearGameTheme.Paper(parent,name,x,y,w,h,color,radius);
            shape.border=3;shape.bottomColor=Color.Lerp(color,new Color32(238,213,180,255),.09f);
            if(shadow)
            {
                Shadow drop=shape.gameObject.AddComponent<Shadow>();
                drop.effectColor=new Color(.32f,.17f,.07f,.11f);
                drop.effectDistance=new Vector2(0,-5);drop.useGraphicAlpha=true;
            }
            return shape;
        }
        private Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size,TextAnchor alignment=TextAnchor.MiddleCenter,bool numeric=false)
        {
            Text label=BearGameTheme.Label(art,parent,name,value,x,y,w,h,size,numeric);
            label.alignment=alignment;
            label.horizontalOverflow=HorizontalWrapMode.Overflow;
            label.verticalOverflow=VerticalWrapMode.Overflow;
            return label;
        }
        private void BuildIntro(Transform parent)
        {
            RectTransform overlay=BearMenuFactory.Node(parent,"BreakoutIntroduction",0,0,1024,1536);
            BearMenuFactory.Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color=new Color(.23f,.12f,.05f,.6f);
            intro=overlay.gameObject;
            RectTransform safe=BearGameTheme.Frame(overlay,"IntroSafeArea");
            BearGameTheme.Bears(art,safe,272,265,.88f);
            RectTransform card=Paper(safe,"IntroPaper",128,476,768,730,PaperWhite,48).rectTransform;
            introTitle=Label(card,"Title","",50,30,668,80,48);
            Label(card,"Mode","甜蜜弹弹乐",60,124,648,62,42);
            introBody=Label(card,"Rules","",64,220,640,235,32);
            Button begin = BearGameTheme.Button(art,card,"Begin","开始打糖砖",100,504,568,105,BearGameTheme.Gold,()=>
            {
                if(game==null||game.IsPaused)return;
                intro.SetActive(false);game.OnTutorialClosed();
            });
            begin.GetComponent<BearMenuShape>().border=3;
            Shadow beginShadow=begin.gameObject.AddComponent<Shadow>();
            beginShadow.effectColor=new Color(.32f,.17f,.07f,.11f);beginShadow.effectDistance=new Vector2(0,-5);
            Label(card,"NoDeadline","没有倒计时失败，慢慢玩也能通关",55,641,658,46,25).color=SecondaryInk;
        }
        public void Bind(IngameManager manager)
        {
            game=manager;arena.Board=game.Board;arena.FitPresentation();
            foreach(Text label in health)Destroy(label.gameObject);
            health.Clear();
            foreach(var brick in game.Board.Bricks)
                health.Add(Label(arenaLabels,"Health","",0,0,100,50,30,TextAnchor.MiddleCenter,true));
            level.text="第 "+game.CurrentLevel+" 关";
            introTitle.text="第 "+game.CurrentLevel+" 关"+(game.Level.boss?" · 大糖砖":"");
            introBody.text="① 点击糖砖，打碎后获得蜂蜜\n② 购买小球，让它们自动弹跳\n③ 升级威力，清空全部糖砖\n\n通关奖励："+game.Level.coinReward+" 金币";
            intro.SetActive(!game.IsRoundStarted);
            lastHoney=lastDestroyed=lastSeconds=lastStars=lastShopHoney=-1;
            displayedProgress=targetProgress=0;hintUntil=0;
            animationTime=happyTime=celebrateTime=honeyPop=0;
            ResetFeedback();
            RefreshLayout(true);Refresh();
        }
        public void RefreshLayout(bool force=false)
        {
            if(frame==null||arena==null)return;
            roomLayout.Refresh();
            float h=frame.rect.height;
            if(!force&&Mathf.Abs(h-lastHeight)<.1f)return;
            lastHeight=h;
            float shopY=h-662;
            BearGameTheme.Place(shop,frame,36,shopY,1008,638);
            BearGameTheme.Place(shopHeading,frame,36,shopY-48,1008,36);
            float mainY=shopY-186;
            BearGameTheme.Place(mainAction,frame,36,mainY,1008,132);
            BearGameTheme.Place(hintRect,frame,36,mainY-56,1008,40);
            float boardH=mainY-72-344;
            BearGameTheme.Place(board,frame,36,344,1008,boardH);
            BearGameTheme.Place(arena.rectTransform,board,16,12,976,boardH-24);
            float bearScale = game != null && game.Level.boss ? .56f : .75f;
            float bearHeight = 230 * bearScale + 40;
            BearGameTheme.Place(characters,board,0,boardH-bearHeight-14,1008,bearHeight);
            bool boss = game != null && game.Level.boss;
            float bearX = boss ? 190 : (1008-550*bearScale)*.5f;
            BearGameTheme.Place((RectTransform)characters.Find("WhiteBear"),characters,bearX,35,270*bearScale,230*bearScale);
            BearGameTheme.Place((RectTransform)characters.Find("BrownBear"),characters,boss ? 1008-190-270*bearScale : bearX+280*bearScale,35,270*bearScale,230*bearScale);
            BearGameTheme.Place((RectTransform)characters.Find("BearHeart"),characters,boss ? 504-35*bearScale : bearX+242*bearScale,boss ? bearHeight-46 : 0,70*bearScale,61*bearScale);
            whiteRest=whiteBear.anchoredPosition;
            brownRest=brownBear.anchoredPosition;
            heartRest=bearHeart.anchoredPosition;
            arena.FitPresentation();
            PositionHealth();
        }
        private void PositionHealth()
        {
            if(game==null)return;
            for(int i=0;i<health.Count;i++)
            {
                var brick=game.Board.Bricks[i];Rect r=arena.VisualRect(brick.Bounds);
                BearGameTheme.Place(health[i].rectTransform,arenaLabels,r.x-arena.rectTransform.rect.xMin,
                    arena.rectTransform.rect.yMax-r.yMax,r.width,r.height);
                health[i].color=new Color32(60,33,18,255);
                health[i].fontSize=brick.Boss?36:Mathf.Clamp(Mathf.RoundToInt(r.height*.68f),20,34);
            }
        }
        private bool Ready=>game!=null&&game.CanInteract&&game.Board!=null&&!game.Board.Complete;
        private void Buy(int kind)
        {
            if(!Ready)return;
            if(game.Board.Buy(kind))
            {
                purchasePulse[kind]=.32f;
                Notice(CandyBreakoutSimulation.Names[kind]+"加入啦 · "+CandyBreakoutSimulation.Descriptions[kind]);
            }
            Refresh();
        }
        private void Upgrade(int kind)
        {
            if(!Ready)return;
            if(game.Board.Upgrade(kind))
            {
                upgradePulse[kind]=.48f;celebrateTime=.55f;
                Notice(CandyBreakoutSimulation.Names[kind]+"威力提升，同类小球一起升级！");
            }
            Refresh();
        }
        private void UpgradeClick()
        {
            if(Ready&&game.Board.UpgradeClick())
            {
                celebrateTime=.55f;
                Notice("点击威力提升！试着连点糖砖吧");
            }
            Refresh();
        }
        private void Notice(string message){hint.text=message;hintUntil=animationTime+3;}
        private void OnEnable()=>ApplyCanvasScale();
        private void ApplyCanvasScale()
        {
            if(scaler==null||scalerApplied)return;
            previousReference=scaler.referenceResolution;previousMatch=scaler.matchWidthOrHeight;previousMode=scaler.screenMatchMode;
            scaler.referenceResolution=new Vector2(1080,1920);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight=0;scalerApplied=true;
        }
        private void OnDisable()
        {
            if(scaler!=null&&scalerApplied)
            {
                scaler.referenceResolution=previousReference;scaler.matchWidthOrHeight=previousMatch;scaler.screenMatchMode=previousMode;
            }
            scalerApplied=false;
            ResetFeedback();
        }
        private void LateUpdate()
        {
            RefreshLayout();
            if(game==null||arena.Board!=game.Board)return;
            if(game.IsRoundStarted&&intro.activeSelf)intro.SetActive(false);
            Refresh();arena.SetVerticesDirty();
            float delta=game.IsPaused || Time.timeScale<=0 ? 0 : Time.deltaTime;
            if(delta<=0)return;
            animationTime+=delta;
            arena.AdvanceFeedback(delta);
            displayedProgress=Mathf.Lerp(displayedProgress,targetProgress,1-Mathf.Exp(-delta*9));
            float width=1002*displayedProgress;
            progress.sizeDelta=new Vector2(width,20);
            progressGleam.sizeDelta=new Vector2(Mathf.Max(0,width-16),4);
            progressGleam.anchoredPosition=new Vector2(8,-3);
            honeyPop=Mathf.MoveTowards(honeyPop,0,delta*4);
            honey.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(honeyPop*Mathf.PI)*.10f);
            happyTime=Mathf.Max(0,happyTime-delta);
            celebrateTime=Mathf.Max(0,celebrateTime-delta);
            float joy=Mathf.Sin(Mathf.Clamp01(happyTime/.45f)*Mathf.PI);
            float celebration=Mathf.Sin(Mathf.Clamp01(celebrateTime/.55f)*Mathf.PI);
            float idle=game.IsRoundStarted?Mathf.Sin(animationTime*1.6f):0;
            whiteBear.anchoredPosition=whiteRest+Vector2.up*(idle*2.5f+joy*5+celebration*7);
            brownBear.anchoredPosition=brownRest+Vector2.up*(-idle*2.5f+joy*5+celebration*7);
            whiteBear.localScale=brownBear.localScale=Vector3.one*(1+joy*.018f+celebration*.025f);
            bearHeart.anchoredPosition=heartRest+Vector2.up*(joy*3+celebration*4);
            bearHeart.localScale=Vector3.one*(1+(game.IsRoundStarted?Mathf.Sin(animationTime*2.6f)*.025f:0)+joy*.07f+celebration*.10f);
            for(int i=0;i<6;i++)
            {
                purchasePulse[i]=Mathf.Max(0,purchasePulse[i]-delta);
                upgradePulse[i]=Mathf.Max(0,upgradePulse[i]-delta);
                float pop=Mathf.Sin(Mathf.Clamp01(purchasePulse[i]/.32f)*Mathf.PI);
                ballIcons[i].localScale=Vector3.one*(1+pop*.13f);
                Color edge=Color.Lerp(BearGameTheme.Brown,new Color32(214,156,63,255),Mathf.Clamp01(upgradePulse[i]/.48f));
                if(cardFaces[i].borderColor!=edge)
                {
                    cardFaces[i].borderColor=edge;
                    cardFaces[i].SetVerticesDirty();
                }
            }
        }
        private void ResetFeedback()
        {
            happyTime=celebrateTime=honeyPop=0;
            if(characters!=null)characters.localScale=Vector3.one;
            if(whiteBear!=null){whiteBear.anchoredPosition=whiteRest;whiteBear.localScale=Vector3.one;}
            if(brownBear!=null){brownBear.anchoredPosition=brownRest;brownBear.localScale=Vector3.one;}
            if(bearHeart!=null){bearHeart.anchoredPosition=heartRest;bearHeart.localScale=Vector3.one;}
            if(honey!=null)honey.rectTransform.localScale=Vector3.one;
            for(int i=0;i<6;i++)
            {
                purchasePulse[i]=upgradePulse[i]=0;
                if(ballIcons[i]!=null)ballIcons[i].localScale=Vector3.one;
                if(cardFaces[i]!=null){cardFaces[i].borderColor=BearGameTheme.Brown;cardFaces[i].SetVerticesDirty();}
            }
        }
        private void Refresh()
        {
            if(game==null||game.Board==null)return;
            var b=game.Board;bool active=Ready;
            if(lastHoney!=b.Honey){if(lastHoney>=0&&b.Honey>lastHoney){honeyPop=1;happyTime=.45f;}lastHoney=b.Honey;honey.text=b.Honey.ToString();}
            bool shopChanged=lastShopHoney!=b.Honey || lastReady!=active || shownClickLevel!=b.ClickLevel;
            for(int i=0;i<6;i++) shopChanged|=shownCounts[i]!=b.Counts[i] || shownUpgrades[i]!=b.Upgrades[i];
            if(shopChanged)
            {
                lastShopHoney=b.Honey;lastReady=active;shownClickLevel=b.ClickLevel;
                bool clickMax=b.ClickLevel>=CandyBreakoutSimulation.UpgradeLimit;
                bool affordClick=b.Honey>=b.ClickCost;
                clickVisual.SetState(active&&!clickMax&&affordClick,BearGameTheme.Gold,clickMax?"点击已满级":"强化点击",clickMax?"MAX":b.ClickCost.ToString(),clickMax);
                clickPower.text=clickMax?"当前威力 "+(1+b.ClickLevel*2)+" · 已达上限":!affordClick?
                    "蜂蜜不足 · 还差 "+(b.ClickCost-b.Honey):"当前威力 "+(1+b.ClickLevel*2)+"   点击即可提升";
                clickPower.color=affordClick?SecondaryInk:new Color32(145,107,78,255);
                for(int i=0;i<6;i++)
                {
                    shownCounts[i]=b.Counts[i];shownUpgrades[i]=b.Upgrades[i];
                    bool capped=b.Balls.Count>=CandyBreakoutSimulation.BallLimit;
                    bool max=b.Upgrades[i]>=CandyBreakoutSimulation.UpgradeLimit;
                    int cost=b.BuyCost(i),upCost=b.UpgradeCost(i);
                    owned[i].text="× "+b.Counts[i];info[i].text="威力 "+b.Damage(i);ranks[i].text="Lv."+b.Upgrades[i];
                    buyVisual[i].SetState(active&&!capped&&b.Honey>=cost,CandyBreakoutGraphic.CandyColors[i],
                        capped?"数量已满":b.Honey<cost?"还差 "+(cost-b.Honey):"购买",capped?"24 / 24":cost.ToString());
                    buyVisual[i].caption.fontSize=b.Honey<cost&&!capped?23:25;
                    upgradeVisual[i].SetState(active&&b.Counts[i]>0&&!max&&b.Honey>=upCost,Mint,
                        max?"已满级":b.Counts[i]==0?"先购买":b.Honey<upCost?"还差 "+(upCost-b.Honey):"升级",max?"MAX":upCost.ToString(),max);
                    upgradeVisual[i].caption.fontSize=b.Counts[i]>0&&!max&&b.Honey<upCost?23:25;
                    upgradeVisual[i].emphasized=active&&b.Counts[i]>0&&!max&&b.Honey>=upCost;
                }
                ballCount.text="自动弹跳  "+b.Balls.Count+" / 24 球";
            }
            if(lastDestroyed!=b.Destroyed)
            {
                lastDestroyed=b.Destroyed;goal.text="清空糖砖   "+b.Destroyed+" / "+b.Total;
                targetProgress=(float)b.Destroyed/Mathf.Max(1,b.Total);
            }
            int seconds=(int)b.Elapsed;
            if(seconds!=lastSeconds){lastSeconds=seconds;time.text=seconds/60+":"+(seconds%60).ToString("00");}
            if(lastStars!=game.CurrentStar)
            {
                lastStars=game.CurrentStar;
                for(int i=0;i<3;i++) stars[i].color=i<lastStars?Color.white:new Color(.65f,.52f,.37f,.25f);
            }
            for(int i=0;i<health.Count;i++)
            {
                var brick=b.Bricks[i];string value=brick.Alive?brick.Health.ToString():"";
                if(health[i].text!=value)
                {
                    if(!brick.Alive && health[i].text.Length>0)arena.PlayBreak(brick);
                    health[i].text=value;
                }
            }
            if(animationTime>hintUntil)hint.text="点糖砖赚蜂蜜，让小球帮你收集甜蜜";
        }
    }
}
