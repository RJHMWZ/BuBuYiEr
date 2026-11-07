using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace ClawbearGames
{
    public class DailyRewardViewController : BaseViewController
    {
        private void OnDisable()
        {
            if (ServicesManager.Instance != null)
            {
                ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
                ServicesManager.Instance.DailyRewardManager.RewardsReset -= UpdateClaimedPanelOfAllItems;
            }
            StopAllCoroutines();
        }

        [SerializeField] private RectTransform viewTransform = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private RectTransform itemsPanelTrans = null;
        [SerializeField] private RectTransform totalCoinTarget = null;
        [SerializeField] private Text totalCoinsText = null;
        [SerializeField] private DailyRewardItemController dailyRewardItemControllerPrefab = null;
        [SerializeField] private CoinEffectController coinEffectControllerPrefab = null;

        private List<DailyRewardItemController> listDailyRewardItemController = new List<DailyRewardItemController>();
        private List<CoinEffectController> listCoinEffectController = new List<CoinEffectController>();
        private int currentDailyReward = 0;

        /// <summary>
        /// Get an inactive CoinEffectController object.
        /// </summary>
        /// <returns></returns>
        public CoinEffectController GetCoinEffectController()
        {
            //Find in the list
            CoinEffectController coinEffect = null;
            for (int i = 0; i < listCoinEffectController.Count; i++)
            {
                var candidate = listCoinEffectController[i];
                if (candidate != null && !candidate.gameObject.activeSelf) { coinEffect = candidate; break; }
            }

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
        /// Create cash effects.
        /// </summary>
        /// <param name="cashAmount"></param>
        /// <param name="initTrans"></param>
        /// <returns></returns>
        public void CreateCashEffect(int cashAmount, RectTransform initTrans)
        {
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
                    ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CollectCoin);
                    ServicesManager.Instance.CoinManager.AddTotalCoins(cashForThisEffect, 0);
                });
            }
        }


        private void CoinManager_TotalCoinChanged(int obj)
        {
            //Update texts
            totalCoinsText.text = obj.ToString();
        }


        private void Update()
        {
            if (currentDailyReward >= 0 && currentDailyReward < listDailyRewardItemController.Count)
            {
                //Get time rewmains till next reward
                double timeRemains = ServicesManager.Instance.DailyRewardManager.TimeRemainsTillNextReward();
                listDailyRewardItemController[currentDailyReward].TimeRemains = timeRemains;
            }
        }


        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>

        public override void OnShow()
        {
            ServicesManager.Instance.DailyRewardManager.RewardsReset -= UpdateClaimedPanelOfAllItems;
            ServicesManager.Instance.DailyRewardManager.RewardsReset += UpdateClaimedPanelOfAllItems;
            FadeInCanvasGroup(canvasGroup, 0.75f);
            totalCoinsText.text = ServicesManager.Instance.CoinManager.TotalCoins.ToString();
            ServicesManager.Instance.CoinManager.TotalCoinChanged += CoinManager_TotalCoinChanged;

            if (listDailyRewardItemController.Count == 0)
            {
                //Fill the value for the items 
                for (int i = 0; i < ServicesManager.Instance.DailyRewardManager.DailyRewardConfigurations.Length; i++)
                {
                    DailyRewardConfiguration itemConfig = ServicesManager.Instance.DailyRewardManager.DailyRewardConfigurations[i];
                    DailyRewardItemController dailyRewardItemController = Instantiate(dailyRewardItemControllerPrefab, Vector3.zero, Quaternion.identity);
                    dailyRewardItemController.transform.SetParent(itemsPanelTrans);
                    dailyRewardItemController.gameObject.transform.localScale = Vector3.one;
                    dailyRewardItemController.OnSetup(itemConfig.DayType, itemConfig.CoinAmount);
                    listDailyRewardItemController.Add(dailyRewardItemController);
                }
            }


            //Update claimed panel of all items.
            UpdateClaimedPanelOfAllItems();

            //Disable all cash effects
            foreach (CoinEffectController cashEffect in listCoinEffectController)
            {
                cashEffect.gameObject.SetActive(false);
            }
        }

        public override void OnClose()
        {
            if (ServicesManager.Instance != null)
                ServicesManager.Instance.DailyRewardManager.RewardsReset -= UpdateClaimedPanelOfAllItems;
            canvasGroup.alpha = 0f;
            ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
            gameObject.SetActive(false);
        }



        /// <summary>
        /// Reset the claimed panel of all items.
        /// </summary>
        public void UpdateClaimedPanelOfAllItems()
        {
            foreach (DailyRewardItemController o in listDailyRewardItemController)
            {
                o.HandleClaimedPanel();
            }
            currentDailyReward = PlayerDataHandler.GetCurrentRewardIndex();
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>



        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ViewManager.Instance.OnShowView(ViewType.HOME_VIEW);
        }
    }
}
