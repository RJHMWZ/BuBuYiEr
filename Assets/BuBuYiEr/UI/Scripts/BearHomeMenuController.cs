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
        [HideInInspector] public Text soloPlayLabel;
        [HideInInspector] public Text[] dailyStates;
        [HideInInspector] public Button[] dailyClaims;
        [HideInInspector] public BearMenuShape[] dailyCards;
        [HideInInspector] public Image[] dailyCardArt, dailyStatusIcons;
        [HideInInspector] public GameObject[] dailyAvailableArt, dailyClaimBackgrounds;
        [HideInInspector] public Text dailyCooldownText;
        [HideInInspector] public Slider soundSlider, musicSlider;
        [HideInInspector] public RectTransform[] toggleKnobs;
        [HideInInspector] public BearMenuShape[] toggleTracks;
        [HideInInspector] public Image[] toggleTrackImages;
        [HideInInspector] public GameObject[] storyLocks;
        [HideInInspector] public GameObject[] storySelections;
        [HideInInspector] public Text storyTitle, storyBody;
        [HideInInspector] public Image storyPreview;
        [HideInInspector] public GameObject unlockedTravelArt, storyDetailsOverlay;
        [HideInInspector] public GameObject alternateCollection, modal;
        [HideInInspector] public Text alternateTitle, alternateBody, modalTitle, modalBody;
        [HideInInspector] public Image modalPicture;
        [HideInInspector] public BearMenuShape[] collectionTabs;
        [HideInInspector] public Image[] shopTabImages;
        [HideInInspector] public RectTransform[] shopProductRows;
        [HideInInspector] public Text shopCategoryHint;
        [HideInInspector] public Text[] storePrices;
        private HomeViewController owner;
        private int currentPage, currentShopTab;
        private float refreshAt;
        private bool subscribed;
        private CoinManager subscribedCoins;
        private int shownCoins = -1, shownRewardDay = -1, shownCooldown = -1;
        private Text[] shownCoinLabels, shownRewardLabels;
        private BearMenuArt shownRewardArt;
        private const float PreferenceSaveDelay = .25f;
        private bool preferencesDirty;
        private float preferenceSaveAt;
        public BearCollectionPanel CollectionPanel => pages != null && pages.Length > 2 && pages[2] != null
            ? pages[2].GetComponent<BearCollectionPanel>() : null;
        private const string Extras = "BUBU_UI_EXTRA_";
        private static readonly string[] ShopPreviewPrices={"¥6","¥12","¥12","¥30","¥68"};
        private static readonly float[] ShopRowPositions={651,947,1097,1247,1397};
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
            BearMenuFactory.EnsureSoloActions(this);
            if (CollectionPanel == null || CollectionPanel.layoutVersion != BearCollectionPanel.LayoutVersion)
                BearMenuFactory.RebuildCollection(this);
            foreach (Transform child in transform) child.gameObject.SetActive(child == themeRoot);
            themeRoot.gameObject.SetActive(true);
            if (!subscribed)
            {
                subscribedCoins=ServicesManager.Instance.CoinManager;
                subscribedCoins.TotalCoinChanged += OnCoins;
                subscribed = true;
                soundSlider.onValueChanged.AddListener(OnSound);
                musicSlider.onValueChanged.AddListener(OnMusic);
            }
            soundSlider.SetValueWithoutNotify(PlayerDataHandler.GetSoundValue());
            musicSlider.SetValueWithoutNotify(PlayerDataHandler.GetMusicValue());
            OpenPage(0);
            if (BearSoloProgress.PendingStory >= 0)
            {
                int story = BearSoloProgress.PendingStory;
                BearSoloProgress.PendingStory = -1;
                OpenPage(2);
                CollectionPanel?.SelectStory(story);
            }
        }

        public void Hide()
        {
            FlushPreferences();
            if (subscribedCoins != null) subscribedCoins.TotalCoinChanged -= OnCoins;
            subscribedCoins=null;
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(OnSound);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusic);
            subscribed = false;
            if (themeRoot != null) themeRoot.gameObject.SetActive(false);
        }

        private void OnDestroy() => Hide();
        private void OnDisable() => Hide();
        private void Update()
        {
            if(preferencesDirty && Time.unscaledTime>=preferenceSaveAt)FlushPreferences();
            if (!subscribed || themeRoot == null || !themeRoot.gameObject.activeInHierarchy) return;
            if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + 1; Refresh(); }
        }

        public void Handle(string action, int argument = 0)
        {
            if (!Application.isPlaying) return;
            ServicesManager.Instance.SoundManager.PlayCue(action=="page" ? argument==0?GameSoundCue.UiClose:GameSoundCue.UiOpen :
                action=="close-modal"?GameSoundCue.UiClose:GameSoundCue.UiClick);
            switch (action)
            {
                case "page": OpenPage(argument); break;
                case "play": owner.OnClickPlayButton(); break;
                case "levels": owner.OnClickLevelsButton(); break;
                case "claim": Claim(argument); break;
                case "story": SelectStory(argument); break;
                case "read": ReadStory(); break;
                case "collection-tab": CollectionTab(argument); break;
                case "collection-expression": CollectionPanel?.SelectExpression(argument); break;
                case "collection-favorite": CollectionPanel?.ToggleFavorite(); break;
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
                case "help": Message("游戏帮助", "点击糖砖，打碎后获得本关蜂蜜。\n购买六种小球，让它们自动弹跳。\n升级小球威力，清空糖砖即可通关。\n蜂蜜每关重新准备，通关金币存入钱包。\n\n每日奖励每隔 24 小时可领取一次。"); break;
                case "about": Message("关于我们", "BuBuYiEr\n和小熊一起，收藏甜甜的每一天。\n\n版本 " + Application.version); break;
                case "privacy": OpenLink(art.privacyUrl, "隐私政策", "隐私政策暂未发布。"); break;
                case "support": OpenLink(art.supportUrl, "联系客服", "在线客服暂未开放。"); break;
            }
        }

        public void OpenPage(int index)
        {
            FlushPreferences();
            currentPage = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == currentPage);
            modal.SetActive(false);
            if (currentPage == 2) CollectionPanel?.Show();
            if (currentPage == 3) ShopTab(0);
            Refresh();
        }

        private void OnCoins(int amount)
        {
            if(shownCoins==amount && shownCoinLabels==coinTexts)return;
            shownCoins=amount;shownCoinLabels=coinTexts;
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
            int progressLevel = PlayerDataHandler.GetCurrentLevel();
            int playableLevel = BearSoloProgress.PlayableLevel(progressLevel);
            levelText.text = "Lv. " + playableLevel;
            if (soloPlayLabel != null)
                soloPlayLabel.text = PlayerDataHandler.IsWatchedTutorial()
                    ? (playableLevel < progressLevel ? "重温第 " : "继续第 ") + playableLevel + " 关" : "开始打糖砖";
            int cooldown = ServicesManager.Instance.DailyRewardManager.TimeRemainsTillNextReward();
            Transform rewardNotice = pages[0].transform.Find("Navigation/DailyReward/RewardNotice");
            if (rewardNotice != null) rewardNotice.gameObject.SetActive(cooldown == 0);
            int day = PlayerDataHandler.GetCurrentRewardIndex();
            // The template contains nine daily rewards; this theme has a seven-day cycle.
            if (day >= dailyStates.Length && cooldown == 0)
            {
                day = 0;
                PlayerDataHandler.UpdateCurrentRewardIndex(day);
                PlayerPrefs.Save();
            }
            RefreshDailyRewards(day,cooldown);
            SetToggle(0, PlayerPrefs.GetInt("BUBU_VIBRATION_ENABLED", 1) == 1);
            SetToggle(1, ServicesManager.Instance.NotificationManager.NotificationsEnabled);
            if (currentPage == 2) CollectionPanel?.RefreshProgress();
            for (int i = 0; i < storePrices.Length; i++)
            {
                string price = Price(i);
                bool configured=i<art.productIds.Length && !string.IsNullOrEmpty(art.productIds[i]);
                storePrices[i].text = !configured ? "未上架" : price != "N/A" ? price : "连接中";
            }
        }

        private string Price(int index) => index < art.productIds.Length && !string.IsNullOrEmpty(art.productIds[index])
            && ServicesManager.Instance.IAPManager.IsInitialized()
            ? ServicesManager.Instance.IAPManager.GetLocalizedPriceString(art.productIds[index]) : "N/A";

        public void RefreshDailyRewards(int day,int cooldown)
        {
            if(shownRewardLabels==dailyStates && shownRewardArt==art && shownRewardDay==day && shownCooldown==cooldown)return;
            shownRewardLabels=dailyStates;shownRewardArt=art;shownRewardDay=day;shownCooldown=cooldown;
            string time=$"{cooldown / 3600:00}:{cooldown / 60 % 60:00}:{cooldown % 60:00}";
            for(int i=0;i<dailyStates.Length;i++)
            {
                bool claimed=i<day;
                bool available=i==day && cooldown==0;
                bool waiting=i==day && cooldown>0;
                bool showIcon=!available && !waiting;
                dailyClaims[i].interactable=available;
                dailyCardArt[i].gameObject.SetActive(!available || i==6);
                dailyCardArt[i].color=claimed?new Color32(235,221,201,255):Color.white;
                dailyAvailableArt[i].SetActive(available && i!=6);
                dailyClaimBackgrounds[i].SetActive(available);
                dailyStatusIcons[i].gameObject.SetActive(showIcon);
                dailyStatusIcons[i].sprite=art.Get(claimed?"Daily/IconTick":"Daily/IconLock");
                dailyCards[i].border=available && i==6?3:0;
                dailyCards[i].borderColor=new Color32(229,150,24,255);
                dailyCards[i].SetVerticesDirty();
                Text state=dailyStates[i];
                state.gameObject.SetActive(true);
                state.font=waiting?art.numberFont:art.chineseFont;
                state.fontSize=waiting?29:34;
                state.text=claimed?"已领取":available?"领取":waiting?time:"待领取";
                state.rectTransform.anchoredPosition=new Vector2(showIcon?61:0,0);
                state.rectTransform.sizeDelta=new Vector2(showIcon?163:228,69);
            }
            dailyCooldownText.gameObject.SetActive(cooldown>0);
            dailyCooldownText.text=(day>=7?"下一轮奖励：":"下次领取：")+time;
        }

        private void SetToggle(int index, bool enabled)
        {
            Color fill=enabled ? new Color32(255,224,82,255) : new Color32(220,207,189,255);
            Vector2 position=new Vector2(enabled ? 97 : 31, -30);
            bool artMatches=toggleTrackImages==null || index>=toggleTrackImages.Length || toggleTrackImages[index]==null || toggleTrackImages[index].gameObject.activeSelf==enabled;
            if(toggleTracks[index].color==fill && toggleTracks[index].bottomColor==fill && toggleKnobs[index].anchoredPosition==position && artMatches)return;
            toggleTracks[index].color = fill;
            toggleTracks[index].bottomColor = toggleTracks[index].color;
            toggleTracks[index].SetVerticesDirty();
            if(toggleTrackImages!=null && index<toggleTrackImages.Length && toggleTrackImages[index]!=null)
                toggleTrackImages[index].gameObject.SetActive(enabled);
            toggleKnobs[index].anchoredPosition = position;
        }

        private void OnSound(float value)
        {
            PlayerDataHandler.UpdateSoundValue(value);
            ServicesManager.Instance.SoundManager.UpdateVolumes();
            QueuePreferenceSave();
        }
        private void OnMusic(float value)
        {
            PlayerDataHandler.UpdateMusicValue(value);
            ServicesManager.Instance.SoundManager.UpdateVolumes();
            QueuePreferenceSave();
        }
        private void QueuePreferenceSave()
        {
            preferencesDirty=true;
            preferenceSaveAt=Time.unscaledTime+PreferenceSaveDelay;
        }
        private void FlushPreferences()
        {
            if(!preferencesDirty)return;
            PlayerPrefs.Save();
            preferencesDirty=false;
        }
        private void OnApplicationPause(bool paused) { if(paused)FlushPreferences(); }
        private void OnApplicationQuit() => FlushPreferences();

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
            ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Reward);
            PlayerPrefs.Save();
            Refresh();
            Message("领取成功", new[] { "金币 ×100", "爱心 ×3", "四叶草 ×1", "小花 ×5", "装饰券 ×1", "金币 ×300", "糖果罐 ×1" }[index] + "\n奖励已放入你的收藏。");
        }

        private void SelectStory(int index) => CollectionPanel?.SelectStory(index);
        private void ReadStory() => CollectionPanel?.ReadSelected();
        private void CollectionTab(int index) => CollectionPanel?.SelectTab(index);

        private void ShopTab(int index)
        {
            PreviewShopCategory(index);
            Refresh();
        }

        public void PreviewShopCategory(int index)
        {
            currentShopTab=Mathf.Clamp(index,0,4);
            for(int i=0;i<shopTabImages.Length;i++)
            {
                bool selected=i==currentShopTab;
                shopTabImages[i].sprite=art.Get(selected?"Shop/TabSelected":"Shop/TabNormal");
                shopTabImages[i].rectTransform.sizeDelta=new Vector2(172,selected?106:82);
            }
            for(int i=0;i<shopProductRows.Length;i++)
            {
                shopProductRows[i].gameObject.SetActive(currentShopTab==0 || currentShopTab==i);
                Vector2 position=new Vector2(43,-(currentShopTab==0?ShopRowPositions[i]:652));
                var layout=pages[3].GetComponent<BearMenuLayout>();
                if(layout!=null)layout.SetContentPosition(shopProductRows[i],position);
                else shopProductRows[i].anchoredPosition=position;
            }
            shopCategoryHint.gameObject.SetActive(currentShopTab!=0);
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
            if(ServicesManager.Instance!=null)ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.UiOpen);
            modal.SetActive(true);
            modal.transform.SetAsLastSibling();
            modalTitle.text = title;
            modalBody.text = body;
            RectTransform bodyRect = modalBody.rectTransform;
            bodyRect.anchoredPosition = new Vector2(48, picture != null ? -454 : -170);
            bodyRect.sizeDelta = new Vector2(685, picture != null ? 220 : 480);
            modalPicture.sprite = picture;
            BearMenuFactory.CenterImage(modalPicture);
            modalPicture.gameObject.SetActive(picture != null);
        }
    }
}
