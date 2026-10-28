using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace ClawbearGames
{

    #region --------------------Ingame Enums
    public enum IngameState
    {
        Ingame_Playing = 0,
        Ingame_Revive = 1,
        Ingame_GameOver = 2,
        Ingame_CompleteLevel = 3,
    }

    public enum PlayerState
    {
        Player_Prepare = 0,
        Player_Living = 1,
        Player_Died = 2,
        Player_CompletedLevel = 3,
    }


    public enum DayType
    {
        DAY_1 = 0,
        DAY_2 = 1,
        DAY_3 = 2,
        DAY_4 = 3,
        DAY_5 = 4,
        DAY_6 = 5,
        DAY_7 = 6,
        DAY_8 = 7,
        DAY_9 = 8,
    }


    public enum RewardType
    {
        Coins = 0,
        RemoveAds = 1,
        Slot = 2,
        Magnet = 3,
        Hammer = 4,
        Key = 5,
        Cooler = 6,
    }


    #endregion



    #region --------------------Ads Enums
    public enum BannerAdType
    {
        NONE = 0,
        ADMOB = 1,
        UNITY = 2,
    }

    public enum InterstitialAdType
    {
        UNITY = 0,
        ADMOB = 1,
    }


    public enum RewardedAdType
    {
        UNITY = 0,
        ADMOB = 1,
    }


    public enum RewardedAdTarget
    {
        GET_FREE_COINS = 0,
        GET_DOUBLE_COIN = 1,
        REVIVE_PLAYER = 2,
    }

    #endregion



    #region --------------------View Enums
    public enum ViewType
    {
        IAP_VIEW = 0,
        HOME_VIEW = 1,
        LEADERBOARD_VIEW = 2,
        DAILY_REWARD_VIEW = 3,
        LOADING_VIEW = 4,
        INGAME_VIEW = 5,
        REVIVE_VIEW = 6,
        ENDGAME_VIEW = 7,
        LEVEL_VIEW = 8,
    }

    #endregion



    #region --------------------Classes

    [System.Serializable]
    public class CameraConfig
    {
        [SerializeField] private int gridWidth = 8;
        public int GridWidth => gridWidth;
        [SerializeField] private float camYPos = 0f;
        public float CamYPos => camYPos;
        [SerializeField] private float camZPos = 0f;
        public float CamZPos => camZPos;
    }



    [System.Serializable]
    public class IAPProduct
    {
        [SerializeField] private bool bestDeal = false;
        public bool BestDeal => bestDeal;

        [SerializeField] private string productName = string.Empty;
        public string ProductName => productName;

        [SerializeField] private string productID = string.Empty;
        public string ProductID => productID;

        [SerializeField] private ProductType productType = ProductType.Consumable;
        public ProductType ProductType => productType;


        [Header("Rewards")]
        [SerializeField] private List<ProductReward> rewards = new List<ProductReward>();
        public List<ProductReward> Rewards => rewards;
    }


    [System.Serializable]
    public class ProductReward
    {
        [SerializeField] private RewardType rewardType;
        public RewardType RewardType => rewardType;

        [SerializeField] private int amount = 0; // For coins, gems, etc.
        public int Amount => amount;

        [SerializeField] private string itemID = string.Empty; // For characters, skins, power-ups etc.
        public string ItemID => itemID;

        [SerializeField] private Sprite productIcon = null;
        public Sprite ProductIcon => productIcon;
    }



    [System.Serializable]
    public class DailyRewardConfiguration
    {
        [SerializeField] private DayType dayType = DayType.DAY_1;

        /// <summary>
        /// the day type of this DailyRewardItem.
        /// </summary>
        public DayType DayType => dayType;


        [SerializeField] private int coinAmount = 0;


        /// <summary>
        /// The amount of coins reward to player.
        /// </summary>
        public int CoinAmount => coinAmount;
    }


    [System.Serializable]
    public class InterstitialAdConfiguration
    {
        [SerializeField] private IngameState ingameStateWhenShowingAd = IngameState.Ingame_CompleteLevel;
        public IngameState IngameStateWhenShowingAd => ingameStateWhenShowingAd;

        [SerializeField] private int ingameStateAmountWhenShowingAd = 3;
        public int IngameStateAmountWhenShowingAd => ingameStateAmountWhenShowingAd;


        [SerializeField] private float delayTimeWhenShowingAd = 2f;
        public float DelayTimeWhenShowingAd => delayTimeWhenShowingAd;

        [SerializeField] private List<InterstitialAdType> listInterstitialAdType = new List<InterstitialAdType>();
        public List<InterstitialAdType> ListInterstitialAdType => listInterstitialAdType;
    }



    public class PlatformParams
    {
        public int CoinItemAmount { private set; get; }
        public void SetCoinItemAmount(int coinItemAmount)
        {
            CoinItemAmount = coinItemAmount;
        }

        public float MagnetItemFrequency { private set; get; }
        public void SetMagnetItemFrequency(float magnetItemFrequency)
        {
            MagnetItemFrequency = magnetItemFrequency;
        }

        public float ShieldItemFrequency { private set; get; }
        public void SetShieldItemFrequency(float shieldItemFrequency)
        {
            ShieldItemFrequency = shieldItemFrequency;
        }
    }


    public class PlayerParams
    {
        public float PlayerMovementSpeed { private set; get; }
        public void SetPlayerMovementSpeed(float playerMovementSpeed)
        {
            PlayerMovementSpeed = playerMovementSpeed;
        }

        public float PlayerJumpingPoints { private set; get; }
        public void SetPlayerJumpingPoints(float playerJumpingPoints)
        {
            PlayerJumpingPoints = playerJumpingPoints;
        }
    }




    public class LeaderboardParams
    {
        public string Username { private set; get; }
        public void SetUsername(string username)
        {
            Username = username;
        }

        public int Level { private set; get; }
        public void SetLevel(int level)
        {
            Level = level;
        }
    }

    public class LeaderboardComparer : IComparer<LeaderboardParams>
    {
        public int Compare(LeaderboardParams dataX, LeaderboardParams dataY)
        {
            if (dataX.Level < dataY.Level)
                return 1;
            if (dataX.Level > dataY.Level)
                return -1;
            else
                return 0;
        }
    }

    #endregion
}
