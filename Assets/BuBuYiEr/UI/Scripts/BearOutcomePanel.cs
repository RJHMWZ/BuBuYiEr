using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearOutcomePanel : MonoBehaviour
    {
        private Text title, status, body, reward, primaryLabel, secondaryLabel, leftLabel, rightLabel;
        private Image[] stars;
        private Button primary, secondary, left, right;
        private UnityAction primaryAction, secondaryAction, leftAction, rightAction;
        private GameObject root;
        private bool leaving;
        private float revealAge;
        private bool presenting, wonPresentation;
        private int earnedStars, coinReward, shownReward = -1;
        private RectTransform rewardIcon;

        private void Choose(UnityAction action)
        {
            if (leaving || action == null) return;
            leaving = true;
            primary.interactable = secondary.interactable = left.interactable = right.interactable = false;
            action.Invoke();
        }

        public void Initialize(BaseViewController owner, BearMenuArt art)
        {
            foreach (Transform child in owner.transform) child.gameObject.SetActive(false);
            RectTransform overlay = BearMenuFactory.Node(owner.transform, "BearOutcomeUI", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(overlay);
            root = overlay.gameObject;
            overlay.gameObject.AddComponent<BearPanelFade>();
            overlay.gameObject.AddComponent<Image>().color = new Color(.23f, .12f, .05f, .63f);
            RectTransform frame = BearGameTheme.Frame(overlay, "OutcomeSafeArea");
            BearGameTheme.Bears(art, frame, 272, 210, .88f);
            RectTransform card = BearGameTheme.Paper(frame, "OutcomePaper", 128, 403, 768, 990, BearGameTheme.Cream, 64).rectTransform;
            BearGameTheme.Picture(art, card, "TitleRibbon", "Setting/TitleBg", 62, -42, 644, 139, true);
            title = BearGameTheme.Label(art, card, "Title", "", 76, -22, 616, 100, 48);
            status = BearGameTheme.Label(art, card, "RoundStatus", "", 57, 107, 654, 63, 31);
            stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = BearGameTheme.Picture(art, card, "Star" + i, "IconStar", 207 + i * 121, 186, 111, 104);
                BearMenuFactory.CenterImage(stars[i]);
            }
            body = BearGameTheme.Label(art, card, "RoundMessage", "", 65, 321, 638, 127, 30);
            BearGameTheme.Picture(art, card, "RewardPaper", "Setting/ChoicBg", 79, 466, 610, 83, true);
            Image coin=BearGameTheme.Picture(art, card, "PawCoin", "IconCoin", 104, 471, 71, 70);
            BearMenuFactory.CenterImage(coin);rewardIcon=coin.rectTransform;
            reward = BearGameTheme.Label(art, card, "Reward", "", 185, 469, 472, 76, 29);
            primary = BearGameTheme.Button(art, card, "Primary", "", 100, 580, 568, 118, BearGameTheme.Gold, () => Choose(primaryAction));
            secondary = BearGameTheme.Button(art, card, "Secondary", "", 100, 721, 568, 112, BearGameTheme.Pink, () => Choose(secondaryAction));
            left = BearGameTheme.Button(art, card, "Left", "", 62, 854, 311, 108, BearGameTheme.Cream, () => Choose(leftAction));
            right = BearGameTheme.Button(art, card, "Right", "返回小屋", 395, 854, 311, 108, BearGameTheme.Cream, () => Choose(rightAction));
            primaryLabel = primary.GetComponentInChildren<Text>();
            secondaryLabel = secondary.GetComponentInChildren<Text>();
            leftLabel = left.GetComponentInChildren<Text>();
            rightLabel = right.GetComponentInChildren<Text>();
            leftLabel.fontSize = rightLabel.fontSize = 29;
        }

        private void ShowRound()
        {
            root.SetActive(true);
            leaving = false;
            primary.interactable = secondary.interactable = left.interactable = right.interactable = true;
            IngameManager game = IngameManager.Instance;
            int seconds = Mathf.FloorToInt(game.Board.Elapsed);
            status.text = "第 " + game.CurrentLevel + " 关 · 用时 " + seconds / 60 + ":" + (seconds % 60).ToString("00")
                + " · 糖砖 " + game.Board.Destroyed + "/" + game.Board.Total;
            reward.text = game.IngameState == IngameState.Ingame_CompleteLevel
                ? "通关获得 +" + ServicesManager.Instance.CoinManager.CollectedCoins + " 金币\n已存入小钱包"
                : "金币与收藏均已保留\n重新挑战不会扣除金币";
            rightLabel.text = "返回小屋";
        }

        public void ShowResult(EndGameViewController view)
        {
            ShowRound();
            IngameManager game = IngameManager.Instance;
            bool won = game.IngameState == IngameState.Ingame_CompleteLevel;
            body.rectTransform.anchoredPosition = new Vector2(65, won ? -321 : -210);
            body.rectTransform.sizeDelta = new Vector2(638, won ? 127 : 220);
            title.text = won ? "糖砖清空啦" : "休息一下再出发";
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(won);
                stars[i].color = i < game.CurrentStar ? Color.white : new Color(.70f, .56f, .39f, .38f);
            }
            int unlocked = game.NewlyUnlockedStory;
            body.text = won ? unlocked >= 0 ? "解锁了一段新回忆：" + BearCollectionPanel.StoryTitles[unlocked] + "\n回图鉴看看，把这份甜蜜收藏起来。"
                : game.HasNextLevel ? "每一次弹跳，都装着一点小小的快乐。\n准备好了，就去挑战下一盘糖砖吧。" : "已经完成了目前全部关卡！\n可以回小屋看看故事，也可以重玩喜欢的关卡。"
                : game.FailureReason;
            if(won && game.NewlyUnlockedBall>=0)
                body.text="新伙伴加入："+CandyBreakoutSimulation.Names[game.NewlyUnlockedBall]+"！\n"+CandyBreakoutSimulation.Descriptions[game.NewlyUnlockedBall]+" · 下一关可用蜂蜜购买。";
            primaryLabel.text = won ? game.HasNextLevel ? "继续第 " + (game.CurrentLevel + 1) + " 关" : "回到小屋" : "重新开始";
            secondaryLabel.text = won ? unlocked >= 0 ? "查看新回忆" : "看看图鉴" : "选择关卡";
            if(won && game.NewlyUnlockedBall>=0)secondaryLabel.text="认识新伙伴";
            leftLabel.text = won ? "再玩本关" : "看看图鉴";
            primaryAction = view.OnClickPlayButton;
            secondaryAction = won ? (UnityAction)view.OnClickCollectionButton : view.OnClickLevelsButton;
            leftAction = won ? (UnityAction)view.OnClickReplayButton : view.OnClickCollectionButton;
            rightAction = view.OnClickHomeButton;
            wonPresentation=won;earnedStars=game.CurrentStar;
            coinReward=ServicesManager.Instance.CoinManager.CollectedCoins;
            shownReward=-1;revealAge=0;presenting=true;
            Present();
        }

        private void Update()
        {
            if (!presenting || root == null || !root.activeInHierarchy) return;
            revealAge+=Time.unscaledDeltaTime;
            Present();
            if (revealAge>=.65f) presenting=false;
        }

        private void Present()
        {
            if (!wonPresentation) { rewardIcon.localScale=Vector3.one; return; }
            for(int i=0;i<stars.Length;i++)
            {
                bool earned=i<earnedStars;
                float t=Mathf.Clamp01((revealAge-.04f-i*.09f)/.22f);
                stars[i].rectTransform.localScale=Vector3.one*(earned?Mathf.Lerp(.55f,1,t)+Mathf.Sin(t*Mathf.PI)*.15f:1);
                stars[i].color=earned?new Color(1,1,1,Mathf.SmoothStep(0,1,t)):new Color(.70f,.56f,.39f,.38f);
            }
            float count=Mathf.Clamp01((revealAge-.12f)/.34f);
            int value=Mathf.RoundToInt(Mathf.Lerp(0,coinReward,1-Mathf.Pow(1-count,3)));
            if(value!=shownReward)
            {
                shownReward=value;
                reward.text=(IngameManager.Instance.FirstClear?"首次通关":"重玩通关")+" +"+value+" 金币\n含新星奖励 · 已存入钱包";
            }
            rewardIcon.localScale=Vector3.one*(1+Mathf.Sin(count*Mathf.PI)*.09f);
        }

        // Used by still captures; this only settles visuals and never grants rewards.
        public void CompletePresentation()
        {
            revealAge=1;presenting=false;
            Present();
        }

        public void Hide()
        {
            presenting=false;
            if (root != null) root.SetActive(false);
        }
    }
}
