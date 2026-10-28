using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class SlotPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform mainPanelTrans = null;
        

        /// <summary>
        /// Coroutine scale the given RectTransform.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="startScale"></param>
        /// <param name="endScale"></param>
        /// <param name="scalingTime"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator CRScaleRect(RectTransform rect, Vector2 startScale, Vector2 endScale, float scalingTime, Action callback = null)
        {
            rect.localScale = startScale;
            float t = 0;
            while (t < scalingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / scalingTime);
                rect.localScale = Vector2.Lerp(startScale, endScale, factor);
                yield return null;
            }

            callback?.Invoke();
        }


        /// <summary>
        /// Coroutine fade in the given CanvasGroup with fadingTime.
        /// </summary>
        /// <param name="canvasGroup"></param>
        /// <param name="fadingTime"></param>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator CRFadeInCanvasGroup(CanvasGroup canvasGroup, float fadingTime, float delay, Action callback = null)
        {
            yield return new WaitForSeconds(delay);
            canvasGroup.alpha = 0f;
            float t = 0;
            while (t < fadingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / fadingTime);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, factor);
                yield return null;
            }
            callback?.Invoke();
        }



        /// <summary>
        /// Coroutine fade out the given CanvasGroup with fadingTime.
        /// </summary>
        /// <param name="canvasGroup"></param>
        /// <param name="fadingTime"></param>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator CRFadeOutCanvasGroup(CanvasGroup canvasGroup, float fadingTime, float delay, Action callback = null)
        {
            yield return new WaitForSeconds(delay);
            canvasGroup.alpha = 1f;
            float t = 0;
            while (t < fadingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / fadingTime);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, factor);
                yield return null;
            }
            callback?.Invoke();
        }



        public void OnShow()
        {
            mainPanelTrans.localScale = Vector3.zero;
            StartCoroutine(CRScaleRect(mainPanelTrans, Vector3.zero, Vector3.one, 0.5f));
        }


        public void OnClickConfirmButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Rewarded);
            IngameManager.Instance.ConveyorManager.IncreaseSlot();
            PlayerDataHandler.UpdateSlotCount(-1);
            ViewManager.Instance.IngameViewController.ShowSlotPanel(false);
        }


        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            mainPanelTrans.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
    }
}
