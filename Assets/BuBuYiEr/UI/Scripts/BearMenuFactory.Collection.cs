using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static partial class BearMenuFactory
    {
        public static void RebuildCollection(BearHomeMenuController menu)
        {
            RectTransform frame = menu.pages[2].GetComponent<RectTransform>();
            for (int i = frame.childCount - 1; i >= 0; i--)
            {
                GameObject child = frame.GetChild(i).gameObject;
                if (Application.isPlaying) { child.SetActive(false); Object.Destroy(child); }
                else Object.DestroyImmediate(child);
            }
            Collection(menu, frame);
            frame.GetComponent<BearMenuLayout>().CaptureDesign();
        }

        private static void Collection(BearHomeMenuController menu, Transform frame)
        {
            BearCollectionPanel panel = frame.GetComponent<BearCollectionPanel>();
            if (panel == null) panel = frame.gameObject.AddComponent<BearCollectionPanel>();
            panel.menu = menu;
            panel.layoutVersion = BearCollectionPanel.LayoutVersion;
            // Clear the screenshot-based collection bindings in older baked menus.
            menu.storyLocks = new GameObject[0];
            menu.storySelections = new GameObject[0];
            menu.collectionTabs = new BearMenuShape[0];
            menu.unlockedTravelArt = null;
            menu.storyDetailsOverlay = null;
            menu.alternateCollection = null;

            RectTransform footer = Node(frame, "FooterGarden", 0, 1538, 941, 134);
            Picture(menu, footer, "GrassGround", "GrassGround", -20, 0, 981, 144);
            Picture(menu, footer, "LeftFlowers", "BushL", -40, 0, 300, 140);
            Picture(menu, footer, "RightFlowers", "BushR", 681, 0, 300, 140);
            Picture(menu, frame, "RoomWindow", "Window", 211, 126, 518, 244);
            Picture(menu, frame, "CurtainLeft", "CurtainL", 141, 122, 150, 241);
            Picture(menu, frame, "CurtainRight", "CurtainR", 616, 122, 151, 241);
            Picture(menu, frame, "Plant", "Plant1", 14, 168, 118, 191);
            Picture(menu, frame, "WallPicture", "Picture", 813, 139, 111, 135);
            Picture(menu, frame, "Bookshelf", "Desk", 791, 276, 160, 127);
            Picture(menu, frame, "Books", "Book", 837, 205, 100, 93);
            Picture(menu, frame, "TitleCloud", "Setting/TitleBg", 196, 145, 550, 145).type = Image.Type.Sliced;
            Picture(menu, frame, "TitleHeart", "IconLove", 436, 106, 72, 62);
            Picture(menu, frame, "TitleBook", "IconHandBook", 233, 168, 100, 90).preserveAspect = true;
            Label(menu, frame, "Title", "图鉴收藏", 342, 157, 349, 112, 65);
            Picture(menu, frame, "CollectionLibrary", "Setting/UIBg", 26, 398, 889, 1166).type = Image.Type.Sliced;
            panel.tabArtwork = new Image[4];
            string[] tabNames = { "表情", "故事", "伙伴", "旅程" };
            string[] tabIcons = { "BearWhite", "IconHandBook", "Sugar1", "IconStar" };
            for (int i = 0; i < 4; i++)
            {
                RectTransform tab = Node(frame, "CollectionTab" + i, 53 + i * 212, 331, 200, 103);
                panel.tabArtwork[i] = Picture(menu, tab, "TabPaper", i == 1 ? "Shop/TabSelected" : "Shop/TabNormal", 0, 0, 200, i == 1 ? 103 : 82);
                panel.tabArtwork[i].type = Image.Type.Sliced;
                Picture(menu, tab, "TabIcon", tabIcons[i], 18, 18, 48, 48).preserveAspect = true;
                Label(menu, tab, "Label", tabNames[i], 68, 7, 114, 67, 37);
                Action(tab, "collection-tab", i);
            }
            panel.countText = Label(menu, frame, "CollectionProgress", "", 78, 437, 785, 43, 27);
            panel.tabRoots = new GameObject[4];
            RectTransform expressionRoot = Node(frame, "ExpressionShelf", 0, 0, 941, 1672);
            RectTransform storyRoot = Node(frame, "StoryShelf", 0, 0, 941, 1672);
            RectTransform ballRoot = Node(frame, "BallShelf", 0, 0, 941, 1672);
            RectTransform journeyRoot = Node(frame, "JourneyShelf", 0, 0, 941, 1672);
            panel.tabRoots[0] = expressionRoot.gameObject;
            panel.tabRoots[1] = storyRoot.gameObject;
            panel.tabRoots[2] = ballRoot.gameObject;
            panel.tabRoots[3] = journeyRoot.gameObject;
            BuildStoryShelf(menu, panel, storyRoot);
            BuildExpressionShelf(menu, panel, expressionRoot);
            BuildBallShelf(menu,panel,ballRoot);
            BuildJourneyShelf(menu,panel,journeyRoot);
            BuildCollectionDetails(menu, panel, frame);
            Picture(menu, frame, "BottomBow", "Setting/Decoration1", 364, 1513, 213, 116);
            panel.SelectTab(1);
        }

        private static RectTransform CollectionViewport(Transform parent, string name, int rows)
        {
            RectTransform viewport = Node(parent, name, 58, 494, 825, 653);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image drag = viewport.gameObject.AddComponent<Image>();
            drag.color = Color.clear;
            drag.canvasRenderer.cullTransparentMesh = false;
            RectTransform content = Node(viewport, "Cards", 0, 0, 825, rows * 320 + 8);
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 70;
            scroll.decelerationRate = .12f;
            return content;
        }

        private static void BuildStoryShelf(BearHomeMenuController menu, BearCollectionPanel panel, Transform root)
        {
            int count=BearCollectionPanel.StoryTitles.Length;
            RectTransform content = CollectionViewport(root, "StoryViewport", (count+1)/2);
            panel.storyArtwork = new Image[count];
            panel.storyStates = new Text[count];
            panel.storyLocks = new GameObject[count];
            panel.storyHighlights = new GameObject[count];
            string[] icons = { "IconFlower", "IconStar", "IconHeart", "IconFlower", "IconStar", "IconHeart", "IconStar", "IconMoon" };
            for (int i = 0; i < count; i++)
            {
                RectTransform card = Node(content, "Story" + i, i % 2 * 420 + 5, i / 2 * 320 + 5, 388, 300);
                BearMenuShape outline = Shape(card, "SelectedOutline", -4, -4, 396, 308, 43, Color.clear, Color.clear, 5);
                outline.borderColor = new Color32(255, 198, 57, 255);
                panel.storyHighlights[i] = outline.gameObject;
                Picture(menu, card, "CardPaper", "Collect/CardBg", 4, 4, 380, 292).type = Image.Type.Sliced;
                panel.storyArtwork[i] = Picture(menu, card, "StoryArtwork", "Collect/" + BearCollectionPanel.StoryPictures[i], 24, 56, 340, 188);
                CenterImage(panel.storyArtwork[i]);
                Picture(menu, card, "ChapterBadge", "Setting/ChoicBg", 91, 10, 207, 44).type = Image.Type.Sliced;
                panel.storyStates[i] = Label(menu, card, "ChapterLabel", "", 90, 10, 209, 44, 25);
                Picture(menu, card, "TitlePaper", "Setting/ChoicBg", 39, 248, 310, 41).type = Image.Type.Sliced;
                Label(menu, card, "StoryTitle", BearCollectionPanel.StoryTitles[i], 44, 246, 300, 45, 32);
                Picture(menu, card, "PaperTape", i % 2 == 0 ? "Collect/TapePink" : "Collect/TapeYellow", 0, -4, 79, 72).preserveAspect = true;
                Picture(menu, card, "CornerSticker", "Collect/" + icons[i%icons.Length], 337, 251, 47, 45).preserveAspect = true;
                RectTransform locked = Node(card, "LockedArtwork", 121, 94, 145, 126);
                Shape(locked, "LockPaper", 29, 18, 87, 90, 30, new Color(1, .96f, .88f, .91f), new Color(1, .94f, .86f, .91f), 0);
                Picture(menu, locked, "Lock", "Collect/IconLock", 45, 28, 55, 73).preserveAspect = true;
                panel.storyLocks[i] = locked.gameObject;
                Hotspot(card, "SelectStory", 0, 0, 388, 300, "story", i);
            }
        }

        private static void BuildExpressionShelf(BearHomeMenuController menu, BearCollectionPanel panel, Transform root)
        {
            RectTransform content = CollectionViewport(root, "ExpressionViewport", 2);
            panel.expressionArtwork = new Image[4];
            panel.expressionStates = new Text[4];
            panel.expressionLocks = new GameObject[4];
            panel.expressionHighlights = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                RectTransform card = Node(content, "Expression" + i, i % 2 * 420 + 5, i / 2 * 320 + 5, 388, 300);
                BearMenuShape outline = Shape(card, "SelectedOutline", -4, -4, 396, 308, 43, Color.clear, Color.clear, 5);
                outline.borderColor = new Color32(255, 198, 57, 255);
                panel.expressionHighlights[i] = outline.gameObject;
                Picture(menu, card, "CardPaper", "Collect/CardBg", 4, 4, 380, 292).type = Image.Type.Sliced;
                panel.expressionArtwork[i] = Picture(menu, card, "Expression", BearCollectionPanel.ExpressionPictures[i], 79, 37, 230, 187);
                CenterImage(panel.expressionArtwork[i]);
                Label(menu, card, "Title", BearCollectionPanel.ExpressionTitles[i], 24, 224, 340, 45, 31);
                panel.expressionStates[i] = Label(menu, card, "UnlockState", "", 44, 268, 300, 25, 21);
                panel.expressionLocks[i] = Picture(menu, card, "Lock", "Collect/IconLock", 163, 91, 62, 83).gameObject;
                Hotspot(card, "SelectExpression", 0, 0, 388, 300, "collection-expression", i);
            }
        }

        private static void BuildCollectionDetails(BearHomeMenuController menu, BearCollectionPanel panel, Transform frame)
        {
            RectTransform detail = Node(frame, "CollectionDetails", 61, 1171, 819, 343);
            panel.detailRoot = detail.gameObject;
            Picture(menu, detail, "AlbumPaper", "Setting/ChoicBg", 0, 0, 819, 343).type = Image.Type.Sliced;
            Shape(detail, "PhotoFrame", 19, 22, 276, 288, 26, Cream, new Color32(255, 236, 214, 255), 3);
            panel.detailImage = Picture(menu, detail, "SelectedPicture", "Collect/MilkTea", 31, 34, 252, 262);
            CenterImage(panel.detailImage);
            panel.detailBall=Shape(detail,"SelectedBall",65,78,184,184,92,Gold,Gold,5);
            panel.detailBall.enabled=false;
            var detailPortrait=Node(panel.detailBall.transform,"Material",0,0,184,184).gameObject.AddComponent<CandyBallPortrait>();
            detailPortrait.useEquippedSkin=true;detailPortrait.raycastTarget=false;
            panel.detailLock = Picture(menu, detail, "SelectedLock", "Collect/IconLock", 122, 122, 70, 95);
            CenterImage(panel.detailLock);
            Picture(menu, detail, "PhotoTape", "Collect/TapePink", 14, -6, 95, 86).preserveAspect = true;
            panel.detailTitle = Label(menu, detail, "SelectedTitle", "奶茶约会", 314, 20, 475, 57, 40);
            panel.detailBody = Label(menu, detail, "SelectedBody", "", 321, 91, 460, 136, 26);
            panel.detailBody.alignment = TextAnchor.UpperLeft;
            Picture(menu, detail, "ReadButtonPaper", "Shop/BuyButton", 322, 249, 258, 72).type = Image.Type.Sliced;
            panel.readLabel = Label(menu, detail, "ReadButtonLabel", "查看故事", 325, 249, 252, 72, 34);
            panel.readButton = Hotspot(detail, "ReadSelected", 322, 249, 258, 72, "read");
            Picture(menu, detail, "FavoritePaper", "TopUIBg", 600, 249, 194, 72).type = Image.Type.Sliced;
            panel.favoriteIcon = Picture(menu, detail, "FavoriteHeart", "Collect/IconHeart", 615, 269, 35, 31);
            panel.favoriteIcon.preserveAspect = true;
            panel.favoriteLabel = Label(menu, detail, "FavoriteLabel", "收藏", 655, 249, 127, 72, 30);
            panel.favoriteButton = Hotspot(detail, "FavoriteSelected", 600, 249, 194, 72, "collection-favorite");
        }
        private static void BuildBallShelf(BearHomeMenuController menu,BearCollectionPanel panel,Transform root)
        {
            RectTransform content=CollectionViewport(root,"BallViewport",3);
            panel.ballArtwork=new BearMenuShape[6];panel.ballStates=new Text[6];
            panel.ballLocks=new GameObject[6];panel.ballHighlights=new GameObject[6];
            for(int i=0;i<6;i++)
            {
                RectTransform card=Node(content,"Companion"+i,5+i%2*420,5+i/2*320,388,300);
                var outline=Shape(card,"Selected",-4,-4,396,308,43,Color.clear,Color.clear,5);
                outline.borderColor=Gold;panel.ballHighlights[i]=outline.gameObject;
                Picture(menu,card,"Paper","Collect/CardBg",4,4,380,292).type=Image.Type.Sliced;
                Color tint=CandyBreakoutGraphic.CandyColors[i];
                panel.ballArtwork[i]=Shape(card,"Ball",140,35,108,108,54,tint,tint,4);
                panel.ballArtwork[i].enabled=false;
                var portrait=Node(panel.ballArtwork[i].transform,"Material",0,0,108,108).gameObject.AddComponent<CandyBallPortrait>();
                portrait.kind=i;portrait.useEquippedSkin=true;portrait.raycastTarget=false;
                Label(menu,card,"Name",CandyBreakoutSimulation.Names[i],24,153,340,46,36);
                Label(menu,card,"Ability",CandyBreakoutSimulation.Descriptions[i],24,202,340,36,28);
                panel.ballStates[i]=Label(menu,card,"State","",18,249,352,37,25);
                panel.ballLocks[i]=Picture(menu,card,"Lock","Collect/IconLock",169,70,50,66).gameObject;
                Hotspot(card,"SelectCompanion",0,0,388,300,"collection-ball",i);
            }
        }
        private static void BuildJourneyShelf(BearHomeMenuController menu,BearCollectionPanel panel,Transform root)
        {
            RectTransform content=CollectionViewport(root,"JourneyViewport",1);
            content.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(825,1000);
            content.sizeDelta=new Vector2(825,CandyProgression.JourneyCount*207);
            panel.journeyClaims=new Button[CandyProgression.JourneyCount];panel.journeyStates=new Text[CandyProgression.JourneyCount];
            for(int i=0;i<CandyProgression.JourneyCount;i++)
            {
                RectTransform row=Node(content,"Milestone"+i,5,i*207,805,192);
                Picture(menu,row,"Paper","Shop/RowBg",0,0,805,192).type=Image.Type.Sliced;
                Picture(menu,row,"Badge",i<8?"IconLove":"IconStar",26,35,76,74).preserveAspect=true;
                Label(menu,row,"Title",CandyProgression.JourneyTitle(i),123,22,650,48,33).alignment=TextAnchor.MiddleLeft;
                panel.journeyStates[i]=Label(menu,row,"Progress","",123,84,410,70,27);
                panel.journeyStates[i].alignment=TextAnchor.MiddleLeft;
                var button=Node(row,"Claim",550,89,228,78);
                Picture(menu,button,"Paper","Shop/BuyButton",0,0,228,78).type=Image.Type.Sliced;
                Label(menu,button,"Label","",8,0,212,78,31);
                panel.journeyClaims[i]=Action(button,"journey-claim",i);
            }
        }
    }
}
