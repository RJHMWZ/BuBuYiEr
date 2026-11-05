using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class BoostButtonController : MonoBehaviour
    {
        [SerializeField] private PowerUpType powerUpType = PowerUpType.Slot;
        [SerializeField] private RectTransform amountTextTrans = null;
        [SerializeField] private RectTransform plusButtonTrans = null;
        [SerializeField] private RectTransform blockedPanel = null;
        [SerializeField] private RectTransform bottomPanel = null;
        [SerializeField] private Text amountText = null;
        [SerializeField] private Text levelText = null;
        [SerializeField] private Sprite boostIcon = null;


        public PowerUpType PowerUpType => powerUpType;
        public bool IsUnlocked => unlocked;
        public int PurchasePrice => purchasePrice;
        public Sprite BoostIcon => boostIcon;

        private int currentAmount = 0;
        private int purchasePrice = 0;
        private bool unlocked = true;

        public void ApplyBearTheme(BearMenuArt art)
        {
            Image background = GetComponent<Image>();
            background.sprite = art.Get("BtnBg");
            background.color = Color.white;
            background.type = Image.Type.Sliced;
            Transform legacyIcon = transform.Find("ButtonIcon");
            if (legacyIcon != null) legacyIcon.gameObject.SetActive(false);
            BearIllustration illustrated = BearIllustration.Create(transform, "IllustratedTool", BearIllustration.Tool(powerUpType), 17, 4, 130, 124);
            illustrated.transform.SetSiblingIndex(1);
            foreach (Shadow shadow in GetComponentsInChildren<Shadow>(true)) shadow.enabled = false;
            Image badge = bottomPanel.GetComponent<Image>();
            if (badge != null)
            {
                badge.sprite = art.Get("TopUIBg");
                badge.color = Color.white;
                badge.type = Image.Type.Sliced;
            }
            Image blocked = blockedPanel.GetComponent<Image>();
            if (blocked != null)
            {
                blocked.sprite = art.Get("BtnBg");
                blocked.type = Image.Type.Sliced;
                blocked.color = new Color(.90f, .85f, .76f, .97f);
            }
            foreach (Image icon in blockedPanel.GetComponentsInChildren<Image>(true))
                if (icon != blocked) icon.enabled = false;
            BearIllustration.Create(blockedPanel, "IllustratedLock", BearDrawing.Lock, 49, 13, 65, 65);
            Image plus = plusButtonTrans.GetComponent<Image>();
            if (plus != null)
            {
                plus.sprite = art.Get("IconAdd");
                plus.color = Color.white;
            }
            BearGameTheme.StyleText(amountText, art, 28, true);
            BearGameTheme.StyleText(levelText, art, 27, true);
        }


        /// <summary>
        /// Update the current boost amount.
        /// </summary>
        public void UpdateCurrentAmount()
        {
            if (PowerUpType == PowerUpType.Slot)
                currentAmount = PlayerDataHandler.GetSlotCount();
            else if (PowerUpType == PowerUpType.Hammer)
                currentAmount = PlayerDataHandler.GetHammerCount();
            else if (PowerUpType == PowerUpType.Key)
                currentAmount = PlayerDataHandler.GetKeyCount();
            else if (PowerUpType == PowerUpType.Cooler)
                currentAmount = PlayerDataHandler.GetCoolerCount();
            else if (PowerUpType == PowerUpType.Magnet)
                currentAmount = PlayerDataHandler.GetMagnetCount();
            if (currentAmount > 0)
            {
                amountText.text = currentAmount.ToString();
                amountTextTrans.gameObject.SetActive(true);
                plusButtonTrans.gameObject.SetActive(false);
            }
            else
            {
                amountTextTrans.gameObject.SetActive(false);
                plusButtonTrans.gameObject.SetActive(true);
            }
        }


        /// <summary>
        /// Init this boost button.
        /// </summary>
        /// <param name="data"></param>
        public void OnInit(PowerUpData data)
        {
            purchasePrice = data.cost;
            unlocked = IngameManager.Instance.CurrentLevel >= data.unlockLevel;
            UpdateCurrentAmount();

            if (IngameManager.Instance.CurrentLevel < data.unlockLevel)
            {
                bottomPanel.gameObject.SetActive(false);
                blockedPanel.gameObject.SetActive(true);
                levelText.text = "Lv." + data.unlockLevel.ToString();
            }
            else
            {
                blockedPanel.gameObject.SetActive(false);
                bottomPanel.gameObject.SetActive(true);
            }
        }



        /// <summary>
        /// Add an amount of boost to this boost button (after purchased)
        /// </summary>
        /// <param name="amount"></param>
        public void AddBoostAmount(int amount)
        {
            if (PowerUpType == PowerUpType.Slot)
                PlayerDataHandler.UpdateSlotCount(amount);
            else if (PowerUpType == PowerUpType.Hammer)
                PlayerDataHandler.UpdateHammerCount(amount);
            else if (PowerUpType == PowerUpType.Key)
                PlayerDataHandler.UpdateKeyCount(amount);
            else if (PowerUpType == PowerUpType.Cooler)
                PlayerDataHandler.UpdateCoolerCountt(amount);
            else if (PowerUpType == PowerUpType.Magnet)
                PlayerDataHandler.UpdateMagnetCount(amount);
            UpdateCurrentAmount();
        }


        /// <summary>
        /// Remove1 boost to this boost button (after used)
        /// </summary>
        public void RemoveOneBoost()
        {
            if (PowerUpType == PowerUpType.Slot)
                PlayerDataHandler.UpdateSlotCount(-1);
            else if (PowerUpType == PowerUpType.Hammer)
                PlayerDataHandler.UpdateHammerCount(-1);
            else if (PowerUpType == PowerUpType.Key)
                PlayerDataHandler.UpdateKeyCount(-1);
            else if (PowerUpType == PowerUpType.Cooler)
                PlayerDataHandler.UpdateCoolerCountt(-1);
            else if (PowerUpType == PowerUpType.Magnet)
                PlayerDataHandler.UpdateMagnetCount(-1);
            UpdateCurrentAmount();
        }


        public void OnClick()
        {
            if (!unlocked || IngameManager.Instance == null || IngameManager.Instance.IsPaused ||
                !IngameManager.Instance.IsRoundStarted || IngameManager.Instance.StopControl ||
                IngameManager.Instance.IngameState != IngameState.Ingame_Playing) return;
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            if (plusButtonTrans.gameObject.activeSelf)
            {
                //Show boost panel to purchase
                ViewManager.Instance.IngameViewController.ShowBoostPanel(this);
            }
            else
            {
                if (powerUpType == PowerUpType.Slot)
                    ViewManager.Instance.IngameViewController.ShowSlotPanel(true);
                else
                    ViewManager.Instance.IngameViewController.ShowMessagePanel(this);
            }
        }
    }
}
