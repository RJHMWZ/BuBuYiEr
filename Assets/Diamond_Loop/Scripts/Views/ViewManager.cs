using UnityEngine;
using UnityEngine.EventSystems;

namespace ClawbearGames
{
    public class ViewManager : MonoBehaviour
    {
        public static ViewManager Instance { private set; get; }

        [SerializeField] private IAPViewController iAPViewControllerPrefab = null;
        [SerializeField] private HomeViewController homeViewControllerPrefab = null;
        [SerializeField] private LeaderboardViewController leaderboardViewControllerPrefab = null;
        [SerializeField] private DailyRewardViewController dailyRewardViewControllerPrefab = null;
        [SerializeField] private LoadingViewController loadingViewControllerPrefab = null;
        [SerializeField] private IngameViewController ingameViewControllerPrefab = null;
        [SerializeField] private ReviveViewController reviveViewControllerPrefab = null;
        [SerializeField] private EndGameViewController endGameViewControllerPrefab = null;
        [SerializeField] private LevelViewController levelViewControllerPrefab = null;
        [SerializeField] private EventSystem eventSystemPrefab = null;
        public IAPViewController IAPViewController { private set; get; }
        public HomeViewController HomeViewController { private set; get; }
        public LeaderboardViewController LeaderboardViewController { private set; get; }
        public DailyRewardViewController DailyRewardViewController { private set; get; }
        public LoadingViewController LoadingViewController { private set; get; }
        public IngameViewController IngameViewController { private set; get; }
        public ReviveViewController ReviveViewController { private set; get; }
        public EndGameViewController EndGameViewController { private set; get; }
        public LevelViewController LevelViewController { private set; get; }

        public ViewType ActiveViewType { private set; get; }
        private BaseViewController currentViewController = null;


        private void Awake()
        {
            if (Instance)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                //Instantiate the views
                if (transform.childCount == 0)
                {
                    //Instantiate IAPView
                    IAPViewController = Instantiate(iAPViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    IAPViewController.transform.SetParent(transform, false);
                    IAPViewController.gameObject.name = iAPViewControllerPrefab.gameObject.name;
                    IAPViewController.OnClose();

                    //Instantiate HomeView
                    HomeViewController = Instantiate(homeViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    HomeViewController.transform.SetParent(transform, false);
                    HomeViewController.gameObject.name = homeViewControllerPrefab.gameObject.name;
                    HomeViewController.OnClose();

                    //Instantiate LeaderboardView
                    LeaderboardViewController = Instantiate(leaderboardViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    LeaderboardViewController.transform.SetParent(transform, false);
                    LeaderboardViewController.gameObject.name = leaderboardViewControllerPrefab.gameObject.name;
                    LeaderboardViewController.OnClose();


                    //Instantiate DailyRewardView
                    DailyRewardViewController = Instantiate(dailyRewardViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    DailyRewardViewController.transform.SetParent(transform, false);
                    DailyRewardViewController.gameObject.name = dailyRewardViewControllerPrefab.gameObject.name;
                    DailyRewardViewController.OnClose();


                    //Instantiate LoadingView
                    LoadingViewController = Instantiate(loadingViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    LoadingViewController.transform.SetParent(transform, false);
                    LoadingViewController.gameObject.name = loadingViewControllerPrefab.gameObject.name;
                    LoadingViewController.OnClose();

                    //Instantiate IngameView
                    IngameViewController = Instantiate(ingameViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    IngameViewController.transform.SetParent(transform, false);
                    IngameViewController.gameObject.name = ingameViewControllerPrefab.gameObject.name;
                    IngameViewController.OnClose();

                    //Instantiate ReviveView
                    ReviveViewController = Instantiate(reviveViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    ReviveViewController.transform.SetParent(transform, false);
                    ReviveViewController.gameObject.name = reviveViewControllerPrefab.gameObject.name;
                    ReviveViewController.OnClose();


                    //Instantiate EndGameView
                    EndGameViewController = Instantiate(endGameViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    EndGameViewController.transform.SetParent(transform, false);
                    EndGameViewController.gameObject.name = endGameViewControllerPrefab.gameObject.name;
                    EndGameViewController.OnClose();


                    //Instantiate LevelView
                    LevelViewController = Instantiate(levelViewControllerPrefab, Vector3.zero, Quaternion.identity);
                    LevelViewController.transform.SetParent(transform, false);
                    LevelViewController.gameObject.name = levelViewControllerPrefab.gameObject.name;
                    LevelViewController.OnClose();

                    //Instantiate EventSystem
                    EventSystem eventSystem = Instantiate(eventSystemPrefab, Vector3.zero, Quaternion.identity);
                    eventSystem.transform.SetParent(transform, false);
                }
            }
        }


        /// <summary>
        /// Close the current view and show the new view base on viewType.
        /// </summary>
        /// <param name="sceneName"></param>
        public void OnShowView(ViewType viewType)
        {
            if (currentViewController != null)
            {
                currentViewController.OnClose();
            }

            switch (viewType)
            {
                case ViewType.IAP_VIEW:
                    {
                        IAPViewController.gameObject.SetActive(true);
                        IAPViewController.OnShow();
                        ActiveViewType = ViewType.IAP_VIEW;
                        currentViewController = IAPViewController;
                        return;
                    }
                case ViewType.HOME_VIEW:
                    {
                        HomeViewController.gameObject.SetActive(true);
                        HomeViewController.OnShow();
                        ActiveViewType = ViewType.HOME_VIEW;
                        currentViewController = HomeViewController;
                        return;
                    }
                case ViewType.LEADERBOARD_VIEW:
                    {
                        LeaderboardViewController.gameObject.SetActive(true);
                        LeaderboardViewController.OnShow();
                        ActiveViewType = ViewType.LEADERBOARD_VIEW;
                        currentViewController = LeaderboardViewController;
                        return;
                    }
                case ViewType.DAILY_REWARD_VIEW:
                    {
                        DailyRewardViewController.gameObject.SetActive(true);
                        DailyRewardViewController.OnShow();
                        ActiveViewType = ViewType.DAILY_REWARD_VIEW;
                        currentViewController = DailyRewardViewController;
                        return;
                    }
                case ViewType.LOADING_VIEW:
                    {
                        LoadingViewController.gameObject.SetActive(true);
                        LoadingViewController.OnShow();
                        ActiveViewType = ViewType.LOADING_VIEW;
                        currentViewController = LoadingViewController;
                        return;
                    }
                case ViewType.INGAME_VIEW:
                    {
                        IngameViewController.gameObject.SetActive(true);
                        IngameViewController.OnShow();
                        ActiveViewType = ViewType.INGAME_VIEW;
                        currentViewController = IngameViewController;
                        return;
                    }
                case ViewType.REVIVE_VIEW:
                    {
                        ReviveViewController.gameObject.SetActive(true);
                        ReviveViewController.OnShow();
                        ActiveViewType = ViewType.REVIVE_VIEW;
                        currentViewController = ReviveViewController;
                        return;
                    }
                case ViewType.ENDGAME_VIEW:
                    {
                        EndGameViewController.gameObject.SetActive(true);
                        EndGameViewController.OnShow();
                        ActiveViewType = ViewType.ENDGAME_VIEW;
                        currentViewController = EndGameViewController;
                        return;
                    }
                case ViewType.LEVEL_VIEW:
                    {
                        LevelViewController.gameObject.SetActive(true);
                        LevelViewController.OnShow();
                        ActiveViewType = ViewType.LEVEL_VIEW;
                        currentViewController = LevelViewController;
                        return;
                    }
            }
        }
    }
}
