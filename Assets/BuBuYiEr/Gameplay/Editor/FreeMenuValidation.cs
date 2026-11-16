using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames.EditorTools
{
    public static class FreeMenuValidation
    {
        private static void Check(bool condition, string message) => CandyBreakoutValidation.Check(condition, message);


        public static void BuildAndroid()
        {
            Check(Application.productName == "BuBuYiErBreakoutValidation" &&
                Application.dataPath.Replace("\\", "/").EndsWith("/Temp/BreakoutValidation/Assets"),
                "Android verification requires the isolated validation project");
            BearMenuBuilder.Build();
            var report = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions {
                scenes = UnityEditor.EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToArray(),
                locationPathName = "Builds/BuBuYiEr-Free-Validation.apk",
                target = UnityEditor.BuildTarget.Android,
                options = UnityEditor.BuildOptions.None
            });
            Check(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded,
                "Android APK build: " + report.summary.result);
            Debug.Log("FREE ANDROID APK BUILD PASSED: " + report.summary.totalSize + " bytes");
        }

        public static void CheckPurchases()
        {
            string snapshot = PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA, "");
            var catalog = ScriptableObject.CreateInstance<BearShopCatalog>();
            try
            {
                catalog.items = new[] {
                    new BearShopItem { id="test_hat", title="Hat", coinPrice=120, category=BearShopCategory.Decoration },
                    new BearShopItem { id="test_item", title="Item", coinPrice=50, consumable=true, category=BearShopCategory.Item },
                    new BearShopItem { id="test_hidden", title="Hidden", coinPrice=10, available=false }
                };
                PlayerDataHandler.SaveData(new PlayerData { TotalCoins=200 });
                Check(BearCoinShop.Buy(catalog, -1)==BearPurchaseResult.Unavailable, "invalid product rejected");
                Check(BearCoinShop.Buy(catalog, 2)==BearPurchaseResult.Unavailable, "unlisted product rejected");
                Check(BearCoinShop.Buy(catalog, 0)==BearPurchaseResult.Success, "coin purchase succeeds offline");
                Check(PlayerDataHandler.GetTotalCoin()==80 && BearCoinShop.Quantity("test_hat")==1, "wallet and ownership committed together");
                Check(BearCoinShop.Buy(catalog, 0)==BearPurchaseResult.AlreadyOwned && PlayerDataHandler.GetTotalCoin()==80, "double tap cannot charge twice for unique item");
                Check(BearCoinShop.Buy(catalog, 1)==BearPurchaseResult.Success, "consumable purchase");
                Check(BearCoinShop.Buy(catalog, 1)==BearPurchaseResult.NotEnoughCoins && PlayerDataHandler.GetTotalCoin()==30 && BearCoinShop.Quantity("test_item")==1, "insufficient balance cannot change wallet or inventory");
                var persisted = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA));
                Check(persisted.TotalCoins==30 && persisted.ShopInventory.Count==2, "one serialized transaction contains wallet and inventory");
                typeof(PlayerDataHandler).GetMethod("ResetCache", System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
                Check(PlayerDataHandler.GetTotalCoin()==30 && BearCoinShop.Quantity("test_hat")==1, "inventory survives cache reset / relaunch");
                catalog.items[1].coinPrice = -1;
                Check(BearCoinShop.Buy(catalog, 1)==BearPurchaseResult.Unavailable, "negative price cannot grant coins");
                catalog.items[1].coinPrice = 50;
                catalog.items[1].id = "test_hat";
                Check(!catalog.IsValid(0) && !catalog.IsValid(1), "duplicate product IDs fail closed");
                PlayerDataHandler.SaveData(JsonUtility.FromJson<PlayerData>("{\"CurrentLevel\":7,\"TotalCoins\":800}"));
                Check(PlayerDataHandler.GetCurrentLevel()==7 && PlayerDataHandler.GetTotalCoin()==800 && BearCoinShop.Quantity("test_hat")==0, "legacy saves keep progress and wallet with empty inventory");
                Debug.Log("FREE SHOP: offline purchase, atomic save, insufficient funds, duplicate taps/IDs, invalid price and old save migration PASS");
            }
            finally
            {
                if (string.IsNullOrEmpty(snapshot)) PlayerPrefs.DeleteKey(PlayerDataHandler.PLAYER_DATA);
                else PlayerDataHandler.SaveData(JsonUtility.FromJson<PlayerData>(snapshot));
                PlayerPrefs.Save();
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        public static void CheckRuntime(BearHomeMenuController menu)
        {
            foreach (int page in new[] { 1, 4 })
            {
                Check(menu.pages[page].transform.Find("CloseIcon") == null, "redundant panel close icon removed");
                Check(menu.pages[page].transform.Find(page == 1 ? "CloseDailyReward" : "CloseSettings") == null,
                    "redundant panel close hit target removed");
            }
            var coin = menu.coinTexts.Single();
            var corners = new Vector3[4];
            foreach (Vector2 viewport in new[] { new Vector2(1080,1920), new Vector2(1080,2400), new Vector2(1024,1536), new Vector2(1536,2048), new Vector2(1920,1080) })
            {
                Rect safe = new Rect(24, 80, viewport.x-48, viewport.y-180);
                Vector3[] baseline = null;
                int fontSize = coin.fontSize;
                for (int page = 0; page < 5; page++)
                {
                    menu.OpenPage(page);
                    Canvas.ForceUpdateCanvases();
                    menu.header.Refresh(safe, viewport);
                    coin.rectTransform.GetWorldCorners(corners);
                    if (baseline == null) baseline = (Vector3[])corners.Clone();
                    for (int c = 0; c < 4; c++) Check(Vector3.Distance(baseline[c],corners[c])<.001f, "coin screen bounds identical on every page " + viewport);
                    Check(menu.coinTexts[0]==coin && coin.fontSize==fontSize, "same coin object and font size on page " + page);
                    Check(menu.header.levelStatus.activeSelf==(page==0) && menu.header.backStatus.activeSelf==(page!=0), "header switches only left control");
                    Check(menu.pages[page].transform.Find("Header")==null, "no duplicate page header");
                }
                var parent = (RectTransform)menu.themeRoot;
                float top = (safe.yMax/viewport.y-.5f)*parent.rect.height;
                float left = (safe.xMin/viewport.x-.5f)*parent.rect.width;
                float right = (safe.xMax/viewport.x-.5f)*parent.rect.width;
                var headerRect = (RectTransform)menu.header.transform;
                headerRect.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 local = parent.InverseTransformPoint(corner);
                    Check(local.x>=left-.01f && local.x<=right+.01f && local.y<=top+.01f, "header fits asymmetric notch / gesture safe area");
                }
            }

            string snapshot = PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA);
            BearShopCatalog previous = menu.art.shopCatalog;
            var catalog = ScriptableObject.CreateInstance<BearShopCatalog>();
            try
            {
                catalog.items = Enumerable.Range(0,14).Select(i => new BearShopItem { id="ui_test_"+i, title="小物 "+i, coinPrice=100, category=(BearShopCategory)(i%3) }).ToArray();
                menu.art.shopCatalog = catalog;
                menu.OpenPage(3);
                Check(!menu.shopEmpty.activeSelf && menu.shopProductRows.Length==14 && menu.shopContent.rect.height>menu.shopContent.parent.GetComponent<RectTransform>().rect.height, "catalog builds a scrollable shop beyond one screen");
                menu.PreviewShopCategory(2);
                Check(menu.shopProductRows.Count(x=>x.gameObject.activeSelf)==5, "skin filter shows exactly its own products");
                ServicesManager.Instance.CoinManager.DebugSetTotalCoins(1000);
                menu.PreviewShopCategory(0);
                // Capture the configured catalog as well as the shipped empty state.
                CandyBreakoutFlowValidation.Render(menu.themeRoot.gameObject, "free-shop-products.png", 1080, 1920);
                menu.shopProductRows[0].Find("Purchase").GetComponent<Button>().onClick.Invoke();
                Check(BearCoinShop.Quantity("ui_test_0")==1 && PlayerDataHandler.GetTotalCoin()==900, "real shop button grants inventory and spends coins");
                Check(menu.storePrices[0].text=="已拥有" && !menu.storePrices[0].GetComponentInParent<Button>().interactable && menu.coinTexts[0].text=="900", "purchased state and shared wallet refresh immediately");
                menu.Handle("earn-coins");
                Check(menu.modal.activeSelf && menu.modalTitle.text=="获取金币", "plus opens earned coin sources");
                Check(menu.header.GetComponentsInChildren<Image>(true).All(x=>x.sprite==null || !x.sprite.name.Contains("Reference")), "header uses shared source sprites");
                Debug.Log("FREE MENU: five screen shapes, five pages, shared header bounds, notch, catalog scrolling/filter, purchase UI and coin sources PASS");
            }
            finally
            {
                menu.art.shopCatalog = previous;
                PlayerDataHandler.SaveData(JsonUtility.FromJson<PlayerData>(snapshot));
                ServicesManager.Instance.CoinManager.NotifyBalanceChanged();
                menu.OpenPage(0);
                menu.header.Refresh(Screen.safeArea, new Vector2(Screen.width,Screen.height));
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }
    }
}
