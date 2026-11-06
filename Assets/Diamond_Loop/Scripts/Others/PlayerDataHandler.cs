using UnityEngine;

namespace ClawbearGames
{
    public class PlayerDataHandler
    {
        public const string PLAYER_DATA = "CBGAMES_PLAYER_DATA";
        private static int CurrentTotalCoin = -1;
        private static int CurrentRewardIndex = -1;


        /// <summary>
        /// Check if the player data is initialized/saved.
        /// </summary>
        /// <returns></returns>
        public static bool HasData()
        {
            if (PlayerPrefs.HasKey(PLAYER_DATA))
            {
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
                CurrentTotalCoin = playerData.TotalCoins;
                CurrentRewardIndex = playerData.CurrentRewardIndex;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Convert the given player data object then save to the PlayerPrefs.
        /// </summary>
        /// <param name="playerData"></param>
        public static void SaveData(PlayerData playerData)
        {
            string json = JsonUtility.ToJson(playerData);
            PlayerPrefs.SetString(PLAYER_DATA, json);
            CurrentTotalCoin = playerData.TotalCoins;
            CurrentRewardIndex = playerData.CurrentRewardIndex;
        }



        /// <summary>
        /// Enable remove ads index data (user wont see ads). 
        /// </summary>
        public static void EnableRemoveAds()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.RemoveAds = 1;
            SaveData(playerData);
        }



        /// <summary>
        /// Check if remove ads index is set.
        /// </summary>
        /// <returns></returns>
        public static bool IsRemovedAds()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.RemoveAds == 1;
        }



        /// <summary>
        /// Get the current level of the player.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentLevel()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.CurrentLevel;
        }

