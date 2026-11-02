using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    // Used by the editor baker and as a runtime fallback. Every control remains a normal uGUI object.
    public static class BearMenuFactory
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
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1;
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
                else
                {
                    Picture(menu, frame, "ReferenceArtwork", new[] { "", "DailyFrame", "CollectionFrame", "ShopFrame", "SettingsFrame" }[i], 0, 0, 941, 1672);
                    Hotspot(frame, "Back", 18, 15, 245, 108, "page", 0);
                    Hotspot(frame, "CoinPlus", 828, 17, 96, 105, "page", 3);
                    Picture(menu, frame, "CoinPaper", i == 2 ? "CollectionCoinPaper" : "SubCoinPaper", i == 2 ? 700 : 680, i == 2 ? 31 : 32, i == 2 ? 134 : 154, i == 2 ? 56 : 67);
                    menu.coinTexts[i] = Label(menu, frame, "LiveCoins", "0", i == 2 ? 700 : 679, i == 2 ? 26 : 37, i == 2 ? 134 : 154, 63, i == 2 ? 48 : 51, true);
                    if (i == 1) Daily(menu, frame);
                    if (i == 2) Collection(menu, frame);
                    if (i == 3) Shop(menu, frame);
                    if (i == 4) Settings(menu, frame);
                }
            }
            Modal(menu);
            for (int i = 0; i < menu.pages.Length; i++) menu.pages[i].SetActive(i == 0);
        }

        private static void Main(BearHomeMenuController menu, RectTransform frame)
        {
            RectTransform top = Picture(menu, frame, "TopStatus", "HomeTop", 0, 0, 1024, 130).rectTransform;
            Picture(menu,top,"HeaderCleanPaper","CleanPaper",450,120,130,10);
            RectTransform logo = Picture(menu, frame, "BrandTitle", "HomeLogo", 0, 130, 1024, 230).rectTransform;
            logo.anchorMin = logo.anchorMax = logo.pivot = new Vector2(.5f,.5f);
            logo.anchoredPosition = new Vector2(0,523);
            Picture(menu,logo,"HeartTopArtwork","HomeHeartCap",450,-10,130,10);
            RectTransform room = Picture(menu, frame, "BearsRoomAndCandy", "HomeRoom", 0, 360, 1024, 595).rectTransform;
            Bottom(room,581);
            RectTransform play = Picture(menu, frame, "StartGameArtwork", "HomePlay", 0, 955, 1024, 190).rectTransform;
            Bottom(play, 391);
            Hotspot(play, "StartGame", 190, 5, 650, 173, "play");
            RectTransform navigation = Picture(menu, frame, "NavigationArtwork", "HomeNavigation", 0, 1145, 1024, 255).rectTransform;
            Bottom(navigation, 136);
            string[] names = { "DailyReward", "Collection", "Shop", "Settings" };
            for (int i = 0; i < 4; i++) Hotspot(navigation, names[i], 38 + i * 244, 20, 220, 217, "page", i + 1);
            RectTransform grass = Picture(menu, frame, "FlowerGardenFooter", "HomeFooter", 0, 1400, 1024, 136).rectTransform;
            Bottom(grass, 0);
            Picture(menu, top, "LevelPaper", "LevelPaper", 153, 36, 153, 50);
            menu.levelText = Label(menu, top, "LiveLevel", "Lv. 1", 151, 31, 157, 61, 48, true);
            Picture(menu, top, "CoinPaper", "CoinPaper", 747, 42, 150, 57);
            menu.coinTexts[0] = Label(menu, top, "LiveCoins", "0", 746, 35, 149, 68, 54, true);
            Hotspot(top, "CoinPlus", 900, 18, 100, 105, "page", 3);
        }

        private static void Daily(BearHomeMenuController menu, Transform frame)
        {
            Hotspot(frame, "CloseDailyReward", 798, 477, 112, 113, "page", 0);
            menu.dailyCards = new BearMenuShape[7];
            menu.dailyStates = new Text[7];
            menu.dailyClaims = new Button[7];
            menu.dailyStateArt = new BearMenuCutout[7];
            menu.dailyFooterPapers = new GameObject[7];
            for (int i = 0; i < 7; i++)
            {
                bool last = i == 6;
                float[] xs = {79,349,612};
                RectTransform card = Node(frame, "Day" + (i + 1), last ? 77 : xs[i % 3], last ? 1283 : i < 3 ? 623 : 960, last ? 786 : 252, last ? 203 : i < 3 ? 312 : 302);
                // Fixed reward illustrations and lettering remain the original pixels.
                // Only state footers change with the save; avoid repainting the artwork.
                menu.dailyCards[i] = Shape(card, "AvailableOutline", 0, 0, last ? 786 : 252, last ? 203 : i < 3 ? 312 : 302, last ? 48 : 47, Color.clear, Color.clear, 0);
                RectTransform clip = Node(card,"FooterClip",4,4,last ? 778 : 244,last ? 195 : i < 3 ? 304 : 294);
                BearMenuShape clipGraphic = clip.gameObject.AddComponent<BearMenuShape>();
                clipGraphic.radius = last ? 44 : 43;
                clipGraphic.border = 0;
                clipGraphic.color = clipGraphic.bottomColor = Color.white;
                clipGraphic.raycastTarget = false;
                clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                RectTransform claim = Node(clip, "Claim", last ? 552 : 2, last ? 107 : i < 3 ? 221 : 216, last ? 229 : 240, last ? 68 : 80);
                menu.dailyFooterPapers[i] = Picture(menu,claim,"StatePaper",last ? "WideClaimPaper" : "DailyFooterPaper",0,0,last ? 229 : 240,last ? 68 : 80).gameObject;
                RectTransform background = Node(claim,"ClaimBackground",0,0,last ? 229 : 240,last ? 68 : 80);
                menu.dailyStateArt[i] = Node(background,"OriginalStateArtwork",0,0,last ? 229 : 240,last ? 68 : 80).gameObject.AddComponent<BearMenuCutout>();
                menu.dailyStateArt[i].sprite = menu.art.Get("DailyLockedFooter");
                menu.dailyStateArt[i].raycastTarget = false;
                menu.dailyStates[i] = Label(menu, claim, "ClaimState", "", 3, 0, last ? 223 : 234, last ? 68 : 80, 33, true);
                menu.dailyClaims[i] = Action(claim, "claim", i);
            }
        }

        private static void Collection(BearHomeMenuController menu, Transform frame)
        {
            menu.collectionTabs = new BearMenuShape[3];
            float[] tabX = { 120, 351, 594 };
            float[] tabW = { 225, 238, 229 };
            string[] tabs = { "表情", "故事", "动画" };
            for (int i = 0; i < 3; i++)
            {
                menu.collectionTabs[i] = Shape(frame, "Selected" + tabs[i], tabX[i], 331, tabW[i], 77, 38, Gold, new Color32(255,213,64,255), 4);
                Label(menu, menu.collectionTabs[i].transform, "TabText", tabs[i], 0, 0, tabW[i], 77, 41);
                if (i == 1) Picture(menu,menu.collectionTabs[i].transform,"OriginalStoryTab","CollectionSelectedTab",0,0,tabW[i],77);
                Hotspot(frame, "Tab" + i, tabX[i], 331, tabW[i], 77, "collection-tab", i);
            }
            menu.unlockedTravelArt = Picture(menu,frame,"UnlockedTravelArtwork","StoryImage4",95,1018,341,100).gameObject;
            menu.storyLocks = new GameObject[6];
            menu.storySelections = new GameObject[6];
            for (int i = 0; i < 6; i++)
            {
                float x = i % 2 == 0 ? 74 : 484;
                float y = i < 2 ? 451 : i < 4 ? 735 : 969;
                float h = i < 2 ? 260 : 208;
                RectTransform selection = Node(frame, "Chapter" + (i + 1) + "Selection", x - 7, y - 7, 395, h + 14);
                BearMenuShape outline = Shape(selection, "Outline", 0, 0, 395, h + 14, 46, new Color(1,1,1,0), new Color(1,1,1,0), 5);
                outline.borderColor = new Color32(255,203,56,255);
                menu.storySelections[i] = selection.gameObject;
                RectTransform locked = Node(frame, "Chapter" + (i + 1) + "Locked", x + 15, y + 50, 345, h - 66);
                Image tint = locked.gameObject.AddComponent<Image>();
                tint.color = new Color(.96f,.91f,.81f,.77f);
                Picture(menu, locked, "Lock", "LockIcon", 142, 19, 60, 72);
                menu.storyLocks[i] = locked.gameObject;
                Hotspot(frame, "Chapter" + (i + 1), x, y, 380, h, "story", i);
            }
            RectTransform details = Node(frame,"SelectedStoryDetails",0,0,941,1672);
            menu.storyDetailsOverlay = details.gameObject;
            Patch(menu, details, 531, 1235, 260, 62);
            menu.storyTitle = Label(menu, details, "SelectedStoryTitle", "奶茶约会", 532, 1230, 263, 69, 44);
            Patch(menu, details, 464, 1306, 390, 108);
            menu.storyBody = Label(menu, details, "SelectedStoryBody", "", 466, 1306, 385, 115, 27);
            menu.storyBody.alignment = TextAnchor.UpperLeft;
            menu.storyPreview = Picture(menu, details, "SelectedStoryPicture", "StoryPreview0", 92, 1218, 354, 321);
            Hotspot(frame, "ReadStory", 522, 1440, 256, 91, "read");
            RectTransform alternate = Node(frame, "OtherCollectionTabs", 65, 441, 810, 1120);
            menu.alternateCollection = alternate.gameObject;
            Shape(alternate, "EmptyCollectionPaper", 0, 0, 810, 1120, 45, Cream, new Color32(255,237,209,255), 4);
            Cut(menu, alternate, "Heart", "HeartIcon", "heart", 305, 195, 200, 180);
            menu.alternateTitle = Label(menu, alternate, "CollectionName", "", 80, 438, 650, 85, 53);
            menu.alternateBody = Label(menu, alternate, "CollectionDescription", "", 75, 550, 660, 185, 32);
        }

        private static void Shop(BearHomeMenuController menu, Transform frame)
        {
            menu.shopTabs = new BearMenuShape[5];
            string[] names = { "礼包", "金币", "爱心", "装饰券", "图鉴券" };
            for (int i = 0; i < 5; i++)
            {
                float x = 30 + i * 179;
                menu.shopTabs[i] = Shape(frame, "Selected" + names[i], x, 499, 173, 83, 42, Gold, new Color32(255,212,68,255), 4);
                Label(menu, menu.shopTabs[i].transform, "TabText", names[i], 0, 0, 173, 83, i < 3 ? 36 : 31);
                if (i == 0) Picture(menu,menu.shopTabs[i].transform,"OriginalGiftTab","ShopSelectedTab",0,0,173,83);
                Hotspot(frame, "ShopTab" + i, x, 499, 173, 83, "shop-tab", i);
            }
            menu.storePrices = new Text[5];
            float[] ys = { 796, 977, 1124, 1271, 1424 };
            for (int i = 0; i < 5; i++)
            {
                Hotspot(frame, "Purchase" + i, i == 0 ? 625 : 655, ys[i], i == 0 ? 235 : 218, 86, "buy", i);
                RectTransform price = Node(frame, "LocalizedPrice" + i, i == 0 ? 635 : 665, ys[i] + 9, i == 0 ? 210 : 190, 62);
                Shape(price, "PriceFill", 0, 0, i == 0 ? 210 : 190, 62, 28, Gold, Gold, 0);
                menu.storePrices[i] = Label(menu, price, "Price", "", 0, 0, i == 0 ? 210 : 190, 62, 43, true);
                price.gameObject.SetActive(false);
            }
            RectTransform filtered = Node(frame, "CategoryProducts", 42, 638, 855, 863);
            menu.filteredShop = filtered.gameObject;
            Shape(filtered, "CategoryPaper", 0, 0, 855, 863, 40, Cream, new Color32(255,239,217,255), 0);
            menu.filteredShopArt = Picture(menu, filtered, "ProductArtwork", "ShopRow1", 0, 55, 855, 136);
            RectTransform filteredPrice = Node(filtered,"LocalizedPrice",622,80,190,62);
            Shape(filteredPrice,"PriceFill",0,0,190,62,28,Gold,Gold,0);
            menu.filteredShopPrice = Label(menu,filteredPrice,"Price","",0,0,190,62,43,true);
            filteredPrice.gameObject.SetActive(false);
            Hotspot(filtered, "PurchaseFiltered", 607, 72, 218, 87, "buy", 1);
            Label(menu, filtered, "MoreProducts", "更多小熊好物，敬请期待", 70, 270, 715, 70, 35);
        }

        private static void Settings(BearHomeMenuController menu, Transform frame)
        {
            Hotspot(frame, "CloseSettings", 796, 479, 112, 112, "page", 0);
            menu.soundSlider = Slider(menu, frame, "SoundVolume", 356, 623, 465, 65);
            menu.musicSlider = Slider(menu, frame, "MusicVolume", 356, 708, 465, 65);
            menu.toggleKnobs = new RectTransform[2];
            menu.toggleTracks = new BearMenuShape[2];
            for (int i = 0; i < 2; i++)
            {
                float y = 832 + i * 85;
                RectTransform toggle = Node(frame, i == 0 ? "HapticToggle" : "NotificationToggle", 688, y, 131, 60);
                Picture(menu,toggle,"CleanToggleBackground","TogglePaper"+i,-6,-5,143,72);
                menu.toggleTracks[i] = Shape(toggle, "Track", 0, 0, 131, 60, 30, Gold, Gold, 4);
                RectTransform knob = Node(toggle, "Knob", 0, 0, 54, 54);
                knob.pivot = new Vector2(.5f,.5f);
                Shape(knob, "KnobArtwork", 0, 0, 54, 54, 27, new Color32(255,251,241,255), new Color32(255,242,220,255), 2);
                menu.toggleKnobs[i] = knob;
                Action(toggle, i == 0 ? "haptic" : "notifications");
            }
            string[] actions = { "language", "help", "about", "privacy" };
            for (int i = 0; i < actions.Length; i++) Hotspot(frame, actions[i], 75, 1014 + i * 93, 791, 81, actions[i]);
            Hotspot(frame, "CustomerSupport", 274, 1403, 393, 122, "support");
        }

        private static Slider Slider(BearHomeMenuController menu, Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform root = Node(parent, name, x, y, w, h);
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.canvasRenderer.cullTransparentMesh = false;
            Picture(menu, root, "CleanControlBackground", "SliderPaper", -8, -8, w + 16, 85);
            Shape(root, "Track", 0, 16, w, 32, 16, Cream, new Color32(255,238,207,255), 4);
            RectTransform area = Node(root, "FillArea", 4, 20, w - 8, 24);
            RectTransform fill = Node(area, "Fill", 0, 0, w - 8, 24);
            Shape(fill, "GoldFill", 0, 0, w - 8, 24, 12, Gold, new Color32(255,207,45,255), 0);
            // A nested stretch lets Slider resize the native fill without rebuilding its art.
            RectTransform fillArt = fill.GetChild(0) as RectTransform;
            Stretch(fillArt);
            Stretch(fill);
            RectTransform handleArea = Node(root, "HandleArea", 0, 0, w, h);
            RectTransform handle = Node(handleArea, "PawHandle", 0, 0, 70, 70);
            handle.pivot = new Vector2(.5f,.5f);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.up;
            handle.sizeDelta = new Vector2(70,0);
            handle.anchoredPosition = Vector2.zero;
            BearMenuCutout handleImage = Node(handle,"PawCoin",0,0,70,70).gameObject.AddComponent<BearMenuCutout>();
            handleImage.sprite = menu.art.Get("PawSlider");
            handleImage.silhouette = "circle";
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
