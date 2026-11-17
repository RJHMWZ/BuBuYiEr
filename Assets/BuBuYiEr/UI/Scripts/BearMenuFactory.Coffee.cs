using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static partial class BearMenuFactory
    {
        public const string CoffeeMessage = "如果这个小游戏给你带来了一点快乐，\n也欢迎请我喝杯咖啡。\n量力而行，礼轻情意重。\n你愿意来玩，就是很棒的支持。";

        // Vector UI keeps the little cup sharp at every screen size, without a new texture.
        private static void CoffeeIcon(Transform parent, float x, float y, float width, float height)
        {
            RectTransform icon = Node(parent, "CoffeeIcon", x, y, width, height);
            Color porcelain = new Color32(255, 241, 224, 255);
            Color glaze = new Color32(248, 201, 168, 255);
            Shape(icon, "Handle", width * .66f, height * .32f, width * .27f, height * .38f,
                width * .13f, porcelain, glaze, width * .045f);
            Shape(icon, "Cup", width * .15f, height * .28f, width * .60f, height * .52f,
                width * .14f, porcelain, glaze, width * .045f);
            Shape(icon, "Coffee", width * .21f, height * .29f, width * .48f, height * .105f,
                width * .05f, new Color32(151, 92, 57, 255), new Color32(117, 66, 39, 255), 0);
            PictureForCoffeeHeart(icon, width, height);
            Shape(icon, "Saucer", width * .10f, height * .83f, width * .76f, height * .10f,
                width * .05f, porcelain, glaze, width * .035f);
            for (int i = 0; i < 2; i++)
            {
                var steam = Shape(icon, "Steam" + i, width * (.32f + i * .18f), height * .04f,
                    width * .05f, height * .16f, width * .025f,
                    new Color32(169, 122, 90, 255), new Color32(169, 122, 90, 255), 0);
                steam.rectTransform.localRotation = Quaternion.Euler(0, 0, i == 0 ? -12 : 12);
            }
        }

        private static void PictureForCoffeeHeart(Transform icon, float width, float height)
        {
            // Two rounded lobes and a rotated square form the small pink heart.
            Color pink = new Color32(229, 145, 154, 255);
            float size = width * .11f;
            Vector2 center = new Vector2(width * .45f, -height * .59f);
            var point = Shape(icon, "HeartPoint", center.x - size * .5f, -center.y - size * .5f,
                size, size, 0, pink, pink, 0).rectTransform;
            point.pivot = new Vector2(.5f,.5f); point.anchoredPosition = center;
            point.localRotation = Quaternion.Euler(0, 0, 45);
            Shape(icon, "HeartLeft", center.x - size * .92f, -center.y - size * 1.10f,
                size * 1.08f, size * 1.08f, size, pink, pink, 0);
            Shape(icon, "HeartRight", center.x - size * .15f, -center.y - size * 1.10f,
                size * 1.08f, size * 1.08f, size, pink, pink, 0);
        }

        private static GameObject CoffeeDialog(BearHomeMenuController menu, Transform overlay)
        {
            RectTransform frame = Node(overlay, "SafeCoffeeDialog", 0, 0, 941, 1672);
            frame.gameObject.AddComponent<BearMenuLayout>().designSize = new Vector2(941,1672);
            RectTransform card = Node(frame, "CoffeeCard", 80, 228, 781, 1216);
            Shape(card, "Paper", 0, 0, 781, 1216, 66, Cream, new Color32(255,237,211,255), 6);
            CoffeeIcon(card, 337, 27, 106, 100);
            Label(menu, card, "Title", "打赏咖啡", 40, 135, 701, 76, 50);
            Label(menu, card, "Message", CoffeeMessage, 44, 229, 693, 180, 29);
            Shape(card, "CodePaper", 126, 426, 529, 577, 26, Color.white, Color.white, 0);
            Image code = Node(card, "WeChatCode", 134, 434, 512, 560).gameObject.AddComponent<Image>();
            code.sprite = Resources.Load<Sprite>("BearSupport/CoffeeWeChat");
            code.preserveAspect = true;
            code.raycastTarget = false;
            Label(menu, card, "CodeHint", "微信扫一扫，或截图后在微信中识别", 44, 1019, 693, 48, 27);
            RectTransform close = Node(card, "Close", 220, 1090, 340, 84);
            Shape(close, "GoldButton", 0, 0, 340, 84, 42, Gold, new Color32(255,211,49,255), 4);
            Label(menu, close, "Label", "继续开心玩", 0, 0, 340, 84, 38);
            Action(close, "close-modal");
            frame.gameObject.SetActive(false);
            return frame.gameObject;
        }
    }
}
