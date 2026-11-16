using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearCollectionPanel : MonoBehaviour
    {
        public const int LayoutVersion = 4;
        public int layoutVersion;
        public BearHomeMenuController menu;
        public Image[] tabArtwork, storyArtwork, expressionArtwork;
        public GameObject[] tabRoots, storyLocks, expressionLocks, storyHighlights, expressionHighlights;
        public Text[] storyStates, expressionStates;
        public Text countText, detailTitle, detailBody, readLabel, favoriteLabel;
        public Image detailImage, detailLock, favoriteIcon;
        public Button readButton, favoriteButton;
        public GameObject detailRoot;
        public BearMenuShape[] ballArtwork;
        public GameObject[] ballLocks, ballHighlights;
        public Text[] ballStates, journeyStates;
        public Button[] journeyClaims;
        public BearMenuShape detailBall;

        private int category = 1, selectedStory, selectedExpression, selectedBall;
        private int lastLevel = -1;
        private const string FavoritePrefix = "BUBU_COLLECTION_FAVORITE_";
        public static readonly string[] StoryTitles = {
            "奶茶约会", "星空晚安", "糖果回忆", "花园散步", "甜蜜旅行", "野餐时光", "午后读书", "窗边小憩", "手作糖果", "雨后彩虹", "星光邮局", "一百零一颗心"
        };
        public static readonly string[] StoryPictures = {
            "MilkTea", "GoodNight", "CandyJar", "Garden", "Travel", "Picnic", "Reading", "CatNap", "CandyJar", "Garden", "GoodNight", "Travel"
        };
        private static readonly string[] StoryBodies = {
            "阳光正好，我们一起去喝了最爱的奶茶。\n甜甜的味道，就像和你在一起的心情。\n下一杯，也想和你分享。",
            "月亮悄悄爬上窗边，星星也亮了起来。\n盖好软软的小被子，靠在你身边。\n今天也想轻轻说一声晚安。",
            "把每一颗糖果装进小罐子，\n也把和你一起的快乐慢慢收藏。\n等到想你的时候，就打开尝一颗。",
            "花开的时候，我们去小花园散步。\n风吹过花瓣，留下淡淡的香。\n每一步，都有你的陪伴。",
            "背上小小的行囊，向新风景出发。\n路上有云朵、有树荫，还有期待。\n身边的你，就是最好的旅伴。",
            "带上三明治、水果和喜欢的饮料，\n在草地上铺开粉色的小餐布。\n最喜欢的午后，是和你一起慢慢度过。",
            "把柔软的靠垫摆好，翻开喜欢的书。\n故事里有新的世界，身边有熟悉的温暖。\n这一页，想读给你听。",
            "窗外的阳光暖暖的，小猫也打起了瞌睡。\n毛线球停在脚边，时间慢了下来。\n就这样，安安静静地陪着你。",
            "第五十盘糖砖清空后，我们试着自己熬一罐糖。\n布布负责搅拌，一二偷偷尝了一小口。\n原来最甜的配方，是一起耐心等待。",
            "雨停了，花园里挂起一条小彩虹。\n我们把一路遇见的伙伴都请来喝茶。\n每一种颜色，都藏着一段共同的旅程。",
            "我们给过去的自己写了一封信。\n谢谢那个愿意从第一块糖砖开始的你。\n信封上画一颗星，寄往下一个期待。",
            "第一百零一盘糖砖，也变成了罐子里的星光。\n旅行有终点，陪伴却不会结束。\n把这一百零一颗小小的心，全部送给你。"
        };
        public static readonly string[] ExpressionTitles = { "布布开心", "一二开心", "给你一颗心", "送你一朵花" };
        public static readonly string[] ExpressionPictures = { "BearWhite", "BearBrown", "IconLove", "Daily/IconFlower" };
        private static readonly string[] ExpressionBodies = {
            "今天也要开开心心呀！", "有你在，每一天都很甜。", "把这颗小小的心送给你。", "给你一朵花，也给你一个好心情。"
        };

        public static int StoryUnlockLevel(int index) => index == 0 ? 1 : index<8?index*5:index==8?51:index==9?71:index==10?91:102;
        private static int ExpressionUnlockLevel(int index) => index < 2 ? 1 : index == 2 ? 5 : 10;
        private int CurrentLevel => Application.isPlaying && PlayerDataHandler.HasData() ? PlayerDataHandler.GetCurrentLevel() : 1;

        public void Show() => SelectTab(category);

        public void SelectTab(int index)
        {
            category = Mathf.Clamp(index, 0, 3);
            for (int i = 0; i < tabRoots.Length; i++)
            {
                tabRoots[i].SetActive(i == category);
                tabArtwork[i].sprite = menu.art.Get(i == category ? "Shop/TabSelected" : "Shop/TabNormal");
                tabArtwork[i].rectTransform.sizeDelta = new Vector2(200, i == category ? 103 : 82);
            }
            detailRoot.SetActive(category != 3);
            RefreshProgress(true);
        }

        public void SelectStory(int index)
        {
            if (index < 0 || index >= StoryTitles.Length) return;
            selectedStory = index;
            if (category != 1) SelectTab(1);
            else RefreshSelected();
        }

        public void SelectExpression(int index)
        {
            if (index < 0 || index >= ExpressionTitles.Length) return;
            selectedExpression = index;
            if (category != 0) SelectTab(0);
            else RefreshSelected();
        }

        public void RefreshProgress(bool force = false)
        {
            int level = CurrentLevel;
            if (!force && level == lastLevel && category != 3) return;
            lastLevel = level;
            int unlockedStories = 0, unlockedExpressions = 0;
            for (int i = 0; i < storyLocks.Length; i++)
            {
                bool unlocked = level >= StoryUnlockLevel(i);
                if (unlocked) unlockedStories++;
                storyLocks[i].SetActive(!unlocked);
                storyArtwork[i].color = unlocked ? Color.white : new Color(.83f, .78f, .72f, 1);
                storyStates[i].text = unlocked ? "第 " + (i + 1) + " 章" : "第 " + StoryUnlockLevel(i) + " 关解锁";
            }
            for (int i = 0; i < expressionLocks.Length; i++)
            {
                bool unlocked = level >= ExpressionUnlockLevel(i);
                if (unlocked) unlockedExpressions++;
                expressionLocks[i].SetActive(!unlocked);
                expressionArtwork[i].color = unlocked ? Color.white : new Color(.83f, .78f, .72f, 1);
                expressionStates[i].text = unlocked ? "已解锁" : "第 " + ExpressionUnlockLevel(i) + " 关解锁";
            }
            int unlockedBalls=0;
            for(int i=0;i<ballArtwork.Length;i++)
            {
                bool unlocked=CandyProgression.BallUnlocked(i,level);
                if(unlocked)unlockedBalls++;
                ballLocks[i].SetActive(!unlocked);
                ballArtwork[i].color=ballArtwork[i].bottomColor=unlocked?CandyCosmetics.SkinColor(i):new Color32(214,204,187,255);
                ballArtwork[i].SetVerticesDirty();
                var portrait=ballArtwork[i].GetComponentInChildren<CandyBallPortrait>(true);
                if(portrait!=null){portrait.locked=!unlocked;portrait.SetVerticesDirty();}
                ballStates[i].text=unlocked?"已加入小队":CandyProgression.UnlockText(i);
            }
            int claimed=0;
            for(int i=0;i<journeyClaims.Length;i++)
            {
                bool done=Application.isPlaying && PlayerDataHandler.IsJourneyClaimed(i);
                int progress=Application.isPlaying?CandyProgression.JourneyProgress(i):0;
                if(done)claimed++;
                journeyClaims[i].interactable=!done && progress>=CandyProgression.JourneyGoals[i];
                journeyClaims[i].GetComponentInChildren<Text>().text=done?"已领取":progress>=CandyProgression.JourneyGoals[i]?"领取奖励":"继续旅行";
                journeyStates[i].text=Mathf.Min(progress,CandyProgression.JourneyGoals[i])+" / "+CandyProgression.JourneyGoals[i]+"    奖励 "+CandyProgression.JourneyCoins[i]+" 金币";
            }
            countText.text = category == 1 ? "故事回忆 · 已解锁 " + unlockedStories + " / " + StoryTitles.Length
                : category == 0 ? "心情表情 · 已解锁 " + unlockedExpressions + " / " + ExpressionTitles.Length
                : category==2?"糖果伙伴 · 已解锁 "+unlockedBalls+" / 6":"旅程手账 · 已领取 "+claimed+" / "+CandyProgression.JourneyCount;
            RefreshSelected();
        }

        private void RefreshSelected()
        {
            for (int i = 0; i < storyHighlights.Length; i++) storyHighlights[i].SetActive(i == selectedStory);
            for (int i = 0; i < expressionHighlights.Length; i++) expressionHighlights[i].SetActive(i == selectedExpression);
            for(int i=0;i<ballHighlights.Length;i++)ballHighlights[i].SetActive(i==selectedBall);
            detailBall.gameObject.SetActive(category==2);
            detailImage.gameObject.SetActive(category!=2);
            if(category==3)return;
            if(category==2)
            {
                bool available=CandyProgression.BallUnlocked(selectedBall,CurrentLevel);
                detailTitle.text=CandyBreakoutSimulation.Names[selectedBall];
                detailBody.text=available?CandyProgression.BallTips[selectedBall]:CandyProgression.UnlockText(selectedBall)+"。\n"+CandyProgression.BallTips[selectedBall];
                detailBall.color=detailBall.bottomColor=CandyCosmetics.SkinColor(selectedBall);detailBall.SetVerticesDirty();
                var portrait=detailBall.GetComponentInChildren<CandyBallPortrait>(true);
                if(portrait!=null){portrait.kind=selectedBall;portrait.locked=!available;portrait.SetVerticesDirty();}
                detailLock.gameObject.SetActive(!available);
                readButton.interactable=available;readLabel.text=available?"伙伴攻略":"尚未解锁";
                favoriteButton.gameObject.SetActive(false);
                detailRoot.transform.Find("FavoritePaper").gameObject.SetActive(false);
                favoriteIcon.gameObject.SetActive(false);favoriteLabel.gameObject.SetActive(false);
                return;
            }
            favoriteButton.gameObject.SetActive(true);
            detailRoot.transform.Find("FavoritePaper").gameObject.SetActive(true);
            favoriteIcon.gameObject.SetActive(true);favoriteLabel.gameObject.SetActive(true);
            bool story = category == 1;
            int index = story ? selectedStory : selectedExpression;
            int unlockLevel = story ? StoryUnlockLevel(index) : ExpressionUnlockLevel(index);
            bool unlocked = CurrentLevel >= unlockLevel;
            detailTitle.text = story ? StoryTitles[index] : ExpressionTitles[index];
            detailImage.sprite = menu.art.Get(story ? "Collect/" + StoryPictures[index] : ExpressionPictures[index]);
            detailImage.color = unlocked ? Color.white : new Color(.83f, .78f, .72f, 1);
            detailLock.gameObject.SetActive(!unlocked);
            detailBody.text = unlocked ? story ? StoryBodies[index] : ExpressionBodies[index]
                : "到达第 " + unlockLevel + " 关，\n解锁这份甜甜的" + (story ? "回忆。" : "心情。") + "\n继续和小熊一起出发吧！";
            readButton.interactable = unlocked;
            favoriteButton.interactable = unlocked;
            readLabel.text = unlocked ? story ? "查看故事" : "查看表情" : "尚未解锁";
            bool favorite = Application.isPlaying && PlayerPrefs.GetInt(FavoriteKey, 0) == 1;
            favoriteLabel.text = favorite ? "已收藏" : "收藏";
            favoriteIcon.color = favorite ? new Color32(255, 158, 170, 255) : Color.white;
        }

        private string FavoriteKey => FavoritePrefix + (category == 1 ? "STORY_" + selectedStory : "EXPRESSION_" + selectedExpression);

        public void ToggleFavorite()
        {
            if (!Application.isPlaying || category == 2 || !favoriteButton.interactable) return;
            PlayerPrefs.SetInt(FavoriteKey, PlayerPrefs.GetInt(FavoriteKey, 0) == 0 ? 1 : 0);
            PlayerPrefs.Save();
            RefreshSelected();
        }

        public void ReadSelected()
        {
            if (!readButton.interactable || category==3) return;
            if(category==2){menu.Message(CandyBreakoutSimulation.Names[selectedBall],CandyProgression.BallTips[selectedBall]+"\n\n"+CandyProgression.UnlockText(selectedBall)+"\n伙伴解锁永久保留；每关用蜂蜜购买与升级。");return;}
            menu.Message(detailTitle.text, category == 1 ? StoryBodies[selectedStory] : ExpressionBodies[selectedExpression], detailImage.sprite);
        }
        public void SelectBall(int index)
        {
            if(index<0 || index>=6)return;
            selectedBall=index;
            if(category!=2)SelectTab(2);else RefreshSelected();
        }
        public void ClaimJourney(int index)
        {
            if(!Application.isPlaying || !PlayerDataHandler.TryClaimJourney(index))return;
            ServicesManager.Instance.CoinManager.NotifyBalanceChanged();
            ServicesManager.Instance.SoundManager.PlayCue(GameSoundCue.Reward);
            RefreshProgress(true);
            menu.Message("旅程奖励",CandyProgression.JourneyTitle(index)+"\n获得 "+CandyProgression.JourneyCoins[index]+" 金币，已存入钱包。");
        }
    }
}
