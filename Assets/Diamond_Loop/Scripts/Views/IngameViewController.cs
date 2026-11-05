using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class IngameViewController : BaseViewController
    {
        [SerializeField] private BearMenuArt themeArt = null;
        public BearMenuArt ThemeArt => themeArt;
        private bool themeBuilt;
        private BearPausePanel pausePanel;
        private BearSoloHud soloHud;
        private BearToolDialog toolDialog;
        public bool IsToolPanelOpen => boostPanel.gameObject.activeSelf || slotPanel.gameObject.activeSelf || messagePanel.gameObject.activeSelf ||
            (toolDialog != null && toolDialog.IsOpen);
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
            CoinEffectController coinEffect = null;
            foreach (CoinEffectController candidate in listCoinEffect)
                if (!candidate.gameObject.activeSelf) { coinEffect = candidate; break; }
            if (coinEffect == null && listCoinEffect.Count >= 12) return null;

            if (coinEffect == null)
            {
                //Did not find one -> create new one
                coinEffect = Instantiate(coinEffectPrefab, Vector3.zero, Quaternion.identity);
                coinEffect.transform.SetParent(transform);
                coinEffect.transform.SetAsLastSibling();
                coinEffect.gameObject.SetActive(false);
                foreach (Graphic graphic in coinEffect.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                Image coin = coinEffect.GetComponentInChildren<Image>(true);
                if (coin != null && themeArt != null) { coin.sprite = themeArt.Get("IconCoin"); coin.color = Color.white; }
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
            if (!themeBuilt && themeArt != null)
            {
                BearGameTheme.BuildIngame(this, themeArt, topPanelTrans, levelText, coinText, coinTargetTrans,
                    starPanel, new[] { slotButton, magnetButton, hammerButton, keyButton, coolerButton }, messagePanel);
                tutorialPanel.ApplyBearTheme(themeArt);
                themeBuilt = true;
            }
            if (pausePanel == null)
            {
                pausePanel = gameObject.AddComponent<BearPausePanel>();
                pausePanel.Initialize(this, themeArt);
            }
            if (soloHud == null && themeArt != null)
            {
                soloHud = gameObject.AddComponent<BearSoloHud>();
                soloHud.Initialize(this, themeArt);
            }
            soloHud?.HideIntroduction();
            toolDialog?.Close();
            pausePanel.ResetPanel();
            starPanel.gameObject.SetActive(true);
            if (!themeBuilt)
                MoveRectTransform(topPanelTrans, topPanelTrans.anchoredPosition, new Vector2(topPanelTrans.anchoredPosition.x, -70f), 0.5f);
            ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
            ServicesManager.Instance.CoinManager.TotalCoinChanged += CoinManager_TotalCoinChanged;
            boostPanel.gameObject.SetActive(false);
            messagePanel.gameObject.SetActive(false);
            slotPanel.gameObject.SetActive(false);

            //Update texts and other fields, parameters
            levelText.text = "第 " + IngameManager.Instance.CurrentLevel + " 关";
            coinText.text = ServicesManager.Instance.CoinManager.TotalCoins.ToString();
            if (!IngameManager.Instance.IsRevived)
            {

            }

            if (!themeBuilt && !PlayerDataHandler.IsWatchedTutorial())
            {
                tutorialPanel.gameObject.SetActive(true);
                tutorialPanel.OnShow();
            }
            else { tutorialPanel.gameObject.SetActive(false); }
        }

        public override void OnClose()
        {
            if (ServicesManager.Instance != null)
                ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
            topPanelTrans.anchoredPosition = new Vector2(topPanelTrans.anchoredPosition.x, 250f);
            boostPanel.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void ApplyWorldTheme(IngameManager manager) => BearGameTheme.ApplyWorld(manager, themeArt);
        public void ShowRoundIntro() => soloHud?.ShowIntroduction();
        public void ShowHint(string message) => soloHud?.ShowHint(message);


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
            if (themeBuilt) BearGameTheme.LayoutAvailableTools(transform, new[] { slotButton, magnetButton, hammerButton, keyButton, coolerButton });
        }


        /// <summary>
        /// Show the boost panel to purchase boosts.
        /// </summary>
        /// <param name="boostButton"></param>
        public void ShowBoostPanel(BoostButtonController boostButton)
        {
            if (themeArt != null)
            {
                EnsureToolDialog();
                toolDialog.Show(boostButton, false);
                return;
            }
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
            if (updateBoost && currentButton != null)
                currentButton.RemoveOneBoost();
            ShowHint(updateBoost ? "小帮手已生效，继续收集糖果吧" : "没有找到合适的目标，道具没有消耗");
        }



        /// <summary>
        /// Show the slot panel
        /// </summary>
        /// <param name="active"></param>
        public void ShowSlotPanel(bool active)
        {
            slotButton.UpdateCurrentAmount();
            if (themeArt != null)
            {
                EnsureToolDialog();
                if (active) toolDialog.Show(slotButton, true);
                else toolDialog.Close();
                return;
            }
            if (active)
            {
                slotPanel.gameObject.SetActive(true);
                slotPanel.OnShow();
            }
            else { slotPanel.gameObject.SetActive(false); }
        }

        private void EnsureToolDialog()
        {
            if (toolDialog != null) return;
            toolDialog = gameObject.AddComponent<BearToolDialog>();
            toolDialog.Initialize(this, themeArt);
        }


        /// <summary>
        /// Create the coin effect.
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="amount"></param>
        public void CreateCoinEffect(Vector3 worldPos, int amount)
        {
            if (amount <= 0 || IngameManager.Instance == null || IngameManager.Instance.IngameState != IngameState.Ingame_Playing) return;
            // Commit the reward before starting decorative effects, so closing a view cannot lose coins.
            ServicesManager.Instance.CoinManager.AddTotalCoins(amount, 0);
            ServicesManager.Instance.CoinManager.AddCollectedCoins(amount, 0);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CollectCoin);
            Vector2 firstPos, finalPos;
            Camera camera = IngameManager.Instance.CameraParent.MainCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, camera.WorldToScreenPoint(worldPos), null, out firstPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, coinTargetTrans.position, null, out finalPos);
            for (int i = 0; i < Mathf.Min(amount, 6); i++)
            {
                CoinEffectController effect = GetCoinEffectController();
                if (effect == null) break;
                effect.gameObject.SetActive(true);
                effect.RectTransform.anchoredPosition = firstPos;
                effect.OnInitCoinRewardEffect(i, firstPos + Random.insideUnitCircle * 65f, finalPos);
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
            if (cashEffect == null) return;
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
