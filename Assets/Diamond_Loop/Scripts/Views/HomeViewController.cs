using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class HomeViewController : BaseViewController
    {
        [SerializeField] private RectTransform viewTransform = null;
        [SerializeField] private RectTransform topPanelTrans = null;
        [SerializeField] private RectTransform bottomPanelTrans = null;
        [SerializeField] private RectTransform gameNameTrans = null;
        [SerializeField] private RectTransform playButtonTrans = null;
        [SerializeField] private RectTransform warningSignTrans = null;
        [SerializeField] private RectTransform watchAdButton = null;
        [SerializeField] private RectTransform watchAdTarget = null;
        [SerializeField] private RectTransform totalCoinTarget = null;
        [SerializeField] private Text currentLevelText = null;
        [SerializeField] private Text totalCoinsText = null;
        [SerializeField] private SettingPanelController settingPanel = null;
        [SerializeField] private CoinEffectController coinEffectControllerPrefab = null;

        private List<CoinEffectController> listCoinEffectController = new List<CoinEffectController>();


        /// <summary>
        /// Get an inactive CoinEffectController object.
        /// </summary>
        /// <returns></returns>
        public CoinEffectController GetCoinEffectController()
        {
            //Find in the list
            CoinEffectController coinEffect = listCoinEffectController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (coinEffect == null)
            {
                //Did not find one -> create new one
                coinEffect = Instantiate(coinEffectControllerPrefab, Vector3.zero, Quaternion.identity);
                coinEffect.gameObject.SetActive(false);
                coinEffect.transform.SetParent(transform);
                coinEffect.transform.SetAsLastSibling();
                listCoinEffectController.Add(coinEffect);
            }

            return coinEffect;
        }

        /// <summary>
        /// Create coin effects.
        /// </summary>
        /// <param name="cashAmount"></param>
        /// <param name="initTrans"></param>
        /// <returns></returns>
        public void CreateCoinEffect(int cashAmount, RectTransform initTrans)
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CollectCoin);

            // Limit the number of visual effects to prevent lag
            const int MAX_VISUAL_EFFECTS = 30;
            int visualEffectCount = Mathf.Min(cashAmount, MAX_VISUAL_EFFECTS);

            // Calculate how much each visual effect represents
            int cashPerEffect = Mathf.CeilToInt((float)cashAmount / visualEffectCount);
            int totalCashAdded = 0;

            for (int i = 0; i < visualEffectCount; i++)
            {
                CoinEffectController cashEffect = GetCoinEffectController();
                cashEffect.gameObject.SetActive(true);

                Vector2 firstPos = Vector2.one;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, initTrans.position, null, out firstPos);
                cashEffect.RectTransform.anchoredPosition = firstPos;

                Vector2 startPos = firstPos + (Random.insideUnitCircle * 100f);
                Vector2 finalPos = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, totalCoinTarget.position, null, out finalPos);

                // Calculate cash for this specific effect (handle remainder on last effect)
                int cashForThisEffect = (i == visualEffectCount - 1)
                    ? (cashAmount - totalCashAdded)
                    : cashPerEffect;
                totalCashAdded += cashForThisEffect;

                cashEffect.OnInitCoinRewardEffect(i, startPos, finalPos, () =>
                {
                    ServicesManager.Instance.CoinManager.AddTotalCoins(cashForThisEffect, 0);
                });
            }
        }


        private void CoinManager_TotalCoinChanged(int obj)
        {
            //Update texts
            totalCoinsText.text = obj.ToString();
        }


        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>


        public override void OnShow()
        {
            BearHomeMenuController bearMenu = GetComponent<BearHomeMenuController>();
            if (bearMenu != null && bearMenu.art != null)
            {
                bearMenu.Show(this);
                return;
            }
            MoveRectTransform(topPanelTrans, topPanelTrans.anchoredPosition, new Vector2(topPanelTrans.anchoredPosition.x, -50f), 0.5f);
            MoveRectTransform(bottomPanelTrans, bottomPanelTrans.anchoredPosition, new Vector2(bottomPanelTrans.anchoredPosition.x, 0f), 0.75f);
            ScaleRectTransform(gameNameTrans, Vector2.zero, Vector2.one, 1f);
            ScaleRectTransform(playButtonTrans, Vector2.zero, Vector2.one, 0.5f);
            ServicesManager.Instance.CoinManager.TotalCoinChanged += CoinManager_TotalCoinChanged;

            currentLevelText.text = "Level: " + PlayerDataHandler.GetCurrentLevel().ToString();
            totalCoinsText.text = ServicesManager.Instance.CoinManager.TotalCoins.ToString();


            //Handle warning sign
            int timeRemains = ServicesManager.Instance.DailyRewardManager.TimeRemainsTillNextReward();
            warningSignTrans.gameObject.SetActive((timeRemains == 0) ? true : false);

            //Handle watch ad buton
            watchAdButton.gameObject.SetActive(ServicesManager.Instance.AdManager.IsRewardedAdReady());
        }


        public override void OnClose()
        {
            BearHomeMenuController bearMenu = GetComponent<BearHomeMenuController>();
            if (bearMenu != null && bearMenu.art != null)
            {
                bearMenu.Hide();
                gameObject.SetActive(false);
                return;
            }
            settingPanel.OnHide();
            topPanelTrans.anchoredPosition = new Vector2(topPanelTrans.anchoredPosition.x, 250f);
            bottomPanelTrans.anchoredPosition = new Vector2(bottomPanelTrans.anchoredPosition.x, -500f);
            gameNameTrans.localScale = Vector2.zero;
            playButtonTrans.localScale = Vector3.zero;
            ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
            gameObject.SetActive(false);
        }



        /// <summary>
        /// Callback whehn user closed rewarded ad.
        /// </summary>
        /// <param name="coinAmount"></param>
        public void RewardedAdCallback(int coinAmount)
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Rewarded);
            CreateCoinEffect(coinAmount, watchAdTarget);
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>


        public void OnClickPlayButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            PlayerDataHandler.UpdateTargetLevel(BearSoloProgress.PlayableLevel(PlayerDataHandler.GetCurrentLevel()));
            LoadScene("Ingame", 0.1f);
        }

        public void OnClickLevelsButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            LoadScene("Level", 0.1f);
        }


        public void OnClickRewardButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ViewManager.Instance.OnShowView(ViewType.DAILY_REWARD_VIEW);
        }


        public void OnClickShopButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ViewManager.Instance.OnShowView(ViewType.IAP_VIEW);
        }

        public void OnClickSettingButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            settingPanel.gameObject.SetActive(true);
            settingPanel.OnShow();
        }


        public void OnClickWatchAdButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ServicesManager.Instance.AdManager.ShowRewardedAd(RewardedAdTarget.GET_FREE_COINS);
            watchAdButton.gameObject.SetActive(false);
        }
    }
}
