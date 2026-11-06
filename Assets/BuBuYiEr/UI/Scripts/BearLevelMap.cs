using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearLevelMap : MonoBehaviour
    {
        private BearMenuArt art;
        private LevelViewController owner;
        private ScrollRect scroll;
        private RectTransform content;
        private GameObject confirmation;
        private Text confirmTitle;
        private Text coins;
        private Image[] confirmStars;
        private readonly List<Button> buttons = new List<Button>();
        private readonly List<GameObject> currentMarkers = new List<GameObject>();
        private readonly List<GameObject> locks = new List<GameObject>();
        private readonly List<Image[]> stars = new List<Image[]>();
        private readonly List<Vector2> points = new List<Vector2>();
        private bool leaving;
        public bool IsBuilt => confirmation != null && scroll != null && content != null && coins != null
            && buttons.Count > 0 && buttons.Count == points.Count && locks.Count == buttons.Count
            && currentMarkers.Count == buttons.Count && stars.Count == buttons.Count;

        private void OnEnable()
        {
            // Include inactive locks left behind by the previous version.
            foreach (MaskableGraphic graphic in GetComponentsInChildren<MaskableGraphic>(true))
                if (graphic.GetComponent<CanvasRenderer>() == null) graphic.gameObject.AddComponent<CanvasRenderer>();
        }

        public void Build(LevelViewController view, BearMenuArt theme, int totalLevels)
        {
            owner = view;
            art = theme;
            // A failed construction can leave this persistent view with only part of a map.
            for (int i = view.transform.childCount - 1; i >= 0; i--)
            {
                Transform previous = view.transform.GetChild(i);
                if (previous.name != "BearLevelMap") continue;
                foreach (MaskableGraphic graphic in previous.GetComponentsInChildren<MaskableGraphic>(true))
                    if (graphic.GetComponent<CanvasRenderer>() == null) graphic.gameObject.AddComponent<CanvasRenderer>();
                previous.gameObject.SetActive(false);
                Destroy(previous.gameObject);
            }
            buttons.Clear();
            currentMarkers.Clear();
            locks.Clear();
            stars.Clear();
            points.Clear();
            confirmation = null;
            totalLevels = Mathf.Max(1, totalLevels);
            foreach (Transform child in view.transform) child.gameObject.SetActive(false);
            RectTransform root = BearMenuFactory.Node(view.transform, "BearLevelMap", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(root);
            root.gameObject.AddComponent<Image>().color = BearGameTheme.Cream;
            RectTransform frame = BearGameTheme.Frame(root, "MapSafeArea", true);
            BearGameTheme.Garden(art, frame);
            BearGameTheme.Button(art, frame, "BackHome", "‹ 返回", 30, 26, 226, 90,
                BearGameTheme.Cream, () => { if (!leaving) { leaving = true; owner.OnClickHomeButton(); } });
            BearGameTheme.Picture(art, frame, "CoinsPaper", "TopUIBg", 660, 26, 334, 90, true);
            BearGameTheme.Picture(art, frame, "PawCoin", "IconCoin", 670, 26, 86, 85);
            coins = BearGameTheme.Label(art, frame, "CoinCount", "", 756, 29, 218, 76, 43, true);
            BearGameTheme.Picture(art, frame, "PageCloud", "Setting/TitleBg", 230, 134, 564, 112, true);
            BearGameTheme.Label(art, frame, "PageTitle", "糖果旅行", 310, 137, 404, 91, 50);
            BearGameTheme.Picture(art, frame, "PageHeart", "IconLove", 258, 151, 63, 55);

            RectTransform viewport = BearMenuFactory.Node(frame, "RouteViewport", 0, 0, 1024, 1100);
            BearMenuFactory.Stretch(viewport);
            viewport.offsetMin = new Vector2(0, 188);
            viewport.offsetMax = new Vector2(0, -250);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image dragSurface = viewport.gameObject.AddComponent<Image>();
            dragSurface.color = Color.clear;
            dragSurface.canvasRenderer.cullTransparentMesh = false;
            float contentHeight = 730 + Mathf.Max(0, totalLevels - 1) * 220;
            content = BearMenuFactory.Node(viewport, "CandyTrail", 0, 0, 1024, contentHeight);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = new Vector2(0, contentHeight);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 90;
            scroll.decelerationRate = .12f;

            BearGameTheme.Picture(art, content, "RoomWindow", "Window", 252, 26, 520, 245);
            BearGameTheme.Picture(art, content, "LeftCurtain", "CurtainL", 193, 20, 150, 241);
            BearGameTheme.Picture(art, content, "RightCurtain", "CurtainR", 683, 20, 150, 241);
            BearGameTheme.Picture(art, content, "PicnicTable", "Table", 170, 276, 684, 153, true);
            BearGameTheme.Bears(art, content, 314, 151, .72f);
            BearGameTheme.Picture(art, content, "CandyJar", "SugarPile", 464, 249, 96, 117);
            BearGameTheme.Label(art, content, "RouteInvitation", "和布布一二一起，收集每一份甜蜜", 150, 404, 724, 45, 29);

            for (int i = 0; i < totalLevels; i++)
                points.Add(new Vector2(512 + Mathf.Sin(i * .92f) * 226, -(535 + i * 220)));
            RectTransform trailRect = BearMenuFactory.Node(content, "HoneyDottedTrail", 0, 0, 1024, contentHeight);
            trailRect.gameObject.AddComponent<CanvasRenderer>();
            BearRouteGraphic trail = trailRect.gameObject.AddComponent<BearRouteGraphic>();
            trail.points = points.ToArray();
            trail.raycastTarget = false;

            for (int i = 0; i < totalLevels; i++) BuildNode(i);
            RectTransform currentButton = BearGameTheme.Button(art, frame, "FindCurrentLevel", "回到当前关卡", 285, 0, 454, 92,
                BearGameTheme.Gold, SnapToCurrent).GetComponent<RectTransform>();
            BearGameTheme.Bottom(currentButton, 285, 88);
            BuildConfirmation(root);
            Refresh();
        }

        private void BuildNode(int index)
        {
            int level = index + 1;
            Vector2 position = points[index];
            RectTransform node = BearMenuFactory.Node(content, "Level" + level, position.x - 76, -position.y - 60, 152, 180);
            BearGameTheme.Paper(node, "LeftBearEar", 7, -12, 45, 45, BearGameTheme.Cream, 22);
            BearGameTheme.Paper(node, "RightBearEar", 100, -12, 45, 45, BearGameTheme.Cream, 22);
            Button button = BearGameTheme.Button(art, node, "LevelButton", level.ToString(), 0, 0, 152, 128,
                BearGameTheme.Cream, () => owner.ShowConfirmPanel(level));
            Text number = button.GetComponentInChildren<Text>();
            BearGameTheme.StyleText(number, art, 49, true);
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(.85f, .80f, .71f);
            button.colors = colors;
            buttons.Add(button);
            locks.Add(BearIllustration.Create(node, "LockedLevel", BearDrawing.Lock, 112, 87, 40, 40).gameObject);
            Image[] row = new Image[3];
            for (int j = 0; j < 3; j++) row[j] = BearGameTheme.Picture(art, node, "Star" + j, "IconStar", 16 + j * 41, 137, 38, 36);
            stars.Add(row);
            GameObject marker = BearGameTheme.Picture(art, node, "CurrentHeart", "IconLove", 43, -71, 66, 57).gameObject;
            currentMarkers.Add(marker);
            if (index % 5 == 2)
            {
                bool left = position.x > 512;
                float x = left ? 62 : 762;
                float y = -position.y - 97;
                BearGameTheme.Picture(art, content, "FlowerPatch" + level, left ? "BushL" : "BushR", x - 26, y + 85, 220, 103);
                BearGameTheme.Picture(art, content, "BearFriend" + level, index % 2 == 0 ? "BearWhite" : "BearBrown", x, y, 170, 145);
                BearGameTheme.Picture(art, content, "LittleCandy" + level, "Sugar" + (index % 3 + 1), x + 118, y + 107, 54, 49).preserveAspect = true;
            }
        }

        private void BuildConfirmation(Transform root)
        {
            RectTransform dim = BearMenuFactory.Node(root, "ChooseLevelOverlay", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(.23f, .12f, .05f, .58f);
            confirmation = dim.gameObject;
            RectTransform safe = BearGameTheme.Frame(dim, "ChooseLevelSafeArea");
            BearGameTheme.Bears(art, safe, 272, 260, .88f);
            RectTransform card = BearGameTheme.Paper(safe, "LevelInvitation", 128, 452, 768, 697, BearGameTheme.Cream, 64).rectTransform;
            BearGameTheme.Picture(art, card, "TitleRibbon", "Setting/TitleBg", 115, -40, 538, 138, true);
            confirmTitle = BearGameTheme.Label(art, card, "LevelTitle", "", 135, -17, 498, 88, 54);
            BearGameTheme.Label(art, card, "Invitation", "一起打碎糖砖，收集甜蜜吧", 74, 139, 620, 66, 35);
            confirmStars = new Image[3];
            for (int i = 0; i < 3; i++)
                confirmStars[i] = BearGameTheme.Picture(art, card, "BestStar" + i, "IconStar", 222 + i * 115, 244, 94, 90);
            BearGameTheme.Button(art, card, "StartLevel", "开始这一关", 100, 395, 568, 108, BearGameTheme.Gold,
                () => { if (!leaving) { leaving = true; owner.OnClickStartButton(); } });
            BearGameTheme.Button(art, card, "CloseLevel", "再逛逛", 204, 539, 360, 86, BearGameTheme.Pink, owner.OnClickCloseButton);
            confirmation.SetActive(false);
        }

        public void Refresh()
        {
            leaving = false;
            confirmation.SetActive(false);
            coins.text = ServicesManager.Instance.CoinManager.TotalCoins.ToString();
            int current = PlayerDataHandler.GetCurrentLevel();
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].interactable = i + 1 <= current;
                locks[i].SetActive(i + 1 > current);
                BearMenuShape paper = buttons[i].GetComponent<BearMenuShape>();
                paper.color = i + 1 == current ? BearGameTheme.Gold : BearGameTheme.Cream;
                paper.bottomColor = paper.color;
                paper.SetVerticesDirty();
                currentMarkers[i].SetActive(i + 1 == current);
                int earned = PlayerDataHandler.GetStar(i + 1);
                for (int j = 0; j < 3; j++) stars[i][j].color = j < earned ? Color.white : new Color(.68f, .51f, .34f, .30f);
            }
            SnapToCurrent();
        }

        public void ShowConfirmation(int level)
        {
            if (leaving) return;
            confirmTitle.text = "第 " + level + " 关";
            int earned = PlayerDataHandler.GetStar(level);
            for (int i = 0; i < 3; i++) confirmStars[i].color = i < earned ? Color.white : new Color(.68f, .51f, .34f, .30f);
            confirmation.SetActive(true);
        }

        public void HideConfirmation() => confirmation.SetActive(false);

        public void SnapToCurrent()
        {
            Canvas.ForceUpdateCanvases();
            int index = Mathf.Clamp(PlayerDataHandler.GetCurrentLevel() - 1, 0, points.Count - 1);
            float max = Mathf.Max(0, content.rect.height - scroll.viewport.rect.height);
            float offset = Mathf.Clamp(-points[index].y - scroll.viewport.rect.height * .48f, 0, max);
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(0, offset);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BearRouteGraphic : MaskableGraphic
    {
        public Vector2[] points;

        protected override void Awake()
        {
            EnsureCanvasRenderer();
            base.Awake();
        }

        protected override void OnEnable()
        {
            EnsureCanvasRenderer();
            base.OnEnable();
        }

        private void EnsureCanvasRenderer()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null) return;
            Vector2 origin = new Vector2(rectTransform.rect.xMin, rectTransform.rect.yMax);
            for (int i = 1; i < points.Length; i++)
            {
                float length = Vector2.Distance(points[i - 1], points[i]);
                int steps = Mathf.CeilToInt(length / 28);
                for (int j = 1; j < steps; j++)
                {
                    Vector2 center = origin + Vector2.Lerp(points[i - 1], points[i], (float)j / steps);
                    Dot(vh, center, 9, new Color32(200, 151, 94, 255));
                    Dot(vh, center + Vector2.up, 5.5f, new Color32(255, 222, 145, 255));
                }
            }
        }

        private static void Dot(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center, tint, Vector2.zero);
            const int count = 10;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, tint, Vector2.zero);
            }
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }
    }
}
