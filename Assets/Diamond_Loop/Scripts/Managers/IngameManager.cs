using System;
using System.Collections;
using UnityEngine;


namespace ClawbearGames
{
    public class IngameManager : MonoBehaviour
    {
        public static IngameManager Instance { private set; get; }
        public static event System.Action<IngameState> IngameStateChanged = delegate { };


        [Header("Set the level data to test. Set back to null to disable this feature.")]
        [SerializeField] private LevelData testLevelData = null;


        [Header("Ingame Configuration")]
        [SerializeField] private float reviveWaitTime = 5f;
        [SerializeField] private AudioClip[] backgroundMusics = null;

        [Header("Ingame References")]
        [SerializeField] private Material backgroundMaterial = null;
        [SerializeField] private GridManager gridManager = null;
        [SerializeField] private CubeManager cubeManager = null;
        [SerializeField] private ConveyorManager conveyorManager = null;
        [SerializeField] private CameraParentController cameraParent = null;
        [SerializeField] private ParticleSystem[] confettiEffects = null;

        public IngameState IngameState
        {
            get { return ingameState; }
            private set
            {
                if (value != ingameState)
                {
                    ingameState = value;
                    IngameStateChanged(ingameState);
                }
            }
        }
        public MaterialPropertyBlock MPB
        {
            get
            {
                if (propertyBlock == null)
                {
                    propertyBlock = new MaterialPropertyBlock();
                }
                return propertyBlock;
            }
        }
        public CameraParentController CameraParent => cameraParent;
        public GridManager GridManager => gridManager;
        public CubeManager CubeManager => cubeManager;
        public ConveyorManager ConveyorManager => conveyorManager;
        public Material BackgroundMaterial => backgroundMaterial;
        public float ReviveWaitTime => reviveWaitTime;
        public int CurrentLevel { private set; get; }
        public int CurrentStar { private set; get; }
        public int CoinReward => levelData.GetCoinReward();
        public bool IsRevived { private set; get; }
        public bool IsPaused { private set; get; }
        public bool IsRoundStarted { private set; get; }
        public bool IsLearningRound { private set; get; }
        public bool HasDeployedPot { private set; get; }
        public PotController TutorialPot { private set; get; }
        public int TotalPots { private set; get; }
        public int CompletedPots { private set; get; }
        public float RemainingTime { private set; get; }
        public string FailureReason { private set; get; } = string.Empty;
        public int NewlyUnlockedStory { private set; get; } = -1;
        public bool HasNextLevel { private set; get; }
        public bool FreeRecovery => CurrentLevel <= 3;
        public bool IsRecoverySuspended => recoverySuspended;
        public const int RecoverySeconds = 60;
        private bool settled, recoverySuspended;
        private float recoveryTimeScale = 1;
        private Coroutine countdown;
        private float timeScaleBeforePause = 1f;
        private bool audioPausedBeforePause;

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused || (paused && IngameState != IngameState.Ingame_Playing)) return;
            IsPaused = paused;
            selectedPot = null;
            if (paused)
            {
                timeScaleBeforePause = Time.timeScale;
                audioPausedBeforePause = AudioListener.pause;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            else
            {
                Time.timeScale = timeScaleBeforePause;
                AudioListener.pause = audioPausedBeforePause;
                InputManager.Update();
            }
        }

        public void PrepareToLeave()
        {
            stopControl = true;
            selectedPot = null;
            CancelInvoke();
            StopAllCoroutines();
            SetPaused(false);
            EndRecoveryPause();
            Time.timeScale = 1f;
        }
        public bool StopControl
        {
            set { stopControl = value; }
            get { return stopControl; }
        }


        private IngameState ingameState = IngameState.Ingame_GameOver;
        private LevelData levelData = null;
        private AudioClip backgroundMusic = null;
        private PotController selectedPot = null;
        private MaterialPropertyBlock propertyBlock = null;
        private bool stopControl = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public LevelData DebugLevelData => levelData;
        public bool DebugTryDeployPot(PotController pot) => TryDeployPot(pot);

