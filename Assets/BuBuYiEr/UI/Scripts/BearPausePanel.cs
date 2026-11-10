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
        private Text levelLabel;
        private Button pauseButton;
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
            RectTransform dialogFrame = BearGameTheme.Frame(dim, "PauseDialogSafeArea");
            RectTransform card = BearGameTheme.Paper(dialogFrame, "CreamPaper", 128, 400, 768, 930,
                BearGameTheme.Cream, 64).rectTransform;
            BearGameTheme.Bears(art, dialogFrame, 272, 207, .88f);
            BearGameTheme.Picture(art, card, "TitleCloud", "Setting/TitleBg", 123, -48, 522, 142, true);
            BearGameTheme.Label(art, card, "Title", "休息一下", 134, -29, 500, 104, 58);
            levelLabel = BearGameTheme.Label(art, card, "Level", "", 110, 109, 548, 58, 35);
            BearGameTheme.Label(art, card, "Hint", "糖果和小熊都会等你回来", 70, 171, 628, 58, 30);
            BearGameTheme.Button(art, card, "Resume", "继续游戏", 100, 272, 568, 110,
                BearGameTheme.Gold, Resume);
            BearGameTheme.Button(art, card, "Restart", "重新开始", 100, 412, 568, 104,
                BearGameTheme.Pink, Restart);
            BearGameTheme.Button(art, card, "Home", "返回主菜单", 100, 546, 568, 104,
                BearGameTheme.Cream, ReturnHome);
            BearGameTheme.Button(art, card, "Quit", "退出游戏", 100, 680, 568, 104,
                BearGameTheme.Cream, Quit);
            BearGameTheme.Picture(art, card, "Bow", "Setting/Decoration1", 291, 837, 186, 101);
            overlay.SetActive(false);
        }

        public void ResetPanel()
        {
            leaving = false;
            if (overlay != null) overlay.SetActive(false);
        }

        private void Update()
        {
            IngameManager manager = IngameManager.Instance;
            if (manager == null || view == null || !view.gameObject.activeInHierarchy) return;
            bool playing = manager.IngameState == IngameState.Ingame_Playing;
            if (pauseButton != null) pauseButton.interactable = playing && !leaving;
            if (overlay != null && overlay.activeSelf != manager.IsPaused) overlay.SetActive(manager.IsPaused);
            if (!leaving && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle()
        {
            if (leaving || IngameManager.Instance == null) return;
            if (IngameManager.Instance.IsPaused) Resume();
            else Pause();
        }

        private void Pause()
        {
            IngameManager manager = IngameManager.Instance;
            if (leaving || manager == null || manager.IngameState != IngameState.Ingame_Playing) return;
            ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Pause);
            manager.SetPaused(true);
            levelLabel.text = "第 " + manager.CurrentLevel + " 关 · 已暂停";
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Resume()
        {
            if (leaving) return;
            if(IngameManager.Instance!=null && IngameManager.Instance.IsPaused)ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Resume);
            if (overlay != null) overlay.SetActive(false);
            IngameManager.Instance?.SetPaused(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        public void Restart()
        {
            if (leaving || IngameManager.Instance == null) return;
            ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.UiClick);
            PlayerDataHandler.UpdateTargetLevel(IngameManager.Instance.CurrentLevel);
            Leave("Ingame");
        }

        public void ReturnHome()
        {
            if(!leaving)ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.UiClose);
            Leave("Home");
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
