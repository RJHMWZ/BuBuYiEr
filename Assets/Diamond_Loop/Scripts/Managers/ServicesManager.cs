using UnityEngine;

namespace ClawbearGames
{
    public class ServicesManager : MonoBehaviour
    {
        public static ServicesManager Instance { private set; get; }

        [SerializeField] private IAPManager iAPManager = null;
        [SerializeField] private AdManager adManager = null;
        [SerializeField] private SoundManager soundManager = null;
        [SerializeField] private CoinManager coinManager = null;
        [SerializeField] private ShareManager shareManager = null;
        [SerializeField] private DailyRewardManager dailyRewardManager = null;
        [SerializeField] private LeaderboardManager leaderboardManager = null;
        [SerializeField] private NotificationManager notificationManager = null;

        public IAPManager IAPManager => iAPManager;
        public AdManager AdManager => adManager;
        public SoundManager SoundManager => soundManager;
        public CoinManager CoinManager => coinManager;
        public ShareManager ShareManager => shareManager;
        public DailyRewardManager DailyRewardManager => dailyRewardManager;
        public LeaderboardManager LeaderboardManager => leaderboardManager;
        public NotificationManager NotificationManager => notificationManager;

        private void Awake()
        {
            if (Instance)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}