        /// <summary>
        /// Update the current level of the player data.
        /// </summary>
        /// <param name="newLevel"></param>
        public static void UpdateCurrentLevel(int newLevel)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.CurrentLevel = newLevel;
            SaveData(playerData);
        }


        /// <summary>
        /// Get the target level of the player.
        /// </summary>
        /// <returns></returns>
        public static int GetTargetLevel()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.TargetLevel;
        }

        /// <summary>
        /// Update the target level of the player data.
        /// </summary>
        /// <param name="newLevel"></param>
        public static void UpdateTargetLevel(int newLevel)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.TargetLevel = newLevel;
            SaveData(playerData);
        }






        /// <summary>
        /// Get the user name of the player.
        /// </summary>
        /// <returns></returns>
        public static string GetUserName()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.UserName;
        }

        /// <summary>
        /// Update the user name of the player.
        /// </summary>
        /// <param name="userName"></param>
        public static void UpdateUserName(string userName)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.UserName = userName;
            SaveData(playerData);
        }






        /// <summary>
        /// Get the total coins of the player data.
        /// </summary>
        /// <returns></returns>
        public static int GetTotalCoin()
        {
            if (CurrentTotalCoin < 0)
            {
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
                CurrentTotalCoin = playerData.TotalCoins;
            }
            return CurrentTotalCoin;
        }

        /// <summary>
        /// Update the total coins of the player data.
        /// </summary>
        /// <param name="newCoin"></param>
        public static void UpdateTotalCoin(int newCoin)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.TotalCoins = newCoin;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the sound value.
        /// </summary>
        /// <returns></returns>
        public static float GetSoundValue()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.SoundValue;
        }


        /// <summary>
        /// Update the sound value.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateSoundValue(float value)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.SoundValue = value;
            SaveData(playerData);
        }


        /// <summary>
        /// Get the music value.
        /// </summary>
        /// <returns></returns>
        public static float GetMusicValue()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.MusicValue;
        }


        /// <summary>
        /// Update the music value.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateMusicValue(float value)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.MusicValue = value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the fighterIndex of current daily reward.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentRewardIndex()
        {
            if (CurrentRewardIndex < 0)
            {
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
                CurrentRewardIndex = playerData.CurrentRewardIndex;
            }
            return CurrentRewardIndex;
        }

        /// <summary>
        /// Update the current reward fighterIndex of the player data.
        /// </summary>
        /// <param name="newIndex"></param>
        public static void UpdateCurrentRewardIndex(int newIndex)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.CurrentRewardIndex = newIndex;
            CurrentRewardIndex = newIndex;
            SaveData(playerData);
        }






        /// <summary>
        /// Get the time of last reward.
        /// </summary>
        /// <returns></returns>
        public static string GetTimeOfLastReward()
        {
            if (!HasData()) { return string.Empty; }
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.TimeOfLastReward;
        }

        /// <summary>
        /// Update time of last reward the player data.
        /// </summary>
        /// <param name="timeOfLastReward"></param>
        public static void UpdateTimeOfLastReward(string timeOfLastReward)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.TimeOfLastReward = timeOfLastReward;
            SaveData(playerData);
        }




        /// <summary>
        /// Check if the tutorial is watched.
        /// </summary>
        /// <returns></returns>
        public static bool IsWatchedTutorial()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.WatchedTutorial == 1 ? true : false;
        }

        /// <summary>
        /// Update watched tutorial.
        /// </summary>
        public static void UpdateWatchedTutorial()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.WatchedTutorial = 1;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the star of the given level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int GetStar(int level)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            foreach (StarData data in playerData.ListStarData)
            {
                if (data.Level == level) { return data.Star; }
            }
            return 0;
        }


        /// <summary>
        /// Update the star of the given level.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="star"></param>
        public static void UpdateStar(int level, int star)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            bool exist = false;
            foreach (StarData data in playerData.ListStarData)
            {
                if (data.Level == level)
                {
                    exist = true;
                    data.Star = Mathf.Max(data.Star, star);
                    break;
                }
            }
            if (!exist)
            {
                playerData.ListStarData.Add(new StarData(level, star));
            }
            SaveData(playerData);
        }



        /// <summary>
        /// Update the iap product if that already purchased (non-consumable).
        /// </summary>
        /// <param name="productId"></param>
        public static void UpdatePurchasedProduct(string productId)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            if (!playerData.ListPurchasedProduct.Contains(productId))
            {
                playerData.ListPurchasedProduct.Add(productId);
                SaveData(playerData);
            }
        }


        /// <summary>
        /// Update the iap product if that already purchased (non-consumable).
        /// </summary>
        /// <param name="productId"></param>
        public static bool IsPurchasedProduct(string productId)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.ListPurchasedProduct.Contains(productId);
        }



        /// <summary>
        /// Get the current slot count.
        /// </summary>
        /// <returns></returns>
        public static int GetSlotCount()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.SlotCount;
        }


        /// <summary>
        /// Add to SlotCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateSlotCount(int value)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.SlotCount += value;
            SaveData(playerData);
        }


        /// <summary>
        /// Get the MagnetCount count.
        /// </summary>
        /// <returns></returns>
        public static int GetMagnetCount()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.MagnetCount;
        }


        /// <summary>
        /// Add to MagnetCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateMagnetCount(int value)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.MagnetCount += value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the hammer count.
        /// </summary>
        /// <returns></returns>
        public static int GetHammerCount()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.HammerCount;
        }


        /// <summary>
        /// Add to HammerCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateHammerCount(int value)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.HammerCount += value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the KeyCount count.
        /// </summary>
        /// <returns></returns>
        public static int GetKeyCount()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.KeyCount;
        }


        /// <summary>
        /// Add to KeyCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateKeyCount(int value)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.KeyCount += value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the CoolerCount count.
        /// </summary>
        /// <returns></returns>
        public static int GetCoolerCount()
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            return playerData.CoolerCount;
        }


        /// <summary>
        /// Add to CoolerCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateCoolerCountt(int value)
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PLAYER_DATA));
            playerData.CoolerCount += value;
            SaveData(playerData);
        }

 
    }
}