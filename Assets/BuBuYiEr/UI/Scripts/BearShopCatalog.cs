using System;
using UnityEngine;

namespace ClawbearGames
{
    public enum BearShopCategory { Decoration, Skin, Item }
    public enum BearShopEffect { None, Decoration, BallSkin, HoneyJar, CandyHammer, SweetWind }

    [Serializable]
    public sealed class BearShopItem
    {
        [Tooltip("永久唯一 ID；发布后不要更改，否则无法识别已有库存。")]
        public string id;
        public string title;
        [TextArea] public string description;
        public BearShopCategory category;
        [Min(1)] public int coinPrice = 100;
        public Sprite icon;
        public bool available = true;
        [Tooltip("装扮和皮肤通常不勾选；勾选后可重复购买并累计库存。")]
        public bool consumable;
        public BearShopEffect effect;
        public string artKey;
        [Min(1)] public int unlockLevel = 1;
    }

    [CreateAssetMenu(menuName = "BuBuYiEr/Coin Shop Catalog")]
    public sealed class BearShopCatalog : ScriptableObject
    {
        public BearShopItem[] items = Array.Empty<BearShopItem>();

        public bool IsValid(int index)
        {
            if (items == null || index < 0 || index >= items.Length) return false;
            BearShopItem item = items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.id) || string.IsNullOrWhiteSpace(item.title)
                || item.coinPrice <= 0 || !Enum.IsDefined(typeof(BearShopCategory), item.category)) return false;
            // Duplicate IDs must never charge for, or grant, the wrong item.
            for (int i = 0; i < items.Length; i++)
                if (i != index && items[i] != null && items[i].id == item.id) return false;
            return true;
        }
    }

    [Serializable]
    public sealed class BearInventoryEntry
    {
        public string id;
        public int quantity;
    }

    public enum BearPurchaseResult { Success, Unavailable, AlreadyOwned, NotEnoughCoins, InventoryFull, Locked }

    public static class BearCoinShop
    {
        public static int Quantity(string id) => PlayerDataHandler.GetShopQuantity(id);

        public static BearPurchaseResult Buy(BearShopCatalog catalog, int index)
        {
            if (catalog == null || !catalog.IsValid(index) || !catalog.items[index].available)
                return BearPurchaseResult.Unavailable;
            BearPurchaseResult result = PlayerDataHandler.TryPurchaseShopItem(catalog.items[index]);
            if (result == BearPurchaseResult.Success && ServicesManager.Instance != null)
                ServicesManager.Instance.CoinManager.NotifyBalanceChanged();
            return result;
        }
    }
}
