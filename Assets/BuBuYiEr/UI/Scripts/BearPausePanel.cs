using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearPausePanel : MonoBehaviour
    {
        private IngameViewController view;
        private GameObject overlay;
        private Text levelLabel, hintLabel;
        private Button pauseButton;
        private Button resumeButton, restartButton, homeButton, quitButton;
        private enum LeaveChoice { None, Restart, Home, Quit }
        private LeaveChoice pendingChoice;
        private bool leaving;

        public void Initialize(IngameViewController owner, BearMenuArt art)
        {
            view = owner;
            if (overlay != null) return;
            RectTransform root = BearMenuFactory.Node(owner.transform, "PauseCanvas", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(root);
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            root.gameObject.AddComponent<GraphicRaycaster>();
            RectTransform header = owner.transform.Find("CandyBreakoutUI/CandyRoom/Header") as RectTransform;
            RectTransform frame = header != null ? header : BearGameTheme.Frame(root, "PauseButtonSafeArea");
            pauseButton = BearGameTheme.Button(art, frame, "PauseButton", "", header != null ? 940 : 894,
                header != null ? 0 : 26, 104, 104, new Color32(255,222,211,255), Toggle);
            BearMenuShape face = pauseButton.GetComponent<BearMenuShape>();
            face.border = 3; face.radius = 30;
            face.bottomColor = new Color32(250,211,197,255);
            Shadow shadow = pauseButton.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.32f,.17f,.07f,.11f);
            shadow.effectDistance = new Vector2(0,-5);
            BearGameTheme.Paper(pauseButton.transform, "PauseLeftBar", 33, 31, 12, 42, BearGameTheme.Brown, 6).border = 0;
            BearGameTheme.Paper(pauseButton.transform, "PauseRightBar", 59, 31, 12, 42, BearGameTheme.Brown, 6).border = 0;

            RectTransform dim = BearMenuFactory.Node(root, "PauseOverlay", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(dim);
            Image blocker = dim.gameObject.AddComponent<Image>();
            blocker.color = new Color(.23f, .12f, .05f, .62f);
            blocker.raycastTarget = true;
            overlay = dim.gameObject;
            dim.gameObject.AddComponent<BearPanelFade>();
            RectTransform dialogFrame = BearGameTheme.Frame(dim, "PauseDialogSafeArea");
            RectTransform card = BearGameTheme.Paper(dialogFrame, "CreamPaper", 128, 400, 768, 930,
                BearGameTheme.Cream, 64).rectTransform;
            BearGameTheme.Bears(art, dialogFrame, 272, 207, .88f);
            BearGameTheme.Picture(art, card, "TitleCloud", "Setting/TitleBg", 123, -48, 522, 142, true);
            BearGameTheme.Label(art, card, "Title", "休息一下", 134, -29, 500, 104, 58);
            levelLabel = BearGameTheme.Label(art, card, "Level", "", 110, 109, 548, 58, 35);
            hintLabel = BearGameTheme.Label(art, card, "Hint", "点击糖砖赚蜂蜜，小球会自动弹跳\n左右滑动小队，可购买或升级", 70, 171, 628, 80, 30);
            resumeButton = BearGameTheme.Button(art, card, "Resume", "继续游戏", 100, 272, 568, 110,
                BearGameTheme.Gold, Resume);
            restartButton = BearGameTheme.Button(art, card, "Restart", "重新开始", 100, 412, 568, 104,
                BearGameTheme.Pink, Restart);
            homeButton = BearGameTheme.Button(art, card, "Home", "返回主菜单", 100, 546, 568, 104,
                BearGameTheme.Cream, ReturnHome);
            quitButton = BearGameTheme.Button(art, card, "Quit", "退出游戏", 100, 680, 568, 104,
                BearGameTheme.Cream, Quit);
            BearGameTheme.Picture(art, card, "Bow", "Setting/Decoration1", 291, 837, 186, 101);
            overlay.SetActive(false);
        }

        public void ResetPanel()
        {
            leaving = false;
            ResetConfirmation();
            if (overlay != null) overlay.SetActive(false);
        }

        private void Update()
        {
            IngameManager manager = IngameManager.Instance;
            if (manager == null || view == null || !view.gameObject.activeInHierarchy) return;
            bool playing = manager.IngameState == IngameState.Ingame_Playing;
            if (pauseButton != null) pauseButton.interactable = playing && !leaving;
            if (overlay != null && overlay.activeSelf != manager.IsPaused)
            {
                if (manager.IsPaused)
                {
                    ResetConfirmation();
                    levelLabel.text = "第 " + manager.CurrentLevel + " 关 · 已暂停";
                }
                overlay.SetActive(manager.IsPaused);
            }
            if (!leaving && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle()
        {
            if (leaving || IngameManager.Instance == null) return;
            if (pendingChoice != LeaveChoice.None) { ResetConfirmation(); return; }
            if (IngameManager.Instance.IsPaused) Resume();
            else Pause();
        }

        private void Pause()
        {
            IngameManager manager = IngameManager.Instance;
            if (leaving || manager == null || manager.IngameState != IngameState.Ingame_Playing) return;
            ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Pause);
            ResetConfirmation();
            manager.SetPaused(true);
            levelLabel.text = "第 " + manager.CurrentLevel + " 关 · 已暂停";
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Resume()
        {
            if (leaving) return;
            if (pendingChoice != LeaveChoice.None)
            {
                LeaveChoice choice = pendingChoice;
                pendingChoice = LeaveChoice.None;
                ExecuteLeave(choice);
                return;
            }
            if(IngameManager.Instance!=null && IngameManager.Instance.IsPaused)ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Resume);
            if (overlay != null) overlay.SetActive(false);
            IngameManager.Instance?.SetPaused(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Restart()
        {
            RequestLeave(LeaveChoice.Restart);
        }

        public void ReturnHome()
        {
            RequestLeave(LeaveChoice.Home);
        }

        private void ResetConfirmation()
        {
            pendingChoice = LeaveChoice.None;
            if (resumeButton == null) return;
            resumeButton.GetComponentInChildren<Text>().text = "继续游戏";
            restartButton.GetComponentInChildren<Text>().text = "重新开始";
            homeButton.GetComponentInChildren<Text>().text = "返回主菜单";
            quitButton.GetComponentInChildren<Text>().text = "退出游戏";
            resumeButton.interactable = restartButton.interactable = homeButton.interactable = quitButton.interactable = true;
            hintLabel.text = "点击糖砖赚蜂蜜，小球会自动弹跳\n左右滑动小队，可购买或升级";
        }

        private void RequestLeave(LeaveChoice choice)
        {
            if (leaving || IngameManager.Instance == null) return;
            if (pendingChoice != LeaveChoice.None) { ResetConfirmation(); return; }
            if (!IngameManager.Instance.IsRoundStarted || IngameManager.Instance.Board.Elapsed < 1)
            { ExecuteLeave(choice); return; }
            ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.UiClick);
            IngameManager.Instance.SetPaused(true);
            overlay.SetActive(true);
            pendingChoice = choice;
            hintLabel.text = "本关蜂蜜、小球和进度不会保留\n已获得的金币与通关记录不受影响";
            resumeButton.GetComponentInChildren<Text>().text = choice == LeaveChoice.Restart ? "确认重新开始"
                : choice == LeaveChoice.Home ? "确认返回小屋" : "确认退出游戏";
            restartButton.interactable = choice == LeaveChoice.Restart;
            homeButton.interactable = choice == LeaveChoice.Home;
            quitButton.interactable = choice == LeaveChoice.Quit;
            (choice == LeaveChoice.Restart ? restartButton : choice == LeaveChoice.Home ? homeButton : quitButton)
                .GetComponentInChildren<Text>().text = "取消操作";
        }

        private void ExecuteLeave(LeaveChoice choice)
        {
            if (choice == LeaveChoice.Restart)
            {
                PlayerDataHandler.UpdateTargetLevel(IngameManager.Instance.CurrentLevel);
                Leave("Ingame");
            }
            else if (choice == LeaveChoice.Home) Leave("Home");
            else QuitImmediately();
        }

        private void Leave(string scene)
        {
            if (leaving || IngameManager.Instance == null) return;
            leaving = true;
            IngameManager.Instance.PrepareToLeave();
            ServicesManager.Instance.SoundManager.StopMusic(0);
            LoadingManager.SetTargetScene(scene);
            ViewManager.Instance.OnShowView(ViewType.LOADING_VIEW);
            SceneManager.LoadScene("Loading");
        }

        public void Quit()
        {
            RequestLeave(LeaveChoice.Quit);
        }

        private void QuitImmediately()
        {
            if (leaving) return;
            leaving = true;
            PlayerPrefs.Save();
            IngameManager.Instance?.PrepareToLeave();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && overlay != null) Pause();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && overlay != null) Pause();
        }

        private void OnDisable()
        {
            IngameManager.Instance?.SetPaused(false);
            if (overlay != null) overlay.SetActive(false);
        }
    }
}
