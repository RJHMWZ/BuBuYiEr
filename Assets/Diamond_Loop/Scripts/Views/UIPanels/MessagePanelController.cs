using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class MessagePanelController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private RectTransform panelTrans = null;
        [SerializeField] private RectTransform magnetPanel = null;
        [SerializeField] private RectTransform hammerPanel = null;
        [SerializeField] private RectTransform keyPanel = null;
        [SerializeField] private RectTransform coolerPanel = null;


        public PowerUpType PowerUpType { get; private set; }
        private UnityEngine.UI.Text bearHint;

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
            canvasGroup.alpha = 0f;
            float t = 0;
            while (t < fadingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / fadingTime);
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, factor);
                yield return null;
            }
            yield return new WaitForSeconds(delay);
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
            canvasGroup.alpha = 1f;
            float t = 0;
            while (t < fadingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / fadingTime);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, factor);
                yield return null;
            }
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }



        /// <summary>
        /// Handle on show this panel.
        /// </summary>
        /// <param name="type"></param>
        public void OnShow(PowerUpType type)
        {
            PowerUpType = type;
            magnetPanel.gameObject.SetActive(false);
            hammerPanel.gameObject.SetActive(false);
            keyPanel.gameObject.SetActive(false);
            coolerPanel.gameObject.SetActive(false);
            BearMenuArt art = ViewManager.Instance.IngameViewController.ThemeArt;
            if (art != null)
            {
                if (bearHint == null)
                {
                    Transform close = transform.Find("CloseButton");
                    if (close != null) close.gameObject.SetActive(false);
                    RectTransform paper = BearGameTheme.Paper(panelTrans, "BearToolPrompt", 12, 0, 1000, 185, BearGameTheme.Cream, 40).rectTransform;
                    bearHint = BearGameTheme.Label(art, paper, "Description", "", 29, 20, 730, 145, 30);
                    BearGameTheme.Button(art, paper, "Cancel", "取消", 788, 57, 175, 72, BearGameTheme.Pink, OnClickCloseButton);
                }
                bearHint.text = BearToolDialog.Description(type);
                panelTrans.localScale = Vector3.one;
                canvasGroup.alpha = 1;
                return;
            }

            if (type == PowerUpType.Magnet)
            {
                magnetPanel.gameObject.SetActive(true);
            }
            else if (type == PowerUpType.Hammer)
            {
                hammerPanel.gameObject.SetActive(true);
            }
            else if(type == PowerUpType.Key)
            {
                keyPanel.gameObject.SetActive(true);
            }
            else if (type == PowerUpType.Cooler)
            {
                coolerPanel.gameObject.SetActive(true);
            }


            canvasGroup.alpha = 0f;
            StartCoroutine(CRFadeInCanvasGroup(canvasGroup, 0.5f, 0f));

            Vector3 startScale = new Vector3(0f, 1f, 1f);
            Vector3 endScale = Vector3.one;
            panelTrans.localScale = startScale;
            StartCoroutine(CRScaleRect(panelTrans, startScale, endScale, 0.5f));
        }


        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            gameObject.SetActive(false);
        }
    }
}
