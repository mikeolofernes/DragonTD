using System;

namespace DragonTD.Core
{
    public static class GemStoreCatalog
    {
        public static readonly GemPackDefinition[] Packs =
        {
            new GemPackDefinition("com.dragondominion.gems.small", "Small Gem Pouch", 500, "$0.99"),
            new GemPackDefinition("com.dragondominion.gems.medium", "Gem Bundle", 1200, "$1.99"),
            new GemPackDefinition("com.dragondominion.gems.large", "Dragon Hoard", 3000, "$4.99"),
            new GemPackDefinition("com.dragondominion.gems.epic", "Epic Vault", 6500, "$9.99"),
            new GemPackDefinition("com.dragondominion.gems.legendary", "Legendary Treasury", 14000, "$19.99")
        };

        public static bool TryGetPack(string productId, out GemPackDefinition pack)
        {
            foreach (GemPackDefinition candidate in Packs)
            {
                if (candidate.productId == productId)
                {
                    pack = candidate;
                    return true;
                }
            }

            pack = null;
            return false;
        }
    }

    [Serializable]
    public class GemPackDefinition
    {
        public string productId;
        public string displayName;
        public int gems;
        public string price;

        public GemPackDefinition(string productId, string displayName, int gems, string price)
        {
            this.productId = productId;
            this.displayName = displayName;
            this.gems = gems;
            this.price = price;
        }
    }
}
