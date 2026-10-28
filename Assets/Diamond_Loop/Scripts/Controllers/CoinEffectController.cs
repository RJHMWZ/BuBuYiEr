using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class CoinEffectController : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform = null;
        [SerializeField] private RectTransform updateCoinPanel = null;
        [SerializeField] private RectTransform rewardCoinPanel = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private Text coinText = null;


        public RectTransform RectTransform => rectTransform;




        /// <summary>
        /// Handle the effect of minus/plus coin.
        /// </summary>
        /// <param name="targetPos"></param>
        /// <param name="amount"></param>
        /// <param name="callback"></param>
        public void OnInitCoinUpdateEffect(Vector2 targetPos, int amount, Action callback = null)
        {
            canvasGroup.alpha = 1f;
            updateCoinPanel.gameObject.SetActive(true);
            rewardCoinPanel.gameObject.SetActive(false);
            coinText.text = (amount > 0 ? "+" : "-") + amount.ToString();
            StartCoroutine(CRMoveToTarget(targetPos, callback));
        }


        /// <summary>
        /// Coroutine move to target position then disable.
        /// </summary>
        /// <param name="targetPos"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator CRMoveToTarget(Vector2 targetPos, Action callback)
        {
            float t = 0;
            float moveTime = 0.5f;
            Vector2 startPos = rectTransform.anchoredPosition;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                float factor = t / moveTime;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, factor);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, factor);
                yield return null;
            }
            callback?.Invoke();
            canvasGroup.alpha = 1f;
            gameObject.SetActive(false);
        }





        /// <summary>
        /// Handle the effect of rewarding coin.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="firstPos"></param>
        /// <param name="finalPos"></param>
        /// <param name="callback"></param>
        public void OnInitCoinRewardEffect(int index, Vector2 firstPos, Vector2 finalPos, Action callback = null)
        {
            canvasGroup.alpha = 1f;
            updateCoinPanel.gameObject.SetActive(false);
            rewardCoinPanel.gameObject.SetActive(true);
            StartCoroutine(CRHandlePlusEffect(index, firstPos, finalPos, callback));
        }


        /// <summary>
        /// Coroutine move to first position then final position then disable.
        /// 
        /// <param name="index"></param></summary>
        /// <param name="firstPos"></param>
        /// <param name="finalPos"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator CRHandlePlusEffect(int index, Vector2 firstPos, Vector2 finalPos, Action callback = null)
        {
            float t = 0;
            float moveTime = 0.2f;
            Vector2 startPos = rectTransform.anchoredPosition;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                float factor = t / moveTime;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, firstPos, factor);
                yield return null;
            }
            yield return new WaitForSeconds(index * 0.01f);

            t = 0;
            moveTime = 0.3f;
            startPos = rectTransform.anchoredPosition;
            while (t < moveTime)
            {
                t += Time.deltaTime;
                float factor = t / moveTime;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, finalPos, factor);
                yield return null;
            }
            callback?.Invoke();
            canvasGroup.alpha = 1f;
            gameObject.SetActive(false);
        }
    }
}
