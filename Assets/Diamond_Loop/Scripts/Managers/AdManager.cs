using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public class AdManager : MonoBehaviour
    {

        [Header("Banner Ad Configuration")]
        [SerializeField] private BannerAdType bannerAdType = BannerAdType.NONE;
        [SerializeField] private float delayTimeWhenShowingBannerAd = 0.5f;


        [Header("Interstitial Ad Configuration")]
        [SerializeField] private List<InterstitialAdConfiguration> listInterstitialAdConfiguration = new List<InterstitialAdConfiguration>();

        [Header("Rewarded Ad Configuration")]
        [SerializeField] private float delayTimeWhenShowingRewardedAd = 0.25f;
        [SerializeField] private float delayTimeWhenRewardingCoins = 0.5f;
        [SerializeField] private List<RewardedAdType> listRewardedAdType = new List<RewardedAdType>();

        [Header("AdManager References")]
        [SerializeField] private AdmobController admobController = null;
        [SerializeField] private UnityAdController unityAdController = null;

        private List<int> listIngameStateAmount = new List<int>();
        private RewardedAdType readyAdType = RewardedAdType.UNITY;
        private RewardedAdTarget rewardedAdTarget = RewardedAdTarget.GET_FREE_COINS;

        private bool isCalledback = false;
        private bool isRewarded = false;
        private void OnEnable()
        {
            IngameManager.IngameStateChanged += IngameManager_IngameStateChanged;
        }

        private void OnDisable()
        {
            IngameManager.IngameStateChanged -= IngameManager_IngameStateChanged;
        }


        private IEnumerator Start()
        {
            foreach (InterstitialAdConfiguration o in listInterstitialAdConfiguration)
            {
                listIngameStateAmount.Add(o.IngameStateAmountWhenShowingAd);
            }

            yield return null;
            if (!PlayerDataHandler.IsRemovedAds())
            {
                //Show banner ad
                if (bannerAdType == BannerAdType.ADMOB)
                {
                    admobController.ShowBannerAd(delayTimeWhenShowingBannerAd);
                }
                else if (bannerAdType == BannerAdType.UNITY)
                {
                    unityAdController.ShowBannerAd(delayTimeWhenShowingBannerAd);
                }
            }
        }

        private void Update()
        {
            if (isCalledback)
            {
                isCalledback = false;
                if (isRewarded)
                {
                    if (rewardedAdTarget == RewardedAdTarget.GET_FREE_COINS)
                    {
                        //Reward coins
                        int rewardedCoins = ServicesManager.Instance.CoinManager.GetRewardedCoins();
                        ViewManager.Instance.HomeViewController.RewardedAdCallback(rewardedCoins);
                    }
                    else if (rewardedAdTarget == RewardedAdTarget.GET_DOUBLE_COIN)
                    {
                        //Double collected coins
                        int collectedCoins = ServicesManager.Instance.CoinManager.CollectedCoins;
                        ServicesManager.Instance.CoinManager.AddCollectedCoins(collectedCoins, delayTimeWhenRewardingCoins);
                    }
                    else if (rewardedAdTarget == RewardedAdTarget.REVIVE_PLAYER)
                    {
                        IngameManager.Instance.SetContinueGame(); //Revive 
                    }
                }
                else if (rewardedAdTarget == RewardedAdTarget.REVIVE_PLAYER)
                {
                    ViewManager.Instance.ReviveViewController.OnRecoveryAdClosed(false);
                }
            }
        }



        /// <summary>
        /// Get calls everytime IngameState changed.
        /// </summary>
        /// <param name="ingameState"></param>
        private void IngameManager_IngameStateChanged(IngameState ingameState)
        {
            if (IngameManager.Instance != null && IngameManager.Instance.CurrentLevel <= 3) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.IsPreviewLevel) return;
#endif
            for (int i = 0; i < listIngameStateAmount.Count; i++)
            {
                if (listInterstitialAdConfiguration[i].IngameStateWhenShowingAd == ingameState)
                {
                    //Count down the number of detected IngameState 
                    listIngameStateAmount[i]--;

                    //The number of detected IngameState already counted down to 0 -> check the priority to show interstitial ad.
                    if (listIngameStateAmount[i] == 0)
                    {
                        //Reset IngameStateNumber
                        listIngameStateAmount[i] = listInterstitialAdConfiguration[i].IngameStateAmountWhenShowingAd;

                        //Stop if already removed ads
                        if (PlayerDataHandler.IsRemovedAds()) return;

                        for (int a = 0; a < listInterstitialAdConfiguration[i].ListInterstitialAdType.Count; a++)
                        {
                            InterstitialAdType type = listInterstitialAdConfiguration[i].ListInterstitialAdType[a];
                            if (type == InterstitialAdType.ADMOB && admobController.IsInterstitialAdReady())
                            {
                                admobController.ShowInterstitialAd(listInterstitialAdConfiguration[i].DelayTimeWhenShowingAd);
                                break;
                            }
                            else if (type == InterstitialAdType.UNITY && unityAdController.IsInterstitialAdReady())
                            {
                                unityAdController.ShowInterstitialAd(listInterstitialAdConfiguration[i].DelayTimeWhenShowingAd);
                                break;
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Hide the banner ad.
        /// </summary>
        public void HideBannerAd()
        {
            if (bannerAdType == BannerAdType.ADMOB)
            {
                admobController.HideBannerAd();
            }
            else if (bannerAdType == BannerAdType.UNITY)
            {
                unityAdController.HideBannerAd();
            }
        }



        /// <summary>
        /// Determines whether the rewarded ad is ready.
        /// </summary>
        /// <returns></returns>
        public bool IsRewardedAdReady()
        {
            for (int i = 0; i < listRewardedAdType.Count; i++)
            {
                if (listRewardedAdType[i] == RewardedAdType.UNITY && unityAdController.IsRewardedAdReady())
                {
                    readyAdType = RewardedAdType.UNITY;
                    return true;
                }
                else if (listRewardedAdType[i] == RewardedAdType.ADMOB && admobController.IsRewardedAdReady())
                {
                    readyAdType = RewardedAdType.ADMOB;
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Show the rewarded ad with given RewardedAdTarget.
        /// </summary>
        /// <param name="adTarget"></param>
        public void ShowRewardedAd(RewardedAdTarget adTarget)
        {
            rewardedAdTarget = adTarget;
            if (readyAdType == RewardedAdType.UNITY)
            {
                unityAdController.ShowRewardedAd(delayTimeWhenShowingRewardedAd);
            }
            else if (readyAdType == RewardedAdType.ADMOB)
            {
                admobController.ShowRewardedAd(delayTimeWhenShowingRewardedAd);
            }
        }




        /// <summary>
        /// Handle actions when rewarded ad was closed.
        /// </summary>
        /// <param name="isCompleted"></param>
        public void OnRewardedAdClosed(bool isCompleted)
        {
            isCalledback = true;
            isRewarded = isCompleted;
        }
    }
}
