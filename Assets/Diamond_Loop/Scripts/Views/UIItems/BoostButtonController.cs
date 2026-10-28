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
        public int PurchasePrice => purchasePrice;
        public Sprite BoostIcon => boostIcon;

        private int currentAmount = 0;
        private int purchasePrice = 0;


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
