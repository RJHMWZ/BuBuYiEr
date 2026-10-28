using System;
using System.Collections;
using UnityEngine;

#if CB_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

namespace ClawbearGames
{
    /// <summary>
    /// AdMob Controller for Google Mobile Ads Unity Plugin v10.6.0
    /// SDK Versions: Android 24.5.0, iOS 12.12.0
    /// Requires iOS 13.0+
    /// </summary>
    public class AdmobController : MonoBehaviour
    {
#if CB_ADMOB

        [Header("Banner ID")]
#if UNITY_ANDROID
        [SerializeField] private string androidBannerID = "ca-app-pub-1064078647772222/9329609006";
#elif UNITY_IOS
        [SerializeField] private string iOSBannerID = "ca-app-pub-1064078647772222/9329609006";
#endif
        [SerializeField] private AdPosition bannerPosition = AdPosition.Bottom;

        [Header("Interstitial Ad ID")]
#if UNITY_ANDROID
        [SerializeField] private string androidInterstitialAdID = "ca-app-pub-1064078647772222/2139808686";
#elif UNITY_IOS
        [SerializeField] private string iOSInterstitialAdID = "ca-app-pub-1064078647772222/2139808686";
#endif

        [Header("Rewarded Ad ID")]
#if UNITY_ANDROID
        [SerializeField] private string androidRewardedAdID = "ca-app-pub-1064078647772222/9919321234";
#elif UNITY_IOS
        [SerializeField] private string iOSRewardedAdID = "ca-app-pub-1064078647772222/9919321234";
#endif



        private BannerView bannerView = null;
        private InterstitialAd interstitialAd = null;
        private RewardedAd rewardedAd = null;
        
        private int maxRetryAttempts = 3;
        private float retryDelay = 2f;

        private int interstitialRetryCount = 0;
        private int rewardedAdRetryCount = 0;
        private bool isInitialized = false;
        private bool hasEarnedReward = false;

#endif

        private void Start()
        {
#if CB_ADMOB
            if (!isInitialized)
            {
                InitializeAdMob();
            }
#endif
        }

        #region Initialization

#if CB_ADMOB
        /// <summary>
        /// Initialize AdMob SDK
        /// </summary>
        private void InitializeAdMob()
        {
            MobileAds.SetiOSAppPauseOnBackground(true);

            MobileAds.Initialize(initStatus =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    isInitialized = true;
                    LoadInterstitialAd();
                    LoadRewardedAd();
                });
            });
        }
#endif

        #endregion

        #region Banner Ads

        /// <summary>
        /// Load and show a banner ad.
        /// </summary>
        public void ShowBannerAd(float delay)
        {
            StartCoroutine(CRLoadAndShowBanner(delay));
        }

        /// <summary>
        /// Coroutine to load and show banner with delay.
        /// </summary>
        private IEnumerator CRLoadAndShowBanner(float delay)
        {
            yield return new WaitForSeconds(delay);
#if CB_ADMOB
            while (!isInitialized)
            {
                yield return null;
            }

            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
            }

            AdSize adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

#if UNITY_ANDROID
            bannerView = new BannerView(androidBannerID, adSize, bannerPosition);
#elif UNITY_IOS
            bannerView = new BannerView(iOSBannerID, adSize, bannerPosition);
#endif

            bannerView.OnBannerAdLoadFailed += OnBannerLoadFailed;

            AdRequest adRequest = new AdRequest();
            bannerView.LoadAd(adRequest);
#endif
        }

        /// <summary>
        /// Hide the current banner ad.
        /// </summary>
        public void HideBannerAd()
        {
#if CB_ADMOB
            if (bannerView != null)
            {
                bannerView.Hide();
            }
#endif
        }

        /// <summary>
        /// Destroy the current banner ad.
        /// </summary>
        public void DestroyBannerAd()
        {
#if CB_ADMOB
            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
            }
#endif
        }

#if CB_ADMOB
        private void OnBannerLoadFailed(LoadAdError error)
        {
            Debug.LogError($"AdMob: Banner failed to load - {error.GetMessage()}");
        }
#endif

