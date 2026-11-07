using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    // Used by the editor baker and as a runtime fallback. Every control remains a normal uGUI object.
    public static partial class BearMenuFactory
    {
        private static readonly Color Brown = new Color32(82, 42, 10, 255);
        private static readonly Color Cream = new Color32(255, 247, 232, 255);
        private static readonly Color Gold = new Color32(255, 229, 93, 255);

        public static void Build(BearHomeMenuController menu)
        {
            if (menu.art == null) return;
            if (menu.themeRoot != null)
            {
                if (Application.isPlaying) Object.Destroy(menu.themeRoot.gameObject);
                else Object.DestroyImmediate(menu.themeRoot.gameObject);
            }
            Canvas canvas = menu.GetComponent<Canvas>();
            if (canvas == null) canvas = menu.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            if (menu.GetComponent<GraphicRaycaster>() == null) menu.gameObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = menu.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = menu.gameObject.AddComponent<CanvasScaler>();
            ConfigureScaler(scaler);
            foreach (Transform child in menu.transform) child.gameObject.SetActive(false);
            menu.themeRoot = Node(menu.transform, "BearMenuUI", 0, 0, 1024, 1536);
            Stretch(menu.themeRoot);
            Image backdrop = menu.themeRoot.gameObject.AddComponent<Image>();
            backdrop.color = Cream;
            backdrop.raycastTarget = true;
            menu.pages = new GameObject[5];
            menu.coinTexts = new Text[5];
            for (int i = 0; i < menu.pages.Length; i++)
            {
                RectTransform frame = Node(menu.themeRoot, new[] { "Home", "DailyReward", "Collection", "Shop", "Settings" }[i], 0, 0, i == 0 ? 1024 : 941, i == 0 ? 1536 : 1672);
                BearMenuLayout layout = frame.gameObject.AddComponent<BearMenuLayout>();
                layout.designSize = i == 0 ? new Vector2(1024, 1536) : new Vector2(941, 1672);
                layout.flexibleMain = i == 0;
                menu.pages[i] = frame.gameObject;
                if (i == 0) Main(menu, frame);
                else if (i == 1) Daily(menu, frame);
                else if (i == 3) Shop(menu, frame);
                else if (i == 4) Settings(menu, frame);
                else if (i == 2) Collection(menu, frame);
                else
                {
                    Picture(menu, frame, "ReferenceArtwork", new[] { "", "DailyFrame", "CollectionFrame", "ShopFrame", "SettingsFrame" }[i], 0, 0, 941, 1672);
                    Hotspot(frame, "Back", 18, 15, 245, 108, "page", 0);
                    Hotspot(frame, "CoinPlus", 828, 17, 96, 105, "page", 3);
                    Picture(menu, frame, "CoinPaper", i == 2 ? "CollectionCoinPaper" : "SubCoinPaper", i == 2 ? 700 : 680, i == 2 ? 31 : 32, i == 2 ? 134 : 154, i == 2 ? 56 : 67);
                    menu.coinTexts[i] = Label(menu, frame, "LiveCoins", "0", i == 2 ? 700 : 679, i == 2 ? 26 : 37, i == 2 ? 134 : 154, 63, i == 2 ? 48 : 51, true);
                }
            }
            Modal(menu);
            for (int i = 0; i < menu.pages.Length; i++) menu.pages[i].SetActive(i == 0);
        }

        private static void Main(BearHomeMenuController menu, RectTransform frame)
        {
            RectTransform footer = Node(frame,"FooterGarden",0,1400,1024,136);
            Bottom(footer,0);
            Picture(menu,footer,"GrassGround","GrassGround",-12,-5,1048,155);
            Picture(menu,footer,"LeftFlowers","BushL",-42,-10,300,140);
            Picture(menu,footer,"RightFlowers","BushR",766,-10,300,140);
            Picture(menu,footer,"GrassTuftLeft","Grass",285,62,54,32);
            Picture(menu,footer,"GrassTuftCenter","Grass",488,87,54,32);
            Picture(menu,footer,"GrassTuftRight","Grass",692,62,54,32);

            RectTransform header = Node(frame,"Header",0,26,1024,112);
            RectTransform level = Node(header,"LevelStatus",30,0,303,104);
            Picture(menu,level,"Panel","TopUIBg",0,0,303,96).type=Image.Type.Sliced;
            Picture(menu,level,"Star","IconStar",16,1,88,83);
            menu.levelText=Label(menu,level,"LiveLevel","Lv. 1",112,3,170,59,48,true);
            Picture(menu,level,"ProgressTrack","SceneArt_5",110,67,160,19);
            Picture(menu,level,"ProgressFill","SceneArt_6",115,71,49,12);
            RectTransform coins=Node(header,"CoinStatus",624,0,370,104);
            Picture(menu,coins,"Panel","TopUIBg",0,0,370,96).type=Image.Type.Sliced;
            Picture(menu,coins,"Coin","IconCoin",4,-4,106,104);
            menu.coinTexts[0]=Label(menu,coins,"LiveCoins","0",123,7,149,68,54,true);
            Picture(menu,coins,"AddIcon","IconAdd",282,-4,106,107);
            Hotspot(coins,"AddCoins",282,-4,106,107,"page",3);

            RectTransform title=Node(frame,"BrandTitle",0,130,1024,258);
            Picture(menu,title,"TitleCloud","TitleBg",182,24,660,230);
            Picture(menu,title,"TitleLetters","TitleText",247,78,530,139);
            Picture(menu,title,"Heart","IconLove",468,-5,88,76);
            Picture(menu,title,"LeftRays","PlayBtnDeL",103,86,68,119);
            Picture(menu,title,"RightRays","PlayBtnDeR",854,86,68,119);

            RectTransform room=Node(frame,"BearRoom",0,358,1024,640);
            room.anchorMin=room.anchorMax=room.pivot=new Vector2(.5f,.5f);
            room.anchoredPosition=new Vector2(0,90);
            Picture(menu,room,"Window","Window",198,8,628,296);
            Picture(menu,room,"CurtainLeft","CurtainL",128,0,181,291);
            Picture(menu,room,"CurtainRight","CurtainR",685,0,183,292);
            Picture(menu,room,"WallPicture","Picture",874,25,148,181);
            Picture(menu,room,"Plant","Plant1",2,153,180,291);
            Picture(menu,room,"Cabinet","Desk",866,330,201,160);
            Picture(menu,room,"Books","Book",917,217,135,125);
            Picture(menu,room,"Table","Table",25,422,974,218).type=Image.Type.Sliced;
            Picture(menu,room,"WhiteBear","BearWhite",199,208,324,275);
            Picture(menu,room,"BrownBear","BearBrown",523,208,325,271);
            Picture(menu,room,"BetweenBearsHeart","IconLove",475,189,88,76);
            Picture(menu,room,"CandyJar","SugarPile",436,381,153,186);
            Picture(menu,room,"PinkCandy","Sugar1",327,522,67,56);
            Picture(menu,room,"YellowCandy","Sugar2",369,542,61,52);
            Picture(menu,room,"BlueCandy","Sugar3",623,530,77,64);

            RectTransform play=Node(frame,"PlayAction",0,996,1024,190);
            Bottom(play,350);
            Picture(menu,play,"LeftRays","PlayBtnDeL",96,13,84,147);
            Picture(menu,play,"RightRays","PlayBtnDeR",844,13,84,147);
            Picture(menu,play,"ButtonArtwork","PlayBtn",187,0,650,195);
            Hotspot(play,"StartGame",187,0,650,195,"play");

            RectTransform navigation=Node(frame,"Navigation",0,1186,1024,220);
            Bottom(navigation,130);
            string[] names={"DailyReward","Collection","Shop","Settings"};
            string[] icons={"IconReward","IconHandBook","IconShop","HandSetting"};
            string[] labels={"每日奖励","图鉴收藏","商店","设置"};
            for(int i=0;i<4;i++)
            {
                RectTransform button=Node(navigation,names[i],40+i*244,0,212,212);
                Picture(menu,button,"Panel","BtnBg",0,0,212,212).type=Image.Type.Sliced;
                Image icon=Picture(menu,button,"Icon",icons[i],42,20,128,112);
                icon.preserveAspect=true;
                Label(menu,button,"Label",labels[i],8,137,196,62,i<2?37:43);
                if(i==0) Picture(menu,button,"RewardNotice","IconThing",164,-13,64,60);
                Action(button,"page",i+1);
            }
            EnsureSoloActions(menu);
        }

        public static void RebuildMain(BearHomeMenuController menu)
        {
            ConfigureScaler(menu.GetComponent<CanvasScaler>());
            RectTransform frame=menu.pages[0].GetComponent<RectTransform>();
            for(int i=frame.childCount-1;i>=0;i--) Object.DestroyImmediate(frame.GetChild(i).gameObject);
            Main(menu,frame);
        }

        private static void ConfigureScaler(CanvasScaler scaler)
        {
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1024,1536);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight=0;
            scaler.referencePixelsPerUnit=100;
        }

        private static void Daily(BearHomeMenuController menu, Transform frame)
        {
            RectTransform footer=Node(frame,"FooterGarden",0,1538,941,134);
            Picture(menu,footer,"GrassGround","GrassGround",-20,0,981,144);
            Picture(menu,footer,"LeftFlowers","BushL",-40,0,300,140);
            Picture(menu,footer,"RightFlowers","BushR",681,0,300,140);
            RectTransform header=Node(frame,"Header",0,0,941,120);
            Picture(menu,header,"BackPanel","TopUIBg",25,22,225,88).type=Image.Type.Sliced;
            Image back=Picture(menu,header,"BackArrow","Setting/IconArrow",51,42,27,42);
            CenterImage(back);
            back.rectTransform.localEulerAngles=new Vector3(0,0,180);
            Label(menu,header,"BackLabel","??",95,29,126,66,43);
            Hotspot(header,"Back",25,22,225,88,"page",0);
            Picture(menu,header,"CoinPanel","TopUIBg",572,22,340,88).type=Image.Type.Sliced;
            Picture(menu,header,"CoinIcon","IconCoin",582,18,88,86);
            menu.coinTexts[1]=Label(menu,header,"LiveCoins","0",679,37,154,63,51,true);
            Picture(menu,header,"AddIcon","IconAdd",833,19,84,85);
            Hotspot(header,"AddCoins",833,19,84,85,"page",3);
            RectTransform room=Node(frame,"RoomDecorations",0,125,941,360);
            Picture(menu,room,"Window","Window",211,4,518,244);
            Picture(menu,room,"CurtainLeft","CurtainL",141,0,150,241);
            Picture(menu,room,"CurtainRight","CurtainR",616,0,151,241);
            Picture(menu,room,"Plant","Plant1",25,83,126,204);
            Picture(menu,room,"WallPicture","Picture",808,12,112,137);
            Picture(menu,room,"Cabinet","Desk",792,231,160,127);
            Picture(menu,room,"Books","Book",838,160,100,93);
            Picture(menu,frame,"RewardsPanel","Setting/UIBg",26,485,889,1072).type=Image.Type.Sliced;
            Picture(menu,frame,"TitleCloud","Setting/TitleBg",194,435,550,145).type=Image.Type.Sliced;
            Picture(menu,frame,"WhiteBear","BearWhite",197,257,255,217);
            Picture(menu,frame,"BrownBear","BearBrown",480,257,265,221);
            Picture(menu,frame,"Heart","IconLove",434,222,78,67);
            Picture(menu,frame,"TitleIcon","IconReward",239,464,91,81).preserveAspect=true;
            Label(menu,frame,"TitleLabel","????",344,457,352,108,68);
            Image close=Picture(menu,frame,"CloseIcon","IconAdd",796,479,112,112);
            close.rectTransform.localEulerAngles=new Vector3(0,0,45);
            Hotspot(frame,"CloseDailyReward",796,479,112,112,"page",0);
            menu.dailyCooldownText=Label(menu,frame,"RewardCooldown","",100,586,741,32,25);
            menu.dailyCooldownText.gameObject.SetActive(false);
            menu.dailyCards=new BearMenuShape[7];
            menu.dailyCardArt=new Image[7];
            menu.dailyStatusIcons=new Image[7];
            menu.dailyAvailableArt=new GameObject[7];
            menu.dailyClaimBackgrounds=new GameObject[7];
            menu.dailyStates=new Text[7];
            menu.dailyClaims=new Button[7];
            string[] icons={"IconCoin","IconLove","IconGrass","IconFlower","IconTicket","IconCoi","IconBottle"};
            string[] amounts={"?100","?3","?1","?5","?1","?300","?1"};
            float[] xs={79,349,612};
            for(int i=0;i<7;i++)
            {
                bool last=i==6;
                float w=last?786:252, h=last?203:310;
                RectTransform card=Node(frame,"Day"+(i+1),last?77:xs[i%3],last?1283:623+i/3*337,w,h);
                menu.dailyCardArt[i]=Picture(menu,card,"CardArtwork",last?"Daily/FinalRewardBg":"Daily/UnLockRewardBg",0,0,w,h);
                menu.dailyCardArt[i].type=Image.Type.Sliced;
                menu.dailyAvailableArt[i]=Picture(menu,card,"AvailableArtwork","Daily/TodyRewardBg",-23,-61,298,371).gameObject;
                menu.dailyAvailableArt[i].GetComponent<Image>().type=Image.Type.Sliced;
                menu.dailyAvailableArt[i].SetActive(false);
                menu.dailyCards[i]=Shape(card,"SelectionOutline",0,0,w,h,last?44:45,Color.clear,Color.clear,0);
                Picture(menu,card,"DayBadge","Setting/ChoicBg",last?55:50,last?27:15,last?177:152,56).type=Image.Type.Sliced;
                Label(menu,card,"DayLabel","?"+(i+1)+"?",last?55:50,last?27:15,last?177:152,56,37);
                Image icon=Picture(menu,card,"RewardIcon","Daily/"+icons[i],last?288:46,last?23:80,last?143:160,last?158:116);
                icon.preserveAspect=true;
                Label(menu,card,"Amount",amounts[i],last?436:26,last?120:184,last?110:200,49,last?43:40,true);
                RectTransform claim=Node(card,"Claim",last?550:12,last?110:231,228,69);
                menu.dailyClaimBackgrounds[i]=Picture(menu,claim,"ClaimBackground","Setting/FillSlider",0,0,228,69).gameObject;
                menu.dailyClaimBackgrounds[i].GetComponent<Image>().type=Image.Type.Sliced;
                menu.dailyStatusIcons[i]=Picture(menu,claim,"StateIcon","Daily/IconLock",14,13,43,45);
                menu.dailyStatusIcons[i].preserveAspect=true;
                menu.dailyStates[i]=Label(menu,claim,"ClaimState","???",61,0,163,69,34);
                menu.dailyClaims[i]=Action(claim,"claim",i);
            }
            Picture(menu,frame,"BottomBow","Setting/Decoration1",364,1506,213,116);
            menu.RefreshDailyRewards(1,0);
        }

        public static void RebuildDaily(BearHomeMenuController menu)
        {
            RectTransform frame=menu.pages[1].GetComponent<RectTransform>();
            for(int i=frame.childCount-1;i>=0;i--) Object.DestroyImmediate(frame.GetChild(i).gameObject);
            Daily(menu,frame);
        }

        private static void Shop(BearHomeMenuController menu, Transform frame)
        {
            RectTransform footer=Node(frame,"FooterGarden",0,1538,941,134);
            Picture(menu,footer,"GrassGround","GrassGround",-20,0,981,144);
            Picture(menu,footer,"LeftFlowers","BushL",-40,0,300,140);
            Picture(menu,footer,"RightFlowers","BushR",681,0,300,140);
            RectTransform header=Node(frame,"Header",0,0,941,120);
            Picture(menu,header,"BackPanel","TopUIBg",25,22,225,88).type=Image.Type.Sliced;
            Image back=Picture(menu,header,"BackArrow","Setting/IconArrow",51,42,27,42);
            CenterImage(back);
            back.rectTransform.localEulerAngles=new Vector3(0,0,180);
            Label(menu,header,"BackLabel","??",95,29,126,66,43);
            Hotspot(header,"Back",25,22,225,88,"page",0);
            Picture(menu,header,"CoinPanel","TopUIBg",572,22,340,88).type=Image.Type.Sliced;
            Picture(menu,header,"CoinIcon","IconCoin",582,18,88,86);
            menu.coinTexts[3]=Label(menu,header,"LiveCoins","0",679,37,154,63,51,true);
            Picture(menu,header,"AddIcon","IconAdd",833,19,84,85);
            Hotspot(header,"AddCoins",833,19,84,85,"shop-tab",1);
            RectTransform room=Node(frame,"RoomDecorations",0,125,941,360);
            Picture(menu,room,"Window","Window",211,4,518,244);
            Picture(menu,room,"CurtainLeft","CurtainL",141,0,150,241);
            Picture(menu,room,"CurtainRight","CurtainR",616,0,151,241);
            Picture(menu,room,"Plant","Plant1",25,83,126,204);
            Picture(menu,room,"WallPicture","Picture",808,12,112,137);
            Picture(menu,room,"Cabinet","Desk",792,231,160,127);
            Picture(menu,room,"Books","Book",838,160,100,93);
            Picture(menu,frame,"ShopTitle","Shop/Header",187,210,567,290).preserveAspect=true;
            Picture(menu,frame,"ShopPanel","Setting/UIBg",17,606,907,965).type=Image.Type.Sliced;
            menu.shopTabImages=new Image[5];
            string[] names={"??","??","??","???","???"};
            string[] icons={"GiftIcon","CoinIcon","HeartIcon","TicketIcon","AlbumIcon"};
            for(int i=0;i<5;i++)
            {
                RectTransform tab=Node(frame,"Category"+i,29+i*179,499,172,106);
                menu.shopTabImages[i]=Picture(menu,tab,"Background",i==0?"Shop/TabSelected":"Shop/TabNormal",0,0,172,i==0?106:82);
                menu.shopTabImages[i].type=Image.Type.Sliced;
                Picture(menu,tab,"Icon","Shop/"+icons[i],15,17,48,48).preserveAspect=true;
                Label(menu,tab,"Label",names[i],66,9,102,64,i<3?34:28);
                Action(tab,"shop-tab",i);
            }
            menu.storePrices=new Text[5];
            menu.shopProductRows=new RectTransform[5];
            RectTransform featured=Node(frame,"NewPlayerPack",43,651,853,278);
            menu.shopProductRows[0]=featured;
            Picture(menu,featured,"Background","Shop/FeaturedBg",0,0,853,278).type=Image.Type.Sliced;
            Picture(menu,featured,"ValueBanner","Shop/Banner",38,-22,294,89);
            Label(menu,featured,"BannerLabel","????",109,-9,199,66,39);
            Picture(menu,featured,"Gift","IconReward",84,76,184,164).preserveAspect=true;
            Picture(menu,featured,"Coins","Daily/IconCoi",47,190,120,74).preserveAspect=true;
            Picture(menu,featured,"CandyJar","Daily/IconBottle",234,127,96,116).preserveAspect=true;
            Picture(menu,featured,"Hearts","Shop/Hearts",181,202,119,58).preserveAspect=true;
            Picture(menu,featured,"SparkleLeft","IconTenStar",32,90,34,38).preserveAspect=true;
            Picture(menu,featured,"SparkleRight","IconTenStar",310,45,38,42).preserveAspect=true;
            Label(menu,featured,"Name","????",348,22,279,65,49);
            string[] rewardIcons={"Daily/IconCoin","Daily/IconLove","Daily/IconBottle"};
            string[] rewards={"?300","?10","?1"};
            for(int i=0;i<3;i++)
            {
                Picture(menu,featured,"RewardIcon"+i,rewardIcons[i],365,98+i*43,37,37).preserveAspect=true;
                Label(menu,featured,"RewardAmount"+i,rewards[i],412,92+i*43,143,45,34,true);
            }
            Picture(menu,featured,"Bear","Shop/Bear",654,-24,174,138).preserveAspect=true;
            Picture(menu,featured,"PurchaseFrame","Setting/ChoicBg",558,114,273,143).type=Image.Type.Sliced;
            menu.storePrices[0]=ShopPurchase(menu,featured,0,594,146,234,91,"?6");
            string[] rowIcons={"Daily/IconCoi","Shop/Hearts","Daily/IconTicket","IconHandBook"};
            string[] rowNames={"????","????","?????","?????"};
            string[] quantities={"?1200","?30","?5","?1"};
            string[] prices={"?12","?12","?30","?68"};
            for(int i=1;i<5;i++)
            {
                RectTransform row=Node(frame,"Product"+i,43,947+(i-1)*150,853,136);
                menu.shopProductRows[i]=row;
                Picture(menu,row,"Background","Shop/RowBg",0,0,853,136).type=Image.Type.Sliced;
                Picture(menu,row,"ProductIcon",rowIcons[i-1],48,16,174,108).preserveAspect=true;
                Picture(menu,row,"NameBadge","Shop/NameBadge",286,19,190,43).type=Image.Type.Sliced;
                Label(menu,row,"Name",rowNames[i-1],281,15,200,52,i==3?31:34);
                string rewardIcon=i==1?"Daily/IconCoin":i==2?"Daily/IconLove":i==3?"Daily/IconTicket":"IconHandBook";
                Picture(menu,row,"RewardIcon",rewardIcon,305,77,39,39).preserveAspect=true;
                Label(menu,row,"Amount",quantities[i-1],350,71,160,49,39,true);
                menu.storePrices[i]=ShopPurchase(menu,row,i,610,31,218,78,prices[i-1]);
            }
            menu.shopCategoryHint=Label(menu,frame,"CategoryHint","???????????",110,854,720,70,34);
            Picture(menu,frame,"BottomBow","Setting/Decoration1",364,1526,213,116);
            menu.PreviewShopCategory(0);
        }

        private static Text ShopPurchase(BearHomeMenuController menu,Transform parent,int product,float x,float y,float w,float h,string price)
        {
            RectTransform button=Node(parent,"Purchase",x,y,w,h);
            Picture(menu,button,"Background","Shop/BuyButton",0,0,w,h).type=Image.Type.Sliced;
            Text text=Label(menu,button,"Price",price,8,0,w-16,h,product==0?54:49,true);
            text.resizeTextForBestFit=true;text.resizeTextMinSize=25;text.resizeTextMaxSize=product==0?54:49;
            Action(button,"buy",product);
            return text;
        }

        public static void RebuildShop(BearHomeMenuController menu)
        {
            RectTransform frame=menu.pages[3].GetComponent<RectTransform>();
            for(int i=frame.childCount-1;i>=0;i--) Object.DestroyImmediate(frame.GetChild(i).gameObject);
            Shop(menu,frame);
        }

        private static void Settings(BearHomeMenuController menu, Transform frame)
        {
            RectTransform footer=Node(frame,"FooterGarden",0,1538,941,134);
            Picture(menu,footer,"GrassGround","GrassGround",-20,0,981,144);
            Picture(menu,footer,"LeftFlowers","BushL",-40,0,300,140);
            Picture(menu,footer,"RightFlowers","BushR",681,0,300,140);
            RectTransform header=Node(frame,"Header",0,0,941,120);
            Picture(menu,header,"BackPanel","TopUIBg",25,22,225,88).type=Image.Type.Sliced;
            Image back=Picture(menu,header,"BackArrow","Setting/IconArrow",51,42,27,42);
            CenterImage(back);
            back.rectTransform.localEulerAngles=new Vector3(0,0,180);
            Label(menu,header,"BackLabel","返回",95,29,126,66,43);
            Hotspot(header,"Back",25,22,225,88,"page",0);
            Picture(menu,header,"CoinPanel","TopUIBg",572,22,340,88).type=Image.Type.Sliced;
            Picture(menu,header,"CoinIcon","IconCoin",582,18,88,86);
            menu.coinTexts[4]=Label(menu,header,"LiveCoins","0",679,37,154,63,51,true);
            Picture(menu,header,"AddIcon","IconAdd",833,19,84,85);
            Hotspot(header,"AddCoins",833,19,84,85,"page",3);

            RectTransform room=Node(frame,"RoomDecorations",0,125,941,360);
            Picture(menu,room,"Window","Window",211,4,518,244);
            Picture(menu,room,"CurtainLeft","CurtainL",141,0,150,241);
            Picture(menu,room,"CurtainRight","CurtainR",616,0,151,241);
            Picture(menu,room,"Plant","Plant1",25,83,126,204);
            Picture(menu,room,"WallPicture","Picture",808,12,112,137);
            Picture(menu,room,"Cabinet","Desk",792,231,160,127);
            Picture(menu,room,"Books","Book",838,160,100,93);
            Picture(menu,frame,"SettingsPanel","Setting/UIBg",26,485,889,1072).type=Image.Type.Sliced;
            Picture(menu,frame,"TitleCloud","Setting/TitleBg",204,435,530,145).type=Image.Type.Sliced;
            Picture(menu,frame,"WhiteBear","BearWhite",197,257,255,217);
            Picture(menu,frame,"BrownBear","BearBrown",480,257,265,221);
            Picture(menu,frame,"Heart","IconLove",434,222,78,67);
            Picture(menu,frame,"TitleIcon","Setting/IconSetting",309,459,99,99);
            Label(menu,frame,"TitleLabel","设置",427,450,230,112,80);
            Image close=Picture(menu,frame,"CloseIcon","IconAdd",796,479,112,112);
            close.rectTransform.localEulerAngles=new Vector3(0,0,45);
            Hotspot(frame,"CloseSettings",796,479,112,112,"page",0);

            Picture(menu,frame,"AudioPanel","Setting/ChoicBg",76,605,788,187).type=Image.Type.Sliced;
            Picture(menu,frame,"SoundIcon","Setting/IconSound",110,624,85,70).preserveAspect=true;
            Picture(menu,frame,"MusicIcon","Setting/IconMusic",126,709,70,70).preserveAspect=true;
            Label(menu,frame,"SoundLabel","音效",215,624,117,70,43);
            Label(menu,frame,"MusicLabel","音乐",215,709,117,70,43);
            menu.soundSlider = Slider(menu, frame, "SoundVolume", 356, 623, 465, 65);
            menu.musicSlider = Slider(menu, frame, "MusicVolume", 356, 708, 465, 65);
            Picture(menu,frame,"PreferencesPanel","Setting/ChoicBg",76,812,788,181).type=Image.Type.Sliced;
            Picture(menu,frame,"HapticIcon","Setting/IconShake",120,825,79,73).preserveAspect=true;
            Picture(menu,frame,"NotificationIcon","Setting/IconAlarm",125,912,72,73).preserveAspect=true;
            Label(menu,frame,"HapticLabel","震动",252,825,154,73,43);
            Label(menu,frame,"NotificationLabel","通知",252,912,154,73,43);
            menu.toggleKnobs = new RectTransform[2];
            menu.toggleTracks = new BearMenuShape[2];
            menu.toggleTrackImages = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                float y = 832 + i * 85;
                RectTransform toggle = Node(frame, i == 0 ? "HapticToggle" : "NotificationToggle", 688, y, 131, 60);
                menu.toggleTracks[i] = Shape(toggle, "Track", 0, 0, 131, 60, 30, Gold, Gold, 4);
                menu.toggleTrackImages[i]=Picture(menu,toggle,"EnabledTrack","Setting/FillSlider",0,0,131,60);
                menu.toggleTrackImages[i].type=Image.Type.Sliced;
                RectTransform knob = Node(toggle, "Knob", 0, 0, 54, 54);
                knob.pivot = new Vector2(.5f,.5f);
                knob.anchoredPosition=new Vector2(97,-30);
                Shape(knob, "KnobArtwork", 0, 0, 54, 54, 27, new Color32(255,251,241,255), new Color32(255,242,220,255), 2);
                menu.toggleKnobs[i] = knob;
                Action(toggle, i == 0 ? "haptic" : "notifications");
            }
            string[] actions = { "language", "help", "about", "privacy" };
            string[] names={"语言","帮助","关于我们","隐私政策"};
            string[] icons={"Setting/IconInternet","Setting/IconAbout","Setting/IconCommunity","Setting/IconSafe"};
            for (int i = 0; i < actions.Length; i++)
            {
                RectTransform row=Node(frame,actions[i],76,1014+i*93,788,81);
                Picture(menu,row,"Panel","Setting/ChoicBg",0,0,788,81).type=Image.Type.Sliced;
                Picture(menu,row,"Icon",icons[i],57,8,70,65).preserveAspect=true;
                Text label=Label(menu,row,"Label",names[i],175,0,400,81,43);
                label.alignment=TextAnchor.MiddleLeft;
                Picture(menu,row,"Arrow","Setting/IconArrow",711,20,25,42);
                Action(row,actions[i]);
            }
            Picture(menu,frame,"SupportButton","Setting/BtnCommunity",276,1403,390,94);
            Hotspot(frame,"CustomerSupport",276,1403,390,94,"support");
            Picture(menu,frame,"BottomBow","Setting/Decoration1",364,1506,213,116);
        }

        public static void RebuildSettings(BearHomeMenuController menu)
        {
            RectTransform frame=menu.pages[4].GetComponent<RectTransform>();
            for(int i=frame.childCount-1;i>=0;i--) Object.DestroyImmediate(frame.GetChild(i).gameObject);
            Settings(menu,frame);
        }

        private static Slider Slider(BearHomeMenuController menu, Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform root = Node(parent, name, x, y, w, h);
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.canvasRenderer.cullTransparentMesh = false;
            Picture(menu,root,"Track","Setting/FillBgSlider",0,16,w,32).type=Image.Type.Sliced;
            RectTransform area = Node(root, "FillArea", 3, 19, w - 6, 26);
            RectTransform fill = Picture(menu,area,"Fill","Setting/FillSlider",0,0,w-6,26).rectTransform;
            fill.GetComponent<Image>().type=Image.Type.Sliced;
            Stretch(fill);
            RectTransform handleArea = Node(root, "HandleArea", 0, 0, w, h);
            RectTransform handle = Node(handleArea, "PawHandle", 0, 0, 70, 70);
            handle.pivot = new Vector2(.5f,.5f);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.up;
            handle.sizeDelta = new Vector2(70,0);
            handle.anchoredPosition = Vector2.zero;
            Image handleImage = Picture(menu,handle,"PawCoin","Setting/IconSlide",0,0,70,70);
            handleImage.preserveAspect=true;
            handleImage.rectTransform.pivot = new Vector2(.5f,.5f);
            handleImage.rectTransform.anchorMin = handleImage.rectTransform.anchorMax = new Vector2(.5f,.5f);
            handleImage.rectTransform.anchoredPosition = Vector2.zero;
            handleImage.raycastTarget = true;
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.targetGraphic = handleImage;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.SetValueWithoutNotify(name=="SoundVolume"?.575f:.415f);
            return slider;
        }

        private static void Modal(BearHomeMenuController menu)
        {
            RectTransform overlay = Node(menu.themeRoot, "MessageOverlay", 0, 0, 941, 1672);
            Stretch(overlay);
            Image dim = overlay.gameObject.AddComponent<Image>();
            dim.color = new Color(0.20f,.10f,.04f,.42f);
            menu.modal = overlay.gameObject;
            RectTransform frame = Node(overlay, "SafeDialog", 0, 0, 941, 1672);
            frame.gameObject.AddComponent<BearMenuLayout>().designSize = new Vector2(941,1672);
            RectTransform card = Node(frame, "PaperDialog", 80, 400, 781, 860);
            Shape(card, "Paper", 0, 0, 781, 860, 66, Cream, new Color32(255,231,192,255), 6);
            menu.modalTitle = Label(menu, card, "Title", "", 40, 40, 700, 80, 51);
            menu.modalPicture = Picture(menu, card, "Picture", "StoryImage0", 140, 140, 500, 300);
            menu.modalPicture.preserveAspect = true;
            menu.modalBody = Label(menu, card, "Body", "", 48, 454, 685, 220, 34);
            RectTransform close = Node(card, "Close", 220, 730, 340, 84);
            Shape(close, "GoldButton", 0, 0, 340, 84, 42, Gold, new Color32(255,211,49,255), 4);
            Label(menu, close, "Label", "知道啦", 0, 0, 340, 84, 42);
            Action(close, "close-modal");
            overlay.gameObject.SetActive(false);
        }

        private static void Bottom(RectTransform rect, float bottom)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0,0);
            rect.pivot = new Vector2(0,0);
            rect.anchoredPosition = new Vector2(0,bottom);
        }
        public static RectTransform Node(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y);
            rect.sizeDelta = new Vector2(width,height);
            return rect;
        }
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f,.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        public static void CenterImage(Image image)
        {
            RectTransform rect = image.rectTransform;
            Vector2 center = new Vector2(.5f, .5f);
            Vector2 position = rect.anchoredPosition + Vector2.Scale(center - rect.pivot, rect.rect.size);
            rect.pivot = center;
            rect.anchoredPosition = position;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
        private static Image Picture(BearHomeMenuController menu, Transform parent, string name, string sprite, float x, float y, float w, float h)
        {
            Image image = Node(parent,name,x,y,w,h).gameObject.AddComponent<Image>();
            image.sprite = menu.art.Get(sprite);
            image.raycastTarget = false;
            return image;
        }
        private static void Cut(BearHomeMenuController menu, Transform parent, string name, string sprite, string silhouette, float x, float y, float w, float h)
        {
            BearMenuCutout cut = Node(parent,name,x,y,w,h).gameObject.AddComponent<BearMenuCutout>();
            cut.sprite = menu.art.Get(sprite);
            cut.silhouette = silhouette;
            cut.raycastTarget = false;
        }
        private static BearMenuShape Shape(Transform parent, string name, float x, float y, float w, float h, float radius, Color top, Color bottom, float border = 4)
        {
            BearMenuShape shape = Node(parent,name,x,y,w,h).gameObject.AddComponent<BearMenuShape>();
            shape.radius = radius;
            shape.border = border;
            shape.color = top;
            shape.bottomColor = bottom;
            shape.raycastTarget = false;
            return shape;
        }
        private static Text Label(BearHomeMenuController menu, Transform parent, string name, string value, float x, float y, float w, float h, int size, bool numeric = false)
        {
            Text text = Node(parent,name,x,y,w,h).gameObject.AddComponent<Text>();
            text.font = numeric ? menu.art.numberFont : menu.art.chineseFont;
            text.fontSize = size;
            text.color = Brown;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            if (numeric)
            {
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }
        private static void Patch(BearHomeMenuController menu, Transform parent, float x, float y, float w, float h)
            => Picture(menu,parent,"CleanPaperPatch","CleanPaper",x,y,w,h);

        private static Button Hotspot(Transform parent, string name, float x, float y, float w, float h, string action, int argument = 0)
            => Action(Node(parent,name,x,y,w,h), action, argument);

        private static Button Action(RectTransform rect, string action, int argument = 0)
        {
            Image hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            hit.canvasRenderer.cullTransparentMesh = false;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            BearMenuAction command = rect.gameObject.AddComponent<BearMenuAction>();
            command.action = action;
            command.argument = argument;
            return button;
        }
    }
}
