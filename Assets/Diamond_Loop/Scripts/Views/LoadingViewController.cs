using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class LoadingViewController : BaseViewController
    {
        [SerializeField] private BearMenuArt themeArt = null;
        private bool themeBuilt;

        [SerializeField] private Text loadingPercentText = null;
        [SerializeField] private Slider loadingSlider = null;



        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>


        public override void OnShow()
        {
            if (!themeBuilt && themeArt != null)
            {
                BearGameTheme.BuildLoading(this, themeArt, out loadingSlider, out loadingPercentText);
                themeBuilt = true;
            }
            SetLoadingAmount(0f);
        }

        public override void OnClose()
        {
            loadingSlider.value = 0f;
            gameObject.SetActive(false);
        }


        /// <summary>
        /// Set the loading amount.
        /// </summary>
        /// <param name="amount"></param>
        public void SetLoadingAmount(float amount)
        {
            amount = Mathf.Clamp01(amount);
            loadingSlider.value = amount;
            loadingPercentText.text = Mathf.RoundToInt(amount * 100f) + "%";
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>
    }

}