#endregion

        #region Interstitial Ads

        /// <summary>
        /// Check if interstitial ad is ready to show.
        /// </summary>
        public bool IsInterstitialAdReady()
        {
#if CB_ADMOB
            if (!isInitialized)
                return false;

            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                return true;
            }
            else
            {
                if (interstitialRetryCount < maxRetryAttempts)
                {
                    interstitialRetryCount++;
                    LoadInterstitialAd();
                }
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Show interstitial ad with delay.
        /// </summary>
        public void ShowInterstitialAd(float delay)
        {
            StartCoroutine(CRShowInterstitialAd(delay));
        }

        /// <summary>
        /// Coroutine to show interstitial ad.
        /// </summary>
        private IEnumerator CRShowInterstitialAd(float delay)
        {
            yield return new WaitForSeconds(delay);

#if CB_ADMOB
            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                interstitialAd.Show();
            }
            else if (isInitialized)
            {
                LoadInterstitialAd();
            }
#endif
        }

        /// <summary>
        /// Load interstitial ad.
        /// </summary>
        private void LoadInterstitialAd()
        {
#if CB_ADMOB
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            AdRequest adRequest = new AdRequest();

#if UNITY_ANDROID
            string adUnitId = androidInterstitialAdID;
#elif UNITY_IOS
            string adUnitId = iOSInterstitialAdID;
#else
            string adUnitId = "unexpected_platform";
#endif

            InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"AdMob: Interstitial failed to load - {error?.GetMessage()}");
                        
                        if (interstitialRetryCount < maxRetryAttempts)
                        {
                            float retryTime = retryDelay * Mathf.Pow(2, interstitialRetryCount);
                            interstitialRetryCount++;
                            StartCoroutine(CRRetryLoadInterstitial(retryTime));
                        }
                        return;
                    }

                    interstitialAd = ad;
                    interstitialRetryCount = 0;

                    interstitialAd.OnAdFullScreenContentClosed += () =>
                    {
                        MobileAdsEventExecutor.ExecuteInUpdate(() =>
                        {
                            LoadInterstitialAd();
                        });
                    };

                    interstitialAd.OnAdFullScreenContentFailed += (AdError adError) =>
                    {
                        Debug.LogError($"AdMob: Interstitial failed to show - {adError.GetMessage()}");
                        LoadInterstitialAd();
                    };
                });
            });

#endif
        }


        /// <summary>
        /// Coroutine retry load interstitial ad.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRRetryLoadInterstitial(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadInterstitialAd();
        }

#endregion

        #region Rewarded Ads

        /// <summary>
        /// Check if rewarded ad is ready to show.
        /// </summary>
        public bool IsRewardedAdReady()
        {
#if CB_ADMOB
            if (!isInitialized)
                return false;

            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                return true;
            }
            else
            {
                if (rewardedAdRetryCount < maxRetryAttempts)
                {
                    rewardedAdRetryCount++;
                    LoadRewardedAd();
                }
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Show rewarded ad with delay.
        /// </summary>
        public void ShowRewardedAd(float delay)
        {
            StartCoroutine(CRShowRewardedAd(delay));
        }

        /// <summary>
        /// Coroutine to show rewarded ad.
        /// </summary>
        private IEnumerator CRShowRewardedAd(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

#if CB_ADMOB
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                hasEarnedReward = false;
                rewardedAd.Show((Reward reward) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        // User watched the full ad and earned the reward
                        hasEarnedReward = true;
                    });
                });
            }
            else if (isInitialized)
            {
                if (rewardedAdRetryCount < maxRetryAttempts)
                {
                    rewardedAdRetryCount++;
                    LoadRewardedAd();
                }
            }
#endif
        }

        /// <summary>
        /// Load rewarded ad.
        /// </summary>
        private void LoadRewardedAd()
        {
#if CB_ADMOB
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            AdRequest adRequest = new AdRequest();

#if UNITY_ANDROID
            string adUnitId = androidRewardedAdID;
#elif UNITY_IOS
            string adUnitId = iOSRewardedAdID;
#else
            string adUnitId = "unexpected_platform";
#endif

            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"AdMob: Rewarded ad failed to load - {error?.GetMessage()}");
                        
                        if (rewardedAdRetryCount < maxRetryAttempts)
                        {
                            float retryTime = retryDelay * Mathf.Pow(2, rewardedAdRetryCount);
                            rewardedAdRetryCount++;
                            StartCoroutine(CRRetryLoadRewarded(retryTime));
                        }
                        return;
                    }

                    rewardedAd = ad;
                    rewardedAdRetryCount = 0;

                    rewardedAd.OnAdFullScreenContentClosed += () =>
                    {
                        MobileAdsEventExecutor.ExecuteInUpdate(() =>
                        {
                            // Ad closed - grant reward if user earned it
                            ServicesManager.Instance.AdManager.OnRewardedAdClosed(hasEarnedReward);
                            LoadRewardedAd();
                        });
                    };

                    rewardedAd.OnAdFullScreenContentFailed += (AdError adError) =>
                    {
                        MobileAdsEventExecutor.ExecuteInUpdate(() =>
                        {
                            Debug.LogError($"AdMob: Rewarded ad failed to show - {adError.GetMessage()}");
                            ServicesManager.Instance.AdManager.OnRewardedAdClosed(false);
                            LoadRewardedAd();
                        });
                    };
                });
            });
#endif
        }


        /// <summary>
        /// Coroutine retry load rewarded ad.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRRetryLoadRewarded(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadRewardedAd();
        }

#endregion

        #region Cleanup

        private void OnDestroy()
        {
#if CB_ADMOB
            if (bannerView != null)
            {
                bannerView.Destroy();
            }
            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
            }
            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
            }
#endif
        }

        #endregion

    }
}