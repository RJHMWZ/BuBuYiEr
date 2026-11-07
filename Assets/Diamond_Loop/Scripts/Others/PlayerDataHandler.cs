using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public class PlayerDataHandler
    {
        public const string PLAYER_DATA = "CBGAMES_PLAYER_DATA";
        private static PlayerData cachedData;
        private static string cachedJson;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            cachedData = null;
            cachedJson = null;
        }

        private static PlayerData ReadData()
        {
            if (cachedData == null) LoadData(PlayerPrefs.GetString(PLAYER_DATA));
            return cachedData;
        }

        private static void LoadData(string json)
        {
            cachedData = string.IsNullOrEmpty(json) ? new PlayerData() : JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
            cachedData.ListStarData ??= new List<StarData>();
            cachedData.ListPurchasedProduct ??= new List<string>();
            cachedJson = json;
        }


        /// <summary>
        /// Check if the player data is initialized/saved.
        /// </summary>
        /// <returns></returns>
        public static bool HasData()
        {
            if (!PlayerPrefs.HasKey(PLAYER_DATA)) { ResetCache(); return false; }
            // ???????????????? getter ???????? JSON?
            string json = PlayerPrefs.GetString(PLAYER_DATA);
            if (cachedData == null || json != cachedJson) LoadData(json);
            return true;
        }

        /// <summary>
        /// Convert the given player data object then save to the PlayerPrefs.
        /// </summary>
        /// <param name="playerData"></param>
        public static void SaveData(PlayerData playerData)
        {
            if (playerData == null) throw new ArgumentNullException(nameof(playerData));
            string json = JsonUtility.ToJson(playerData);
            PlayerPrefs.SetString(PLAYER_DATA, json);
            // ??????????????????????????????
            if (!ReferenceEquals(cachedData, playerData)) LoadData(json);
            else cachedJson = json;
        }

        /// <summary>
        /// Enable remove ads index data (user wont see ads). 
        /// </summary>
        public static void EnableRemoveAds()
        {
            PlayerData playerData = ReadData();
            playerData.RemoveAds = 1;
            SaveData(playerData);
        }



        /// <summary>
        /// Check if remove ads index is set.
        /// </summary>
        /// <returns></returns>
        public static bool IsRemovedAds()
        {
            PlayerData playerData = ReadData();
            return playerData.RemoveAds == 1;
        }



        /// <summary>
        /// Get the current level of the player.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentLevel()
        {
            PlayerData playerData = ReadData();
            return playerData.CurrentLevel;
        }

        /// <summary>
        /// Update the current level of the player data.
        /// </summary>
        /// <param name="newLevel"></param>
        public static void UpdateCurrentLevel(int newLevel)
        {
            PlayerData playerData = ReadData();
            playerData.CurrentLevel = newLevel;
            SaveData(playerData);
        }


        /// <summary>
        /// Get the target level of the player.
        /// </summary>
        /// <returns></returns>
        public static int GetTargetLevel()
        {
            PlayerData playerData = ReadData();
            return playerData.TargetLevel;
        }

        /// <summary>
        /// Update the target level of the player data.
        /// </summary>
        /// <param name="newLevel"></param>
        public static void UpdateTargetLevel(int newLevel)
        {
            PlayerData playerData = ReadData();
            playerData.TargetLevel = newLevel;
            SaveData(playerData);
        }






        /// <summary>
        /// Get the user name of the player.
        /// </summary>
        /// <returns></returns>
        public static string GetUserName()
        {
            PlayerData playerData = ReadData();
            return playerData.UserName;
        }

        /// <summary>
        /// Update the user name of the player.
        /// </summary>
        /// <param name="userName"></param>
        public static void UpdateUserName(string userName)
        {
            PlayerData playerData = ReadData();
            playerData.UserName = userName;
            SaveData(playerData);
        }






        /// <summary>
        /// Get the total coins of the player data.
        /// </summary>
        /// <returns></returns>
        public static int GetTotalCoin() => ReadData().TotalCoins;

        /// <summary>
        /// Update the total coins of the player data.
        /// </summary>
        /// <param name="newCoin"></param>
        public static void UpdateTotalCoin(int newCoin)
        {
            PlayerData playerData = ReadData();
            playerData.TotalCoins = newCoin;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the sound value.
        /// </summary>
        /// <returns></returns>
        public static float GetSoundValue()
        {
            PlayerData playerData = ReadData();
            return playerData.SoundValue;
        }


        /// <summary>
        /// Update the sound value.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateSoundValue(float value)
        {
            PlayerData playerData = ReadData();
            playerData.SoundValue = value;
            SaveData(playerData);
        }


        /// <summary>
        /// Get the music value.
        /// </summary>
        /// <returns></returns>
        public static float GetMusicValue()
        {
            PlayerData playerData = ReadData();
            return playerData.MusicValue;
        }


        /// <summary>
        /// Update the music value.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateMusicValue(float value)
        {
            PlayerData playerData = ReadData();
            playerData.MusicValue = value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the fighterIndex of current daily reward.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentRewardIndex() => ReadData().CurrentRewardIndex;

        /// <summary>
        /// Update the current reward fighterIndex of the player data.
        /// </summary>
        /// <param name="newIndex"></param>
        public static void UpdateCurrentRewardIndex(int newIndex)
        {
            PlayerData playerData = ReadData();
            playerData.CurrentRewardIndex = newIndex;
            SaveData(playerData);
        }






        /// <summary>
        /// Get the time of last reward.
        /// </summary>
        /// <returns></returns>
        public static string GetTimeOfLastReward()
        {
            if (cachedData == null && !HasData()) { return string.Empty; }
            PlayerData playerData = ReadData();
            return playerData.TimeOfLastReward;
        }

        /// <summary>
        /// Update time of last reward the player data.
        /// </summary>
        /// <param name="timeOfLastReward"></param>
        public static void UpdateTimeOfLastReward(string timeOfLastReward)
        {
            PlayerData playerData = ReadData();
            playerData.TimeOfLastReward = timeOfLastReward;
            SaveData(playerData);
        }




        /// <summary>
        /// Check if the tutorial is watched.
        /// </summary>
        /// <returns></returns>
        public static bool IsWatchedTutorial()
        {
            PlayerData playerData = ReadData();
            return playerData.WatchedTutorial == 1;
        }

        /// <summary>
        /// Update watched tutorial.
        /// </summary>
        public static void UpdateWatchedTutorial()
        {
            PlayerData playerData = ReadData();
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
            PlayerData playerData = ReadData();
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
            PlayerData playerData = ReadData();
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
            PlayerData playerData = ReadData();
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
            PlayerData playerData = ReadData();
            return playerData.ListPurchasedProduct.Contains(productId);
        }



        /// <summary>
        /// Get the current slot count.
        /// </summary>
        /// <returns></returns>
        public static int GetSlotCount()
        {
            PlayerData playerData = ReadData();
            return playerData.SlotCount;
        }


        /// <summary>
        /// Add to SlotCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateSlotCount(int value)
        {
            PlayerData playerData = ReadData();
            playerData.SlotCount += value;
            SaveData(playerData);
        }


        /// <summary>
        /// Get the MagnetCount count.
        /// </summary>
        /// <returns></returns>
        public static int GetMagnetCount()
        {
            PlayerData playerData = ReadData();
            return playerData.MagnetCount;
        }


        /// <summary>
        /// Add to MagnetCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateMagnetCount(int value)
        {
            PlayerData playerData = ReadData();
            playerData.MagnetCount += value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the hammer count.
        /// </summary>
        /// <returns></returns>
        public static int GetHammerCount()
        {
            PlayerData playerData = ReadData();
            return playerData.HammerCount;
        }


        /// <summary>
        /// Add to HammerCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateHammerCount(int value)
        {
            PlayerData playerData = ReadData();
            playerData.HammerCount += value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the KeyCount count.
        /// </summary>
        /// <returns></returns>
        public static int GetKeyCount()
        {
            PlayerData playerData = ReadData();
            return playerData.KeyCount;
        }


        /// <summary>
        /// Add to KeyCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateKeyCount(int value)
        {
            PlayerData playerData = ReadData();
            playerData.KeyCount += value;
            SaveData(playerData);
        }



        /// <summary>
        /// Get the CoolerCount count.
        /// </summary>
        /// <returns></returns>
        public static int GetCoolerCount()
        {
            PlayerData playerData = ReadData();
            return playerData.CoolerCount;
        }


        /// <summary>
        /// Add to CoolerCount an amount.
        /// </summary>
        /// <param name="value"></param>
        public static void UpdateCoolerCountt(int value)
        {
            PlayerData playerData = ReadData();
            playerData.CoolerCount += value;
            SaveData(playerData);
        }

 
    }
}
