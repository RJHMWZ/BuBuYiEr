using System;
using System.Linq;
using UnityEngine;

namespace ClawbearGames.EditorTools
{
    public static class CandyProgressionValidation
    {
        private static void Check(bool condition,string message)=>CandyBreakoutValidation.Check(condition,message);
        public static void RunChecks()
        {
            string snapshot=PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA,"");
            try
            {
                var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<BearShopCatalog>("Assets/BuBuYiEr/UI/BearShopCatalog.asset");
                Check(catalog.items.Length>=12,"release shop has twelve working products");
                for(int i=0;i<catalog.items.Length;i++)Check(catalog.IsValid(i) && catalog.items[i].effect!=BearShopEffect.None,"catalog product valid "+i);
                foreach(int reached in new[]{1,3,4,8,9,15,16,24,25,35,36,102})
                {
                    var data=ScriptableObject.CreateInstance<LevelData>();data.levelNumber=1;data.startingHoney=100000;
                    var board=new CandyBreakoutSimulation(data,reached);
                    for(int kind=0;kind<6;kind++)
                    {
                        int before=board.Honey;
                        bool expected=reached>CandyProgression.BallUnlockClears[kind];
                        Check(board.Buy(kind)==expected,"unlock boundary and replay "+reached+" kind "+kind);
                        if(!expected)Check(board.Honey==before && !board.Upgrade(kind),"locked APIs never spend honey");
                    }
                    UnityEngine.Object.DestroyImmediate(data);
                }
                PlayerDataHandler.SaveData(new PlayerData { CurrentLevel=1,TotalCoins=1000 });
                int locked=Array.FindIndex(catalog.items,x=>x.id=="decor_moon");
                Check(BearCoinShop.Buy(catalog,locked)==BearPurchaseResult.Locked && PlayerDataHandler.GetTotalCoin()==1000,"locked shop API cannot spend coins");
                int decor=Array.FindIndex(catalog.items,x=>x.id=="decor_heart");
                Check(BearCoinShop.Buy(catalog,decor)==BearPurchaseResult.Success,"real decoration purchase");
                Check(PlayerDataHandler.ToggleEquipment(catalog.items[decor]) && PlayerDataHandler.GetEquipment(BearShopEffect.Decoration)=="decor_heart","owned decor equips");
                Check(PlayerDataHandler.ToggleEquipment(catalog.items[decor]) && PlayerDataHandler.GetEquipment(BearShopEffect.Decoration)=="","owned decor unequips");
                Check(!PlayerDataHandler.ToggleEquipment(catalog.items[locked]),"unowned decor cannot equip");
                PlayerDataHandler.GrantShopItem(CandyCosmetics.SupportIds[0],2);
                Check(PlayerDataHandler.TryConsumeShopItem(CandyCosmetics.SupportIds[0]) && PlayerDataHandler.GetShopQuantity(CandyCosmetics.SupportIds[0])==1,"support consumption persists exactly once");
                var probe=ScriptableObject.CreateInstance<LevelData>();probe.brickHealth=1000;probe.startingHoney=100;
                var support=new CandyBreakoutSimulation(probe);
                Check(support.UseSupport(0) && support.Honey==220 && !support.UseSupport(0) && support.Honey==220,"honey support once per round");
                int health=support.Bricks[0].Health;
                Check(support.UseSupport(1) && support.Bricks[0].Health<health && !support.UseSupport(1),"hammer damages real bricks once");
                float speed=support.Balls[0].Velocity.magnitude;
                Check(support.UseSupport(2) && Mathf.Abs(support.Balls[0].Velocity.magnitude-speed*1.35f)<.1f,"wind increases physical speed");
                for(int i=0;i<121;i++)support.Step(.1f);
                Check(support.WindRemaining==0 && Mathf.Abs(support.Balls[0].Velocity.magnitude-speed)<.1f,"wind expires and restores speed");
                UnityEngine.Object.DestroyImmediate(probe);
                Check(!PlayerDataHandler.TryClaimJourney(0),"unearned journey cannot claim");
                PlayerDataHandler.UpdateCurrentLevel(4);
                int coins=PlayerDataHandler.GetTotalCoin();
                Check(PlayerDataHandler.TryClaimJourney(0) && PlayerDataHandler.GetTotalCoin()==coins+60 && !PlayerDataHandler.TryClaimJourney(0),"milestone claim atomic and idempotent");
                typeof(PlayerDataHandler).GetMethod("ResetCache",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,null);
                Check(PlayerDataHandler.IsJourneyClaimed(0) && PlayerDataHandler.GetShopQuantity(CandyCosmetics.SupportIds[0])==1,"inventory and claims survive relaunch");
                PlayerDataHandler.SaveData(JsonUtility.FromJson<PlayerData>("{\"CurrentLevel\":25,\"TotalCoins\":777}"));
                Check(CandyProgression.BallUnlocked(4,CandyProgression.ReachedLevel) && !CandyProgression.BallUnlocked(5,CandyProgression.ReachedLevel) && PlayerDataHandler.GetTotalCoin()==777,"legacy progress unlocks correct companions without resetting wallet");
                var first=Resources.Load<LevelData>("Levels/Level_1");
                Check(CandyProgression.RoundReward(first,3,0)==first.coinReward+30 && CandyProgression.RoundReward(first,3,3)==Mathf.Max(8,first.coinReward/4),"first clear, new stars and replay rewards differ");
                Check(CandyProgression.RoundReward(first,3,1)==Mathf.Max(8,first.coinReward/4)+20,"only newly earned stars grant bonuses");
                Debug.Log("PROGRESSION CHECKS PASS: exact unlock boundaries/replay, locked APIs, ownership/equipment, persisted consumption, one-use supports, wind expiry, atomic claims, legacy save and star rewards");
                Balance();
            }
            finally
            {
                if(string.IsNullOrEmpty(snapshot))PlayerPrefs.DeleteKey(PlayerDataHandler.PLAYER_DATA);
                else PlayerDataHandler.SaveData(JsonUtility.FromJson<PlayerData>(snapshot));
                PlayerPrefs.Save();
            }
        }
        private static void Balance()
        {
            var levels=Resources.LoadAll<LevelData>("Levels").OrderBy(x=>x.levelNumber).ToArray();
            var times=new System.Collections.Generic.List<float>();
            foreach(var level in levels)
            {
                var board=new CandyBreakoutSimulation(level,level.levelNumber);
                float nextDecision=0,nextTap=0;
                while(!board.Complete && board.Elapsed<300)
                {
                    if(board.Elapsed>=nextDecision)
                    {
                        nextDecision=board.Elapsed+.5f;
                        for(int kind=5;kind>=0;kind--)if(board.IsUnlocked(kind) && board.Counts[kind]<2)board.Buy(kind);
                        for(int kind=0;kind<6;kind++)if(board.Upgrades[kind]<4)board.Upgrade(kind);
                        if(board.ClickLevel<2)board.UpgradeClick();
                    }
                    if(board.Elapsed>=nextTap)
                    {
                        nextTap=board.Elapsed+.5f;
                        foreach(var brick in board.Bricks)if(brick.Alive){board.Click(brick.Bounds.center);break;}
                    }
                    board.Step(.1f);
                }
                Check(board.Complete,"no-tools two-taps/sec campaign completion level "+level.levelNumber+" elapsed "+board.Elapsed);
                for(int i=0;i<6;i++)Check(board.Counts[i]==0 || board.IsUnlocked(i),"balance bot respects unlock gates");
                times.Add(board.Elapsed);
                Debug.Log("BALANCE LEVEL "+level.levelNumber+": "+board.Elapsed.ToString("F1")+" sec, target "+level.threeStarTime+", balls "+board.Balls.Count);
            }
            times.Sort();
            Debug.Log("CAMPAIGN BALANCE PASS: 101 levels without consumables, 2 taps/sec, median "+times[times.Count/2].ToString("F1")+" s, min "+times[0].ToString("F1")+" s, max "+times[times.Count-1].ToString("F1")+" s");
        }
        public static void CheckRuntime(BearHomeMenuController menu)
        {
            string snapshot=PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA);
            try
            {
                PlayerDataHandler.SaveData(new PlayerData { CurrentLevel=9,TotalCoins=900,WatchedTutorial=1 });
                ServicesManager.Instance.CoinManager.NotifyBalanceChanged();
                menu.OpenPage(3);
                int index=Array.FindIndex(menu.art.shopCatalog.items,x=>x.id=="decor_heart");
                menu.shopProductRows[index].Find("Purchase").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Check(PlayerDataHandler.GetEquipment(BearShopEffect.Decoration)=="decor_heart" && PlayerDataHandler.GetTotalCoin()==800,"real shop buy auto-equips and saves ownership");
                menu.OpenPage(0);
                Check(menu.pages[0].transform.Find("BearRoom/EquippedOrnament").gameObject.activeSelf,"equipped ornament actually visible in home");
                CandyBreakoutFlowValidation.Render(menu.themeRoot.gameObject,"release-home.png",1080,2400);
                menu.OpenPage(3);menu.PreviewShopCategory(2);
                int skin=Array.FindIndex(menu.art.shopCatalog.items,x=>x.id=="skin_milk");
                menu.shopProductRows[skin].Find("Purchase").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Check(PlayerDataHandler.GetEquipment(BearShopEffect.BallSkin)=="skin_milk" && PlayerDataHandler.GetTotalCoin()==640,"real skin equips without affecting ball stats");
                menu.Handle("close-modal");
                CandyBreakoutFlowValidation.Render(menu.themeRoot.gameObject,"release-skins.png",1080,1920);
                menu.PreviewShopCategory(3);
                CandyBreakoutFlowValidation.Render(menu.themeRoot.gameObject,"release-tools.png",1080,1920);
                menu.OpenPage(2);menu.CollectionPanel.SelectBall(3);
                Check(!menu.CollectionPanel.readButton.interactable && menu.CollectionPanel.ballLocks[3].activeSelf,"locked companion shows requirement and no usable action");
                CandyBreakoutFlowValidation.Render(menu.themeRoot.gameObject,"release-companions.png",1080,2400);
                menu.CollectionPanel.SelectTab(3);
                menu.CollectionPanel.journeyClaims[0].onClick.Invoke();
                Check(PlayerDataHandler.IsJourneyClaimed(0) && PlayerDataHandler.GetTotalCoin()==700,"actual journey button claims one reward");
                menu.CollectionPanel.journeyClaims[0].onClick.Invoke();
                Check(PlayerDataHandler.GetTotalCoin()==700,"repeated journey click cannot grant twice");
                menu.Handle("close-modal");
                CandyBreakoutFlowValidation.Render(menu.themeRoot.gameObject,"release-journey.png",1080,1920);
                // A fresh component must reuse baked design coordinates after serialization/reload.
                foreach(var size in new[]{new Vector2(1080,1920),new Vector2(1080,2400)})
                {
                    menu.CollectionPanel.SelectTab(2);
                    CandyBreakoutFlowValidation.Render(menu.themeRoot.gameObject,"release-partners-"+size.y+".png",(int)size.x,(int)size.y);
                    var page=(RectTransform)menu.pages[2].transform;
                    var detail=(RectTransform)page.Find("CollectionDetails");
                    Check(-detail.anchoredPosition.y+detail.rect.height <= page.rect.height-120,"collection details remain inside safe frame after aspect switch");
                }
                Debug.Log("CONTENT UI PASS: actual product purchase/equip, visible ornament, skin equip, locked companion, actual milestone click and two-ratio detail bounds");
            }
            finally
            {
                PlayerDataHandler.SaveData(JsonUtility.FromJson<PlayerData>(snapshot));
                ServicesManager.Instance.CoinManager.NotifyBalanceChanged();menu.OpenPage(0);
            }
        }
    }
}
