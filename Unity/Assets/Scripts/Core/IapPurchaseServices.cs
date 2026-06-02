using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DragonTD.Core
{
    public interface IIapReceiptValidator
    {
        Task<StorePurchaseResult> ValidateAsync(StorePurchaseRequest request);
    }

    public interface IStorePurchaseService
    {
        Task<StorePurchaseResult> PurchaseGemPackAsync(GemPackDefinition pack);
    }

    public class EditorMockIapPurchaseService : IStorePurchaseService
    {
        private readonly IIapReceiptValidator _validator;

        public EditorMockIapPurchaseService(IIapReceiptValidator validator)
        {
            _validator = validator;
        }

        public Task<StorePurchaseResult> PurchaseGemPackAsync(GemPackDefinition pack)
        {
            if (pack == null)
                return Task.FromResult(StorePurchaseResult.Fail("Unknown product"));

            var request = new StorePurchaseRequest
            {
                productId = pack.productId,
                receipt = $"editor_mock_receipt:{pack.productId}:{pack.gems}",
                platform = Application.platform.ToString(),
                transactionId = Guid.NewGuid().ToString("N"),
                expectedGems = pack.gems
            };

            return _validator.ValidateAsync(request);
        }
    }

    public class LocalIapReceiptValidator : IIapReceiptValidator
    {
        public Task<StorePurchaseResult> ValidateAsync(StorePurchaseRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.productId))
                return Task.FromResult(StorePurchaseResult.Fail("Missing purchase request"));

            if (!GemStoreCatalog.TryGetPack(request.productId, out GemPackDefinition pack))
                return Task.FromResult(StorePurchaseResult.Fail("Unknown product", request.productId));

            string expectedReceipt = $"editor_mock_receipt:{pack.productId}:{pack.gems}";
            if (request.receipt != expectedReceipt || request.expectedGems != pack.gems)
                return Task.FromResult(StorePurchaseResult.Fail("Receipt validation failed", request.productId));

            return Task.FromResult(new StorePurchaseResult
            {
                success = true,
                validated = true,
                productId = pack.productId,
                transactionId = request.transactionId,
                gemsGranted = pack.gems,
                message = $"Validated {pack.displayName}: +{pack.gems} Gems"
            });
        }
    }

    public class ApiIapReceiptValidator : IIapReceiptValidator
    {
        private readonly string _baseUrl;
        private readonly AuthSessionData _session;

        public ApiIapReceiptValidator(string baseUrl, AuthSessionData session)
        {
            _baseUrl = baseUrl;
            _session = session;
        }

        public async Task<StorePurchaseResult> ValidateAsync(StorePurchaseRequest request)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return StorePurchaseResult.Fail("IAP validation endpoint is not configured", request?.productId);
            if (_session == null || !_session.IsAuthenticated)
                return StorePurchaseResult.Fail("IAP validation requires an authenticated API session", request?.productId);

            string json = JsonUtility.ToJson(request);
            string validationUrl = $"{_baseUrl.TrimEnd('/')}/api/v1/iap/validate";
            using (var webRequest = new UnityWebRequest(validationUrl, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(body);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {_session.accessToken}");

                UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (webRequest.result != UnityWebRequest.Result.Success)
                    return StorePurchaseResult.Fail($"IAP validation failed: {webRequest.error}", request?.productId);

                StorePurchaseResult result = JsonUtility.FromJson<StorePurchaseResult>(webRequest.downloadHandler.text);
                return result ?? StorePurchaseResult.Fail("IAP validation returned no result", request?.productId);
            }
        }
    }
}
