using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private Task initializationTask;
        private bool fetchingProducts, destroyed;
        private struct PurchaseCallback
        {
            public string ProductId;
            public Action<Product> Handler;
        }
        private readonly List<PurchaseCallback> pendingCallbacks = new List<PurchaseCallback>();

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
        public Task InitializePurchasing()
        {
            if (destroyed || isInitialized || fetchingProducts) return Task.CompletedTask;
            if (initializationTask != null && !initializationTask.IsCompleted) return initializationTask;
            initializationTask = InitializeStore();
            return initializationTask;
        }

        private async Task InitializeStore()
        {
            if (isInitialized)
            {
                return;
            }
            try
            {
                // Get StoreController instance
                DetachStore();
                storeController = UnityIAPServices.StoreController();
                var connectingStore = storeController;

                // Subscribe to events
                storeController.OnPurchasePending += HandlePurchasePending;
                storeController.OnStoreDisconnected += HandleStoreDisconnected;

                // Connect to store
                await storeController.Connect();
                if (destroyed || this == null || storeController != connectingStore) return;

                // Subscribe to events
                storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
                storeController.OnPurchaseFailed += HandlePurchaseFailed;
                storeController.OnProductsFetched += HandleProductsFetched;
                storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
                storeController.OnPurchasesFetched += HandlePurchasesFetched;
                storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;


                // Prepare product list
                var productDefinitions = new List<ProductDefinition>(listIAPProduct.Count);
                foreach (var config in listIAPProduct)
                {
                    productDefinitions.Add(new ProductDefinition(config.ProductID, config.ProductType));
                }

                // Fetch products
                fetchingProducts = true;
                storeController.FetchProducts(productDefinitions);
            }
            catch (Exception e)
            {
                fetchingProducts = false;
                if (destroyed || this == null) return;
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

            Product product = storeController.GetProductById(productID);
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
                            RemoveCallback(oneTimeCallback);
                            onSuccess(purchasedProduct);
                        }
                    };

                    OnPurchaseSuccess += oneTimeCallback;
                    pendingCallbacks.Add(new PurchaseCallback { ProductId = productID, Handler = oneTimeCallback });
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
                        if (destroyed || this == null) return;
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
            if (destroyed || this == null) return;
            // Fetch purchases after products are loaded
            storeController.FetchPurchases();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
        {
            if (destroyed || this == null) return;
            fetchingProducts = false;
            Debug.LogError($"Products fetch failed: {failure.FailureReason}");
            OnInitializeFailed?.Invoke(failure.FailureReason.ToString());
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            if (destroyed || this == null) return;
            fetchingProducts = false;
            isInitialized = true;
            OnInitializeSuccess?.Invoke();
        }


        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            if (destroyed || this == null) return;
            fetchingProducts = false;
            Debug.LogError($"Purchases fetch failed: {failure.FailureReason}");
        }


        private void HandlePurchasePending(PendingOrder pendingOrder)
        {
            if (destroyed || this == null) return;
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
            if (destroyed || this == null) return;
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
            if (destroyed || this == null) return;
            foreach (var info in failedOrder.Info.PurchasedProductInfo)
            {
                for (int i = pendingCallbacks.Count - 1; i >= 0; i--)
                    if (pendingCallbacks[i].ProductId == info.productId)
                    {
                        OnPurchaseSuccess -= pendingCallbacks[i].Handler;
                        pendingCallbacks.RemoveAt(i);
                    }
                Debug.LogError($"Purchase failed: {info.productId} - Reason: {failedOrder.FailureReason}");
            }
            OnPurchaseFailed?.Invoke(failedOrder);
        }

        private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            if (destroyed || this == null) return;
            fetchingProducts = false;
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
            destroyed = true;
            isInitialized = fetchingProducts = false;
            DetachStore();
            pendingCallbacks.Clear();
            OnInitializeSuccess = null; OnInitializeFailed = null;
            OnPurchaseSuccess = null; OnPurchaseFailed = null;
        }
        private void RemoveCallback(Action<Product> handler)
        {
            for (int i = pendingCallbacks.Count - 1; i >= 0; i--)
                if (pendingCallbacks[i].Handler == handler) { pendingCallbacks.RemoveAt(i); break; }
        }
        private void DetachStore()
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
