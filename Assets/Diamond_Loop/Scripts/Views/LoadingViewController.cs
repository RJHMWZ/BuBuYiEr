using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class LoadingViewController : BaseViewController
    {

        [SerializeField] private Text loadingPercentText = null;
        [SerializeField] private Slider loadingSlider = null;



        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>


        public override void OnShow()
        {
            loadingSlider.value = 0f;
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
            loadingSlider.value = amount;
            loadingPercentText.text = System.Math.Round((amount / 1f) * 100f, 2).ToString() + "%";
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>
    }

}