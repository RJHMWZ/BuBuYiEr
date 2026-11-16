using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static class CandyCosmetics
    {
        public static readonly string[] SupportIds = { "support_honey", "support_hammer", "support_wind" };
        public static readonly string[] SupportNames = { "蜂蜜罐", "糖果锤", "甜蜜风" };
        public static readonly string[] SupportIcons = { "Daily/IconBottle", "IconStar", "Daily/IconGrass" };
        public static readonly string[] SupportHints = { "增加 120 本关蜂蜜", "所有糖砖减少 30% 初始数字", "全队速度提升 35%，持续 12 秒" };
        public static Color SkinColor(int kind)
        {
            return SkinPreview(PlayerDataHandler.GetEquipment(BearShopEffect.BallSkin),kind);
        }
        public static Color SkinPreview(string skin,int kind)
        {
            Color baseColor = CandyBreakoutGraphic.CandyColors[kind];
            if (skin == "skin_milk") return Color.Lerp(baseColor, new Color(1,.71f,.38f), .30f);
            if (skin == "skin_jelly") return Color.Lerp(baseColor, new Color(.16f,.80f,.71f), .55f);
            if (skin == "skin_sunset") return Color.Lerp(baseColor, new Color(1,.42f,.16f), .65f);
            if (skin == "skin_starry") return Color.Lerp(baseColor, new Color(.39f,.27f,.76f), .65f);
            return baseColor;
        }
        public static bool HasSkin => !string.IsNullOrEmpty(PlayerDataHandler.GetEquipment(BearShopEffect.BallSkin));
        public static void ApplyHome(BearHomeMenuController menu)
        {
            if (menu.pages == null || menu.pages.Length == 0 || !menu.pages[0].activeSelf) return;
            var ornament = menu.pages[0].transform.Find("BearRoom/EquippedOrnament");
            if (ornament == null) return;
            string id = PlayerDataHandler.GetEquipment(BearShopEffect.Decoration);
            BearShopItem chosen = null;
            if (menu.art.shopCatalog != null)
                foreach (var item in menu.art.shopCatalog.items)
                    if (item != null && item.id == id && item.effect == BearShopEffect.Decoration && BearCoinShop.Quantity(id) > 0) { chosen = item; break; }
            ornament.gameObject.SetActive(chosen != null);
            if (chosen != null) ornament.GetComponent<Image>().sprite = chosen.icon != null ? chosen.icon : menu.art.Get(chosen.artKey);
        }
    }
}
