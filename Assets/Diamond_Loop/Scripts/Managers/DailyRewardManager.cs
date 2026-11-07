using UnityEngine;
using System;
using System.Globalization;

namespace ClawbearGames
{
    public class DailyRewardManager : MonoBehaviour
    {
        [SerializeField] private DailyRewardConfiguration[] dailyRewardConfigurations = null;
        public DailyRewardConfiguration[] DailyRewardConfigurations { get { return dailyRewardConfigurations; } }
        private string timeOfLastReward;
        private DateTime parsedRewardTime;
        private bool hasRewardTime;
        public event Action RewardsReset;
        private void Start()
        {
            RefreshRewardTime();
        }



        /// <summary>
        /// Get the amount of time remains till next reward.
        /// </summary>
        /// <returns></returns>
        public int TimeRemainsTillNextReward()
        {
            RefreshRewardTime();
            if (!hasRewardTime) return 0;
            TimeSpan passed = DateTime.Now.Subtract(parsedRewardTime);
            int remaining = Mathf.Clamp(86400 - (int)passed.TotalSeconds, 0, 86400);
            if (remaining == 0 && dailyRewardConfigurations != null &&
                PlayerDataHandler.GetCurrentRewardIndex() >= dailyRewardConfigurations.Length)
            {
                PlayerDataHandler.UpdateCurrentRewardIndex(0);
                RewardsReset?.Invoke();
            }
            return remaining;
        }

        private void RefreshRewardTime()
        {
            string saved = PlayerDataHandler.GetTimeOfLastReward();
            if (saved == timeOfLastReward) return;
            timeOfLastReward = saved;
            hasRewardTime = DateTime.TryParseExact(saved, "yyyy:M:d:H:m:s", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out parsedRewardTime);
        }

        /// <summary>
        /// Handle actions when user claimed the current daily reward.
        /// </summary>
        public void HandleClaimedCurrentDailyReward()
        {
            DateTime now = DateTime.Now;
            PlayerDataHandler.UpdateTimeOfLastReward(now.ToString("yyyy:M:d:H:m:s", CultureInfo.InvariantCulture));
            RefreshRewardTime();

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
