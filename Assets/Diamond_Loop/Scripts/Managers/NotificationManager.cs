using UnityEngine;
using System.Collections;
using System;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace ClawbearGames
{

    public class NotificationManager : MonoBehaviour
    {
#if UNITY_ANDROID || UNITY_IOS
        [Header("Daily Notification Configuration")]
        [SerializeField] private string dailyNotificationTitle = "Hey. What's up !!!";
        [SerializeField] private string dailyNotificationText = "Come back. There are new levels !!!";
        [SerializeField] private int fireTime = 86400;

        [Header("Daily Reward Notification Configuration")]
        [SerializeField] private string dailyRewardNotificationTitle = "Hey. What's up !!!";
        [SerializeField] private string dailyRewardNotificationText = "Your reward is ready. Claim it now !!!";

        private string dailyNotificationChanelID = "daily_notification_chanel_id";
        private string dailyRewardNotificationChanelID = "reward_coins_notification_chanel_id";
#endif

#if UNITY_IOS
        private bool isNotificationRequestDone = false;
#endif

        private void Start()
        {
#if UNITY_ANDROID

            //Remove notifications that already been displayed.
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            AndroidNotificationCenter.CancelAllScheduledNotifications();

            //Create a Android Notification chanel to send the message though.
            var dailyNotificationChannel = new AndroidNotificationChannel()
            {
                Id = dailyNotificationChanelID,
                Name = "Daily Notification Channel",
                Importance = Importance.Default,
                Description = "Generic Notifications",
            };
            AndroidNotificationCenter.RegisterNotificationChannel(dailyNotificationChannel);

            var rewardCoinsNotificationChannel = new AndroidNotificationChannel()
            {
                Id = dailyRewardNotificationChanelID,
                Name = "Rewar Coins Notification Channel",
                Importance = Importance.Default,
                Description = "Generic Notifications",
            };
            AndroidNotificationCenter.RegisterNotificationChannel(rewardCoinsNotificationChannel);

#elif UNITY_IOS

            //Reset parameters
            isNotificationRequestDone = false;

            //Cancel all the sheduled and delivered notifications
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
            iOSNotificationCenter.RemoveAllScheduledNotifications();

            //Request the notification from iOS
            StartCoroutine(CRRequestingIOSNotification());
#endif
        }


#if UNITY_IOS
        private IEnumerator CRRequestingIOSNotification()
        {
            //Request notification rights from iOS devices
            var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
            using (var req = new AuthorizationRequest(authorizationOption, true))
            {
                while (!req.IsFinished)
                {
                    yield return null;
                };
                isNotificationRequestDone = true;
            }
        }
#endif

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
#if UNITY_ANDROID

                //Cancel all the scheduled and displayed notifications
                AndroidNotificationCenter.CancelAllDisplayedNotifications();
                AndroidNotificationCenter.CancelAllScheduledNotifications();

                //Create the daily notification that going to be sent and send that notification.
                var androidDailyNotification = new AndroidNotification();
                androidDailyNotification.Title = dailyNotificationTitle;
                androidDailyNotification.Text = dailyNotificationText;
                androidDailyNotification.LargeIcon = "icon";
                androidDailyNotification.SmallIcon = "icon";
                androidDailyNotification.FireTime = DateTime.Now.AddSeconds(fireTime);
                AndroidNotificationCenter.SendNotification(androidDailyNotification, dailyNotificationChanelID);


                //Create the daily reward notification that going to be sent and send that notification.
                var androidDailyRewardNotification = new AndroidNotification();
                androidDailyRewardNotification.Title = dailyRewardNotificationTitle;
                androidDailyRewardNotification.Text = dailyRewardNotificationText;
                androidDailyRewardNotification.LargeIcon = "icon";
                androidDailyRewardNotification.SmallIcon = "icon";
                androidDailyRewardNotification.FireTime = DateTime.Now.AddSeconds(Mathf.Clamp(ServicesManager.Instance.DailyRewardManager.TimeRemainsTillNextReward(), 30, 86400));
                AndroidNotificationCenter.SendNotification(androidDailyRewardNotification, dailyRewardNotificationChanelID);

#elif UNITY_IOS

                if (isNotificationRequestDone)
                {
                    //Cancel all the scheduled and delivered notifications
                    iOSNotificationCenter.RemoveAllDeliveredNotifications();
                    iOSNotificationCenter.RemoveAllScheduledNotifications();

                    //Handle daily notification TimeSpan
                    var dailyNotificationTimeTrigger = new iOSNotificationTimeIntervalTrigger()
                    {
                        TimeInterval = new TimeSpan(0, 0, fireTime),
                        Repeats = false
                    };

                    //Send new daily notification
                    var iOSDailyNotification = new iOSNotification()
                    {
                        // You can specify a custom identifier which can be used to manage the notification later.
                        // If you don't provide one, a unique string will be generated automatically.
                        Identifier = dailyNotificationChanelID,
                        Title = dailyNotificationTitle,
                        Body = dailyNotificationText,
                        Subtitle = dailyNotificationTitle,
                        ShowInForeground = true,
                        ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                        CategoryIdentifier = "category_a",
                        ThreadIdentifier = "thread1",
                        Trigger = dailyNotificationTimeTrigger,
                    };
                    iOSNotificationCenter.ScheduleNotification(iOSDailyNotification);



                    //Handle daily reward notification TimeSpan
                    var dailyRewardNotificationTimeTrigger = new iOSNotificationTimeIntervalTrigger()
                    {                    
                        TimeInterval = new TimeSpan(0, 0, Mathf.Clamp(ServicesManager.Instance.DailyRewardManager.TimeRemainsTillNextReward(), 30, 86400)),
                        Repeats = false
                    };

                    //Send new daily reward notification
                    var iOSDailyRewardNotification = new iOSNotification()
                    {
                        // You can specify a custom identifier which can be used to manage the notification later.
                        // If you don't provide one, a unique string will be generated automatically.
                        Identifier = dailyRewardNotificationChanelID,
                        Title = dailyRewardNotificationTitle,
                        Body = dailyRewardNotificationText,
                        Subtitle = dailyRewardNotificationTitle,
                        ShowInForeground = true,
                        ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                        CategoryIdentifier = "category_a",
                        ThreadIdentifier = "thread1",
                        Trigger = dailyRewardNotificationTimeTrigger,
                    };
                    iOSNotificationCenter.ScheduleNotification(iOSDailyRewardNotification);
                }
#endif
            }
        }
    }
}
