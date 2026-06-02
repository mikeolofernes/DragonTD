using System;

namespace DragonTD.Core
{
    [Serializable]
    public class StorePurchaseRequest
    {
        public string productId;
        public string receipt;
        public string platform;
        public string transactionId;
        public int expectedGems;
    }

    [Serializable]
    public class StorePurchaseResult
    {
        public bool success;
        public bool validated;
        public string productId;
        public string transactionId;
        public int gemsGranted;
        public string message;

        public static StorePurchaseResult Fail(string message, string productId = null)
        {
            return new StorePurchaseResult
            {
                success = false,
                validated = false,
                productId = productId,
                gemsGranted = 0,
                message = message
            };
        }
    }
}
