using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DragonTD.Core
{
    public class UnityIapReceiptCaptureService : IStorePurchaseService
    {
        private readonly IIapReceiptValidator _validator;
        private StoreController _storeController;
        private Task _initializeTask;
        private TaskCompletionSource<bool> _productsFetched;
        private TaskCompletionSource<StorePurchaseResult> _pendingPurchase;
        private string _pendingProductId;

        public UnityIapReceiptCaptureService(IIapReceiptValidator validator)
        {
            _validator = validator;
        }

        public async Task<StorePurchaseResult> PurchaseGemPackAsync(GemPackDefinition pack)
        {
            if (pack == null)
                return StorePurchaseResult.Fail("Unknown product");
            if (_validator == null)
                return StorePurchaseResult.Fail("Receipt validator is not configured", pack.productId);
            if (_pendingPurchase != null)
                return StorePurchaseResult.Fail("Another purchase is already in progress", pack.productId);

            try
            {
                await EnsureInitializedAsync();
            }
            catch (System.Exception ex)
            {
                return StorePurchaseResult.Fail($"Store initialization failed: {ex.Message}", pack.productId);
            }

            Product product = _storeController.GetProductById(pack.productId);
            if (product == null)
                return StorePurchaseResult.Fail("Store product was not loaded", pack.productId);
            if (!product.availableToPurchase)
                return StorePurchaseResult.Fail("Store product is not available to purchase", pack.productId);

            _pendingProductId = pack.productId;
            _pendingPurchase = new TaskCompletionSource<StorePurchaseResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _storeController.PurchaseProduct(product);

            StorePurchaseResult result = await _pendingPurchase.Task;
            _pendingPurchase = null;
            _pendingProductId = null;
            return result;
        }

        private Task EnsureInitializedAsync()
        {
            _initializeTask ??= InitializeAsync();
            return _initializeTask;
        }

        private async Task InitializeAsync()
        {
            _storeController = UnityIAPServices.StoreController();
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            _storeController.OnStoreDisconnected += OnStoreDisconnected;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;

            await _storeController.Connect();

            _productsFetched = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _storeController.FetchProducts(BuildProductDefinitions());
            await _productsFetched.Task;
        }

        private static List<ProductDefinition> BuildProductDefinitions()
        {
            return GemStoreCatalog.Packs
                .Select(pack => new ProductDefinition(pack.productId, ProductType.Consumable))
                .ToList();
        }

        private async void OnPurchasePending(PendingOrder order)
        {
            Product product = GetFirstProduct(order);
            string productId = product?.definition?.id;
            if (string.IsNullOrWhiteSpace(productId))
            {
                CompletePending(StorePurchaseResult.Fail("Store purchase did not include a product"));
                return;
            }

            if (!GemStoreCatalog.TryGetPack(productId, out GemPackDefinition pack))
            {
                CompletePending(StorePurchaseResult.Fail("Store purchase used an unknown product", productId));
                return;
            }

            var request = new StorePurchaseRequest
            {
                productId = productId,
                receipt = order.Info?.Receipt ?? string.Empty,
                platform = Application.platform.ToString(),
                transactionId = order.Info?.TransactionID,
                expectedGems = pack.gems
            };

            StorePurchaseResult result;
            try
            {
                result = await _validator.ValidateAsync(request);
            }
            catch (System.Exception ex)
            {
                result = StorePurchaseResult.Fail($"Receipt validation failed: {ex.Message}", productId);
            }

            if (result != null && result.success && result.validated)
                _storeController.ConfirmPurchase(order);

            CompletePending(result ?? StorePurchaseResult.Fail("Receipt validation returned no result", productId));
        }

        private void OnPurchaseConfirmed(Order order)
        {
            Product product = GetFirstProduct(order);
            Debug.Log($"[UnityIapReceiptCaptureService] Purchase confirmed: {product?.definition?.id ?? "unknown"}");
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            Product product = GetFirstProduct(order);
            string productId = product?.definition?.id ?? _pendingProductId;
            CompletePending(StorePurchaseResult.Fail($"Purchase failed: {order.FailureReason} {order.Details}", productId));
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            Product product = GetFirstProduct(order);
            string productId = product?.definition?.id ?? _pendingProductId;
            CompletePending(StorePurchaseResult.Fail("Purchase was deferred by the store", productId));
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            string message = description == null ? "Store disconnected" : $"Store disconnected: {description.message}";
            CompletePending(StorePurchaseResult.Fail(message, _pendingProductId));
        }

        private void OnProductsFetched(List<Product> products)
        {
            _productsFetched?.TrySetResult(true);
            Debug.Log($"[UnityIapReceiptCaptureService] Products fetched: {products?.Count ?? 0}");
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            string reason = failure?.FailureReason ?? "Unknown product fetch failure";
            _productsFetched?.TrySetException(new System.InvalidOperationException(reason));
        }

        private void CompletePending(StorePurchaseResult result)
        {
            _pendingPurchase?.TrySetResult(result);
        }

        private static Product GetFirstProduct(Order order)
        {
            return order?.CartOrdered?.Items()?.FirstOrDefault()?.Product;
        }
    }
}
