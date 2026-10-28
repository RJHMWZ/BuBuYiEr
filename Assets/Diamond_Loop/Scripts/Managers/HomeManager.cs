using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public class HomeManager : MonoBehaviour
    {
        [SerializeField] private Transform[] tiles = null;

        private float tileWidth = 0f;
        private float totalWidth = 0f;
        private float leftEdge = 0f;
        private float rightEdge = 0f;
        private float moveSpeed = 2f;

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


            if (tiles.Length == 0) return;

            // Assumes tiles are evenly spaced — measure gap between first two
            tileWidth = (tiles.Length > 1)
                ? Mathf.Abs(tiles[1].transform.localPosition.x - tiles[0].transform.localPosition.x)
                : 1f;

            totalWidth = tileWidth * tiles.Length;

            // Left/right bounds based on first and last tile positions
            leftEdge = tiles[0].transform.localPosition.x;
            rightEdge = tiles[tiles.Length - 1].transform.localPosition.x;
        }


        private void Update()
        {
            float delta = moveSpeed * Time.deltaTime;
            foreach (var tile in tiles)
            {
                // Move right
                tile.transform.localPosition += new Vector3(delta, 0f, 0f);

                // If tile passed the right edge, wrap it to the left
                if (tile.transform.localPosition.x > rightEdge + tileWidth * 0.5f)
                {
                    tile.transform.localPosition -= new Vector3(totalWidth, 0f, 0f);
                }
            }
        }
    }
}
