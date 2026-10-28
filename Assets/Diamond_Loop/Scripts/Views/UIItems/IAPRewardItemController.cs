using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class IAPRewardItemController : MonoBehaviour
    {
        [SerializeField] private Image itemIcon = null;
        [SerializeField] private Text amountText = null;


        /// <summary>
        /// Init this iap reward item.
        /// </summary>
        /// <param name="reward"></param>
        public void OnInit(ProductReward reward)
        {
            amountText.gameObject.SetActive(true);
            if (reward.RewardType == RewardType.RemoveAds)
                amountText.gameObject.SetActive(false);
            itemIcon.sprite = reward.ProductIcon;
            amountText.text = "x" + reward.Amount.ToString();
        }
    }
}
