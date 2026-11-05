using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearSoloHud : MonoBehaviour
    {
        private IngameViewController owner;
        private Text goal, hint, introTitle, introBody, introGoal, startCaption;
        private RectTransform fill;
        private GameObject hintRoot, intro;
        private int lastCompleted = -1, lastTotal = -1;
        private float noticeUntil;
        private string notice;

        public void Initialize(IngameViewController view, BearMenuArt art)
        {
            owner = view;
            RectTransform root = BearMenuFactory.Node(view.transform, "SoloRoundUI", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(root);
            RectTransform frame = BearGameTheme.Frame(root, "SoloHudSafeArea", true);
            RectTransform goalPaper = BearGameTheme.Paper(frame, "RoundGoal", 223, 167, 578, 76, BearGameTheme.Cream, 30).rectTransform;
            goal = BearGameTheme.Label(art, goalPaper, "Goal", "", 16, 6, 546, 44, 29);
            BearGameTheme.Paper(goalPaper, "ProgressTrack", 45, 55, 488, 10, new Color32(237, 218, 193, 255), 5).border = 0;
            fill = BearGameTheme.Paper(goalPaper, "ProgressFill", 45, 55, 0, 10, BearGameTheme.Gold, 5).rectTransform;
            fill.GetComponent<BearMenuShape>().border = 0;
            RectTransform hintPaper = BearGameTheme.Paper(frame, "RoundHint", 78, 0, 868, 65, BearGameTheme.Cream, 28).rectTransform;
            BearGameTheme.Bottom(hintPaper, 78, 315);
            hintRoot = hintPaper.gameObject;
            hint = BearGameTheme.Label(art, hintPaper, "Hint", "", 16, 4, 836, 57, 26);

            RectTransform overlay = BearMenuFactory.Node(root, "RoundIntroduction", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color = new Color(.23f, .12f, .05f, .56f);
            intro = overlay.gameObject;
            RectTransform safe = BearGameTheme.Frame(overlay, "IntroductionSafeArea");
            BearGameTheme.Bears(art, safe, 272, 322, .88f);
            RectTransform card = BearGameTheme.Paper(safe, "IntroductionPaper", 128, 514, 768, 650, BearGameTheme.Cream, 64).rectTransform;
            BearGameTheme.Picture(art, card, "TitleRibbon", "Setting/TitleBg", 74, -39, 620, 132, true);
            introTitle = BearGameTheme.Label(art, card, "Title", "", 82, -21, 604, 96, 50);
            introBody = BearGameTheme.Label(art, card, "Instructions", "", 65, 108, 638, 125, 31);
            BearIllustration.Create(card, "CandyJar", BearDrawing.Jar, 337, 239, 94, 94);
            introGoal = BearGameTheme.Label(art, card, "Goal", "", 65, 340, 638, 59, 38);
            Button start = BearGameTheme.Button(art, card, "BeginRound", "开始收集", 100, 432, 568, 104, BearGameTheme.Gold, Begin);
            startCaption = start.GetComponentInChildren<Text>();
            BearGameTheme.Label(art, card, "PauseTip", "随时可以点击右上角暂停", 70, 559, 628, 48, 26);
            intro.SetActive(false);
        }

        public void ShowIntroduction()
        {
            IngameManager game = IngameManager.Instance;
            introTitle.text = "第 " + game.CurrentLevel + " 关";
            introBody.text = game.IsLearningRound
                ? "点选糖果罐，送它上传送带收集同色糖果。\n先试着装满第一罐，这段学习过程不计时。"
                : "选对颜色，把罐子送上糖果传送带。\n安排好空位，装满所有糖果罐吧。";
            introGoal.text = "本关目标：装满 " + game.TotalPots + " 罐";
            startCaption.text = game.IsLearningRound ? "我来试试" : "开始收集";
            intro.SetActive(true);
            RefreshGoal();
        }

        private void Begin()
        {
            if (IngameManager.Instance == null || IngameManager.Instance.IsPaused) return;
            intro.SetActive(false);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            IngameManager.Instance.OnTutorialClosed();
        }

        public void HideIntroduction()
        {
            if (intro != null) intro.SetActive(false);
            noticeUntil = 0;
        }

        public void ShowHint(string message)
        {
            notice = message;
            noticeUntil = Time.unscaledTime + 4;
        }

        private void Update()
        {
            IngameManager game = IngameManager.Instance;
            if (owner == null || game == null) return;
            RefreshGoal();
            bool showHint = game.IsRoundStarted && !owner.IsToolPanelOpen && !intro.activeSelf;
            if (hintRoot.activeSelf != showHint) hintRoot.SetActive(showHint);
            string message = Time.unscaledTime < noticeUntil ? notice : game.IsLearningRound
                ? game.HasDeployedPot ? "很好！罐子经过同色糖果时，会自动收集" : game.TutorialPot != null
                    ? "先点击亮起的糖果罐，试着装满第一罐" : "选择与糖果同色的罐子，试着装满第一罐"
                : "点击罐子收集同色糖果，记得给传送带留些空位";
            if (hint.text != message) hint.text = message;
        }

        private void RefreshGoal()
        {
            IngameManager game = IngameManager.Instance;
            if (game == null || (lastCompleted == game.CompletedPots && lastTotal == game.TotalPots)) return;
            lastCompleted = game.CompletedPots;
            lastTotal = game.TotalPots;
            goal.text = "装满糖果罐  " + lastCompleted + " / " + lastTotal;
            fill.sizeDelta = new Vector2(lastTotal > 0 ? 488f * lastCompleted / lastTotal : 0, 10);
            fill.GetComponent<BearMenuShape>().SetVerticesDirty();
        }
    }
}
