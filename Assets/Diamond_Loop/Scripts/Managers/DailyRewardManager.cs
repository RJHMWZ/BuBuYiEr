using UnityEngine;
using System;
using System.Text;

namespace ClawbearGames
{
    public class DailyRewardManager : MonoBehaviour
    {
        [SerializeField] private DailyRewardConfiguration[] dailyRewardConfigurations = null;
        public DailyRewardConfiguration[] DailyRewardConfigurations { get { return dailyRewardConfigurations; } }
        private string timeOfLastReward = string.Empty;
        private void Start()
        {
            timeOfLastReward = PlayerDataHandler.GetTimeOfLastReward();
        }



        /// <summary>
        /// Get the amount of time remains till next reward.
        /// </summary>
        /// <returns></returns>
        public int TimeRemainsTillNextReward()
        {
            if (string.IsNullOrEmpty(timeOfLastReward))
            {
                return 0;
            }
            else
            {
                //Get the saved date time of latest reward
                string[] dayTimeDatas = timeOfLastReward.Split(':');
                int year = int.Parse(dayTimeDatas[0]);
                int month = int.Parse(dayTimeDatas[1]);
                int day = int.Parse(dayTimeDatas[2]);
                int hour = int.Parse(dayTimeDatas[3]);
                int minute = int.Parse(dayTimeDatas[4]);
                int second = int.Parse(dayTimeDatas[5]);
                DateTime dateTimeOfLatestReward = new DateTime(year, month, day, hour, minute, second);

                TimeSpan timePassed = DateTime.Now.Subtract(dateTimeOfLatestReward);
                int timeRemains = Mathf.Clamp(86400 - ((int)timePassed.TotalSeconds), 0, 86400);

                //User already claimed the last reward and already passed 24 hours
                if (timeRemains == 0 && PlayerDataHandler.GetCurrentRewardIndex() >= dailyRewardConfigurations.Length)
                {
                    //Reset current reward index and reset claimed panel of all daily reward items
                    PlayerDataHandler.UpdateCurrentRewardIndex(0);
                    ViewManager.Instance.DailyRewardViewController.UpdateClaimedPanelOfAllItems();
                }
                return timeRemains;
            }
        }


        /// <summary>
        /// Handle actions when user claimed the current daily reward.
        /// </summary>
        public void HandleClaimedCurrentDailyReward()
        {
            //Set day time values
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(DateTime.Now.Year + ":");
            stringBuilder.Append(DateTime.Now.Month + ":");
            stringBuilder.Append(DateTime.Now.Day + ":");
            stringBuilder.Append(DateTime.Now.Hour + ":");
            stringBuilder.Append(DateTime.Now.Minute + ":");
            stringBuilder.Append(DateTime.Now.Second);
            PlayerDataHandler.UpdateTimeOfLastReward(stringBuilder.ToString().Trim());
            timeOfLastReward = stringBuilder.ToString().Trim();

            //Update current reward index
            PlayerDataHandler.UpdateCurrentRewardIndex(PlayerDataHandler.GetCurrentRewardIndex() + 1);
        }



        /// <summary>
        /// Get the coin amount of current daily reward.
        /// </summary>
        /// <returns></returns>
        public int GetCoinAmountOfCurrentDailyReward()
        {
            int currentIndex = PlayerDataHandler.GetCurrentRewardIndex();
            currentIndex = (currentIndex == dailyRewardConfigurations.Length) ? 0 : currentIndex;
            return dailyRewardConfigurations[currentIndex].CoinAmount;
        }
    }
}
