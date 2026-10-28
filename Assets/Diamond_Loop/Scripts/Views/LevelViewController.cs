using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class LevelViewController : BaseViewController
    {
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color unlockedColor = Color.yellow;
        [SerializeField] private RectTransform confirmPanel = null;
        [SerializeField] private Image centerStar = null;
        [SerializeField] private Image rightStar = null;
        [SerializeField] private Image leftStar = null;
        [SerializeField] private Text levelText = null;

        private MapManager mapManager = null;
        private int targetLevel = 0;

        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>


        public override void OnShow()
        {
            confirmPanel.gameObject.SetActive(false);
            if (mapManager == null) { mapManager = FindFirstObjectByType<MapManager>(); }
        }

        public override void OnClose()
        {
            confirmPanel.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }



        /// <summary>
        /// Show the confirm panel with level.
        /// </summary>
        /// <param name="level"></param>
        public void ShowConfirmPanel(int level)
        {
            targetLevel = level;
            levelText.text = "LEVEL: " + level.ToString();
            confirmPanel.gameObject.SetActive(true);
            if (level == PlayerDataHandler.GetCurrentLevel())
            {
                centerStar.color = lockedColor;
                rightStar.color = lockedColor;
                leftStar.color = lockedColor;
            }
            else
            {
                centerStar.color = unlockedColor;
                rightStar.color = unlockedColor;
                leftStar.color = unlockedColor;
            }
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>
        /// 


        public void OnClickStartButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            PlayerDataHandler.UpdateTargetLevel(targetLevel);
            LoadScene("Ingame", 0.25f);
        }


        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            confirmPanel.gameObject.SetActive(false);
        }


        public void OnClickCurrentLevelButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            mapManager.SnapToCurrent();
        }


        public void OnClickHomeButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            LoadScene("Home", 0.25f);
        }
    }

}