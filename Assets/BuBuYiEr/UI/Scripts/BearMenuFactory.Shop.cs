using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public static partial class BearMenuFactory
    {
        private static void Shop(BearHomeMenuController menu, Transform frame)
        {
            RectTransform footer = Node(frame, "FooterGarden", 0, 1538, 941, 134);
            Picture(menu, footer, "GrassGround", "GrassGround", -20, 0, 981, 144);
            Picture(menu, footer, "LeftFlowers", "BushL", -40, 0, 300, 140);
            Picture(menu, footer, "RightFlowers", "BushR", 681, 0, 300, 140);
            Picture(menu, frame, "TitleCloud", "Setting/TitleBg", 196, 154, 550, 145).type = Image.Type.Sliced;
            Picture(menu, frame, "TitleIcon", "IconShop", 238, 176, 100, 90).preserveAspect = true;
            Label(menu, frame, "Title", "金币小铺", 345, 162, 340, 108, 65);
            Label(menu, frame, "CurrencyHint", "通关攒金币，挑选喜欢的小物", 70, 304, 801, 55, 32);
            Picture(menu, frame, "ShopPanel", "Setting/UIBg", 26, 474, 889, 1090).type = Image.Type.Sliced;
            menu.shopTabImages = new Image[4];
            string[] names = { "全部", "装扮", "皮肤", "道具" };
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform tab = Node(frame, "Category" + i, 53 + i * 212, 390, 200, 106);
                menu.shopTabImages[i] = Picture(menu, tab, "Background", "Shop/TabNormal", 0, 0, 200, 82);
                menu.shopTabImages[i].type = Image.Type.Sliced;
                Label(menu, tab, "Label", names[i], 8, 7, 184, 66, 40);
                Action(tab, "shop-tab", i);
            }

            RectTransform viewport = Node(frame, "ShopShelf", 58, 518, 825, 980);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image drag = viewport.gameObject.AddComponent<Image>();
            drag.color = Color.clear;
            drag.canvasRenderer.cullTransparentMesh = false;
            menu.shopContent = Node(viewport, "Products", 0, 0, 825, 980);
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = menu.shopContent;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.decelerationRate = .12f;
            scroll.scrollSensitivity = 65;
            menu.shopEmpty = Node(viewport, "EmptyShop", 0, 80, 825, 640).gameObject;
            Picture(menu, menu.shopEmpty.transform, "Gift", "IconReward", 292, 20, 240, 220).preserveAspect = true;
            Label(menu, menu.shopEmpty.transform, "Title", "新小物正在准备中", 30, 288, 765, 72, 43);
            Label(menu, menu.shopEmpty.transform, "Hint", "先去闯关攒金币吧\n装扮、皮肤和道具会陆续来到小铺", 30, 390, 765, 132, 31);
            Picture(menu, frame, "BottomBow", "Setting/Decoration1", 364, 1513, 213, 116);
            PopulateShop(menu);
            menu.PreviewShopCategory(0);
        }

        public static void PopulateShop(BearHomeMenuController menu)
        {
            for (int i = menu.shopContent.childCount - 1; i >= 0; i--)
            {
                GameObject child = menu.shopContent.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Object.Destroy(child); else Object.DestroyImmediate(child);
            }
            BearShopCatalog catalog = menu.art.shopCatalog;
            int count = catalog != null && catalog.items != null ? catalog.items.Length : 0;
            menu.shopProductRows = new RectTransform[count];
            menu.storePrices = new Text[count];
            for (int i = 0; i < count; i++)
            {
                if (!catalog.IsValid(i) || !catalog.items[i].available) continue;
                BearShopItem item = catalog.items[i];
                RectTransform row = Node(menu.shopContent, "Product" + i, 0, 0, 825, 260);
                menu.shopProductRows[i] = row;
                Picture(menu, row, "Background", "Shop/RowBg", 0, 0, 825, 260).type = Image.Type.Sliced;
                Image icon = Picture(menu, row, "Icon", "IconReward", 24, 28, 136, 136);
                if (item.icon != null) icon.sprite = item.icon;
                else if(!string.IsNullOrEmpty(item.artKey))icon.sprite=menu.art.Get(item.artKey);
                icon.preserveAspect = true;
                if(item.effect==BearShopEffect.BallSkin)
                {
                    icon.enabled=false;
                    icon.rectTransform.sizeDelta=new Vector2(214,204);
                    icon.rectTransform.anchoredPosition=new Vector2(14,-8);
                    var preview=Node(icon.transform,"MaterialPreview",0,0,214,204).gameObject.AddComponent<CandyBallPortrait>();
                    preview.kind=0;preview.skin=item.id;preview.showTrail=true;preview.raycastTarget=false;
                }
                if(item.effect==BearShopEffect.CandyHammer)
                {
                    icon.enabled=false;
                    Shape(icon.transform,"Handle",60,65,23,69,10,new Color32(215,164,109,255),new Color32(215,164,109,255),3);
                    Shape(icon.transform,"CandyHead",20,24,105,65,22,new Color32(247,183,194,255),new Color32(239,166,180,255),4);
                    Shape(icon.transform,"Shine",34,36,52,8,4,new Color(1,1,1,.7f),new Color(1,1,1,.7f),0);
                }
                Text title = Label(menu, row, "Name", item.title, 184, 19, 600, 60, 37);
                title.alignment = TextAnchor.MiddleLeft;
                Text description = Label(menu, row, "Description", item.description, 184, 82, 600, 53, 25);
                description.alignment = TextAnchor.MiddleLeft;
                Label(menu, row, "Inventory", "", 184, 145, 280, 55, 27).alignment = TextAnchor.MiddleLeft;
                RectTransform buy = Node(row, "Purchase", 505, 124, 294, 120);
                Picture(menu, buy, "Background", "Shop/BuyButton", 0, 0, 294, 120).type = Image.Type.Sliced;
                menu.storePrices[i] = Label(menu, buy, "Price", item.coinPrice + " 金币", 6, 0, 282, 120, 34);
                Button button = Action(buy, "buy", i);
                button.targetGraphic = buy.Find("Background").GetComponent<Image>();
                if(item.effect==BearShopEffect.BallSkin)
                {
                    row.sizeDelta=new Vector2(825,230);
                    ((RectTransform)row.Find("Background")).sizeDelta=row.sizeDelta;
                    title.rectTransform.anchoredPosition=new Vector2(235,-10);title.rectTransform.sizeDelta=new Vector2(560,48);
                    description.rectTransform.anchoredPosition=new Vector2(235,-63);description.rectTransform.sizeDelta=new Vector2(560,40);
                    var inventory=row.Find("Inventory") as RectTransform;
                    inventory.anchoredPosition=new Vector2(235,-154);inventory.sizeDelta=new Vector2(250,50);
                    buy.anchoredPosition=new Vector2(505,-108);
                }
            }
        }
    }
}
