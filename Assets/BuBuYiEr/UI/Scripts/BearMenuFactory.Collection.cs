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
            Picture(menu, frame, "BackPaper", "TopUIBg", 25, 22, 225, 88).type = Image.Type.Sliced;
            Image back = Picture(menu, frame, "BackArrow", "Setting/IconArrow", 51, 42, 27, 42);
            back.rectTransform.localEulerAngles = new Vector3(0, 0, 180);
            Label(menu, frame, "BackLabel", "返回", 95, 29, 126, 66, 43);
            Hotspot(frame, "Back", 25, 22, 225, 88, "page", 0);
            Picture(menu, frame, "CoinsPaper", "TopUIBg", 572, 22, 340, 88).type = Image.Type.Sliced;
            Picture(menu, frame, "PawCoin", "IconCoin", 582, 18, 88, 86);
            menu.coinTexts[2] = Label(menu, frame, "LiveCoins", "0", 679, 37, 154, 63, 51, true);
            Picture(menu, frame, "AddIcon", "IconAdd", 833, 19, 84, 85);
            Hotspot(frame, "AddCoins", 833, 19, 84, 85, "page", 3);

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
            panel.tabArtwork = new Image[3];
            string[] tabNames = { "表情", "故事", "动画" };
            string[] tabIcons = { "BearWhite", "IconHandBook", "IconLove" };
            for (int i = 0; i < 3; i++)
            {
                RectTransform tab = Node(frame, "CollectionTab" + i, 66 + i * 276, 331, 258, 103);
                panel.tabArtwork[i] = Picture(menu, tab, "TabPaper", i == 1 ? "Shop/TabSelected" : "Shop/TabNormal", 0, 0, 258, i == 1 ? 103 : 82);
                panel.tabArtwork[i].type = Image.Type.Sliced;
                Picture(menu, tab, "TabIcon", tabIcons[i], 27, 15, 58, 54).preserveAspect = true;
                Label(menu, tab, "Label", tabNames[i], 94, 5, 137, 67, 43);
                Action(tab, "collection-tab", i);
            }
            panel.countText = Label(menu, frame, "CollectionProgress", "", 78, 437, 785, 43, 27);
            panel.tabRoots = new GameObject[3];
            RectTransform expressionRoot = Node(frame, "ExpressionShelf", 0, 0, 941, 1672);
            RectTransform storyRoot = Node(frame, "StoryShelf", 0, 0, 941, 1672);
            RectTransform animationRoot = Node(frame, "AnimationShelf", 0, 0, 941, 1672);
            panel.tabRoots[0] = expressionRoot.gameObject;
            panel.tabRoots[1] = storyRoot.gameObject;
            panel.tabRoots[2] = animationRoot.gameObject;
            BuildStoryShelf(menu, panel, storyRoot);
            BuildExpressionShelf(menu, panel, expressionRoot);
            Picture(menu, animationRoot, "BookOfMemories", "IconHandBook", 330, 645, 281, 265).preserveAspect = true;
            Picture(menu, animationRoot, "TinyMoon", "Collect/IconMoon", 619, 606, 79, 83).preserveAspect = true;
            Label(menu, animationRoot, "EmptyTitle", "还没有动态回忆", 145, 957, 651, 73, 44);
            Label(menu, animationRoot, "EmptyHint", "先去看看已收集的故事和表情吧", 116, 1057, 709, 85, 30);
            Hotspot(animationRoot, "BrowseStories", 287, 1211, 367, 98, "collection-tab", 1);
            Picture(menu, animationRoot, "BrowseButton", "Shop/BuyButton", 287, 1211, 367, 98).type = Image.Type.Sliced;
            Label(menu, animationRoot, "BrowseLabel", "去看故事", 297, 1211, 347, 98, 39);
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
            RectTransform content = CollectionViewport(root, "StoryViewport", 4);
            panel.storyArtwork = new Image[8];
            panel.storyStates = new Text[8];
            panel.storyLocks = new GameObject[8];
            panel.storyHighlights = new GameObject[8];
            string[] icons = { "IconFlower", "IconStar", "IconHeart", "IconFlower", "IconStar", "IconHeart", "IconStar", "IconMoon" };
            for (int i = 0; i < 8; i++)
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
                Picture(menu, card, "CornerSticker", "Collect/" + icons[i], 337, 251, 47, 45).preserveAspect = true;
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
    }
}
