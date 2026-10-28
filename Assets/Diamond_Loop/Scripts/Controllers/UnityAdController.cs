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
            StartCoroutine(CRShowBannerAd(delay));
        }

        /// <summary>
        /// Hide the current banner ad.
        /// </summary>
        public void HideBannerAd()
        {
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
            Advertisement.Load(InterstitialID, this);
            Advertisement.Load(RewardedID, this);
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Advertisement.Initialize(GameID, false, this);
        }

        #endregion

        #region Load Callbacks

        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (placementId.Equals(InterstitialID))
            {
                isInterstitialAdReady = true;
            }
            else if (placementId.Equals(RewardedID))
            {
                isRewardedVideoAdReady = true;
            }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            if (placementId.Equals(InterstitialID))
            {
                isInterstitialAdReady = false;
            }
            else if (placementId.Equals(RewardedID))
            {
                isRewardedVideoAdReady = false;
            }

            // Reload ads
            Advertisement.Load(InterstitialID, this);
            Advertisement.Load(RewardedID, this);
        }

        #endregion

        #region Show Callbacks

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
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

        #endregion
    }
}