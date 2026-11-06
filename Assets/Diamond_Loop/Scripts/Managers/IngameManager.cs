using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public sealed class IngameManager : MonoBehaviour
    {
        public static IngameManager Instance { get; private set; }
        public static event Action<IngameState> IngameStateChanged = delegate { };
        [SerializeField] private LevelData testLevelData;
        [SerializeField] private AudioClip[] backgroundMusics;
        public CandyBreakoutSimulation Board { get; private set; }
        public LevelData Level { get; private set; }
        public IngameState IngameState { get; private set; } = IngameState.Ingame_GameOver;
        public int CurrentLevel { get; private set; }
        public int CurrentStar { get; private set; } = 3;
        public bool IsPaused { get; private set; }
        public bool IsRoundStarted { get; private set; }
        public bool HasNextLevel => BearSoloProgress.HasNext(CurrentLevel);
        public int NewlyUnlockedStory { get; private set; } = -1;
        public string FailureReason { get; private set; }
        private bool settled, leaving;
        private float pausedScale = 1;
        private bool pausedAudio;
        private float soundReady;
        private AudioClip hitSound, breakSound;

        private void Awake() => Instance = this;
        private void Start()
        {
            Application.targetFrameRate = 60;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.TryGetLevelOverride(out LevelData preview)) Level = preview;
            else
#endif
            Level = testLevelData != null ? testLevelData : Resources.Load<LevelData>("Levels/Level_" + BearSoloProgress.PlayableLevel(PlayerDataHandler.GetTargetLevel()));
            if (Level == null) { Debug.LogError("找不到糖果打砖块关卡数据"); return; }
            CurrentLevel = Level.levelNumber;
            Board = new CandyBreakoutSimulation(Level);
            hitSound = Resources.Load<AudioClip>("Breakout/Hit");
            breakSound = Resources.Load<AudioClip>("Breakout/Pop");
            Board.Hit += OnHit;
            ServicesManager.Instance.CoinManager.SetCollectedCoins(0);
            ChangeState(IngameState.Ingame_Playing);
            ViewManager.Instance.OnShowView(ViewType.INGAME_VIEW);
            if (backgroundMusics != null && backgroundMusics.Length > 0)
                ServicesManager.Instance.SoundManager.PlayMusic(backgroundMusics[(CurrentLevel - 1) % backgroundMusics.Length], .3f);
        }
        public bool CanInteract => IsRoundStarted && !IsPaused && !settled && !leaving && Time.timeScale > 0;
        private void Update()
        {
            if (!CanInteract || Board == null) return;
            Board.Step(Time.deltaTime);
            CurrentStar = Board.Elapsed <= Level.threeStarTime ? 3 : Board.Elapsed <= Level.twoStarTime ? 2 : 1;
            if (Board.Complete) CompletedLevel();
        }
        private void OnHit(Vector2 position, bool destroyed)
        {
            if (Time.unscaledTime < soundReady) return;
            soundReady = Time.unscaledTime + .07f;
            AudioClip clip = destroyed ? breakSound : hitSound;
            if (clip != null) ServicesManager.Instance.SoundManager.PlaySound(clip);
        }
        public void OnTutorialClosed()
        {
            if (settled || IsRoundStarted) return;
            IsRoundStarted = true;
            PlayerDataHandler.UpdateWatchedTutorial();
        }
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused || (paused && settled)) return;
            IsPaused = paused;
            if (paused)
            {
                pausedScale = Time.timeScale;
                pausedAudio = AudioListener.pause;
                Time.timeScale = 0;
                AudioListener.pause = true;
            }
            else { Time.timeScale = pausedScale; AudioListener.pause = pausedAudio; }
        }
        public void PrepareToLeave()
        {
            leaving = true;
            SetPaused(false);
            Time.timeScale = 1;
            StopAllCoroutines();
        }
        private void ChangeState(IngameState state)
        {
            IngameState = state;
            IngameStateChanged(state);
        }
        public void CompletedLevel()
        {
            if (settled || IngameState != IngameState.Ingame_Playing) return;
            settled = true;
            SetPaused(false);
            int previous = PlayerDataHandler.GetCurrentLevel();
            bool preview = testLevelData != null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            preview |= GameDebugService.IsPreviewLevel;
#endif
            if (!preview)
            {
                PlayerDataHandler.UpdateStar(CurrentLevel, CurrentStar);
                PlayerDataHandler.UpdateTargetLevel(HasNextLevel ? CurrentLevel + 1 : CurrentLevel);
                if (CurrentLevel >= previous)
                {
                    PlayerDataHandler.UpdateCurrentLevel(CurrentLevel + 1);
                    for (int i = 0; i < BearCollectionPanel.StoryTitles.Length; i++)
                        if (BearCollectionPanel.StoryUnlockLevel(i) > previous && BearCollectionPanel.StoryUnlockLevel(i) <= CurrentLevel + 1)
                            NewlyUnlockedStory = i;
                }
                // One reward per completed round; the settled guard prevents duplicate callbacks.
                ServicesManager.Instance.CoinManager.AddTotalCoins(Level.coinReward, 0);
                ServicesManager.Instance.CoinManager.AddCollectedCoins(Level.coinReward, 0);
                PlayerPrefs.Save();
            }
            ChangeState(IngameState.Ingame_CompleteLevel);
            ServicesManager.Instance.SoundManager.StopMusic(.3f);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.LevelCompleted);
            StartCoroutine(ShowResult(.7f));
        }
        public void GameOver() => GameOver("这次先休息一下，再和小熊一起打碎糖砖吧。");
        public void GameOver(string reason)
        {
            if (settled) return;
            settled = true;
            SetPaused(false);
            FailureReason = reason;
            ChangeState(IngameState.Ingame_GameOver);
            ServicesManager.Instance.SoundManager.StopMusic(.2f);
            StartCoroutine(ShowResult(.2f));
        }
        private IEnumerator ShowResult(float delay)
        {
            yield return new WaitForSeconds(delay);
            ViewManager.Instance.OnShowView(ViewType.ENDGAME_VIEW);
        }
        private void OnDestroy()
        {
            SetPaused(false);
            if (Board != null) Board.Hit -= OnHit;
            if (Instance == this) Instance = null;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public LevelData DebugLevelData => Level;
        public void DebugFinishLevel(bool won, int stars)
        {
            CurrentStar = Mathf.Clamp(stars, 1, 3);
            if (won) CompletedLevel(); else GameOver();
        }
#endif
    }
}