        // Use the real result flow, but cancel startup/timer callbacks first.
        public void DebugFinishLevel(bool won, int stars)
        {
            CancelInvoke();
            StopAllCoroutines();
            stopControl = true;
            selectedPot = null;
            CurrentStar = Mathf.Clamp(stars, 1, 3);
            if (won) CompletedLevel();
            else GameOver();
        }
#endif


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                DestroyImmediate(Instance.gameObject);
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SetPaused(false);
                EndRecoveryPause();
                Instance = null;
            }
        }

        private void Start()
        {
            InputManager.Initialize();
            Application.targetFrameRate = 60;
            ServicesManager.Instance.CoinManager.SetCollectedCoins(0);
            StartCoroutine(CRShowViewWithDelay(ViewType.INGAME_VIEW, 0f));

            //Setup variables
            IsRevived = false;
            stopControl = true;
            backgroundMusic = backgroundMusics[UnityEngine.Random.Range(0, backgroundMusics.Length)];
            confettiEffects[0].transform.parent.gameObject.SetActive(false);

            //Load level parameters
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.TryGetLevelOverride(out LevelData debugLevel))
            {
                levelData = debugLevel;
                CurrentLevel = GameDebugService.GetLevelNumber(debugLevel);
            }
            else
#endif
            if (testLevelData != null)
            {
                int level = 1;
                levelData = testLevelData;
                bool canParse = int.TryParse(levelData.name.Split('_')[1], out level);
                CurrentLevel = level;
            }
            else
            {
                CurrentLevel = BearSoloProgress.PlayableLevel(PlayerDataHandler.GetTargetLevel());
                levelData = Resources.Load<LevelData>("Levels/Level_" + CurrentLevel);
            }

            cubeManager.OnInit(levelData);
            gridManager.OnInit(levelData);
            conveyorManager.OnInit(levelData.conveyorSlot);
            TotalPots = gridManager.AllPots.Count;
            CurrentStar = 3;
            RemainingTime = Mathf.Max(1, levelData.totalLevelTime);
            HasNextLevel = BearSoloProgress.HasNext(CurrentLevel);
            IsLearningRound = !PlayerDataHandler.IsWatchedTutorial();
            TutorialPot = cubeManager.FindTutorialPot(gridManager.AllPots);
            ViewManager.Instance.IngameViewController.ApplyWorldTheme(this);
            Invoke(nameof(StartGame), 0.15f);
        }


        private void Update()
        {
            if (IsPaused)
            {
                InputManager.Update();
                selectedPot = null;
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.IsBlockingInput)
            {
                InputManager.Update();
                selectedPot = null;
                return;
            }
#endif
            if (stopControl || !IsRoundStarted || settled) { InputManager.Update(); return; }

            InputManager.Update();
            if (InputManager.GetMouseButtonDown(true))
            {
                //Is using power-up -> return
                if (ViewManager.Instance.IngameViewController.MessagePanel.gameObject.activeSelf) return;

                PotController pot = PickPot(InputManager.GetMousePosition());
                if (pot != null)
                {
                    if (pot.PotState == PotState.Locked || !pot.CanSelect)
                    {
                        pot.Shake(0);
                        ViewManager.Instance.IngameViewController.ShowHint(pot.PotState == PotState.Locked
                            ? "???????????????" : "??????????????");
                    }
                    else
                    {
                        ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.SelectPot);
                        selectedPot = pot;
                    }
                }
            }

            else if (InputManager.GetMouseButtonUp())
            {
                if (selectedPot != null)
                {
                    PotController jumpingPot = selectedPot;
                    selectedPot = null;
                    if (!TryDeployPot(jumpingPot)) conveyorManager.ShakeCanvas();
                }
                else
                {
                    if (InputManager.IsPointerOverUI()) return;

                    if (ViewManager.Instance.IngameViewController.MessagePanel.gameObject.activeSelf)
                    {
                        stopControl = true;
                        PowerUpType type = ViewManager.Instance.IngameViewController.MessagePanel.PowerUpType;
                        if (type == PowerUpType.Magnet) //Handle Magnet
                        {
                            gridManager.HandleMagnet(InputManager.GetMousePosition(), (completed) =>
                            {
                                ViewManager.Instance.IngameViewController.CloseMessagePanel(completed);
                                stopControl = false;
                            });
                        }
                        else if (type == PowerUpType.Hammer) //Handle Hammer
                        {
                            gridManager.HandleHammer(InputManager.GetMousePosition(), (completed) =>
                            {
                                ViewManager.Instance.IngameViewController.CloseMessagePanel(completed);
                                stopControl = false;
                            });
                        }
                        else if (type == PowerUpType.Key) //Handle Key
                        {
                            gridManager.HandleKey(InputManager.GetMousePosition(), (completed) =>
                            {
                                ViewManager.Instance.IngameViewController.CloseMessagePanel(completed);
                                stopControl = false;
                            });
                        }
                        else if (type == PowerUpType.Cooler) //Handle Cooler
                        {
                            gridManager.HandleCooler(InputManager.GetMousePosition(), (completed) =>
                            {
                                ViewManager.Instance.IngameViewController.CloseMessagePanel(completed);
                                stopControl = false;
                            });
                        }
                    }
                }
            }

        }

        // Manual input and the development auto-player share the same deployment rules.
        private bool TryDeployPot(PotController pot)
        {
            if (IsPaused || stopControl || !IsRoundStarted || ingameState != IngameState.Ingame_Playing || pot == null ||
                !pot.gameObject.activeInHierarchy || !pot.CanSelect || pot.IsOnConveyor ||
                pot.IsFilled || pot.PotState == PotState.Locked || pot.OwnerCell == null ||
                pot.OwnerCell.GetCurrentPot() != pot) return false;

            if (IsLearningRound && !HasDeployedPot && TutorialPot != null && pot != TutorialPot)
            {
                ViewManager.Instance.IngameViewController.ShowHint("先点亮起的糖果罐，试着收集一次吧");
                return false;
            }
            TileController tile = conveyorManager.GetClosestEmptyTile(pot.transform.position);
            if (tile == null)
            {
                ViewManager.Instance.IngameViewController.ShowHint("传送带暂时满啦，等罐子装满，或使用加一格");
                return false;
            }
            HasDeployedPot = true;
            stopControl = true;
            selectedPot = null;
            pot.JumpToTile(tile, () =>
            {
                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.PotJumped);
                pot.OwnerCell?.OnPotLeft();
                stopControl = false;
            });
            return true;
        }



        /// <summary>
        /// Call IngameState.Ingame_Playing event and handle other actions.
        /// Actual start the game.
        /// </summary>
        public void StartGame()
        {
            if (settled) return;
            IngameState = IngameState.Ingame_Playing;
            if (IsRevived)
            {
                ViewManager.Instance.OnShowView(ViewType.INGAME_VIEW);
                ServicesManager.Instance.SoundManager.ResumeMusic(.2f);
                stopControl = false;
                StartCountdown();
                return;
            }
            ServicesManager.Instance.SoundManager.PlayMusic(backgroundMusic, .3f);
            ViewManager.Instance.IngameViewController.InitBoostButtons(levelData);
            if (ViewManager.Instance.IngameViewController.ThemeArt != null)
                ViewManager.Instance.IngameViewController.ShowRoundIntro();
            else if (PlayerDataHandler.IsWatchedTutorial()) OnTutorialClosed();
        }

        private PotController PickPot(Vector2 screenPoint)
        {
            Camera camera = cameraParent.MainCamera;
            PotController nearest = null;
            float best = float.MaxValue;
            foreach (PotController pot in gridManager.AllPots)
            {
                if (pot == null || !pot.gameObject.activeInHierarchy || pot.IsOnConveyor || pot.IsFilled ||
                    pot.OwnerCell == null || pot.OwnerCell.GetCurrentPot() != pot) continue;
                Vector3 center = camera.WorldToScreenPoint(pot.transform.position + Vector3.up * .65f);
                if (center.z <= 0) continue;
                Vector2 edge = camera.WorldToScreenPoint(pot.transform.position + Vector3.right * gridManager.TileSpacing);
                Vector2 origin = camera.WorldToScreenPoint(pot.transform.position);
                float radius = Mathf.Max(18, Vector2.Distance(origin, edge) * .65f);
                float distance = (screenPoint - (Vector2)center).sqrMagnitude;
                if (distance <= radius * radius && distance < best) { best = distance; nearest = pot; }
            }
            return nearest;
        }

        private void StartCountdown()
        {
            if (countdown != null) StopCoroutine(countdown);
            countdown = StartCoroutine(CRCountdownTime());
        }

        private void EndRecoveryPause()
        {
            if (!recoverySuspended) return;
            recoverySuspended = false;
            Time.timeScale = recoveryTimeScale;
        }

        /// <summary>
        /// Call IngameState.Ingame_Revive event and handle other actions.
        /// </summary>
        public void Revive()
        {
            if (settled || IngameState != IngameState.Ingame_Playing) return;
            stopControl = true;
            selectedPot = null;
            IngameState = IngameState.Ingame_Revive;
            ServicesManager.Instance.SoundManager.PauseMusic(0);
            ViewManager.Instance.OnShowView(ViewType.REVIVE_VIEW);
            recoveryTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
            recoverySuspended = true;
            Time.timeScale = 0;
        }

        /// <summary>
        /// Call IngameState.Ingame_GameOver event and handle other actions.
        /// </summary>
        public void GameOver() => GameOver(null);

        public void GameOver(string reason)
        {
            if (settled) return;
            settled = true;
            stopControl = true;
            selectedPot = null;
            SetPaused(false);
            EndRecoveryPause();
            FailureReason = string.IsNullOrEmpty(reason) ? "??????????????????" : reason;
            IngameState = IngameState.Ingame_GameOver;
            StartCoroutine(CRShowViewWithDelay(ViewType.ENDGAME_VIEW, .35f));
            ServicesManager.Instance.SoundManager.StopMusic(.2f);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.LevelFailed);
        }

        /// <summary>
        /// Call IngameState.Ingame_CompleteLevel event and handle other actions.
        /// </summary>
        public void CompletedLevel()
        {
            if (settled || IngameState != IngameState.Ingame_Playing) return;
            settled = true;
            stopControl = true;
            selectedPot = null;
            CurrentStar = Mathf.Clamp(CurrentStar, 1, 3);
            int previousProgress = PlayerDataHandler.GetCurrentLevel();

            //Fire event
            IngameState = IngameState.Ingame_CompleteLevel;
            ingameState = IngameState.Ingame_CompleteLevel;

            //Other actions
            StartCoroutine(CRShowViewWithDelay(ViewType.ENDGAME_VIEW, .8f));
            ServicesManager.Instance.SoundManager.StopMusic(0.5f);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.LevelCompleted);

            //Play effects
            confettiEffects[0].transform.parent.gameObject.SetActive(true);
            foreach (ParticleSystem particle in confettiEffects)
            {
                particle.Play();
            }


            if (testLevelData == null
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                && !GameDebugService.IsPreviewLevel
#endif
                )
            {
                //Update and report level to leaderboard
                PlayerDataHandler.UpdateStar(CurrentLevel, CurrentStar);
                PlayerDataHandler.UpdateTargetLevel(HasNextLevel ? CurrentLevel + 1 : CurrentLevel);
                if (CurrentLevel >= PlayerDataHandler.GetCurrentLevel())
                {
                    PlayerDataHandler.UpdateCurrentLevel(CurrentLevel + 1);
                    for (int i = 0; i < BearCollectionPanel.StoryTitles.Length; i++)
                        if (BearCollectionPanel.StoryUnlockLevel(i) > previousProgress &&
                            BearCollectionPanel.StoryUnlockLevel(i) <= CurrentLevel + 1) NewlyUnlockedStory = i;
                    PlayerPrefs.Save();
                    if (!string.IsNullOrEmpty(PlayerDataHandler.GetUserName()))
                    {
                        ServicesManager.Instance.LeaderboardManager.UpdatePlayerLeaderboardData();
                    }
                }
            }
        }


        /// <summary>
        /// Coroutine show the view with given viewType and delay time.
        /// </summary>
        /// <param name="viewType"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRShowViewWithDelay(ViewType viewType, float delay)
        {
            yield return new WaitForSeconds(delay);
            ViewManager.Instance.OnShowView(viewType);
        }



        /// <summary>
        /// Coroutine countdown the time to calculate the level's star.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRCountdownTime()
        {
            while (!settled && ingameState == IngameState.Ingame_Playing)
            {
                if (!IsPaused && IsRoundStarted && !IsLearningRound)
                    RemainingTime = Mathf.Max(0, RemainingTime - Time.deltaTime);
                if (!IsRevived)
                    CurrentStar = RemainingTime >= levelData.GetStarTimeThreshold(3) ? 3
                        : RemainingTime >= levelData.GetStarTimeThreshold(2) ? 2 : 1;
                ViewManager.Instance.IngameViewController.StarPanel.UpdateTime(RemainingTime);
                if (RemainingTime <= 0)
                {
                    if (!IsRevived && (FreeRecovery || ServicesManager.Instance.AdManager.IsRewardedAdReady())) Revive();
                    else GameOver("?????????????????????????");
                    yield break;
                }
                yield return null;
            }
        }

        //////////////////////////////////////Publish functions



        /// <summary>
        /// Continue the game
        /// </summary>
        public void SetContinueGame()
        {
            if (settled || IsRevived || IngameState != IngameState.Ingame_Revive) return;
            EndRecoveryPause();
            IsRevived = true;
            IsRoundStarted = true;
            RemainingTime = RecoverySeconds;
            CurrentStar = 1;
            Invoke(nameof(StartGame), .05f);
        }

        /// <summary>
        /// Handle close tutorial panel.
        /// </summary>
        public void OnTutorialClosed()
        {
            if (IsRoundStarted || settled || IngameState != IngameState.Ingame_Playing) return;
            IsRoundStarted = true;
            stopControl = false;
            StartCountdown();
            cubeManager.StartSimulation();
        }

        /// <summary>
        /// Handle action when a pot completed.
        /// </summary>
        public void OnPotCompleted()
        {
            // Late animation callbacks must not settle a level twice.
            if (IngameState != IngameState.Ingame_Playing || settled) return;
            CompletedPots = Mathf.Min(TotalPots, CompletedPots + 1);
            if (IsLearningRound)
            {
                IsLearningRound = false;
                PlayerDataHandler.UpdateWatchedTutorial();
                PlayerPrefs.Save();
                ViewManager.Instance.IngameViewController.ShowHint("?????????????????????");
            }

            bool allCompleted = true;
            foreach(CellController cell in gridManager.AllCells())
            {
                if (cell.GetCurrentPot() != null)
                {
                    allCompleted = false;
                    break;
                }
            }

            if (conveyorManager.GetUsedTile() > 0)
            {
                allCompleted = false;
            }

            if (allCompleted)
            {
                if (cubeManager.IsEmpty())
                {
                    CompletedLevel();
                }
                else
                {
                    GameOver();
                }
            }
        }
    }
}
