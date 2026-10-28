using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class IngameViewController : BaseViewController
    {
        [SerializeField] private RectTransform viewTransform = null;
        [SerializeField] private RectTransform topPanelTrans = null;
        [SerializeField] private RectTransform coinTargetTrans = null;
        [SerializeField] private Text levelText = null;
        [SerializeField] private Text coinText = null;
        [SerializeField] private BoostButtonController slotButton = null;
        [SerializeField] private BoostButtonController magnetButton = null;
        [SerializeField] private BoostButtonController hammerButton = null;
        [SerializeField] private BoostButtonController keyButton = null;
        [SerializeField] private BoostButtonController coolerButton = null;
        [SerializeField] private BoostPanelController boostPanel = null;
        [SerializeField] private SlotPanelController slotPanel = null;
        [SerializeField] private MessagePanelController messagePanel = null;
        [SerializeField] private StarPanelController starPanel = null;
        [SerializeField] private CoinEffectController coinEffectPrefab = null;
        [SerializeField] private TutorialPanelController tutorialPanel = null;

        public MessagePanelController MessagePanel => messagePanel;
        public StarPanelController StarPanel => starPanel;

        private List<CoinEffectController> listCoinEffect = new List<CoinEffectController>();
        private BoostButtonController currentButton = null;


        /// <summary>
        /// Get an inactive CoinEffectController object.
        /// </summary>
        /// <returns></returns>
        public CoinEffectController GetCoinEffectController()
        {
            //Find in the list
            CoinEffectController coinEffect = listCoinEffect.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (coinEffect == null)
            {
                //Did not find one -> create new one
                coinEffect = Instantiate(coinEffectPrefab, Vector3.zero, Quaternion.identity);
                coinEffect.transform.SetParent(transform);
                coinEffect.transform.SetAsLastSibling();
                coinEffect.gameObject.SetActive(false);
                listCoinEffect.Add(coinEffect);
            }

            return coinEffect;
        }

        private void CoinManager_TotalCoinChanged(int obj)
        {
            coinText.text = obj.ToString();
        }

        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>


        public override void OnShow()
        {
            starPanel.gameObject.SetActive(true);
            MoveRectTransform(topPanelTrans, topPanelTrans.anchoredPosition, new Vector2(topPanelTrans.anchoredPosition.x, -70f), 0.5f);
            ServicesManager.Instance.CoinManager.TotalCoinChanged += CoinManager_TotalCoinChanged;
            messagePanel.gameObject.SetActive(false);
            slotPanel.gameObject.SetActive(false);

            //Update texts and other fields, parameters
            levelText.text = "Level: " + IngameManager.Instance.CurrentLevel.ToString();
            coinText.text = ServicesManager.Instance.CoinManager.TotalCoins.ToString();
            if (!IngameManager.Instance.IsRevived)
            {

            }

            if (!PlayerDataHandler.IsWatchedTutorial())
            {
                tutorialPanel.gameObject.SetActive(true);
                tutorialPanel.OnShow();
            }
            else { tutorialPanel.gameObject.SetActive(false); }
        }

        public override void OnClose()
        {
            topPanelTrans.anchoredPosition = new Vector2(topPanelTrans.anchoredPosition.x, 250f);
            boostPanel.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }


        /// <summary>
        /// Init this boost buttons.
        /// </summary>
        /// <param name="levelData"></param>
        public void InitBoostButtons(LevelData levelData)
        {
            slotButton.OnInit(levelData.GetPowerUpData(PowerUpType.Slot));
            magnetButton.OnInit(levelData.GetPowerUpData(PowerUpType.Magnet));
            hammerButton.OnInit(levelData.GetPowerUpData(PowerUpType.Hammer));
            keyButton.OnInit(levelData.GetPowerUpData(PowerUpType.Key));
            coolerButton.OnInit(levelData.GetPowerUpData(PowerUpType.Cooler));
        }


        /// <summary>
        /// Show the boost panel to purchase boosts.
        /// </summary>
        /// <param name="boostButton"></param>
        public void ShowBoostPanel(BoostButtonController boostButton)
        {
            boostPanel.gameObject.SetActive(true);
            boostPanel.OnShow(boostButton);
        }


        /// <summary>
        /// Show the message panel (for Hammer and Rocket type).
        /// </summary>
        /// <param name="boostButton"></param>
        public void ShowMessagePanel(BoostButtonController boostButton)
        {
            currentButton = boostButton;
            messagePanel.gameObject.SetActive(true);
            messagePanel.OnShow(currentButton.PowerUpType);
        }


        /// <summary>
        /// Close the message panel after use.
        /// </summary>
        /// <param name="updateBoost"></param>
        public void CloseMessagePanel(bool updateBoost)
        {
            messagePanel.gameObject.SetActive(false);
            if (updateBoost)
                currentButton.RemoveOneBoost();
        }



        /// <summary>
        /// Show the slot panel
        /// </summary>
        /// <param name="active"></param>
        public void ShowSlotPanel(bool active)
        {
            slotButton.UpdateCurrentAmount();
            if (active)
            {
                slotPanel.gameObject.SetActive(true);
                slotPanel.OnShow();
            }
            else { slotPanel.gameObject.SetActive(false); }
        }


        /// <summary>
        /// Create the coin effect.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="amount"></param>
        public void CreateCoinEffect(Vector3 worldPos, int amount)
        {
            // Limit the number of visual effects to prevent lag
            const int MAX_VISUAL_EFFECTS = 30;
            int visualEffectCount = Mathf.Min(amount, MAX_VISUAL_EFFECTS);

            // Calculate how much each visual effect represents
            int cashPerEffect = Mathf.CeilToInt((float)amount / visualEffectCount);
            int totalCashAdded = 0;

            for (int i = 0; i < visualEffectCount; i++)
            {
                CoinEffectController cashEffect = GetCoinEffectController();
                cashEffect.gameObject.SetActive(true);

                Vector2 firstPos = Vector2.zero;
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, screenPos, null, out firstPos);
                cashEffect.RectTransform.anchoredPosition = firstPos;

                Vector2 startPos = firstPos + (Random.insideUnitCircle * 100f);
                Vector2 finalPos = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, coinTargetTrans.position, null, out finalPos);

                // Calculate cash for this specific effect (handle remainder on last effect)
                int cashForThisEffect = (i == visualEffectCount - 1)
                    ? (amount - totalCashAdded)
                    : cashPerEffect;
                totalCashAdded += cashForThisEffect;

                cashEffect.OnInitCoinRewardEffect(i, startPos, finalPos, () =>
                {
                    ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CollectCoin);
                    ServicesManager.Instance.CoinManager.AddTotalCoins(cashForThisEffect, 0);
                });
            }
        }



        /// <summary>
        /// Create minus coin effect when user click on purchase button.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="buttonPos"></param>
        public void CreateMinusCoinEffect(int amount, Vector2 buttonPos)
        {
            CoinEffectController cashEffect = GetCoinEffectController();
            cashEffect.transform.SetParent(viewTransform);
            cashEffect.transform.localScale = Vector3.one;
            Vector2 localPos = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, buttonPos, null, out localPos);
            cashEffect.RectTransform.anchoredPosition = localPos;
            cashEffect.gameObject.SetActive(true);
            cashEffect.OnInitCoinUpdateEffect(localPos + Vector2.up * 200f, amount);
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>


    }
}
