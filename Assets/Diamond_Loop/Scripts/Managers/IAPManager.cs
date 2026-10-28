using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace ClawbearGames
{
    public class IAPManager : MonoBehaviour
    {
        [SerializeField] private List<IAPProduct> listIAPProduct = new List<IAPProduct>();

        public List<IAPProduct> ListIAPProduct => listIAPProduct;


        private StoreController storeController;
        private bool isInitialized = false;

        public event Action OnInitializeSuccess;
        public event Action<string> OnInitializeFailed;
        public event Action<Product> OnPurchaseSuccess;
        public event Action<FailedOrder> OnPurchaseFailed;

        private async void Start()
        {
            await InitializePurchasing();
        }

        /// <summary>
        /// Initialize Unity IAP with configured products
        /// </summary>
        public async System.Threading.Tasks.Task InitializePurchasing()
        {
            if (isInitialized)
            {
                return;
            }
            try
            {
                // Get StoreController instance
                storeController = UnityIAPServices.StoreController();

                // Subscribe to events
                storeController.OnPurchasePending += HandlePurchasePending;
                storeController.OnStoreDisconnected += HandleStoreDisconnected;

                // Connect to store
                await storeController.Connect();

                // Subscribe to events
                storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
                storeController.OnPurchaseFailed += HandlePurchaseFailed;
                storeController.OnProductsFetched += HandleProductsFetched;
                storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
                storeController.OnPurchasesFetched += HandlePurchasesFetched;
                storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;


                // Prepare product list
                var productDefinitions = new List<ProductDefinition>();
                foreach (var config in listIAPProduct)
                {
                    productDefinitions.Add(new ProductDefinition(config.ProductID, config.ProductType));
                }

                // Fetch products
                storeController.FetchProducts(productDefinitions);
            }
            catch (Exception e)
            {
                Debug.LogError($"IAPProductController Initialization Error: {e.Message}");
                OnInitializeFailed?.Invoke(e.Message);
            }
        }


        /// <summary>
        /// Get localized price string by product ID
        /// </summary>
        public string GetLocalizedPriceString(string productID)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("IAPProductController not initialized yet");
                return "N/A";
            }

            Product product = GetProduct(productID);
            if (product != null && product.availableToPurchase)
            {
                return product.metadata.localizedPriceString;
            }

            return "N/A";
        }


        /// <summary>
        /// Get localized price string by product configuration
        /// </summary>
        public string GetLocalizedPriceString(IAPProduct product)
        {
            return GetLocalizedPriceString(product.ProductID);
        }


        /// <summary>
        /// Purchase a product by ID with success callback
        /// </summary>
        public void PurchaseProduct(string productID, Action<Product> onSuccess = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("IAPProductController not initialized. Cannot purchase product.");
                return;
            }

            Product product = storeController.GetProducts().FirstOrDefault(p => p.definition.id == productID);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"Purchasing product: {productID}");

                if (onSuccess != null)
                {
                    Action<Product> oneTimeCallback = null;
                    oneTimeCallback = (purchasedProduct) =>
                    {
                        // Only invoke if it matches the product we're purchasing
                        if (purchasedProduct.definition.id == productID)
                        {
                            OnPurchaseSuccess -= oneTimeCallback;
                            onSuccess(purchasedProduct);
                        }
                    };

                    OnPurchaseSuccess += oneTimeCallback;
                }

                storeController.PurchaseProduct(productID);
            }
            else
            {
                Debug.LogError($"Product {productID} not found or not available for purchase");
            }
        }


        /// <summary>
        /// Purchase a product by configuration with success callback
        /// </summary>
        public void PurchaseProduct(IAPProduct product, Action<Product> onSuccess = null)
        {
            PurchaseProduct(product.ProductID, onSuccess);
        }


        /// <summary>
        /// Restore purchases (iOS only)
        /// </summary>
        public void RestorePurchases(Action onComplete = null)
        {
            if (!isInitialized)
            {
                Debug.LogError("IAPProductController not initialized. Cannot restore purchases.");
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.Log("Restoring purchases...");

                try
                {
                    storeController.RestoreTransactions((success, message) =>
                    {
                        if (success)
                        {
                            Debug.Log("Restore purchases completed");
                            onComplete?.Invoke();
                        }
                        else
                        {
                            Debug.Log($"Restore purchases failed with message: {message}");
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Restore purchases failed: {e.Message}");
                    onComplete?.Invoke();
                }
            }
            else
            {
                Debug.LogWarning("Restore purchases is only supported on iOS/macOS");
                onComplete?.Invoke();
            }
        }

        #region Event Handlers

        private void HandleProductsFetched(List<Product> products)
        {
            // Fetch purchases after products are loaded
            storeController.FetchPurchases();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogError($"Products fetch failed: {failure.FailureReason}");
            OnInitializeFailed?.Invoke(failure.FailureReason.ToString());
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            isInitialized = true;
            OnInitializeSuccess?.Invoke();
        }


        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogError($"Purchases fetch failed: {failure.FailureReason}");
        }


        private void HandlePurchasePending(PendingOrder pendingOrder)
        {
            // Get the product
            foreach (var info in pendingOrder.Info.PurchasedProductInfo)
            {
                Product product = GetProduct(info.productId);
                if (product != null)
                {
                    OnPurchaseSuccess?.Invoke(product);
                }
            }

            // Then confirm the purchase
            storeController.ConfirmPurchase(pendingOrder);
        }


        private void HandlePurchaseConfirmed(Order order)
        {
            foreach(var info in order.Info.PurchasedProductInfo)
            {
                Debug.Log($"Purchase confirmed: {info.productId}");

                // Get the product
                Product product = GetProduct(info.productId);
                if (product != null)
                {
                    OnPurchaseSuccess?.Invoke(product);
                }
            }
        }

        private void HandlePurchaseFailed(FailedOrder failedOrder)
        {
            foreach (var info in failedOrder.Info.PurchasedProductInfo)
            {
                Debug.LogError($"Purchase failed: {info.productId} - Reason: {failedOrder.FailureReason}");
            }
            OnPurchaseFailed?.Invoke(failedOrder);
        }

        private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogError($"Store disconnected: {failure.Message}");
            OnInitializeFailed?.Invoke(failure.Message);
        }

        #endregion

        /// <summary>
        /// Check if IAP is initialized
        /// </summary>
        public bool IsInitialized() => isInitialized;

        /// <summary>
        /// Get product by ID
        /// </summary>
        public Product GetProduct(string productID)
        {
            if (!isInitialized) return null;
            return storeController.GetProductById(productID);
        }


        /// <summary>
        /// Get product by configuration
        /// </summary>
        public Product GetProduct(IAPProduct product)
        {
            return GetProduct(product.ProductID);
        }

        private void OnDestroy()
        {
            if (storeController != null)
            {
                // Unsubscribe from events
                storeController.OnPurchasePending -= HandlePurchasePending;
                storeController.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
                storeController.OnPurchaseFailed -= HandlePurchaseFailed;
                storeController.OnProductsFetched -= HandleProductsFetched;
                storeController.OnProductsFetchFailed -= HandleProductsFetchFailed;
                storeController.OnPurchasesFetched -= HandlePurchasesFetched;
                storeController.OnStoreDisconnected -= HandleStoreDisconnected;
                storeController.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
            }
        }
    }
}
