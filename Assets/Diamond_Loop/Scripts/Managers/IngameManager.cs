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
        public float ReviveWaitTime => reviveWaitTime;
        public int CurrentLevel { private set; get; }
        public int CurrentStar { private set; get; }
        public int CoinReward => levelData.GetCoinReward();
        public bool IsRevived { private set; get; }
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
                CurrentLevel = PlayerDataHandler.GetCurrentLevel();
                levelData = Resources.Load<LevelData>("Levels/Level_" + CurrentLevel);
            }
            backgroundMaterial.SetColor("_TopColor", levelData.backgroundTopColor);
            backgroundMaterial.SetColor("_BottomColor", levelData.backgroundBottomColor);

            cubeManager.OnInit(levelData);
            gridManager.OnInit(levelData);
            conveyorManager.OnInit(levelData.conveyorSlot);
            Invoke(nameof(StartGame), 0.15f);
        }


        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.IsBlockingInput)
            {
                InputManager.Update();
                selectedPot = null;
                return;
            }
#endif
            if (stopControl) return;

            InputManager.Update();
            if (InputManager.GetMouseButtonDown(true))
            {
                //Is using power-up -> return
                if (ViewManager.Instance.IngameViewController.MessagePanel.gameObject.activeSelf) return;

                Ray ray = Camera.main.ScreenPointToRay(InputManager.GetMousePosition());
                Plane ground = new Plane(Vector3.up, new Vector3(0f, gridManager.GridWorldY, 0f));
                if (ground.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    PotController pot = gridManager.GetClosestPot(hitPoint);
                    if (pot != null)
                    {
                        if (pot.PotState == PotState.Locked || !pot.CanSelect)
                        {
                            pot.Shake(0.1f);
                        }
                        else
                        {
                            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.SelectPot);
                            selectedPot = pot;
                        }
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
            if (stopControl || ingameState != IngameState.Ingame_Playing || pot == null ||
                !pot.gameObject.activeInHierarchy || !pot.CanSelect || pot.IsOnConveyor ||
                pot.IsFilled || pot.PotState == PotState.Locked || pot.OwnerCell == null ||
                pot.OwnerCell.GetCurrentPot() != pot) return false;

            TileController tile = conveyorManager.GetClosestEmptyTile(pot.transform.position);
            if (tile == null) return false;
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
            //Fire event
            IngameState = IngameState.Ingame_Playing;
            ingameState = IngameState.Ingame_Playing;

            //Other actions
            if (IsRevived)
            {
                StartCoroutine(CRShowViewWithDelay(ViewType.INGAME_VIEW, 0f));
                ServicesManager.Instance.SoundManager.ResumeMusic(0.5f);

                //Enable control
                stopControl = false;

                //Count down time
                StartCoroutine(CRCountdownTime());
            }
            else
            {
                ServicesManager.Instance.SoundManager.PlayMusic(backgroundMusic, 0.5f);
                ViewManager.Instance.IngameViewController.InitBoostButtons(levelData);

                //Enable control
                if (PlayerDataHandler.IsWatchedTutorial())
                {
                    StartCoroutine(CRCountdownTime());
                    cubeManager.StartSimulation();
                    stopControl = false;
                }
            }
        }


        /// <summary>
        /// Call IngameState.Ingame_Revive event and handle other actions.
        /// </summary>
        public void Revive()
        {
            //Fire event
            IngameState = IngameState.Ingame_Revive;
            ingameState = IngameState.Ingame_Revive;

            //Add another actions here
            StartCoroutine(CRShowViewWithDelay(ViewType.REVIVE_VIEW, 1f));
            ServicesManager.Instance.SoundManager.PauseMusic(0.5f);
        }


        /// <summary>
        /// Call IngameState.Ingame_GameOver event and handle other actions.
        /// </summary>
        public void GameOver()
        {
            //Fire event
            IngameState = IngameState.Ingame_GameOver;
            ingameState = IngameState.Ingame_GameOver;

            //Add another actions here
            StartCoroutine(CRShowViewWithDelay(ViewType.ENDGAME_VIEW, 0.25f));
            ServicesManager.Instance.SoundManager.StopMusic(0.5f);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.LevelFailed);
        }


        /// <summary>
        /// Call IngameState.Ingame_CompleteLevel event and handle other actions.
        /// </summary>
        public void CompletedLevel()
        {
            //Fire event
            IngameState = IngameState.Ingame_CompleteLevel;
            ingameState = IngameState.Ingame_CompleteLevel;

            //Other actions
            StartCoroutine(CRShowViewWithDelay(ViewType.ENDGAME_VIEW, 2f));
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
                PlayerDataHandler.UpdateTargetLevel(CurrentLevel + 1);
                if (CurrentLevel >= PlayerDataHandler.GetCurrentLevel())
                {
                    PlayerDataHandler.UpdateCurrentLevel(CurrentLevel + 1);
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
            float totalTime = levelData.totalLevelTime;
            float threeStartime = levelData.GetStarTimeThreshold(3);
            float twoStarTime = levelData.GetStarTimeThreshold(2);
            float oneStarTime = levelData.GetStarTimeThreshold(1);
            float currentTime = totalTime;
            while (currentTime > 0)
            {
                ViewManager.Instance.IngameViewController.StarPanel.UpdateTime(currentTime);
                currentTime -= Time.deltaTime; ;
                yield return null;

                //Break if not playing state
                if (ingameState != IngameState.Ingame_Playing) { break; }

                if (currentTime >= threeStartime)
                    CurrentStar = 3;
                else if (currentTime >= twoStarTime)
                    CurrentStar = 2;
                else if (currentTime >= oneStarTime)
                    CurrentStar = 1;
            }


            if (currentTime <= 0)
            {
                stopControl = true;

                if (IsRevived || !ServicesManager.Instance.AdManager.IsRewardedAdReady())
                {
                    //Fire event
                    IngameState = IngameState.Ingame_GameOver;
                    ingameState = IngameState.Ingame_GameOver;

                    //Add another actions here
                    cameraParent.Shake(0.2f);
                    StartCoroutine(CRShowViewWithDelay(ViewType.ENDGAME_VIEW, 1f));
                    ServicesManager.Instance.SoundManager.StopMusic(0.5f);
                    ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.LevelFailed);
                }
                else
                {
                    Revive();
                    cameraParent.Shake(0.2f);
                }
            }
        }


        //////////////////////////////////////Publish functions



        /// <summary>
        /// Continue the game
        /// </summary>
        public void SetContinueGame()
        {
            IsRevived = true;
            Invoke(nameof(StartGame), 0.05f);
        }


        /// <summary>
        /// Handle close tutorial panel.
        /// </summary>
        public void OnTutorialClosed()
        {
            stopControl = false;
            StartCoroutine(CRCountdownTime());
            cubeManager.StartSimulation();
        }



        /// <summary>
        /// Handle action when a pot completed.
        /// </summary>
        public void OnPotCompleted()
        {
            // Late animation callbacks must not settle a level twice.
            if (IngameState != IngameState.Ingame_Playing) return;

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
