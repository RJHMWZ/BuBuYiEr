using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class ReviveViewController : BaseViewController
    {
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private RectTransform reviveButtonTrans = null;
        [SerializeField] private Image reviveSlider = null;
        [SerializeField] private Text countDownText = null;


        /// <summary>
        /// Coroutine fill out the revive slider. 
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRFillOutReviveSlider(float delay)
        {
            countDownText.text = IngameManager.Instance.ReviveWaitTime.ToString();
            yield return new WaitForSeconds(delay);
            float waitTime = IngameManager.Instance.ReviveWaitTime;
            float t = 0;
            while (t < waitTime)
            {
                t += Time.deltaTime;
                float factor = t / waitTime;
                reviveSlider.fillAmount = Mathf.Lerp(1f, 0f, factor);

                yield return null;
            }
            IngameManager.Instance.GameOver();
        }



        /// <summary>
        /// Coroutine count the count down text. 
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRCountDownText(float delay)
        {
            float waitTime = IngameManager.Instance.ReviveWaitTime;
            countDownText.text = waitTime.ToString();
            yield return new WaitForSeconds(delay);
            while (waitTime > 0)
            {
                waitTime--;
                yield return new WaitForSeconds(1f);
                countDownText.text = waitTime.ToString();
            }
        }



        /// <summary>
        /// Coroutine scale the revive button.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRScaleReviveButton()
        {
            Vector3 startScale = Vector3.one * 0.8f;
            Vector3 endScale = Vector3.one;
            reviveButtonTrans.localScale = startScale;

            float scalingTime = 0.25f;
            float t = 0;
            while (gameObject.activeInHierarchy)
            {
                t = 0;
                while (t < scalingTime)
                {
                    t += Time.deltaTime;
                    float factor = t / scalingTime;
                    reviveButtonTrans.localScale = Vector3.Lerp(startScale, endScale, factor);
                    yield return null;
                }

                t = 0;
                while (t < scalingTime)
                {
                    t += Time.deltaTime;
                    float factor = t / scalingTime;
                    reviveButtonTrans.localScale = Vector3.Lerp(endScale, startScale, factor);
                    yield return null;
                }
            }
        }




        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>


        public override void OnShow()
        {
            FadeInCanvasGroup(canvasGroup, 1f);
            StartCoroutine(CRScaleReviveButton());
            StartCoroutine(CRFillOutReviveSlider(0.75f));
            StartCoroutine(CRCountDownText(0.75f));
        }

        public override void OnClose()
        {
            canvasGroup.alpha = 0f;
            reviveSlider.fillAmount = 1f;
            reviveButtonTrans.localScale = Vector3.one;
            gameObject.SetActive(false);
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>

        public void OnClickReviveButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ServicesManager.Instance.AdManager.ShowRewardedAd(RewardedAdTarget.REVIVE_PLAYER);
        }

        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            IngameManager.Instance.GameOver();
        }
    }
}
