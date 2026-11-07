using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;

namespace ClawbearGames
{
    /// <summary>
    /// Unity Ads Controller for Advertisement Legacy Package v4.16.4
    /// Note: This package is functional but no longer receives updates.
    /// Consider migrating to Unity LevelPlay for future projects.
    /// </summary>
    public class UnityAdController : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
#if UNITY_IOS
        [SerializeField] private string iOSGameID = "5426904";
#else
        [SerializeField] private string androidGameID = "5426905";
#endif

#if UNITY_IOS
        [Space(10)]
        [SerializeField] private string iOSBannerID = "Banner_iOS";
        [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
#else
        [Space(10)]
        [SerializeField] private string androidBannerID = "Banner_Android";
        [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
#endif

#if UNITY_IOS
        [Space(10)]
        [SerializeField] private string iOSInterstitialID = "Interstitial_iOS";
#else
        [Space(10)]
        [SerializeField] private string androidInterstitialID = "Interstitial_Android";
#endif

#if UNITY_IOS
        [Space(10)]
        [SerializeField] private string iOSRewardedID = "Rewarded_iOS";
#else        
        [Space(10)]
        [SerializeField] private string androidRewardedID = "Rewarded_Android";
#endif

        private string GameID
        {
            get
            {
#if UNITY_IOS
                return iOSGameID;
#else
                return androidGameID;
#endif
            }
        }

        private string BannerID
        {
            get
            {
#if UNITY_IOS
                return iOSBannerID;
#else
                return androidBannerID;
#endif
            }
        }

        private string InterstitialID
        {
            get
            {
#if UNITY_IOS
                return iOSInterstitialID;
#else
                return androidInterstitialID;
#endif
            }
        }

        private string RewardedID
        {
            get
            {
#if UNITY_IOS
                return iOSRewardedID;
#else
                return androidRewardedID;
#endif
            }
        }

        private bool isInterstitialAdReady = false;
        private bool isRewardedVideoAdReady = false;
        private const float RetryDelay = 5f;
        private Coroutine bannerRoutine, initializationRetry, interstitialRetry, rewardedRetry;
        private enum RetryKind { Initialization, Interstitial, Rewarded }

        private void Start()
        {
            isInterstitialAdReady = false;
            isRewardedVideoAdReady = false;

            // Initialize with test mode = false
            // For testing, set to true
            Advertisement.Initialize(GameID, false, this);
        }

        #region Banner Ads

        /// <summary>
        /// Show the banner ad with given delay time.
        /// </summary>
        public void ShowBannerAd(float delay)
        {
            if(bannerRoutine!=null)StopCoroutine(bannerRoutine);
            bannerRoutine=StartCoroutine(CRShowBannerAd(delay));
        }

        /// <summary>
        /// Hide the current banner ad.
        /// </summary>
        public void HideBannerAd()
        {
            if(bannerRoutine!=null){StopCoroutine(bannerRoutine);bannerRoutine=null;}
            Advertisement.Banner.Hide();
        }

        /// <summary>
        /// Coroutine wait for banner ready then show.
        /// </summary>
        private IEnumerator CRShowBannerAd(float delay)
        {
            float timer = 0;
            yield return new WaitForSeconds(delay);

            while (!Advertisement.Banner.isLoaded)
            {
                yield return null;
                timer += Time.deltaTime;
                if (timer >= 5f)
                {
                    timer = 0;
                    Advertisement.Banner.Load(BannerID);
                }
            }

            Advertisement.Banner.SetPosition(bannerPosition);
            Advertisement.Banner.Show(BannerID);
            bannerRoutine=null;
        }

        #endregion

        #region Interstitial Ads

        /// <summary>
        /// Determine whether the interstitial ad is ready
        /// </summary>
        public bool IsInterstitialAdReady()
        {
            return isInterstitialAdReady;
        }

        /// <summary>
        /// Show interstitial ad with given delay time
        /// </summary>
        public void ShowInterstitialAd(float delay)
        {
            StartCoroutine(CRShowInterstitial(delay));
        }

        /// <summary>
        /// Coroutine show interstitial ad.
        /// </summary>
        private IEnumerator CRShowInterstitial(float delay)
        {
            yield return new WaitForSeconds(delay);
            Advertisement.Show(InterstitialID, this);
        }

        #endregion

        #region Rewarded Ads

        /// <summary>
        /// Determine whether the rewarded ad is ready.
        /// </summary>
        public bool IsRewardedAdReady()
        {
            return isRewardedVideoAdReady;
        }

        /// <summary>
        /// Show rewarded video with given delay time.
        /// </summary>
        public void ShowRewardedAd(float delay)
        {
            StartCoroutine(CRShowRewardedAd(delay));
        }

        /// <summary>
        /// Coroutine show the rewarded ad with delay.
        /// </summary>
        private IEnumerator CRShowRewardedAd(float delay)
        {
            yield return new WaitForSeconds(delay);
            Advertisement.Show(RewardedID, this);
        }

        #endregion

        #region Initialization Callbacks

        public void OnInitializationComplete()
        {
            if(this==null || !isActiveAndEnabled)return;
            if(initializationRetry!=null){StopCoroutine(initializationRetry);initializationRetry=null;}
            Advertisement.Load(InterstitialID, this);
            Advertisement.Load(RewardedID, this);
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            if(this==null || !isActiveAndEnabled)return;
            if(initializationRetry==null)initializationRetry=StartCoroutine(Retry(RetryKind.Initialization));
        }

        #endregion

        #region Load Callbacks

        public void OnUnityAdsAdLoaded(string placementId)
        {
            if(this==null || !isActiveAndEnabled)return;
            if (placementId.Equals(InterstitialID))
            {
                isInterstitialAdReady = true;
                if(interstitialRetry!=null){StopCoroutine(interstitialRetry);interstitialRetry=null;}
            }
            else if (placementId.Equals(RewardedID))
            {
                isRewardedVideoAdReady = true;
                if(rewardedRetry!=null){StopCoroutine(rewardedRetry);rewardedRetry=null;}
            }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            if(this==null || !isActiveAndEnabled)return;
            if (placementId.Equals(InterstitialID))
            {
                isInterstitialAdReady = false;
            }
            else if (placementId.Equals(RewardedID))
            {
                isRewardedVideoAdReady = false;
            }

            // Reload ads
            if(interstitialRetry==null)interstitialRetry=StartCoroutine(Retry(RetryKind.Interstitial));
            if(rewardedRetry==null)rewardedRetry=StartCoroutine(Retry(RetryKind.Rewarded));
        }

        #endregion

        #region Show Callbacks

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            if(this==null || !isActiveAndEnabled || ServicesManager.Instance==null)return;
            isInterstitialAdReady = false;
            isRewardedVideoAdReady = false;

            if (showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
            {
                if (placementId.Equals(RewardedID))
                {
                    ServicesManager.Instance.AdManager.OnRewardedAdClosed(true);
                    Advertisement.Load(RewardedID, this);
                }
                else
                {
                    Advertisement.Load(InterstitialID, this);
                }
            }
            else
            {
                if (placementId.Equals(RewardedID))
                {
                    ServicesManager.Instance.AdManager.OnRewardedAdClosed(false);
                    Advertisement.Load(RewardedID, this);
                }
                else
                {
                    Advertisement.Load(InterstitialID, this);
                }
            }
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            if(this==null || !isActiveAndEnabled)return;
            isInterstitialAdReady = false;
            isRewardedVideoAdReady = false;

            if (placementId.Equals(RewardedID))
            {
                Advertisement.Load(RewardedID, this);
            }
            else
            {
                Advertisement.Load(InterstitialID, this);
            }
        }

        public void OnUnityAdsShowStart(string placementId) { }

        public void OnUnityAdsShowClick(string placementId) { }

        private IEnumerator Retry(RetryKind kind)
        {
            // 失败回调可能连续到达，每种请求最多保留一个延迟重试。
            yield return new WaitForSecondsRealtime(RetryDelay);
            if(kind==RetryKind.Initialization)
            {
                initializationRetry=null;
                Advertisement.Initialize(GameID,false,this);
            }
            else if(kind==RetryKind.Interstitial)
            {
                interstitialRetry=null;
                Advertisement.Load(InterstitialID,this);
            }
            else
            {
                rewardedRetry=null;
                Advertisement.Load(RewardedID,this);
            }
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            bannerRoutine=initializationRetry=interstitialRetry=rewardedRetry=null;
        }

        #endregion
    }
}
