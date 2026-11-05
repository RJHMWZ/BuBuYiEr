using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class ReviveViewController : BaseViewController
    {
        private BearOutcomePanel outcome;
        private bool adPending;
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
            adPending = false;
            BearMenuArt art = ViewManager.Instance.IngameViewController.ThemeArt;
            if (art != null)
            {
                StopAllCoroutines();
                canvasGroup.alpha = 1;
                canvasGroup.interactable = canvasGroup.blocksRaycasts = true;
                if (outcome == null)
                {
                    outcome = gameObject.AddComponent<BearOutcomePanel>();
                    outcome.Initialize(this, art);
                }
                outcome.ShowRecovery(this);
                return;
            }
            FadeInCanvasGroup(canvasGroup, 1f);
            StartCoroutine(CRScaleReviveButton());
            StartCoroutine(CRFillOutReviveSlider(0.75f));
            StartCoroutine(CRCountDownText(0.75f));
        }

        public override void OnClose()
        {
            StopAllCoroutines();
            if (outcome != null)
            {
                outcome.Hide();
                gameObject.SetActive(false);
                return;
            }
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
            if (adPending || IngameManager.Instance == null) return;
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            if (IngameManager.Instance.FreeRecovery)
            {
                IngameManager.Instance.SetContinueGame();
                return;
            }
            if (!ServicesManager.Instance.AdManager.IsRewardedAdReady())
            {
                outcome?.SetAdPending(false, "暂时没有可观看的视频，可以重新开始或返回小屋。\n已经获得的糖果奖励会保留。");
                return;
            }
            adPending = true;
            outcome?.SetAdPending(true, "视频正在准备，完成观看后会继续本局。");
            ServicesManager.Instance.AdManager.ShowRewardedAd(RewardedAdTarget.REVIVE_PLAYER);
        }

        public void OnRecoveryAdClosed(bool completed)
        {
            adPending = false;
            if (!completed) outcome?.SetAdPending(false, "本次视频没有看完，你可以再试一次。\n也可以重新开始，已经获得的奖励会保留。");
        }

        public void OnClickReplayButton()
        {
            if (adPending) return;
            PlayerDataHandler.UpdateTargetLevel(IngameManager.Instance.CurrentLevel);
            LoadScene("Ingame", 0);
        }

        public void OnClickHomeButton()
        {
            if (!adPending) LoadScene("Home", 0);
        }

        public void OnClickCloseButton()
        {
            if (adPending) return;
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            IngameManager.Instance.GameOver();
        }
    }
}
