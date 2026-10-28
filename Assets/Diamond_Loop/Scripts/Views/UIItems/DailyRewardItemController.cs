using UnityEngine;
using UnityEngine.UI;


namespace ClawbearGames
{
    public class DailyRewardItemController : MonoBehaviour
    {
        [Header("Daily Reward Item References")]
        [SerializeField] private Text rewardDayTxt = null;
        [SerializeField] private Text rewardedCoinTxt = null;
        [SerializeField] private Text countdownText = null;
        [SerializeField] private RectTransform claimTarget = null;
        [SerializeField] private GameObject claimButton = null;
        [SerializeField] private GameObject claimedPanel = null;


        public double TimeRemains
        {
            set
            {
                if (value > 0)
                {
                    claimButton.gameObject.SetActive(false);
                    countdownText.gameObject.SetActive(true);
                    countdownText.text = Utilities.SecondsToTimeFormat(value);
                }
                else
                {
                    countdownText.gameObject.SetActive(false);
                    claimButton.gameObject.SetActive(true);
                }
            }
        }



        /// <summary>
        /// Setup this daily reward item.
        /// </summary>
        /// <param name="dayType"></param>
        /// <param name="rewardedCoins"></param>
        public void OnSetup(DayType dayType, int rewardedCoins)
        {
            rewardDayTxt.text = ("DAY " + dayType.ToString().Split('_')[1]).ToUpper();
            rewardedCoinTxt.text = rewardedCoins.ToString();
            countdownText.gameObject.SetActive(false);
            claimButton.gameObject.SetActive(false);
        }


        /// <summary>
        /// Active or deactive claimed panel of this item.
        /// </summary>
        public void HandleClaimedPanel()
        {
            int itemIndex = int.Parse(rewardDayTxt.text[rewardDayTxt.text.Length - 1].ToString()) - 1;
            int currentDailyRewardIndex = PlayerDataHandler.GetCurrentRewardIndex();
            claimedPanel.SetActive((itemIndex < currentDailyRewardIndex) ? true : false);
        }



        public void OnClickClaimButton()
        {
            //Create cash effect and play sound
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            int amount = ServicesManager.Instance.DailyRewardManager.GetCoinAmountOfCurrentDailyReward();
            ViewManager.Instance.DailyRewardViewController.CreateCashEffect(amount, claimTarget);

            //Claim the reward of the current daily reward item.
            ServicesManager.Instance.DailyRewardManager.HandleClaimedCurrentDailyReward();

            //Update claimed panel of all items
            ViewManager.Instance.DailyRewardViewController.UpdateClaimedPanelOfAllItems();
        }
    }
}
