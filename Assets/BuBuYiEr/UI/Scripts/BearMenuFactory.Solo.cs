using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static partial class BearMenuFactory
    {
        public static void EnsureSoloActions(BearHomeMenuController menu)
        {
            Transform main = menu.pages[0].transform;
            Transform play = main.Find("PlayAction");
            if (play == null) return;
            Transform solo = play.Find("SoloPlay");
            if (solo == null)
            {
                Transform artwork = play.Find("ButtonArtwork");
                Transform legacy = play.Find("StartGame");
                if (artwork != null) artwork.gameObject.SetActive(false);
                if (legacy != null) legacy.gameObject.SetActive(false);
                BearMenuShape paper = Shape(play, "SoloPlay", 187, 0, 650, 190, 90, Gold, new Color32(255, 204, 62, 255), 8);
                menu.soloPlayLabel = Label(menu, paper.transform, "Caption", "开始收集", 20, 0, 610, 190, 61);
                Hotspot(paper.transform, "Start", 0, 0, 650, 190, "play");
            }
            else menu.soloPlayLabel = solo.GetComponentInChildren<Text>();
            Transform level = main.Find("Header/LevelStatus");
            if (level != null && level.GetComponent<BearMenuAction>() == null)
            {
                Transform track = level.Find("ProgressTrack");
                Transform fill = level.Find("ProgressFill");
                if (track != null) track.gameObject.SetActive(false);
                if (fill != null) fill.gameObject.SetActive(false);
                Label(menu, level, "ChooseLevelHint", "选择关卡 ›", 109, 65, 173, 29, 23);
                Action((RectTransform)level, "levels");
            }
        }
    }
}
