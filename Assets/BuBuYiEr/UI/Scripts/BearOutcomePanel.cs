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

        public void Initialize(BaseViewController owner, BearMenuArt art)
        {
            foreach (Transform child in owner.transform) child.gameObject.SetActive(false);
            RectTransform overlay = BearMenuFactory.Node(owner.transform, "BearOutcomeUI", 0, 0, 1024, 1536);
            BearMenuFactory.Stretch(overlay);
            root = overlay.gameObject;
            overlay.gameObject.AddComponent<Image>().color = new Color(.23f, .12f, .05f, .63f);
            RectTransform frame = BearGameTheme.Frame(overlay, "OutcomeSafeArea");
            BearGameTheme.Bears(art, frame, 272, 210, .88f);
            RectTransform card = BearGameTheme.Paper(frame, "OutcomePaper", 128, 403, 768, 990, BearGameTheme.Cream, 64).rectTransform;
            BearGameTheme.Picture(art, card, "TitleRibbon", "Setting/TitleBg", 62, -42, 644, 139, true);
            title = BearGameTheme.Label(art, card, "Title", "", 76, -22, 616, 100, 48);
            status = BearGameTheme.Label(art, card, "RoundStatus", "", 57, 107, 654, 63, 31);
            stars = new Image[3];
            for (int i = 0; i < 3; i++) stars[i] = BearGameTheme.Picture(art, card, "Star" + i, "IconStar", 207 + i * 121, 186, 111, 104);
            body = BearGameTheme.Label(art, card, "RoundMessage", "", 65, 321, 638, 127, 30);
            BearGameTheme.Picture(art, card, "RewardPaper", "Setting/ChoicBg", 79, 466, 610, 83, true);
            BearGameTheme.Picture(art, card, "PawCoin", "IconCoin", 104, 471, 71, 70);
            reward = BearGameTheme.Label(art, card, "Reward", "", 185, 469, 472, 76, 29);
            primary = BearGameTheme.Button(art, card, "Primary", "", 100, 593, 568, 105, BearGameTheme.Gold, () => primaryAction?.Invoke());
            secondary = BearGameTheme.Button(art, card, "Secondary", "", 100, 727, 568, 94, BearGameTheme.Pink, () => secondaryAction?.Invoke());
            left = BearGameTheme.Button(art, card, "Left", "", 62, 861, 311, 79, BearGameTheme.Cream, () => leftAction?.Invoke());
            right = BearGameTheme.Button(art, card, "Right", "返回小屋", 395, 861, 311, 79, BearGameTheme.Cream, () => rightAction?.Invoke());
            primaryLabel = primary.GetComponentInChildren<Text>();
            secondaryLabel = secondary.GetComponentInChildren<Text>();
            leftLabel = left.GetComponentInChildren<Text>();
            rightLabel = right.GetComponentInChildren<Text>();
            leftLabel.fontSize = rightLabel.fontSize = 29;
        }

        private void ShowRound()
        {
            root.SetActive(true);
            primary.interactable = secondary.interactable = left.interactable = right.interactable = true;
            IngameManager game = IngameManager.Instance;
            status.text = "第 " + game.CurrentLevel + " 关 · 已打碎 " + game.Board.Destroyed + " / " + game.Board.Total + " 块糖砖";
            reward.text = "本局收集 +" + ServicesManager.Instance.CoinManager.CollectedCoins + " 金币\n已自动存入小钱包";
            rightLabel.text = "返回小屋";
        }

        public void ShowResult(EndGameViewController view)
        {
            ShowRound();
            IngameManager game = IngameManager.Instance;
            bool won = game.IngameState == IngameState.Ingame_CompleteLevel;
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
            primaryLabel.text = won ? game.HasNextLevel ? "继续第 " + (game.CurrentLevel + 1) + " 关" : "回到小屋" : "重新开始";
            secondaryLabel.text = won ? unlocked >= 0 ? "查看新回忆" : "看看图鉴" : "选择关卡";
            leftLabel.text = won ? "再玩本关" : "看看图鉴";
            primaryAction = view.OnClickPlayButton;
            secondaryAction = won ? (UnityAction)view.OnClickCollectionButton : view.OnClickLevelsButton;
            leftAction = won ? (UnityAction)view.OnClickReplayButton : view.OnClickCollectionButton;
            rightAction = view.OnClickHomeButton;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
