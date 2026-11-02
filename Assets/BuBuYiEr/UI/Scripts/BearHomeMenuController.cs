using System;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearHomeMenuController : MonoBehaviour
    {
        public BearMenuArt art;
        [HideInInspector] public RectTransform themeRoot;
        [HideInInspector] public GameObject[] pages;
        [HideInInspector] public Text[] coinTexts;
        [HideInInspector] public Text levelText;
        [HideInInspector] public Text[] dailyStates;
        [HideInInspector] public Button[] dailyClaims;
        [HideInInspector] public BearMenuShape[] dailyCards;
        [HideInInspector] public BearMenuCutout[] dailyStateArt;
        [HideInInspector] public GameObject[] dailyFooterPapers;
        [HideInInspector] public Slider soundSlider, musicSlider;
        [HideInInspector] public RectTransform[] toggleKnobs;
        [HideInInspector] public BearMenuShape[] toggleTracks;
        [HideInInspector] public GameObject[] storyLocks;
        [HideInInspector] public GameObject[] storySelections;
        [HideInInspector] public Text storyTitle, storyBody;
        [HideInInspector] public Image storyPreview;
        [HideInInspector] public GameObject unlockedTravelArt, storyDetailsOverlay;
        [HideInInspector] public GameObject alternateCollection, filteredShop, modal;
        [HideInInspector] public Text alternateTitle, alternateBody, modalTitle, modalBody;
        [HideInInspector] public Image filteredShopArt, modalPicture;
        [HideInInspector] public BearMenuShape[] collectionTabs, shopTabs;
        [HideInInspector] public Text[] storePrices;
        [HideInInspector] public Text filteredShopPrice;
        private HomeViewController owner;
        private int currentPage, currentStory, currentShopTab;
        private float refreshAt;
        private bool subscribed;
        private const string Extras = "BUBU_UI_EXTRA_";
        private static readonly string[] Titles = { "奶茶约会", "星空晚安", "糖果回忆", "花园散步", "甜蜜旅行", "特别的约定" };
        private static readonly string[] Stories = {
            "阳光正好，\n我们一起去喝了最爱的奶茶，\n甜甜的味道，就像我们的心情♡",
            "夜空亮起小小的星星，\n靠在你身边，\n今天也想轻轻说一声晚安♡",
            "把每一颗糖果装进小罐子，\n也把和你一起的快乐，\n慢慢收藏起来♡",
            "花开的时候一起散步，\n风吹过小小的花园，\n每一步都有你的陪伴♡",
            "带着最爱的点心出发，\n走过新的风景，\n身边的你就是最好的旅伴♡",
            "用一张小小的心意券，\n许下属于我们的约定，\n以后也一起收集更多回忆♡" };

        public void Show(HomeViewController view)
        {
            owner = view;
            // The template parents all views to its scaled canvas. This menu owns its
            // own pixel canvas, so keep it as a persistent root to avoid double scaling.
            if (transform.parent != null)
            {
                transform.SetParent(null, false);
                transform.localScale = Vector3.one;
                DontDestroyOnLoad(gameObject);
            }
            if (themeRoot == null) BearMenuFactory.Build(this);
            if (themeRoot == null) return;
            foreach (Transform child in transform) child.gameObject.SetActive(child == themeRoot);
            themeRoot.gameObject.SetActive(true);
            if (!subscribed)
            {
                ServicesManager.Instance.CoinManager.TotalCoinChanged += OnCoins;
                subscribed = true;
                soundSlider.onValueChanged.AddListener(OnSound);
                musicSlider.onValueChanged.AddListener(OnMusic);
            }
            soundSlider.SetValueWithoutNotify(PlayerDataHandler.GetSoundValue());
            musicSlider.SetValueWithoutNotify(PlayerDataHandler.GetMusicValue());
            OpenPage(0);
        }

        public void Hide()
        {
            if (subscribed && ServicesManager.Instance != null)
                ServicesManager.Instance.CoinManager.TotalCoinChanged -= OnCoins;
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(OnSound);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusic);
            subscribed = false;
            if (themeRoot != null) themeRoot.gameObject.SetActive(false);
        }

        private void OnDestroy() => Hide();
        private void Update()
        {
            if (!subscribed || themeRoot == null || !themeRoot.gameObject.activeInHierarchy) return;
            if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + 1; Refresh(); }
        }

        public void Handle(string action, int argument = 0)
        {
            if (!Application.isPlaying) return;
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            switch (action)
            {
                case "page": OpenPage(argument); break;
                case "play": owner.OnClickPlayButton(); break;
                case "claim": Claim(argument); break;
                case "story": SelectStory(argument); break;
                case "read": ReadStory(); break;
                case "collection-tab": CollectionTab(argument); break;
                case "shop-tab": ShopTab(argument); break;
                case "buy": Buy(argument); break;
                case "close-modal": modal.SetActive(false); break;
                case "haptic":
                    bool enabled = PlayerPrefs.GetInt("BUBU_VIBRATION_ENABLED", 1) == 0;
                    PlayerPrefs.SetInt("BUBU_VIBRATION_ENABLED", enabled ? 1 : 0);
                    PlayerPrefs.Save();
                    if (enabled && Application.isMobilePlatform) Handheld.Vibrate();
                    Refresh(); break;
                case "notifications":
                    ServicesManager.Instance.NotificationManager.SetNotificationsEnabled(!ServicesManager.Instance.NotificationManager.NotificationsEnabled);
                    Refresh(); break;
                case "language": Message("语言", "当前界面语言：简体中文\n更多语言将在后续版本开放。"); break;
                case "help": Message("游戏帮助", "点击罐子，将它放到传送带。\n收集相同颜色的方块，装满罐子。\n合理安排空位，完成关卡目标。\n\n每日奖励每隔 24 小时可领取一次。"); break;
                case "about": Message("关于我们", "BuBuYiEr\n和小熊一起，收藏甜甜的每一天。\n\n版本 " + Application.version); break;
                case "privacy": OpenLink(art.privacyUrl, "隐私政策", "隐私政策暂未发布。"); break;
                case "support": OpenLink(art.supportUrl, "联系客服", "在线客服暂未开放。"); break;
            }
        }

        public void OpenPage(int index)
        {
            currentPage = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == currentPage);
            modal.SetActive(false);
            if (currentPage == 2) { CollectionTab(1); SelectStory(currentStory); }
            if (currentPage == 3) ShopTab(0);
            Refresh();
        }

        private void OnCoins(int amount)
        {
            string value = amount.ToString();
            for (int i = 0; i < coinTexts.Length; i++)
            {
                coinTexts[i].text = value;
                int normalSize = i == 0 ? 54 : i == 2 ? 48 : 51;
                coinTexts[i].fontSize = Mathf.RoundToInt(normalSize * Mathf.Min(1, 4.6f / value.Length));
            }
        }
        private void Refresh()
        {
            if (ServicesManager.Instance == null || !PlayerDataHandler.HasData()) return;
            OnCoins(PlayerDataHandler.GetTotalCoin());
            levelText.text = "Lv. " + PlayerDataHandler.GetCurrentLevel();
            int cooldown = ServicesManager.Instance.DailyRewardManager.TimeRemainsTillNextReward();
            int day = PlayerDataHandler.GetCurrentRewardIndex();
            // The template contains nine daily rewards; this theme has a seven-day cycle.
            if (day >= dailyStates.Length && cooldown == 0)
            {
                day = 0;
                PlayerDataHandler.UpdateCurrentRewardIndex(day);
                PlayerPrefs.Save();
            }
            for (int i = 0; i < dailyStates.Length; i++)
            {
                bool available = i == day && cooldown == 0;
                dailyClaims[i].interactable = available;
                bool claimed = i < day;
                bool waiting = i == day && cooldown > 0;
                bool originalState = i == 0 ? claimed : i == 1 ? available : !claimed && !available && !waiting;
                dailyFooterPapers[i].SetActive(!originalState);
                dailyStateArt[i].transform.parent.gameObject.SetActive(!originalState && !waiting);
                dailyStateArt[i].sprite = art.Get(claimed ? "DailyClaimedFooter" : available ? "DailyClaimButton" : i == 6 ? "DailyWideLockedFooter" : "DailyLockedFooter");
                dailyStateArt[i].silhouette = available ? "capsule" : "rectangle";
                dailyStateArt[i].SetVerticesDirty();
                RectTransform stateRect = dailyStateArt[i].rectTransform;
                stateRect.anchoredPosition = new Vector2(available ? (i == 6 ? 9 : 14) : 0, 0);
                stateRect.sizeDelta = new Vector2(available ? 211 : i == 6 ? 229 : 240, i == 6 ? 68 : 80);
                dailyCards[i].border = available && i != 1 ? 4 : 0;
                dailyCards[i].borderColor = new Color32(229, 150, 24, 255);
                dailyCards[i].SetVerticesDirty();
                dailyStates[i].text = i < day ? "已领取" : available ? "领取" : i == day ?
                    $"{cooldown / 3600:00}:{cooldown / 60 % 60:00}:{cooldown % 60:00}" : "待领取";
                dailyStates[i].gameObject.SetActive(waiting);
            }
            SetToggle(0, PlayerPrefs.GetInt("BUBU_VIBRATION_ENABLED", 1) == 1);
            SetToggle(1, ServicesManager.Instance.NotificationManager.NotificationsEnabled);
            for (int i = 0; i < storyLocks.Length; i++) storyLocks[i].SetActive(PlayerDataHandler.GetCurrentLevel() < UnlockLevel(i));
            if (unlockedTravelArt != null) unlockedTravelArt.SetActive(PlayerDataHandler.GetCurrentLevel() >= UnlockLevel(4));
            for (int i = 0; i < storePrices.Length; i++)
            {
                string price = Price(i);
                storePrices[i].transform.parent.gameObject.SetActive(price != "N/A");
                if (price != "N/A") storePrices[i].text = price;
            }
            string filteredPrice = Price(currentShopTab);
            filteredShopPrice.transform.parent.gameObject.SetActive(currentShopTab > 0 && filteredPrice != "N/A");
            if (filteredPrice != "N/A") filteredShopPrice.text = filteredPrice;
        }

        private string Price(int index) => index < art.productIds.Length && !string.IsNullOrEmpty(art.productIds[index])
            ? ServicesManager.Instance.IAPManager.GetLocalizedPriceString(art.productIds[index]) : "N/A";

        private void SetToggle(int index, bool enabled)
        {
            toggleTracks[index].color = enabled ? new Color32(255,224,82,255) : new Color32(220,207,189,255);
            toggleTracks[index].bottomColor = toggleTracks[index].color;
            toggleTracks[index].SetVerticesDirty();
            toggleKnobs[index].anchoredPosition = new Vector2(enabled ? 97 : 31, -30);
        }

        private void OnSound(float value)
        {
            PlayerDataHandler.UpdateSoundValue(value);
            ServicesManager.Instance.SoundManager.UpdateVolumes();
            PlayerPrefs.Save();
        }
        private void OnMusic(float value)
        {
            PlayerDataHandler.UpdateMusicValue(value);
            ServicesManager.Instance.SoundManager.UpdateVolumes();
            PlayerPrefs.Save();
        }

        private void Claim(int index)
        {
            DailyRewardManager rewards = ServicesManager.Instance.DailyRewardManager;
            if (rewards.TimeRemainsTillNextReward() != 0 || PlayerDataHandler.GetCurrentRewardIndex() != index) return;
            if (index == 0 || index == 5) ServicesManager.Instance.CoinManager.AddTotalCoins(index == 0 ? 100 : 300, 0);
            else
            {
                string type = new[] { "", "hearts", "clovers", "flowers", "decorationTickets", "", "candyJars" }[index];
                int amount = index == 1 ? 3 : index == 3 ? 5 : 1;
                PlayerPrefs.SetInt(Extras + type, PlayerPrefs.GetInt(Extras + type, 0) + amount);
            }
            rewards.HandleClaimedCurrentDailyReward();
            PlayerPrefs.Save();
            Refresh();
            Message("领取成功", new[] { "金币 ×100", "爱心 ×3", "四叶草 ×1", "小花 ×5", "装饰券 ×1", "金币 ×300", "糖果罐 ×1" }[index] + "\n奖励已放入你的收藏。");
        }

        private static int UnlockLevel(int chapter) => chapter == 0 ? 1 : chapter * 5;
        private void SelectStory(int index)
        {
            if (PlayerDataHandler.GetCurrentLevel() < UnlockLevel(index)) { Message("还差一点点", "完成到第 " + UnlockLevel(index) + " 关，解锁这段回忆。"); return; }
            currentStory = index;
            for (int i = 0; i < storySelections.Length; i++) storySelections[i].SetActive(i == index);
            storyTitle.text = Titles[index];
            storyBody.text = Stories[index];
            storyPreview.sprite = art.Get(index == 0 ? "StoryPreview0" : "StoryImage" + index);
            storyDetailsOverlay.SetActive(index != 0);
        }
        private void ReadStory() => Message(Titles[currentStory], Stories[currentStory], art.Get("StoryImage" + currentStory));

        private void CollectionTab(int index)
        {
            for (int i = 0; i < collectionTabs.Length; i++) collectionTabs[i].gameObject.SetActive(i == index);
            alternateCollection.SetActive(index != 1);
            alternateTitle.text = index == 0 ? "表情收藏" : "动画收藏";
            alternateBody.text = index == 0 ? "小熊的每一种心情，都值得收藏。\n\n更多表情内容即将加入。" : "一起记录会动的小小幸福。\n\n更多动画内容即将加入。";
        }
        private void ShopTab(int index)
        {
            currentShopTab = index;
            for (int i = 0; i < shopTabs.Length; i++) shopTabs[i].gameObject.SetActive(i == index);
            filteredShop.SetActive(index != 0);
            if (index != 0)
            {
                filteredShopArt.sprite = art.Get("ShopRow" + index);
                BearMenuAction button = filteredShop.GetComponentInChildren<BearMenuAction>();
                button.argument = index;
            }
            Refresh();
        }
        private void Buy(int index)
        {
            if (index >= art.productIds.Length || string.IsNullOrEmpty(art.productIds[index]))
            { Message("商店", "这件商品暂未上架，敬请期待。"); return; }
            if (ServicesManager.Instance.IAPManager.GetLocalizedPriceString(art.productIds[index]) == "N/A")
            { Message("商店连接中", "暂时无法连接商店，请稍后再试。"); return; }
            ServicesManager.Instance.IAPManager.PurchaseProduct(art.productIds[index]);
        }

        private void OpenLink(string url, string title, string emptyMessage)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && (uri.Scheme == "https" || uri.Scheme == "http")) Application.OpenURL(url);
            else Message(title, emptyMessage);
        }
        public void Message(string title, string body, Sprite picture = null)
        {
            modal.SetActive(true);
            modal.transform.SetAsLastSibling();
            modalTitle.text = title;
            modalBody.text = body;
            RectTransform bodyRect = modalBody.rectTransform;
            bodyRect.anchoredPosition = new Vector2(48, picture != null ? -454 : -170);
            bodyRect.sizeDelta = new Vector2(685, picture != null ? 220 : 480);
            modalPicture.sprite = picture;
            modalPicture.gameObject.SetActive(picture != null);
        }
    }
}
