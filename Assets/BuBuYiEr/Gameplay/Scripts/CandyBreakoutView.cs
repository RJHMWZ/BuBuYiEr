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
        private CandyRoomBackdrop roomBackdrop;
        private RectTransform bakeryWall, bakeryTray;
        private RectTransform frame, board, arenaLabels, progress, progressGleam, shop, mainAction, hintRect, characters, shopHeading;
        private BearMenuLayout roomLayout;
        private RectTransform whiteBear, brownBear, bearHeart;
        private Vector2 whiteRest, brownRest, heartRest;
        private readonly RectTransform[] ballIcons = new RectTransform[6];
        private readonly CandyBallPortrait[] ballPortraits = new CandyBallPortrait[6];
        private readonly BearMenuShape[] cardFaces = new BearMenuShape[6];
        private readonly float[] purchasePulse = new float[6], upgradePulse = new float[6];
        private float animationTime, happyTime, celebrateTime;
        private Text level, honey, honeyGain, goal, time, hint, introTitle, introBody, clickPower, ballCount;
        private float gainAge;
        private int gainAmount;
        private readonly BearMenuShape[] shopDots = new BearMenuShape[3];
        private int shownShopPage=-1;
        private Image[] stars;
        private GameObject intro;
        private Button clickUpgrade;
        private CandyBreakoutButton clickVisual;
        private readonly Button[] buy = new Button[6], upgrade = new Button[6];
        private readonly CandyBreakoutButton[] buyVisual = new CandyBreakoutButton[6], upgradeVisual = new CandyBreakoutButton[6];
        private readonly Text[] info = new Text[6], owned = new Text[6], ranks = new Text[6];
        private readonly Text[] unlockLabels = new Text[6];
        private RectTransform supportRow;
        private readonly Button[] supportButtons = new Button[3];
        private readonly Text[] supportLabels = new Text[3];
        private readonly List<Text> health = new List<Text>();
        private int[] shownHealth;
        private float[] healthPulse;
        private float lastArenaTime = -1;
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
        private string defaultHint = "";
        private bool showingBag;

        public void Build(IngameViewController owner, BearMenuArt theme)
        {
            art = theme;
            foreach (Transform child in owner.transform) child.gameObject.SetActive(false);
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            if (canvas != null) scaler = canvas.rootCanvas.GetComponent<CanvasScaler>();
            ApplyCanvasScale();
            RectTransform root = BearMenuFactory.Node(owner.transform, "CandyBreakoutUI", 0, 0, 1080, 1920);
            BearMenuFactory.Stretch(root);
            roomBackdrop=root.gameObject.AddComponent<CandyRoomBackdrop>();roomBackdrop.raycastTarget=false;
            frame = BearMenuFactory.Node(root, "CandyRoom", 0, 0, 1080, 1920);
            roomLayout = frame.gameObject.AddComponent<BearMenuLayout>();
            roomLayout.designSize = new Vector2(1080,1760);
            roomLayout.flexibleMain = true;
            roomLayout.Refresh();
            BuildHeader();
            board = Paper(frame,"CandyBoard",36,236,1008,656,Color.clear,34,false).rectTransform;
            board.GetComponent<BearMenuShape>().border=0;
            board.gameObject.AddComponent<RectMask2D>();
            var bakerySprites=Resources.LoadAll<Sprite>("CandyArt/SweetBakeryBackdrop");
            Image wall=BearGameTheme.Picture(art,board,"BakeryWall","",0,0,1008,240);
            Image tray=BearGameTheme.Picture(art,board,"BiscuitTray","",0,200,1008,800);
            foreach(var sprite in bakerySprites)
            {
                if(sprite.name=="BakeryRoom")wall.sprite=sprite;
                if(sprite.name=="BakeryTray")tray.sprite=sprite;
            }
            bakeryWall=wall.rectTransform;bakeryTray=tray.rectTransform;
            characters = BearMenuFactory.Node(board,"BearCompanions",0,0,1008,190);
            BearGameTheme.Bears(art,characters,298,30,.75f);
            whiteBear=(RectTransform)characters.Find("WhiteBear");
            brownBear=(RectTransform)characters.Find("BrownBear");
            bearHeart=(RectTransform)characters.Find("BearHeart");
            // Companions stay outside the collision field.
            RectTransform field = BearMenuFactory.Node(board,"Arena",16,12,976,632);
            arena = field.gameObject.AddComponent<CandyBreakoutGraphic>();
            arena.color = Color.white;
            arenaLabels = BearMenuFactory.Node(field,"BrickHealthLabels",0,0,976,632);
            BearMenuFactory.Stretch(arenaLabels);
            hint = Label(frame,"Hint","",36,1016,1008,40,29);
            hint.color = SecondaryInk;
            hintRect = hint.rectTransform;
            BuildMainAction();
            BuildSupportRow();
            shopHeading = BearMenuFactory.Node(frame,"ShopHeading",36,1210,1008,36);
            Label(shopHeading,"Title","糖果小队  ›",0,0,500,42,34,TextAnchor.MiddleLeft);
            ballCount = Label(shopHeading,"BallCount","",500,0,508,42,30,TextAnchor.MiddleRight);
            ballCount.color = SecondaryInk;
            shop = BearMenuFactory.Node(frame,"BallShop",36,1258,1008,350);
            shop.gameObject.AddComponent<RectMask2D>();
            shop.gameObject.AddComponent<Image>().color=Color.clear;
            RectTransform content=BearMenuFactory.Node(shop,"ShopCards",0,0,3030,350);
            ScrollRect scroll=shop.gameObject.AddComponent<ScrollRect>();
            scroll.viewport=shop;scroll.content=content;scroll.horizontal=true;scroll.vertical=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.decelerationRate=.12f;scroll.scrollSensitivity=45;
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(490,350);
            grid.spacing = new Vector2(18,18);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
            grid.childAlignment = TextAnchor.UpperLeft;
            for (int i=0;i<6;i++) BuildCard(content,i);
            RectTransform dots=BearMenuFactory.Node(frame,"ShopPages",0,0,64,12);
            BearGameTheme.Bottom(dots,508,6);
            for(int i=0;i<shopDots.Length;i++)
            {
                shopDots[i]=Paper(dots,"Page"+i,i*22,0,12,12,new Color32(206,180,145,255),6,false);
                shopDots[i].border=0;
            }
            scroll.onValueChanged.AddListener(UpdateShopPage);
            UpdateShopPage(Vector2.zero);
            BuildIntro(root);
            RefreshLayout(true);
        }
        private void BuildHeader()
        {
            RectTransform header = BearMenuFactory.Node(frame,"Header",0,28,1080,104);
            Paper(header,"LevelPaper",36,0,280,104,PaperWhite,30);
            level=Label(header,"Level","",50,20,252,64,41);
            Paper(header,"HoneyPaper",340,0,576,104,new Color32(255,239,194,255),30);
            BearGameTheme.Picture(art,header,"HoneyIcon","Daily/IconBottle",366,19,64,66);
            honey=Label(header,"Honey","",449,22,440,60,46,TextAnchor.MiddleLeft,true);
            honeyGain=Label(header,"HoneyGain","",740,28,142,52,30,TextAnchor.MiddleRight,true);
            honeyGain.color=new Color32(100,139,75,255);
            honeyGain.gameObject.SetActive(false);
            Label(frame,"Mode","",36,148,400,40,31,TextAnchor.MiddleLeft);
            goal=Label(frame,"Goal","",445,148,165,40,30,TextAnchor.MiddleLeft,true);
            stars=new Image[3];
            for(int i=0;i<3;i++) stars[i]=BearGameTheme.Picture(art,frame,"RatingStar"+i,"IconStar",655+i*46,149,37,35);
            time=Label(frame,"TimeAndStars","0:00",864,145,180,46,34,TextAnchor.MiddleRight,true);
            BearMenuShape track=Paper(frame,"ProgressTrack",36,206,1008,14,new Color32(238,218,190,255),7,false);
            track.border=2; track.borderColor=new Color32(188,153,117,255);
            progress=Paper(track.transform,"Progress",3,3,0,8,new Color32(227,154,115,255),4,false).rectTransform;
            progress.GetComponent<BearMenuShape>().border=0;
            progressGleam=Paper(progress,"ProgressGleam",0,3,0,4,new Color(1,1,1,.43f),2,false).rectTransform;
            progressGleam.GetComponent<BearMenuShape>().border=0;
        }
        private void BuildMainAction()
        {
            mainAction=BearMenuFactory.Node(frame,"MainAction",36,1072,1008,132);
            clickVisual=Action(mainAction,"ClickUpgrade",0,0,1008,132,BearGameTheme.Gold,UpgradeClick,true);
            clickUpgrade=clickVisual.button;
            BearGameTheme.Place(clickVisual.caption.rectTransform,clickVisual.face.transform,107,14,450,54);
            clickVisual.caption.fontSize=41;clickVisual.caption.alignment=TextAnchor.MiddleLeft;
            BearGameTheme.Place(clickVisual.price.rectTransform,clickVisual.face.transform,784,32,184,60);
            clickVisual.price.fontSize=44;
            clickPower=Label(clickVisual.face.transform,"ClickPower","",108,70,464,36,30,TextAnchor.MiddleLeft);
            BearGameTheme.Picture(art,clickVisual.face.transform,"HoneyPriceIcon","Daily/IconBottle",735,44,39,42);
            BearMenuShape icon=Paper(clickVisual.face.transform,"UpgradeIcon",23,24,68,68,new Color32(255,245,202,255),23,false);
            icon.border=0;
            Paper(icon.transform,"PlusHorizontal",17,30,34,8,BearGameTheme.Brown,4,false).border=0;
            Paper(icon.transform,"PlusVertical",30,17,8,34,BearGameTheme.Brown,4,false).border=0;
        }
        private void UpdateShopPage(Vector2 position)
        {
            int page=Mathf.Clamp(Mathf.RoundToInt(position.x*2),0,2);
            if(shownShopPage==page)return;
            shownShopPage=page;
            for(int i=0;i<shopDots.Length;i++)
            {
                Color color=i==page?(Color)BearGameTheme.Gold:new Color32(206,180,145,255);
                shopDots[i].color=shopDots[i].bottomColor=color;
                shopDots[i].SetVerticesDirty();
            }
        }
        private void BuildCard(Transform parent,int kind)
        {
            Color tint=CandyBreakoutGraphic.CandyColors[kind];
            RectTransform card=Paper(parent,"BallShop"+kind,0,0,490,350,PaperWhite,28).rectTransform;
            BearMenuShape icon=Paper(card,"CandyBall",20,20,84,84,tint,42,false);
            icon.border=0;icon.color=icon.bottomColor=Color.clear;
            cardFaces[kind]=card.GetComponent<BearMenuShape>();
            ballIcons[kind]=icon.rectTransform;
            ballIcons[kind].pivot=new Vector2(.5f,.5f);
            ballIcons[kind].anchoredPosition=new Vector2(62,-62);
            ballPortraits[kind]=BearMenuFactory.Node(icon.transform,"Material",0,0,84,84).gameObject.AddComponent<CandyBallPortrait>();
            ballPortraits[kind].kind=kind;ballPortraits[kind].useEquippedSkin=true;ballPortraits[kind].raycastTarget=false;
            Label(card,"Name",CandyBreakoutSimulation.Names[kind],122,18,344,52,42,TextAnchor.MiddleLeft);
            Label(card,"Description",CandyBreakoutSimulation.Descriptions[kind],122,75,344,40,32,TextAnchor.MiddleLeft).color=SecondaryInk;
            Paper(card,"OwnedPill",24,125,100,40,Color.Lerp(tint,PaperWhite,.65f),20,false).border=0;
            owned[kind]=Label(card,"Owned","",24,125,100,40,31);
            info[kind]=Label(card,"Stats","",145,125,190,40,31,TextAnchor.MiddleLeft);
            ranks[kind]=Label(card,"Rank","",350,125,116,40,30,TextAnchor.MiddleRight);
            owned[kind].font = ranks[kind].font = art.numberFont;
            ranks[kind].color=SecondaryInk;
            unlockLabels[kind]=Label(card,"UnlockRequirement",CandyProgression.UnlockText(kind),24,122,442,54,28);
            unlockLabels[kind].color=SecondaryInk;
            buyVisual[kind]=Action(card,"Buy",16,192,220,140,tint,()=>Buy(kind));
            upgradeVisual[kind]=Action(card,"Upgrade",254,192,220,140,Mint,()=>Upgrade(kind));
            buy[kind]=buyVisual[kind].button;upgrade[kind]=upgradeVisual[kind].button;
        }
        private void BuildSupportRow()
        {
            supportRow=BearMenuFactory.Node(frame,"TravelBag",36,0,1008,88);
            for(int i=0;i<3;i++)
            {
                int kind=i;
                supportButtons[i]=BearGameTheme.Button(art,supportRow,"Support"+i,"",i*340,0,328,88,PaperWhite,()=>UseSupport(kind));
                supportLabels[i]=supportButtons[i].GetComponentInChildren<Text>();
                supportLabels[i].fontSize=27;
            }
        }
        private void UseSupport(int kind)
        {
            if(game==null || !Ready)return;
            if(game.UseSupport(kind))
            {
                Notice(CandyCosmetics.SupportNames[kind]+" · "+CandyCosmetics.SupportHints[kind]);
                happyTime=.45f;
                arena.SetVerticesDirty();RefreshSupport();Refresh();
            }
        }
        private void RefreshSupport()
        {
            bool hasBag=false;
            for(int i=0;i<3;i++)
            {
                int quantity=PlayerDataHandler.GetShopQuantity(CandyCosmetics.SupportIds[i]);
                bool used=game.Board.UsedSupport[i];
                hasBag|=quantity>0 || used;
                supportLabels[i].text=used?CandyCosmetics.SupportNames[i]+" · 已用":CandyCosmetics.SupportNames[i]+" ×"+quantity;
                supportButtons[i].interactable=Ready && quantity>0 && !used;
            }
            supportRow.gameObject.SetActive(hasBag);
            if(showingBag!=hasBag){showingBag=hasBag;lastHeight=-1;}
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
            visual.caption=Label(face.transform,"Label","",5,9,w-14,44,32);
            visual.price=Label(face.transform,"Price","",8,57,w-20,56,primary?39:40,TextAnchor.MiddleCenter,true);
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
            int required=game.Board.Bricks.Count;
            while(health.Count<required)health.Add(Label(arenaLabels,"Health","",0,0,100,50,30,TextAnchor.MiddleCenter,true));
            if(shownHealth==null || shownHealth.Length<required)shownHealth=new int[health.Count];
            if(healthPulse==null || healthPulse.Length<required)healthPulse=new float[health.Count];
            for(int i=0;i<health.Count;i++)
            {
                health[i].gameObject.SetActive(i<required);
                if(i<required){health[i].text="";shownHealth[i]=-1;healthPulse[i]=0;health[i].rectTransform.localScale=Vector3.one;}
            }
            lastArenaTime=-1;
            level.text="第 "+game.CurrentLevel+" 关";
            int chapter=CandyProgression.Chapter(game.CurrentLevel);
            frame.Find("Mode").GetComponent<Text>().text=CandyProgression.ChapterNames[chapter];
            roomBackdrop.chapter=chapter;roomBackdrop.SetVerticesDirty();
            introTitle.text="第 "+game.CurrentLevel+" 关"+(game.Level.boss?" · 大糖砖":"");
            introBody.text="① 点击糖砖，赚取本关蜂蜜\n② 买奶糖球并升级，清空糖砖即通关\n③ 通关第 3 关后解锁草莓球\n\n首次通关："+game.Level.coinReward+" 金币 + 星星奖励";
            intro.SetActive(!game.IsRoundStarted);
            lastHoney=lastDestroyed=lastSeconds=lastStars=lastShopHoney=-1;
            displayedProgress=targetProgress=0;hintUntil=0;
            hint.text="";hint.gameObject.SetActive(false);
            animationTime=happyTime=celebrateTime=honeyPop=0;
            ResetFeedback();
            RefreshLayout(true);Refresh();
            // Teach the first round once; retries and later levels enter play directly.
            if (PlayerDataHandler.IsWatchedTutorial())
            {
                intro.SetActive(false);
                game.OnTutorialClosed();
            }
            RefreshSupport();
        }
        public void RefreshLayout(bool force=false)
        {
            if(frame==null||arena==null)return;
            roomLayout.Refresh();
            float h=frame.rect.height;
            if(!force&&Mathf.Abs(h-lastHeight)<.1f)return;
            lastHeight=h;
            float shopY=h-374;
            BearGameTheme.Place(shop,frame,36,shopY,1008,350);
            BearGameTheme.Place(shopHeading,frame,36,shopY-52,1008,42);
            float mainY=shopY-186;
            BearGameTheme.Place(mainAction,frame,36,mainY,1008,132);
            BearGameTheme.Place(supportRow,frame,36,mainY-100,1008,88);
            BearGameTheme.Place(hintRect,frame,52,mainY-(showingBag?143:48),976,36);
            float boardH=mainY-(showingBag?158:62)-236;
            BearGameTheme.Place(board,frame,36,236,1008,boardH);
            // Size the visible field to the collision board with an identical X/Y scale.
            float fieldH=Mathf.Min(boardH-88,896*CandyBreakoutSimulation.Height/CandyBreakoutSimulation.Width);
            float companionSpace=boardH-fieldH-88;
            // On tall phones move the interactive field down toward the player's thumb.
            BearGameTheme.Place(bakeryTray,board,0,companionSpace,1008,fieldH+88);
            BearGameTheme.Place(arena.rectTransform,board,56,44+companionSpace,896,fieldH);
            bakeryWall.gameObject.SetActive(companionSpace>=100);
            BearGameTheme.Place(bakeryWall,board,0,0,1008,Mathf.Max(1,companionSpace+10));
            characters.gameObject.SetActive(companionSpace>=100);
            float bearScale = Mathf.Clamp((companionSpace-28)/270,.25f,1);
            float bearHeight = 230 * bearScale + 40;
            BearGameTheme.Place(characters,board,0,Mathf.Max(0,companionSpace-bearHeight+12),1008,bearHeight);
            float bearX = (1008-550*bearScale)*.5f;
            BearGameTheme.Place(whiteBear,characters,bearX,35,270*bearScale,230*bearScale);
            BearGameTheme.Place(brownBear,characters,bearX+280*bearScale,35,270*bearScale,230*bearScale);
            BearGameTheme.Place(bearHeart,characters,bearX+242*bearScale,0,70*bearScale,61*bearScale);
            whiteRest=whiteBear.anchoredPosition;
            brownRest=brownBear.anchoredPosition;
            heartRest=bearHeart.anchoredPosition;
            arena.FitPresentation();
            PositionHealth();
        }
        private void PositionHealth()
        {
            if(game==null)return;
            for(int i=0;i<game.Board.Bricks.Count;i++)
            {
                var brick=game.Board.Bricks[i];Rect r=arena.VisualRect(brick.Bounds);
                BearGameTheme.Place(health[i].rectTransform,arenaLabels,r.x-arena.rectTransform.rect.xMin,
                    arena.rectTransform.rect.yMax-r.yMax,r.width,r.height);
                RectTransform label=health[i].rectTransform;
                label.anchoredPosition+=new Vector2(r.width*.5f,-r.height*.5f);
                label.pivot=new Vector2(.5f,.5f);
                health[i].color=new Color32(60,33,18,255);
                health[i].fontSize=brick.Boss?42:Mathf.Clamp(Mathf.RoundToInt(r.height*.88f),30,40);
            }
        }
        private bool Ready=>game!=null&&game.CanInteract&&game.Board!=null&&!game.Board.Complete;
        private void Buy(int kind)
        {
            if(!Ready)return;
            if(game.Board.Buy(kind))
            {
                ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Purchase);
                purchasePulse[kind]=.32f;
                Notice(CandyBreakoutSimulation.Names[kind]+"加入小队");
            }
            Refresh();
        }
        private void Upgrade(int kind)
        {
            if(!Ready)return;
            if(game.Board.Upgrade(kind))
            {
                ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Upgrade);
                upgradePulse[kind]=.48f;celebrateTime=.55f;
                Notice(CandyBreakoutSimulation.Names[kind]+"威力提升");
            }
            Refresh();
        }
        private void UpgradeClick()
        {
            if(Ready&&game.Board.UpgradeClick())
            {
                ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Upgrade);
                celebrateTime=.55f;
                Notice("点击威力提升");
            }
            Refresh();
        }
        private void Notice(string message){hint.gameObject.SetActive(true);hint.text=message;hintUntil=animationTime+2;}
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
            if(!gameObject.activeInHierarchy)
            {
                game=null;
                if(arena!=null)arena.ReleaseBoard();
            }
        }
        private void LateUpdate()
        {
            RefreshLayout();
            if(game==null||arena.Board!=game.Board)return;
            if(game.IsRoundStarted&&intro.activeSelf)intro.SetActive(false);
            Refresh();
            if(lastArenaTime!=game.Board.Elapsed)
            {
                lastArenaTime=game.Board.Elapsed;
                arena.SetVerticesDirty();
            }
            float delta=game.IsPaused || Time.timeScale<=0 ? 0 : Time.deltaTime;
            if(delta<=0)return;
            animationTime+=delta;
            arena.AdvanceFeedback(delta);
            if(honeyGain.gameObject.activeSelf)
            {
                gainAge+=delta;
                float t=Mathf.Clamp01(gainAge/.65f);
                honeyGain.rectTransform.anchoredPosition=new Vector2(740,-28+t*14);
                honeyGain.color=new Color(100/255f,139/255f,75/255f,1-Mathf.SmoothStep(.35f,1,t));
                if(t>=1){honeyGain.gameObject.SetActive(false);gainAmount=0;}
            }
            for(int i=0;i<health.Count;i++)
            {
                if(healthPulse==null || i>=healthPulse.Length || healthPulse[i]<=0)continue;
                healthPulse[i]=Mathf.Max(0,healthPulse[i]-delta);
                health[i].rectTransform.localScale=Vector3.one*(1+Mathf.Sin(healthPulse[i]/.16f*Mathf.PI)*.12f);
            }
            displayedProgress=Mathf.Lerp(displayedProgress,targetProgress,1-Mathf.Exp(-delta*9));
            float width=1002*displayedProgress;
            progress.sizeDelta=new Vector2(width,8);
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
            if(honeyGain!=null)honeyGain.gameObject.SetActive(false);
            gainAmount=0;gainAge=0;
            for(int i=0;i<health.Count;i++)
            {
                if(healthPulse!=null && i<healthPulse.Length)healthPulse[i]=0;
                if(health[i]!=null)health[i].rectTransform.localScale=Vector3.one;
            }
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
            if(lastHoney!=b.Honey)
            {
                if(lastHoney>=0 && b.Honey>lastHoney)
                {
                    honeyPop=1;happyTime=.45f;
                    gainAmount=(int)System.Math.Min(int.MaxValue,(long)gainAmount+b.Honey-lastHoney);
                    gainAge=0;honeyGain.text="+"+gainAmount;
                    honeyGain.gameObject.SetActive(true);
                }
                else if(lastHoney>=0){gainAmount=0;honeyGain.gameObject.SetActive(false);}
                lastHoney=b.Honey;honey.text=b.Honey.ToString();
            }
            bool shopChanged=lastShopHoney!=b.Honey || lastReady!=active || shownClickLevel!=b.ClickLevel;
            for(int i=0;i<6;i++) shopChanged|=shownCounts[i]!=b.Counts[i] || shownUpgrades[i]!=b.Upgrades[i];
            if(shopChanged)
            {
                lastShopHoney=b.Honey;lastReady=active;shownClickLevel=b.ClickLevel;
                bool clickMax=b.ClickLevel>=CandyBreakoutSimulation.UpgradeLimit;
                bool affordClick=b.Honey>=b.ClickCost;
                clickVisual.SetState(active&&!clickMax&&affordClick,BearGameTheme.Gold,clickMax?"点击已满级":"强化点击",clickMax?"MAX":b.ClickCost.ToString(),clickMax);
                clickPower.text=clickMax?"威力 "+(1+b.ClickLevel*2):"威力 "+(1+b.ClickLevel*2)+" → "+(3+b.ClickLevel*2);
                clickPower.color=affordClick?SecondaryInk:new Color32(145,107,78,255);
                for(int i=0;i<6;i++)
                {
                    shownCounts[i]=b.Counts[i];shownUpgrades[i]=b.Upgrades[i];
                    bool capped=b.Balls.Count>=CandyBreakoutSimulation.BallLimit;
                    bool max=b.Upgrades[i]>=CandyBreakoutSimulation.UpgradeLimit;
                    int cost=b.BuyCost(i),upCost=b.UpgradeCost(i);
                    bool unlocked=b.IsUnlocked(i);
                    unlockLabels[i].gameObject.SetActive(!unlocked);
                    owned[i].gameObject.SetActive(unlocked);info[i].gameObject.SetActive(unlocked);ranks[i].gameObject.SetActive(unlocked && b.Counts[i]>0);
                    cardFaces[i].transform.Find("OwnedPill").gameObject.SetActive(unlocked);
                    ballPortraits[i].locked=!unlocked;ballPortraits[i].SetVerticesDirty();
                    buyVisual[i].price.font=unlocked?art.numberFont:art.chineseFont;
                    if(!unlocked)
                    {
                        buyVisual[i].SetState(false,CandyBreakoutGraphic.CandyColors[i],"尚未解锁","第 "+CandyProgression.BallUnlockClears[i]+" 关");
                        upgradeVisual[i].SetState(false,Mint,"通关解锁","");
                        continue;
                    }
                    owned[i].text="× "+b.Counts[i];info[i].text="威力 "+b.Damage(i);ranks[i].text="Lv."+b.Upgrades[i];
                    buyVisual[i].SetState(active&&!capped&&b.Honey>=cost,CandyBreakoutGraphic.CandyColors[i],
                        capped?"数量已满":b.Honey<cost?"还差 "+(cost-b.Honey):"购买",capped?"24 / 24":cost.ToString());
                    buyVisual[i].caption.fontSize=32;
                    upgradeVisual[i].SetState(active&&b.Counts[i]>0&&!max&&b.Honey>=upCost,Mint,
                        max?"已满级":b.Counts[i]==0?"先购买":b.Honey<upCost?"还差 "+(upCost-b.Honey):"升级",max?"MAX":upCost.ToString(),max);
                    upgradeVisual[i].caption.fontSize=32;
                    upgradeVisual[i].emphasized=active&&b.Counts[i]>0&&!max&&b.Honey>=upCost;
                }
                ballCount.text="球数上限："+b.Balls.Count+" / 24";
                RefreshSupport();
            }
            if(lastDestroyed!=b.Destroyed)
            {
                lastDestroyed=b.Destroyed;goal.text=b.Destroyed+" / "+b.Total;
                targetProgress=(float)b.Destroyed/Mathf.Max(1,b.Total);
                defaultHint="";
            }
            int seconds=(int)b.Elapsed;
            if(seconds!=lastSeconds){lastSeconds=seconds;time.text=seconds/60+":"+(seconds%60).ToString("00");}
            if(lastStars!=game.CurrentStar)
            {
                lastStars=game.CurrentStar;
                for(int i=0;i<3;i++) stars[i].color=i<lastStars?Color.white:new Color(.65f,.52f,.37f,.25f);
            }
            for(int i=0;i<b.Bricks.Count;i++)
            {
                var brick=b.Bricks[i];
                if(shownHealth[i]==brick.Health)continue;
                if(brick.Alive && shownHealth[i]>brick.Health)healthPulse[i]=.16f;
                if(!brick.Alive && shownHealth[i]>0)arena.PlayBreak(brick);
                shownHealth[i]=brick.Health;
                health[i].text=brick.Alive?brick.Health.ToString():"";
            }
            if(animationTime>hintUntil){hint.text=defaultHint;hint.gameObject.SetActive(false);}
        }
    }
}
