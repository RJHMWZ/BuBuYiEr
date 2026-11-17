using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
        [HideInInspector] public GameObject messageDialog, coffeeDialog;
        [HideInInspector] public Text alternateTitle, alternateBody, modalTitle, modalBody;
        [HideInInspector] public Image modalPicture;
        [HideInInspector] public BearMenuShape[] collectionTabs;
        [HideInInspector] public Image[] shopTabImages;
        [HideInInspector] public RectTransform[] shopProductRows;
        [HideInInspector] public Text shopCategoryHint;
        [HideInInspector] public Text[] storePrices;
        public const int LayoutVersion = 7;
        [HideInInspector] public int layoutVersion;
        [HideInInspector] public BearMenuHeader header;
        [HideInInspector] public RectTransform shopContent;
        [HideInInspector] public GameObject shopEmpty;
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
            if (themeRoot == null || layoutVersion != LayoutVersion) BearMenuFactory.Build(this);
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
            if(BearSoloProgress.PendingBall>=0)
            {
                int ball=BearSoloProgress.PendingBall;BearSoloProgress.PendingBall=-1;BearSoloProgress.PendingStory=-1;
                OpenPage(2);CollectionPanel?.SelectBall(ball);
            }
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
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (modal.activeSelf) modal.SetActive(false);
                else if (currentPage != 0) OpenPage(0);
            }
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
                case "collection-ball": CollectionPanel?.SelectBall(argument); break;
                case "journey-claim": CollectionPanel?.ClaimJourney(argument); Refresh(); break;
                case "earn-coins": Message("获取金币", "通关糖砖关卡可以获得金币。\n每日奖励也能领取金币。\n\n攒下的金币可以在小铺兑换装扮、皮肤和道具。"); break;
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
                case "help": Message("游戏帮助", "点击糖砖赚取本关蜂蜜，买球与升级，清空即通关。\n通关第 3、8、15、24、35 关，依次迎接新伙伴。\n蜂蜜每关重置；金币、解锁与收藏永久保留。\n每颗新星额外奖励 10 金币，重玩也有少量金币。\n小铺的摆件和外观可装备；道具每种每关可用一次。\n图鉴的旅程栏可领取进度与集星奖励。"); break;
                case "about": Message("关于我们", "BuBuYiEr\n和小熊一起，收藏甜甜的每一天。\n\n版本 " + Application.version); break;
                case "coffee": ShowCoffee(); break;
                case "support": OpenLink(art.supportUrl, "联系客服", "在线客服暂未开放。"); break;
            }
        }

        public void OpenPage(int index)
        {
            FlushPreferences();
            currentPage = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == currentPage);
            modal.SetActive(false);
            header.ShowPage(currentPage);
            if (currentPage == 2) CollectionPanel?.Show();
            if (currentPage == 3)
            {
                BearMenuFactory.PopulateShop(this);
                ShopTab(0);
            }
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
                const int normalSize = 54;
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
            var journeyGoal=pages[0].transform.Find("JourneyGoal");
            if(journeyGoal!=null)journeyGoal.GetComponent<Text>().text=CandyProgression.NextGoal(progressLevel);
            CandyCosmetics.ApplyHome(this);
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
            RefreshShopPrices();
        }

        private void RefreshShopPrices()
        {
            BearShopCatalog catalog = art.shopCatalog;
            if (catalog == null || catalog.items == null) return;
            for (int i = 0; i < storePrices.Length && i < catalog.items.Length; i++)
            {
                if (storePrices[i] == null) continue;
                BearShopItem item = catalog.items[i];
                int owned = BearCoinShop.Quantity(item.id);
                bool alreadyOwned = !item.consumable && owned > 0;
                bool cosmetic=PlayerDataHandler.IsCosmetic(item.effect);
                bool equipped=cosmetic && PlayerDataHandler.GetEquipment(item.effect)==item.id;
                bool locked=PlayerDataHandler.GetCurrentLevel()<item.unlockLevel;
                storePrices[i].text = locked?"第 "+item.unlockLevel+" 关开放":alreadyOwned ? cosmetic?equipped?"卸下":"装备":"已拥有" : item.coinPrice + " 金币";
                var button = storePrices[i].GetComponentInParent<Button>(true);
                button.interactable = !locked && (alreadyOwned?cosmetic:(!item.consumable || owned<99) && PlayerDataHandler.GetTotalCoin()>=item.coinPrice);
                var inventory = shopProductRows[i].Find("Inventory").GetComponent<Text>();
                inventory.text = locked?"到达第 "+item.unlockLevel+" 关":owned > 0 ? item.consumable ? "旅行包 ×" + owned : equipped?"使用中 · 永久":"已拥有 · 永久" : item.consumable?"消耗品 · 每关限用 1 次":"永久收藏";
            }
        }

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
        private void OnApplicationPause(bool paused) { if(paused) { FlushPreferences(); PlayerPrefs.Save(); } }
        private void OnApplicationQuit() => FlushPreferences();

        private void Claim(int index)
        {
            DailyRewardManager rewards = ServicesManager.Instance.DailyRewardManager;
            if (rewards.TimeRemainsTillNextReward() != 0 || PlayerDataHandler.GetCurrentRewardIndex() != index) return;
            if (index == 0 || index == 4 || index == 5) ServicesManager.Instance.CoinManager.AddTotalCoins(index == 5 ? 200 : index==4?150:100, 0);
            else PlayerDataHandler.GrantShopItem(CandyCosmetics.SupportIds[index==1||index==6?0:index==2?1:2],index==6?2:1);
            rewards.HandleClaimedCurrentDailyReward();
            ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Reward);
            PlayerPrefs.Save();
            Refresh();
            Message("领取成功", new[] { "金币 ×100", "蜂蜜罐 ×1", "糖果锤 ×1", "甜蜜风 ×1", "金币 ×150", "金币 ×200", "蜂蜜罐 ×2" }[index] + "\n金币存入钱包，道具可在关内旅行包使用。");
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
            currentShopTab = Mathf.Clamp(index, 0, 3);
            for (int i = 0; i < shopTabImages.Length; i++)
            {
                bool selected = i == currentShopTab;
                shopTabImages[i].sprite = art.Get(selected ? "Shop/TabSelected" : "Shop/TabNormal");
                shopTabImages[i].rectTransform.sizeDelta = new Vector2(200, selected ? 106 : 82);
            }
            int visible = 0;
            float shelfHeight=0;
            BearShopCatalog catalog = art.shopCatalog;
            for (int i = 0; i < shopProductRows.Length; i++)
            {
                if (shopProductRows[i] == null) continue;
                bool show = currentShopTab == 0 || (int)catalog.items[i].category == currentShopTab - 1;
                shopProductRows[i].gameObject.SetActive(show);
                if (show)
                {
                    shopProductRows[i].anchoredPosition=new Vector2(0,-shelfHeight);
                    shelfHeight+=shopProductRows[i].rect.height+18;visible++;
                }
            }
            shopContent.sizeDelta = new Vector2(825, Mathf.Max(980, shelfHeight-18));
            shopContent.anchoredPosition = Vector2.zero;
            shopContent.parent.GetComponent<ScrollRect>().velocity = Vector2.zero;
            shopEmpty.SetActive(visible == 0);
        }

        private void Buy(int index)
        {
            if(art.shopCatalog==null || !art.shopCatalog.IsValid(index))return;
            var item=art.shopCatalog.items[index];
            if(BearCoinShop.Quantity(item.id)>0 && PlayerDataHandler.ToggleEquipment(item))
            {
                Refresh();
                string location=item.effect==BearShopEffect.Decoration?"回到小屋即可看到桌面摆件。":
                    item.effect==BearShopEffect.BrickSkin?"进入关卡即可看到新的糖砖外观。":"进入关卡即可看到新的小球外观。";
                Message(PlayerDataHandler.GetEquipment(item.effect)==item.id?"已装备":"已卸下",item.title+"\n"+location);
                return;
            }
            BearPurchaseResult result = BearCoinShop.Buy(art.shopCatalog, index);
            Refresh();
            switch (result)
            {
                case BearPurchaseResult.Success:
                    PlayerDataHandler.ToggleEquipment(item);
                    Refresh();
                    ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Reward);
                    Message("兑换成功", item.title + "\n"+(item.consumable?"已放入旅行包，关内点击道具即可使用。":"已自动装备，退出游戏后也会保留。")); break;
                case BearPurchaseResult.AlreadyOwned: Message("已拥有", "这件小物已经在你的收藏里了。"); break;
                case BearPurchaseResult.NotEnoughCoins: Message("金币不足", "去闯关或领取每日奖励，攒够金币再来吧。"); break;
                case BearPurchaseResult.InventoryFull: Message("库存已满", "这件道具的库存已达到上限。"); break;
                case BearPurchaseResult.Locked: Message("继续旅行", "到达第 "+item.unlockLevel+" 关后，这件小物就会开放。"); break;
                default: Message("暂未上架", "这件小物还在准备中。"); break;
            }
        }

        private void OpenLink(string url, string title, string emptyMessage)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && (uri.Scheme == "https" || uri.Scheme == "http")) Application.OpenURL(url);
            else Message(title, emptyMessage);
        }
        private void ShowCoffee()
        {
            Message("", "");
            messageDialog.SetActive(false);
            coffeeDialog.SetActive(true);
        }
        public void Message(string title, string body, Sprite picture = null)
        {
            if(ServicesManager.Instance!=null)ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.UiOpen);
            modal.SetActive(true);
            modal.transform.SetAsLastSibling();
            messageDialog.SetActive(true);
            coffeeDialog.SetActive(false);
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
