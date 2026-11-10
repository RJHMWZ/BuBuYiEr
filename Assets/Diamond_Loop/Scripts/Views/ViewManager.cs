using UnityEngine;
using UnityEngine.EventSystems;

namespace ClawbearGames
{
    public class ViewManager : MonoBehaviour
    {
        public static ViewManager Instance { private set; get; }

        [SerializeField] private IAPViewController iAPViewControllerPrefab = null;
        [SerializeField] private HomeViewController homeViewControllerPrefab = null;
        [SerializeField] private HomeViewController sceneHomeViewController = null;
        [SerializeField] private LeaderboardViewController leaderboardViewControllerPrefab = null;
        [SerializeField] private DailyRewardViewController dailyRewardViewControllerPrefab = null;
        [SerializeField] private LoadingViewController loadingViewControllerPrefab = null;
        [SerializeField] private IngameViewController ingameViewControllerPrefab = null;
        [SerializeField] private EndGameViewController endGameViewControllerPrefab = null;
        [SerializeField] private LevelViewController levelViewControllerPrefab = null;
        [SerializeField] private EventSystem eventSystemPrefab = null;
        [SerializeField] private EventSystem sceneEventSystem = null;
        public IAPViewController IAPViewController { private set; get; }
        public HomeViewController HomeViewController { private set; get; }
        public LeaderboardViewController LeaderboardViewController { private set; get; }
        public DailyRewardViewController DailyRewardViewController { private set; get; }
        public LoadingViewController LoadingViewController { private set; get; }
        public IngameViewController IngameViewController { private set; get; }
        public EndGameViewController EndGameViewController { private set; get; }
        public LevelViewController LevelViewController { private set; get; }

        public ViewType ActiveViewType { private set; get; }
        private BaseViewController currentViewController = null;


        private void Awake()
        {
            if (Instance)
            {
                // Returning Home loads an editable scene copy. Keep the persistent view.
                if (sceneHomeViewController != null && sceneHomeViewController != Instance.HomeViewController)
                    Destroy(sceneHomeViewController.gameObject);
                if (sceneEventSystem != null && sceneEventSystem.transform.parent != Instance.transform)
                    Destroy(sceneEventSystem.gameObject);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                //Instantiate the views
                if (transform.childCount == 0)
                {
                    IAPViewController = CreateView(iAPViewControllerPrefab);
                    HomeViewController = CreateView(homeViewControllerPrefab, sceneHomeViewController);
                    LeaderboardViewController = CreateView(leaderboardViewControllerPrefab);
                    DailyRewardViewController = CreateView(dailyRewardViewControllerPrefab);
                    LoadingViewController = CreateView(loadingViewControllerPrefab);
                    IngameViewController = CreateView(ingameViewControllerPrefab);
                    EndGameViewController = CreateView(endGameViewControllerPrefab);
                    LevelViewController = CreateView(levelViewControllerPrefab);

                    //Instantiate EventSystem
                    EventSystem eventSystem = sceneEventSystem != null ? sceneEventSystem
                        : Instantiate(eventSystemPrefab, Vector3.zero, Quaternion.identity);
                    eventSystem.transform.SetParent(transform, false);
                }
            }
        }
        private T CreateView<T>(T prefab, T sceneView = null) where T : BaseViewController
        {
            T view = sceneView != null ? sceneView : Instantiate(prefab, Vector3.zero, Quaternion.identity);
            view.transform.SetParent(transform, false);
            view.gameObject.name = prefab.gameObject.name;
            view.OnClose();
            return view;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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

            BaseViewController next;
            switch (viewType)
            {
                case ViewType.IAP_VIEW: next = IAPViewController; break;
                case ViewType.HOME_VIEW: next = HomeViewController; break;
                case ViewType.LEADERBOARD_VIEW: next = LeaderboardViewController; break;
                case ViewType.DAILY_REWARD_VIEW: next = DailyRewardViewController; break;
                case ViewType.LOADING_VIEW: next = LoadingViewController; break;
                case ViewType.INGAME_VIEW: next = IngameViewController; break;
                case ViewType.ENDGAME_VIEW: next = EndGameViewController; break;
                case ViewType.LEVEL_VIEW: next = LevelViewController; break;
                default: return;
            }
            if (next == null) return;
            next.gameObject.SetActive(true);
            next.OnShow();
            if(viewType!=ViewType.INGAME_VIEW && viewType!=ViewType.ENDGAME_VIEW && viewType!=ViewType.LOADING_VIEW && ServicesManager.Instance!=null)
                ServicesManager.Instance.SoundManager.PlayHomeMusic();
            ActiveViewType = viewType;
            currentViewController = next;
        }
    }
}
