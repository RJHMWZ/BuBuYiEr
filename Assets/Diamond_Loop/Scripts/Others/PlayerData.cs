using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    [System.Serializable]
    public class PlayerData
    {
        public int RemoveAds = 0;
        public int CurrentLevel = 1;
        public int TargetLevel = 1;
        public int TotalCoins = 500;
        public float SoundValue = 1f;
        public float MusicValue = 1f;
        public int WatchedTutorial = 0;
        public int CurrentRewardIndex = 0;
        public string UserName = string.Empty;
        public string TimeOfLastReward = string.Empty;
        public List<StarData> ListStarData = new List<StarData>();
        public List<string> ListPurchasedProduct = new List<string>();
        public int SlotCount = 0;
        public int MagnetCount = 0;
        public int HammerCount = 0;
        public int KeyCount = 0;
        public int CoolerCount = 0;
    }

    [System.Serializable]
    public class StarData
    {
        public int Level = 1;
        public int Star = 1;

        public StarData(int level, int star)
        {
            Level = level;
            Star = star;
        }

        public StarData() { }
    }
}