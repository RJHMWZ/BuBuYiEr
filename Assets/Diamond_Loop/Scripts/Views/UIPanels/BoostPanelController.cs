using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class BoostPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform mainPanelTrans = null;
        [SerializeField] private Button purchaseButton = null;
        [SerializeField] private Button plusButton = null;
        [SerializeField] private Button minusButton = null;
        [SerializeField] private RectTransform buttonTarget = null;
        [SerializeField] private Image boostIcon = null;
        [SerializeField] private Text priceText = null;
        [SerializeField] private Text amountText = null;

        private BoostButtonController clickedButton = null;
        private int boostAmount = 1;
        private int currentPrice = 0;
        private BearIllustration illustratedIcon;

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



        public void OnShow(BoostButtonController boostButton)
        {
            clickedButton = boostButton;
            currentPrice = boostButton.PurchasePrice;
            boostIcon.sprite = boostButton.BoostIcon;
            if (ViewManager.Instance.IngameViewController.ThemeArt != null)
            {
                boostIcon.enabled = false;
                if (illustratedIcon == null)
                {
                    illustratedIcon = BearIllustration.Create(boostIcon.transform, "IllustratedTool", BearIllustration.Tool(boostButton.PowerUpType), 0, 0, 128, 128);
                    BearMenuFactory.Stretch(illustratedIcon.rectTransform);
                }
                illustratedIcon.drawing = BearIllustration.Tool(boostButton.PowerUpType);
                illustratedIcon.SetVerticesDirty();
            }
            priceText.text = boostButton.PurchasePrice.ToString();
            purchaseButton.interactable = ServicesManager.Instance.CoinManager.TotalCoins >= boostButton.PurchasePrice;
            mainPanelTrans.localScale = Vector3.zero;
            StartCoroutine(CRScaleRect(mainPanelTrans, Vector3.zero, Vector3.one, 0.5f));

            boostAmount = 1;
            amountText.text = "x" + boostAmount.ToString();
            plusButton.interactable = true;
            minusButton.interactable = false;
        }


        public void OnClickPlusButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            boostAmount += 1;
            amountText.text = "x" + boostAmount.ToString();
            currentPrice += clickedButton.PurchasePrice;
            minusButton.interactable = true;
            priceText.text = currentPrice.ToString();
            purchaseButton.interactable = ServicesManager.Instance.CoinManager.TotalCoins >= currentPrice;
        }

        public void OnClickMinusButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            boostAmount -= 1;
            amountText.text = "x" + boostAmount.ToString();
            currentPrice -= clickedButton.PurchasePrice;
            minusButton.interactable = (boostAmount > 1);
            priceText.text = currentPrice.ToString();
            purchaseButton.interactable = ServicesManager.Instance.CoinManager.TotalCoins >= currentPrice;
        }


        public void OnClickPurchaseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Rewarded);
            if (!ServicesManager.Instance.CoinManager.TrySpendCoins(currentPrice))
            {
                ViewManager.Instance.IngameViewController.ShowHint("金币还不够，装满糖果罐就能继续收集金币");
                return;
            }
            clickedButton.AddBoostAmount(boostAmount);
            ViewManager.Instance.IngameViewController.CreateMinusCoinEffect(currentPrice, buttonTarget.position);
            gameObject.SetActive(false);
        }


        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            mainPanelTrans.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
    }
}
