using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class ResultPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform panelTrans = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private RectTransform levelFailed = null;
        [SerializeField] private RectTransform levelCompleted = null;
        [SerializeField] private RectTransform leftStar = null;
        [SerializeField] private RectTransform leftCover = null;
        [SerializeField] private RectTransform centerStar = null;
        [SerializeField] private RectTransform centerCover = null;
        [SerializeField] private RectTransform rightStar = null;
        [SerializeField] private RectTransform rightCover = null;


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



        /// <summary>
        /// Handle on show this panel.
        /// </summary>
        public void OnShow()
        {
            if (IngameManager.Instance.IngameState == IngameState.Ingame_CompleteLevel)
            {
                levelFailed.gameObject.SetActive(false);
                levelCompleted.gameObject.SetActive(true);
            }
            else
            {
                levelFailed.gameObject.SetActive(true);
                levelCompleted.gameObject.SetActive(false);
            }

            leftCover.gameObject.SetActive(false);
            leftStar.gameObject.SetActive(false);
            centerStar.gameObject.SetActive(false);
            centerCover.gameObject.SetActive(false);
            rightStar.gameObject.SetActive(false);
            rightCover.gameObject.SetActive(false);

            StartCoroutine(CRFadeInCanvasGroup(canvasGroup, 0.5f, 0f));
            StartCoroutine(CRScaleRect(panelTrans, Vector3.zero, Vector3.one, 0.5f, () =>
            {
                if (IngameManager.Instance.IngameState == IngameState.Ingame_CompleteLevel)
                {
                    int star = IngameManager.Instance.CurrentStar;
                    if (star == 1)
                    {
                        leftStar.gameObject.SetActive(true);
                        leftStar.transform.localScale = Vector3.zero;
                        StartCoroutine(CRScaleRect(leftStar, Vector3.zero, Vector3.one, 0.2f, () =>
                        {
                            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CountStar);
                            leftCover.gameObject.SetActive(true);
                        }));
                    }
                    else if (star == 2)
                    {
                        leftStar.gameObject.SetActive(true);
                        centerStar.gameObject.SetActive(true);
                        leftStar.transform.localScale = Vector3.zero;
                        centerStar.transform.localScale = Vector3.zero;
                        StartCoroutine(CRScaleRect(leftStar, Vector3.zero, Vector3.one, 0.2f, () =>
                        {
                            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CountStar);
                            leftCover.gameObject.SetActive(true);

                            StartCoroutine(CRScaleRect(centerStar, Vector3.zero, Vector3.one, 0.2f, () =>
                            {
                                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CountStar);
                                centerCover.gameObject.SetActive(true);
                            }));

                        }));
                    }
                    else
                    {
                        leftStar.gameObject.SetActive(true);
                        centerStar.gameObject.SetActive(true);
                        rightStar.gameObject.SetActive(true);
                        leftStar.transform.localScale = Vector3.zero;
                        centerStar.transform.localScale = Vector3.zero;
                        rightStar.transform.localScale = Vector3.zero;
                        StartCoroutine(CRScaleRect(leftStar, Vector3.zero, Vector3.one, 0.2f, () =>
                        {
                            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CountStar);
                            leftCover.gameObject.SetActive(true);

                            StartCoroutine(CRScaleRect(centerStar, Vector3.zero, Vector3.one, 0.2f, () =>
                            {
                                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CountStar);
                                centerCover.gameObject.SetActive(true);

                                StartCoroutine(CRScaleRect(rightStar, Vector3.zero, Vector3.one, 0.2f, () =>
                                {
                                    ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CountStar);
                                    rightCover.gameObject.SetActive(true);
                                }));

                            }));

                        }));
                    }
                }
            }));
        }
    }
}
