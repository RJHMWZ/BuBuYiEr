using UnityEngine;

namespace ClawbearGames
{
    public sealed class IngameViewController : BaseViewController
    {
        [SerializeField] private BearMenuArt themeArt;
        public BearMenuArt ThemeArt => themeArt;
        private CandyBreakoutView gameplay;
        private BearPausePanel pausePanel;
        public override void OnShow()
        {
            if (gameplay == null)
            {
                gameplay = gameObject.AddComponent<CandyBreakoutView>();
                gameplay.Build(this, themeArt);
                pausePanel = gameObject.AddComponent<BearPausePanel>();
                pausePanel.Initialize(this, themeArt);
            }
            pausePanel.ResetPanel();
            gameplay.Bind(IngameManager.Instance);
        }
        public override void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
