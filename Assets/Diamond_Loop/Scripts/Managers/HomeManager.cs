using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public class HomeManager : MonoBehaviour
    {
        private void Start()
        {
            //Init player data
            if (!PlayerDataHandler.HasData())
            {
                PlayerData playerData = new PlayerData();
                playerData.RemoveAds = 0;
                playerData.SoundValue = 1f;
                playerData.MusicValue = 1f;
                playerData.CurrentLevel = 1;
                playerData.TargetLevel = 1;
                playerData.WatchedTutorial = 0;
                playerData.CurrentRewardIndex = 0;
                playerData.UserName = string.Empty;
                playerData.TimeOfLastReward = string.Empty;
                playerData.TotalCoins = ServicesManager.Instance.CoinManager.InitialCoins;
                playerData.ListPurchasedProduct = new List<string>();
                playerData.SlotCount = 1;
                playerData.MagnetCount = 1;
                playerData.HammerCount = 1;
                playerData.KeyCount = 1;
                playerData.CoolerCount = 1;
                PlayerDataHandler.SaveData(playerData);
            }

            Application.targetFrameRate = 60;
            ViewManager.Instance.OnShowView(ViewType.HOME_VIEW);
        }
    }
}
