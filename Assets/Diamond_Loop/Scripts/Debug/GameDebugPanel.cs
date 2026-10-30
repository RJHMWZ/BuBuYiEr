#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClawbearGames
{
    /// <summary>Prefab-free, touch-friendly development UI using the project's new Input System.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GameDebugPanel : MonoBehaviour
    {
        private readonly Color surface = new Color(0.08f, 0.11f, 0.17f, 0.99f);
        private readonly Color buttonColor = new Color(0.18f, 0.25f, 0.36f);
        private readonly Color accent = new Color(0.12f, 0.55f, 0.63f);
        private RectTransform safeRoot;
        private GameObject panel, launcher, confirmation;
        private GameObject[] pages;
        private Text summary, status, inventory, logText, confirmationText;
        private InputField levelInput, coinsInput, boostsInput;
        private ScrollRect scroll;
        private EventSystem ownedEventSystem;
        private Font font;
        private Action pendingAction;
        private float nextRefresh, frameTime;
        private int activePage;

        private void Start()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Droid Sans Fallback", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
            GameDebugService.RefreshLevels();
            GameDebugService.Changed += MarkDirty;
            RefreshInfo();
        }

        private void Update()
        {
            frameTime = Mathf.Lerp(frameTime, Time.unscaledDeltaTime, 0.08f);
            EnsureEventSystem();
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                GameDebugService.SetPanelVisible(!GameDebugService.PanelVisible);
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && GameDebugService.PanelVisible)
                GameDebugService.SetPanelVisible(false);
            if (panel == null) return;
            panel.SetActive(GameDebugService.PanelVisible);
            launcher.SetActive(!GameDebugService.PanelVisible);
            Rect safe = Screen.safeArea;
            if (Screen.width > 0 && Screen.height > 0)
            {
                safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
                safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            }
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.35f;
                RefreshInfo();
            }
        }

        private void EnsureEventSystem()
        {
            // Template scenes supply their own persistent EventSystem. Also support an empty test scene.
            if (ownedEventSystem != null)
            {
                foreach (EventSystem other in FindObjectsOfType<EventSystem>())
                    if (other != ownedEventSystem)
                    {
                        ownedEventSystem.gameObject.SetActive(false);
                        Destroy(ownedEventSystem.gameObject);
                        ownedEventSystem = null;
                        EventSystem.current = other;
                        break;
                    }
            }
            if (EventSystem.current == null)
            {
                var events = new GameObject("Debug EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                ownedEventSystem = events.GetComponent<EventSystem>();
            }
        }

        private void MarkDirty() => nextRefresh = 0;

        private void OnDestroy()
        {
            GameDebugService.Changed -= MarkDirty;
            Time.timeScale = 1f;
            if (font != null && font.name != "LegacyRuntime") Destroy(font);
        }

        private void BuildUI()
        {
            RectTransform canvasRect = Rect("Debug Canvas", transform);
            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1500);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            safeRoot = Rect("Safe Area", canvasRect);
            Stretch(safeRoot);

            Button launch = Button(safeRoot, "调试 F1", () => GameDebugService.SetPanelVisible(true));
            launcher = launch.gameObject;
            RectTransform launchRect = (RectTransform)launch.transform;
            launchRect.anchorMin = launchRect.anchorMax = new Vector2(1, 1);
            launchRect.pivot = new Vector2(1, 1);
            launchRect.anchoredPosition = new Vector2(-16, -16);
            launchRect.sizeDelta = new Vector2(148, 64);
            launch.image.color = accent;

            RectTransform overlay = Rect("Debug Overlay", safeRoot);
            panel = overlay.gameObject;
            Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.72f);
            RectTransform card = Rect("Panel", overlay);
            card.anchorMin = new Vector2(0.04f, 0.05f);
            card.anchorMax = new Vector2(0.96f, 0.95f);
            card.offsetMin = card.offsetMax = Vector2.zero;
            card.gameObject.AddComponent<Image>().color = surface;
            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 12;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            Label(card, "开发调试 / GAME DEBUG", 30, 48).fontStyle = FontStyle.Bold;
            summary = Label(card, "", 21, 88);
            RectTransform tabs = Row(card);
            Button(tabs, "关卡", () => SelectPage(0));
            Button(tabs, "资源 / 存档", () => SelectPage(1));
            Button(tabs, "运行 / 日志", () => SelectPage(2));

            RectTransform viewport = Rect("Scroll View", card);
            viewport.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.11f, 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            LayoutElement viewportLayout = viewport.gameObject.AddComponent<LayoutElement>();
            viewportLayout.minHeight = 120;
            viewportLayout.flexibleHeight = 1;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.padding = new RectOffset(12, 12, 12, 12);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            pages = new GameObject[3];
            for (int i = 0; i < pages.Length; i++)
            {
                RectTransform page = Rect("Page " + i, content);
                VerticalLayoutGroup group = page.gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = 12;
                group.childControlHeight = group.childControlWidth = true;
                group.childForceExpandHeight = false;
                pages[i] = page.gameObject;
            }
            BuildLevelPage(pages[0].transform);
            BuildInventoryPage(pages[1].transform);
            BuildRuntimePage(pages[2].transform);

            RectTransform confirm = Rect("Confirmation", card);
            confirmation = confirm.gameObject;
            VerticalLayoutGroup confirmLayout = confirm.gameObject.AddComponent<VerticalLayoutGroup>();
            confirmLayout.childForceExpandHeight = false;
            confirmationText = Label(confirm, "", 21, 60);
            RectTransform confirmRow = Row(confirm);
            Button(confirmRow, "确认执行", () => { Action action = pendingAction; CancelConfirmation(); action?.Invoke(); });
            Button(confirmRow, "取消", CancelConfirmation);
            confirmation.SetActive(false);
            status = Label(card, "", 21, 86);
            status.color = new Color(0.46f, 0.87f, 0.88f);
            Button(card, "关闭面板 / 继续游戏  [F1 / Esc]", () => GameDebugService.SetPanelVisible(false));
            SelectPage(0);
            panel.SetActive(false);
        }

        private void BuildLevelPage(Transform page)
        {
            Label(page, "跳关与重试使用试玩关卡，不写入关卡进度。", 22, 64);
            levelInput = Field(page, "关卡编号，例如 12", "1");
            Button(page, "进入指定关卡", () => Parse(levelInput, n => GameDebugService.JumpToLevel(n)));
            RectTransform row = Row(page);
            Button(row, "上一关", () => GameDebugService.LoadAdjacentLevel(-1));
            Button(row, "重试本关", () => GameDebugService.RetryLevel());
            Button(row, "下一关", () => GameDebugService.LoadAdjacentLevel(1));
            Label(page, "模拟结算：使用真实结算界面，不写入星级与进度。", 22, 68);
            RectTransform stars = Row(page);
            for (int i = 1; i <= 3; i++)
            {
                int count = i;
                Button(stars, i + " 星胜利", () => GameDebugService.FinishLevel(true, count));
            }
            Button(page, "模拟失败", () => GameDebugService.FinishLevel(false));
            Button(page, "跳过引导并重试", () => GameDebugService.SkipTutorial());
            Button(page, "返回主页 / 退出试玩", () => GameDebugService.ReturnHome());
            Button(page, "刷新关卡列表", () => { GameDebugService.RefreshLevels(); RefreshInfo(); });
        }

        private void BuildInventoryPage(Transform page)
        {
            Label(page, "资源修改会保存；首次修改前自动备份一份存档。", 22, 68);
            inventory = Label(page, "", 22, 120);
            coinsInput = Field(page, "目标金币数量", "10000");
            Button(page, "设置金币", () => Parse(coinsInput, n => GameDebugService.SetCoins(n)));
            boostsInput = Field(page, "每种道具增加数量", "10");
            Button(page, "五种道具各增加", () => Parse(boostsInput, n => GameDebugService.AddAllBoosts(n)));
            Button(page, "备份当前存档", () =>
            {
                if (GameDebugService.HasBackup) Confirm("将覆盖已有备份，确认保存当前存档？", () => GameDebugService.BackupPlayerData());
                else GameDebugService.BackupPlayerData();
            });
            Button(page, "恢复备份并回主页", () => Confirm("将用备份覆盖当前游戏存档，并返回主页。", () => GameDebugService.RestorePlayerData()));
            Button(page, "重置新玩家并回主页", () => Confirm("将重置本游戏存档；已有备份会保留。", () => GameDebugService.ResetPlayerData()));
        }

        private void BuildRuntimePage(Transform page)
        {
            Label(page, "打开面板时暂停。所选倍速在关闭面板后生效。", 22, 68);
            RectTransform row = Row(page);
            foreach (float speed in new[] { 0f, 0.25f, 0.5f, 1f, 2f, 4f })
            {
                float value = speed;
                Button(row, speed == 0 ? "暂停" : speed + "x", () => GameDebugService.SetGameSpeed(value));
            }
            Button(page, "恢复正常速度并关闭", () => { GameDebugService.SetGameSpeed(1); GameDebugService.SetPanelVisible(false); });
            RectTransform actions = Row(page);
            Button(actions, "清空日志", GameDebugService.ClearLogs);
            Button(actions, "导出日志", () => GameDebugService.ExportLogs());
            Label(page, "最近 12 条日志预览；导出包含最多 120 条及错误堆栈。", 20, 64);
            logText = Label(page, "", 18, 0);
            logText.gameObject.GetComponent<LayoutElement>().preferredHeight = -1;
            logText.gameObject.GetComponent<LayoutElement>().minHeight = 100;
            logText.alignment = TextAnchor.UpperLeft;
        }

        private void RefreshInfo()
        {
            if (summary == null) return;
            PlayerData data = GameDebugService.GetPlayerData();
            IngameManager game = IngameManager.Instance;
            string level = game != null ? game.CurrentLevel.ToString() : "—";
            summary.text = $"场景 {SceneManager.GetActiveScene().name}  ·  当前关卡 {level}  ·  存档关卡 {data.CurrentLevel}\n" +
                $"{(GameDebugService.IsPreviewLevel ? "试玩模式" : "正常模式")}  ·  {1f / Mathf.Max(0.001f, frameTime):0} FPS  ·  {GameDebugService.GameSpeed:0.##}x  ·  关卡数 {GameDebugService.LevelNumbers.Count}";
            inventory.text = $"金币 {data.TotalCoins}  ·  备份 {(GameDebugService.HasBackup ? "已有" : "暂无")}\n" +
                $"槽位 {data.SlotCount} / 磁铁 {data.MagnetCount} / 锤子 {data.HammerCount}\n钥匙 {data.KeyCount} / 冷却 {data.CoolerCount}";
            status.text = GameDebugService.Status;
            if (activePage == 2 && GameDebugService.PanelVisible)
            {
                var entries = GameDebugService.Logs;
                var builder = new System.Text.StringBuilder();
                for (int i = Mathf.Max(0, entries.Count - 12); i < entries.Count; i++)
                    builder.AppendLine(entries[i].Length > 500 ? entries[i].Substring(0, 500) + "…" : entries[i]).AppendLine();
                logText.text = builder.Length == 0 ? "暂无日志" : builder.ToString();
            }
        }

        private void SelectPage(int index)
        {
            activePage = index;
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == index);
            CancelConfirmation();
            scroll.verticalNormalizedPosition = 1;
            MarkDirty();
        }

        private void Confirm(string message, Action action)
        {
            pendingAction = action;
            confirmationText.text = message;
            confirmation.SetActive(true);
        }

        private void CancelConfirmation() { pendingAction = null; confirmation.SetActive(false); }

        private static void Parse(InputField field, Action<int> action)
        {
            if (int.TryParse(field.text, out int value)) action(value);
            else GameDebugService.Report(false, "请输入有效整数");
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Size(GameObject obj, float height)
        {
            LayoutElement element = obj.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleWidth = 1;
        }

        private Text Label(Transform parent, string value, int size, float height)
        {
            Text label = Rect("Text", parent).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.color = new Color(0.9f, 0.94f, 1);
            label.text = value;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            Size(label.gameObject, height);
            return label;
        }

        private Button Button(Transform parent, string title, Action action)
        {
            RectTransform rect = Rect(title, parent);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = buttonColor;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => action());
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Size(rect.gameObject, 72);
            Text label = Label(rect, title, 23, 0);
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 23;
            Stretch((RectTransform)label.transform);
            return button;
        }

        private InputField Field(Transform parent, string hint, string value)
        {
            RectTransform rect = Rect("Input", parent);
            rect.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.2f, 0.28f);
            Size(rect.gameObject, 72);
            Text text = Label(rect, value, 25, 0);
            Stretch((RectTransform)text.transform);
            ((RectTransform)text.transform).offsetMin = new Vector2(18, 4);
            ((RectTransform)text.transform).offsetMax = new Vector2(-18, -4);
            Text placeholder = Label(rect, hint, 23, 0);
            Stretch((RectTransform)placeholder.transform);
            placeholder.color = new Color(0.5f, 0.58f, 0.68f);
            InputField input = rect.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 10;
            input.text = value;
            return input;
        }

        private static RectTransform Row(Transform parent)
        {
            RectTransform row = Rect("Row", parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            Size(row.gameObject, 72);
            return row;
        }
    }
}
#endif
