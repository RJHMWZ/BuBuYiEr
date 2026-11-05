using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static class BearGameTheme
    {
        private static Material candyMaterial;
        private static Material pixelMaterial;

        public static void ApplyPixelMaterial(MeshRenderer renderer)
        {
            if (pixelMaterial == null)
            {
                pixelMaterial = Resources.Load<Material>("BearCandyPixels");
                if (pixelMaterial == null)
                {
                    Shader shader = Resources.Load<Shader>("BearCandyPixels");
                    if (shader == null) { ApplyCandyMaterial(renderer); return; }
                    pixelMaterial = new Material(shader) { name = "BearCandyPixels", enableInstancing = true };
                    pixelMaterial.SetColor("_Ink", Brown);
                }
            }
            renderer.sharedMaterial = pixelMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        public static void ApplyCandyMaterial(MeshRenderer renderer)
        {
            if (candyMaterial == null)
            {
                Shader shader = Resources.Load<Shader>("BearCandy");
                if (shader == null) return;
                candyMaterial = new Material(shader) { name = "BearCandyInk" };
                candyMaterial.SetColor("_Ink", Brown);
            }
            renderer.sharedMaterial = candyMaterial;
        }
        public static readonly Color Cream = new Color32(255, 247, 232, 255);
        public static readonly Color Brown = new Color32(82, 42, 10, 255);
        public static readonly Color Pink = new Color32(255, 214, 211, 255);
        public static readonly Color Gold = new Color32(255, 229, 93, 255);

        public static RectTransform Frame(Transform parent, string name, bool flexible = false)
        {
            RectTransform frame = BearMenuFactory.Node(parent, name, 0, 0, 1024, 1536);
            BearMenuLayout layout = frame.gameObject.AddComponent<BearMenuLayout>();
            layout.designSize = new Vector2(1024, 1536);
            layout.flexibleMain = flexible;
            layout.Refresh();
            return frame;
        }

        public static Image Picture(BearMenuArt art, Transform parent, string name, string sprite,
            float x, float y, float width, float height, bool sliced = false)
        {
            Image image = BearMenuFactory.Node(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = art != null ? art.Get(sprite) : null;
            image.raycastTarget = false;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        public static BearMenuShape Paper(Transform parent, string name, float x, float y,
            float width, float height, Color color, float radius = 40)
        {
            BearMenuShape shape = BearMenuFactory.Node(parent, name, x, y, width, height).gameObject.AddComponent<BearMenuShape>();
            shape.color = color;
            shape.bottomColor = Color.Lerp(color, new Color32(255, 230, 188, 255), .3f);
            shape.borderColor = Brown;
            shape.radius = radius;
            shape.border = 5;
            shape.raycastTarget = false;
            return shape;
        }

        public static Text Label(BearMenuArt art, Transform parent, string name, string value,
            float x, float y, float width, float height, int size, bool numeric = false)
        {
            Text text = BearMenuFactory.Node(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            StyleText(text, art, size, numeric);
            text.text = value;
            return text;
        }

        public static void StyleText(Text text, BearMenuArt art, int size, bool numeric = false)
        {
            if (art != null) text.font = numeric ? art.numberFont : art.chineseFont;
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Brown;
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            foreach (Shadow shadow in text.GetComponents<Shadow>()) shadow.enabled = false;
        }

        public static Button Button(BearMenuArt art, Transform parent, string name, string caption,
            float x, float y, float width, float height, Color color, UnityAction action)
        {
            BearMenuShape shape = Paper(parent, name, x, y, width, height, color, height * .45f);
            shape.raycastTarget = true;
            Button button = shape.gameObject.AddComponent<Button>();
            button.targetGraphic = shape;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1, .96f, .90f);
            colors.pressedColor = new Color(.90f, .81f, .71f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Label(art, shape.transform, "Label", caption, 10, 0, width - 20, height, 38);
            return button;
        }

        public static void Place(RectTransform rect, Transform parent, float x, float y, float width, float height)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        public static void Bottom(RectTransform rect, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
        }

        public static void Garden(BearMenuArt art, Transform frame)
        {
            RectTransform garden = BearMenuFactory.Node(frame, "FooterGarden", 0, 0, 1024, 115);
            Bottom(garden, 0, 0);
            Picture(art, garden, "Grass", "GrassGround", -12, 0, 1048, 130);
            Picture(art, garden, "FlowersLeft", "BushL", -45, -6, 280, 130);
            Picture(art, garden, "FlowersRight", "BushR", 789, -6, 280, 130);
        }

        public static void Bears(BearMenuArt art, Transform parent, float x, float y, float scale)
        {
            Picture(art, parent, "WhiteBear", "BearWhite", x, y, 270 * scale, 230 * scale);
            Picture(art, parent, "BrownBear", "BearBrown", x + 280 * scale, y, 270 * scale, 230 * scale);
            Picture(art, parent, "BearHeart", "IconLove", x + 242 * scale, y - 40 * scale, 70 * scale, 61 * scale);
        }

        public static void BuildLoading(LoadingViewController view, BearMenuArt art, out Slider slider, out Text percent)
        {
            foreach (Transform child in view.transform) child.gameObject.SetActive(false);
            RectTransform root = BearMenuFactory.Node(view.transform, "BearLoadingUI", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(root);
            Image backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = Cream;
            RectTransform frame = Frame(root, "LoadingRoom", true);
            Garden(art, frame);
            Picture(art, frame, "BrandCloud", "TitleBg", 182, 165, 660, 230);
            Picture(art, frame, "Brand", "TitleText", 247, 218, 530, 139);
            Picture(art, frame, "BrandHeart", "IconLove", 468, 130, 88, 76);
            Picture(art, frame, "Window", "Window", 220, 490, 584, 276);
            Picture(art, frame, "CurtainLeft", "CurtainL", 156, 480, 170, 275);
            Picture(art, frame, "CurtainRight", "CurtainR", 699, 480, 170, 275);
            Picture(art, frame, "Plant", "Plant1", 8, 650, 150, 244);
            Picture(art, frame, "Table", "Table", 40, 859, 944, 210, true);
            Bears(art, frame, 226, 660, 1);
            Picture(art, frame, "CandyJar", "SugarPile", 436, 823, 153, 186);
            Label(art, frame, "LoadingTitle", "布布一二正在准备糖果…", 130, 1082, 764, 74, 43);
            RectTransform track = Paper(frame, "CandyProgress", 168, 1180, 688, 64, Cream, 32).rectTransform;
            RectTransform fillArea = BearMenuFactory.Node(track, "FillArea", 9, 9, 670, 46);
            fillArea.gameObject.AddComponent<RectMask2D>();
            RectTransform fill = Paper(fillArea, "HoneyFill", 0, 0, 670, 46, Gold, 23).rectTransform;
            // Slider drives the anchors; a fixed sizeDelta would be added to the filled width.
            BearMenuFactory.Stretch(fill);
            fill.GetComponent<BearMenuShape>().border = 0;
            slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(0);
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            percent = Label(art, frame, "LoadingPercent", "0%", 362, 1260, 300, 62, 40, true);
            Label(art, frame, "LoadingHint", "把同色糖果装进罐子，收集甜蜜时光", 100, 1345, 824, 54, 29);
        }

        public static void BuildIngame(IngameViewController view, BearMenuArt art, RectTransform oldTop,
            Text level, Text coins, RectTransform coinTarget, StarPanelController stars,
            BoostButtonController[] boosts, MessagePanelController message)
        {
            RectTransform root = BearMenuFactory.Node(view.transform, "BearIngameUI", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(root);
            root.SetAsFirstSibling();
            RectTransform frame = Frame(root, "GameFrame", true);
            Garden(art, frame);
            Picture(art, frame, "LevelPaper", "TopUIBg", 30, 26, 290, 96, true);
            Picture(art, frame, "LevelStar", "IconStar", 42, 30, 79, 75);
            Place(level.rectTransform, frame, 119, 32, 183, 70);
            StyleText(level, art, 42);
            Picture(art, frame, "CoinPaper", "TopUIBg", 562, 26, 306, 96, true);
            Picture(art, frame, "Coin", "IconCoin", 572, 26, 91, 89);
            Place(coins.rectTransform, frame, 660, 33, 192, 70);
            StyleText(coins, art, 44, true);
            Place(coinTarget, frame, 572, 26, 91, 89);
            Place((RectTransform)stars.transform, frame, 346, 25, 190, 140);
            stars.ApplyBearTheme(art);
            oldTop.gameObject.SetActive(false);

            Transform oldBottom = boosts[0].transform.parent;
            RectTransform tray = Paper(frame, "CandyToolbox", 32, 0, 960, 213, Cream, 45).rectTransform;
            Bottom(tray, 32, 84);
            string[] names = { "加一格", "磁铁", "小木锤", "钥匙", "降降温" };
            for (int i = 0; i < boosts.Length; i++)
            {
                Place((RectTransform)boosts[i].transform, tray, 27 + i * 185, 17, 164, 145);
                boosts[i].ApplyBearTheme(art);
                Label(art, tray, "ToolName" + i, names[i], 22 + i * 185, 167, 174, 40, 28);
            }
            Place((RectTransform)message.transform, frame, 0, 0, 1024, 185);
            Bottom((RectTransform)message.transform, 0, 316);
            oldBottom.gameObject.SetActive(false);
            SkinPanels(view.transform, art);
        }

        public static void LayoutAvailableTools(Transform view, BoostButtonController[] tools)
        {
            Transform tray = view.Find("BearIngameUI/GameFrame/CandyToolbox");
            if (tray == null) return;
            int count = 0;
            foreach (BoostButtonController tool in tools) if (tool.IsUnlocked) count++;
            float start = (960 - (count * 164 + Mathf.Max(0, count - 1) * 21)) * .5f;
            int visible = 0;
            for (int i = 0; i < tools.Length; i++)
            {
                bool available = tools[i].IsUnlocked;
                tools[i].gameObject.SetActive(available);
                Transform label = tray.Find("ToolName" + i);
                if (label != null) label.gameObject.SetActive(available);
                if (!available) continue;
                float x = start + visible++ * 185;
                Place((RectTransform)tools[i].transform, tray, x, 17, 164, 145);
                if (label != null) Place((RectTransform)label, tray, x - 5, 167, 174, 40);
            }
        }

        private static void SkinPanels(Transform root, BearMenuArt art)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                int size = text.fontSize;
                StyleText(text, art, size, text.font == art.numberFont);
                switch (text.text.Trim())
                {
                    case "Tap On Block To Collect !": text.text = "点击同色罐子收集糖果"; break;
                    case "Tap On Block To Smash !": text.text = "点击罐子，使用小木锤"; break;
                    case "Tap On Locked Block To Unlock !": text.text = "点击上锁的罐子解锁"; break;
                    case "Tap On Burning Block To Cool Off !": text.text = "点击发烫的罐子降温"; break;
                    case "Purchase": case "PURCHASE": text.text = "购买"; break;
                    case "Confirm": case "CONFIRM": text.text = "确定"; break;
                }
            }
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.transform.IsChildOf(root.Find("BearIngameUI"))) continue;
                string name = image.gameObject.name;
                string sprite = null;
                if (name == "Background" && image.transform.parent.name == "MainPanel") sprite = "Setting/UIBg";
                else if (name == "TopPanel" && image.transform.parent.name == "MainPanel") sprite = "Setting/TitleBg";
                else if (name == "PurchaseButton" || name == "ComfirmButton") sprite = "Setting/FillSlider";
                else if (name == "CloseButton") sprite = "IconAdd";
                if (sprite == null) continue;
                image.sprite = art.Get(sprite);
                image.color = Color.white;
                image.type = Image.Type.Sliced;
                if (name == "CloseButton") image.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
                foreach (Shadow shadow in image.GetComponents<Shadow>()) shadow.enabled = false;
            }
        }

        public static void ApplyWorld(IngameManager manager, BearMenuArt art)
        {
            if (art == null) return;
            foreach (Renderer renderer in manager.gameObject.scene.GetRootGameObjectsRenderers())
            {
                Material material = renderer.sharedMaterial;
                if (material == null) continue;
                if (material == manager.BackgroundMaterial)
                {
                    MaterialPropertyBlock background = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(background);
                    background.SetColor("_TopColor", Cream);
                    background.SetColor("_BottomColor", Pink);
                    renderer.SetPropertyBlock(background);
                    continue;
                }
                Color tint;
                switch (material.name)
                {
                    case "Border": tint = new Color32(149, 101, 64, 255); break;
                    case "Cell": tint = new Color32(255, 223, 209, 255); break;
                    case "Conveyors": tint = new Color32(255, 236, 193, 255); break;
                    case "Pole": tint = new Color32(183, 131, 89, 255); break;
                    default: continue;
                }
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", tint);
                if (renderer is MeshRenderer mesh) ApplyCandyMaterial(mesh);
                renderer.SetPropertyBlock(block);
            }
            foreach (Text text in manager.ConveyorManager.GetComponentsInChildren<Text>(true))
                StyleText(text, art, text.fontSize, true);
        }

        private static System.Collections.Generic.IEnumerable<Renderer> GetRootGameObjectsRenderers(this UnityEngine.SceneManagement.Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) yield return renderer;
        }
    }
}
