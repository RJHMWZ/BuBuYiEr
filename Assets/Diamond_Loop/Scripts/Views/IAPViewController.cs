using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace ClawbearGames
{
    public class IAPViewController : BaseViewController
    {
        private void OnDisable()
        {
            if (ServicesManager.Instance != null)
            {
                ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
            }
            StopAllCoroutines();
        }

        [SerializeField] private RectTransform viewTransform = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private CanvasGroup confirmGroup = null;
        [SerializeField] private RectTransform confirmPanel = null;
        [SerializeField] private RectTransform productContent = null;
        [SerializeField] private RectTransform purchasedContent = null;
        [SerializeField] private RectTransform restoreButton = null;
        [SerializeField] private RectTransform confirmButton = null;
        [SerializeField] private RectTransform totalCoinTarget = null;
        [SerializeField] private Text totalCoinsText = null;
        [SerializeField] private IAPProductController productControllerPrefab = null;
        [SerializeField] private PurchasedItemController purchasedItemPrefab = null;
        [SerializeField] private CoinEffectController coinEffectControllerPrefab = null;

        private List<IAPProductController> listIAPProductController = new List<IAPProductController>();
        private List<PurchasedItemController> listPurchasedItemController = new List<PurchasedItemController>();
        private List<CoinEffectController> listCoinEffectController = new List<CoinEffectController>();
        private IAPProduct purchasedProduct = null;


        /// <summary>
        /// Get an inactive IAPProductController object.
        /// </summary>
        /// <returns></returns>
        public IAPProductController GetIAPProductController()
        {
            //Find in the list
            IAPProductController productController = null;
            for (int i = 0; i < listIAPProductController.Count; i++)
            {
                var candidate = listIAPProductController[i];
                if (candidate != null && !candidate.gameObject.activeSelf) { productController = candidate; break; }
            }

            if (productController == null)
            {
                //Did not find one -> create new one
                productController = Instantiate(productControllerPrefab, Vector3.zero, Quaternion.identity);
                productController.gameObject.SetActive(false);
                productController.transform.SetParent(productContent);
                productController.transform.SetAsLastSibling();
                productController.transform.localScale = Vector3.one;
                listIAPProductController.Add(productController);
            }

            return productController;
        }


        /// <summary>
        /// Get an inactive PurchasedItemController object.
        /// </summary>
        /// <returns></returns>
        public PurchasedItemController GetPurchasedItemController()
        {
            //Find in the list
            PurchasedItemController purchasedItem = null;
            for (int i = 0; i < listPurchasedItemController.Count; i++)
            {
                var candidate = listPurchasedItemController[i];
                if (candidate != null && !candidate.gameObject.activeSelf) { purchasedItem = candidate; break; }
            }

            if (purchasedItem == null)
            {
                //Did not find one -> create new one
                purchasedItem = Instantiate(purchasedItemPrefab, Vector3.zero, Quaternion.identity);
                purchasedItem.gameObject.SetActive(false);
                purchasedItem.transform.SetParent(purchasedContent);
                purchasedItem.transform.localScale = Vector3.one;
                listPurchasedItemController.Add(purchasedItem);
            }

            return purchasedItem;
        }



        /// <summary>
        /// Get an inactive CoinEffectController object.
        /// </summary>
        /// <returns></returns>
        public CoinEffectController GetCoinEffectController()
        {
            //Find in the list
            CoinEffectController cashEffect = null;
            for (int i = 0; i < listCoinEffectController.Count; i++)
            {
                var candidate = listCoinEffectController[i];
                if (candidate != null && !candidate.gameObject.activeSelf) { cashEffect = candidate; break; }
            }

            if (cashEffect == null)
            {
                //Did not find one -> create new one
                cashEffect = Instantiate(coinEffectControllerPrefab, Vector3.zero, Quaternion.identity);
                cashEffect.gameObject.SetActive(false);
                cashEffect.transform.SetParent(transform);
                cashEffect.transform.SetAsLastSibling();
                cashEffect.transform.localScale = Vector3.one;
                listCoinEffectController.Add(cashEffect);
            }

            return cashEffect;
        }


        /// <summary>
        /// Create coin effects.
        /// </summary>
        /// <param name="coinAmount"></param>
        /// <param name="initTrans"></param>
        /// <returns></returns>
        private void CreateCoinEffect(int coinAmount, RectTransform initTrans)
        {
            // Limit the number of visual effects to prevent lag
            const int MAX_VISUAL_EFFECTS = 30;
            int visualEffectCount = Mathf.Min(coinAmount, MAX_VISUAL_EFFECTS);

            // Calculate how much each visual effect represents
            int cashPerEffect = Mathf.CeilToInt((float)coinAmount / visualEffectCount);
            int totalCashAdded = 0;

            for (int i = 0; i < visualEffectCount; i++)
            {
                CoinEffectController cashEffect = GetCoinEffectController();
                cashEffect.gameObject.SetActive(true);

                Vector2 firstPos = Vector2.one;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, initTrans.position, null, out firstPos);
                cashEffect.RectTransform.anchoredPosition = firstPos;

                Vector2 startPos = firstPos + (Random.insideUnitCircle * 100f);
                Vector2 finalPos = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewTransform, totalCoinTarget.position, null, out finalPos);

                // Calculate cash for this specific effect (handle remainder on last effect)
                int cashForThisEffect = (i == visualEffectCount - 1)
                    ? (coinAmount - totalCashAdded)
                    : cashPerEffect;
                totalCashAdded += cashForThisEffect;

                cashEffect.OnInitCoinRewardEffect(i, startPos, finalPos, () =>
                {
                    ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CollectCoin);
                    ServicesManager.Instance.CoinManager.AddTotalCoins(cashForThisEffect, 0);
                });
            }
        }




        /// <summary>
        /// Coroutine handle after purchased a product.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRAfterPurchasedProduct()
        {
            confirmPanel.gameObject.SetActive(true);
            FadeInCanvasGroup(confirmGroup, 0.5f);
            yield return new WaitForSeconds(0.5f);
            foreach(ProductReward reward in purchasedProduct.Rewards)
            {
                PurchasedItemController purchasedItem = GetPurchasedItemController();
                purchasedItem.gameObject.SetActive(true);
                purchasedItem.OnInit(reward);
                yield return new WaitForSeconds(0.2f);
            }
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Rewarded);
            confirmButton.gameObject.SetActive(true);
        }



        private void CoinManager_TotalCoinChanged(int obj)
        {
            //Update texts
            totalCoinsText.text = obj.ToString();
        }


        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>

        public override void OnShow()
        {
            FadeInCanvasGroup(canvasGroup, 0.75f);
            totalCoinsText.text = ServicesManager.Instance.CoinManager.TotalCoins.ToString();
            ServicesManager.Instance.CoinManager.TotalCoinChanged += CoinManager_TotalCoinChanged;
            confirmPanel.gameObject.SetActive(false);
            confirmButton.gameObject.SetActive(false);

            //Create product items
            if (productContent.childCount <= 0)
            {
                for (int i = 0; i < ServicesManager.Instance.IAPManager.ListIAPProduct.Count; i++)
                {
                    IAPProduct config = ServicesManager.Instance.IAPManager.ListIAPProduct[i];
                    if (PlayerDataHandler.IsPurchasedProduct(config.ProductID)) continue;
                    IAPProductController productController = GetIAPProductController();
                    productController.gameObject.SetActive(true);
                    productController.OnInit(config);
                }
            }


            //Disable all purchased item
            foreach (PurchasedItemController purchasedItem in listPurchasedItemController)
            {
                purchasedItem.gameObject.SetActive(false);
            }

            //Disable all cash effects
            foreach (CoinEffectController cashEffect in listCoinEffectController)
            {
                cashEffect.gameObject.SetActive(false);
            }

            //Handle restore button
            bool applePlatform = (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer);
            restoreButton.gameObject.SetActive(applePlatform);
        }

        public override void OnClose()
        {
            canvasGroup.alpha = 0f;
            confirmGroup.alpha = 0f;
            confirmPanel.gameObject.SetActive(false);
            confirmButton.gameObject.SetActive(false);
            ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
            gameObject.SetActive(false);
        }




        /// <summary>
        /// Handle after purchased a product.
        /// </summary>
        /// <param name="product"></param>
        public void OnPurchasedProduct(IAPProduct product)
        {
            purchasedProduct = product;

            //Disable all purchased item
            foreach (PurchasedItemController purchasedItem in listPurchasedItemController)
            {
                purchasedItem.gameObject.SetActive(false);
            }

            confirmPanel.gameObject.SetActive(true);
            StartCoroutine(CRAfterPurchasedProduct());
        }


        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>


        public void OnClickRestoreButton()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
                ServicesManager.Instance.IAPManager.RestorePurchases();
            }
        }


        public void OnClickConfirmButton()
        {
            confirmPanel.gameObject.SetActive(false);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            int cashAmount = 0;
            foreach (ProductReward reward in purchasedProduct.Rewards)
            {
                if (reward.RewardType == RewardType.Coins)
                {
                    cashAmount += reward.Amount;
                }
            }
            CreateCoinEffect(cashAmount, purchasedContent);
        }


        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            if (IngameManager.Instance != null)
            {
                LoadScene("Home", 0.1f);
            }
            else { ViewManager.Instance.OnShowView(ViewType.HOME_VIEW); }
        }
    }
}
