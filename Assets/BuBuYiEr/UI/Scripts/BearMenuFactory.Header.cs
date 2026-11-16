using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static partial class BearMenuFactory
    {
        private static void SharedHeader(BearHomeMenuController menu)
        {
            RectTransform root = Node(menu.themeRoot, "SharedHeader", 0, 0, 1024, 140);
            menu.header = root.gameObject.AddComponent<BearMenuHeader>();
            RectTransform content = Node(root, "Header", 0, 26, 1024, 112);
            RectTransform level = Node(content, "LevelStatus", 30, 0, 303, 104);
            Picture(menu, level, "Panel", "TopUIBg", 0, 0, 303, 96).type = Image.Type.Sliced;
            Picture(menu, level, "Star", "IconStar", 16, 1, 88, 83).preserveAspect = true;
            menu.levelText = Label(menu, level, "LiveLevel", "Lv. 1", 112, 3, 170, 59, 48, true);
            Label(menu, level, "ChooseLevelHint", "选择关卡 ›", 109, 55, 173, 29, 23);
            Action(level, "levels");
            menu.header.levelStatus = level.gameObject;

            RectTransform back = Node(content, "BackStatus", 30, 0, 240, 104);
            Picture(menu, back, "Panel", "TopUIBg", 0, 0, 240, 96).type = Image.Type.Sliced;
            Image arrow = Picture(menu, back, "Arrow", "Setting/IconArrow", 27, 28, 27, 42);
            CenterImage(arrow);
            arrow.rectTransform.localEulerAngles = new Vector3(0, 0, 180);
            Label(menu, back, "Caption", "返回", 73, 8, 140, 74, 43);
            Action(back, "page", 0);
            menu.header.backStatus = back.gameObject;

            RectTransform coins = Node(content, "CoinStatus", 624, 0, 370, 104);
            Picture(menu, coins, "Panel", "TopUIBg", 0, 0, 370, 96).type = Image.Type.Sliced;
            Picture(menu, coins, "Coin", "IconCoin", 4, -4, 106, 104).preserveAspect = true;
            menu.coinTexts[0] = Label(menu, coins, "LiveCoins", "0", 117, 7, 159, 68, 54, true);
            Picture(menu, coins, "AddIcon", "IconAdd", 282, -4, 106, 107).preserveAspect = true;
            Hotspot(coins, "CoinSources", 282, -4, 106, 107, "earn-coins");
            menu.header.ShowPage(0);
        }
    }
}
