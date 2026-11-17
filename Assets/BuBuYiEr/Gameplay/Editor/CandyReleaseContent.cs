using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ClawbearGames.EditorTools
{
    public static class CandyReleaseContent
    {
        [MenuItem("Tools/BuBuYiEr/应用首版旅程内容与平衡")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("请先退出 Play Mode。");
            var catalog=AssetDatabase.LoadAssetAtPath<BearShopCatalog>("Assets/BuBuYiEr/UI/BearShopCatalog.asset");
            if(catalog==null)throw new System.InvalidOperationException("缺少金币商店配置。");
            var items=new List<BearShopItem>(catalog.items ?? new BearShopItem[0]);
            Add(items,"decor_heart","心意桌饰","小屋桌面摆件 · 一颗暖暖的心",BearShopCategory.Decoration,100,1,BearShopEffect.Decoration,"IconLove");
            Add(items,"decor_flower","小花时光","小屋桌面摆件 · 把花园带回家",BearShopCategory.Decoration,140,4,BearShopEffect.Decoration,"Daily/IconFlower");
            Add(items,"decor_star","星星心愿","小屋桌面摆件 · 收藏一颗小星星",BearShopCategory.Decoration,180,9,BearShopEffect.Decoration,"IconStar");
            Add(items,"decor_moon","月亮晚安","小屋桌面摆件 · 陪伴每一个好梦",BearShopCategory.Decoration,240,16,BearShopEffect.Decoration,"Collect/IconMoon");
            Add(items,"decor_book","桌边故事","小屋桌面摆件 · 把喜欢的书放在身边",BearShopCategory.Decoration,320,36,BearShopEffect.Decoration,"Book");
            Add(items,"skin_milk","奶霜曲奇","曲奇奶霜主题 · 六种球独立配色",BearShopCategory.Skin,160,1,BearShopEffect.BallSkin,"Daily/IconBottle");
            Add(items,"skin_jelly","果冻泡泡","透明果冻主题 · 六色球体与气泡尾迹",BearShopCategory.Skin,220,9,BearShopEffect.BallSkin,"Daily/IconGrass");
            Add(items,"skin_sunset","焦糖流星","流星糖纹主题 · 六色球体与彗星尾迹",BearShopCategory.Skin,280,16,BearShopEffect.BallSkin,"Daily/IconFlower");
            Add(items,"skin_starry","星光棱晶","宝石棱面主题 · 六色晶球与闪星尾迹",BearShopCategory.Skin,360,25,BearShopEffect.BallSkin,"Collect/IconMoon");
            Add(items,"brick_mint","薄荷云朵糖砖","薄荷奶霜与小云朵 · 全盘统一外观",BearShopCategory.Skin,140,1,BearShopEffect.BrickSkin,"");
            Add(items,"brick_blueberry","蓝莓星糖砖","蓝莓糖霜与小星星 · 全盘统一外观",BearShopCategory.Skin,180,4,BearShopEffect.BrickSkin,"");
            Add(items,"brick_pudding","蜂蜜布丁糖砖","蜂蜜布丁与小花朵 · 全盘统一外观",BearShopCategory.Skin,160,9,BearShopEffect.BrickSkin,"");
            Add(items,CandyCosmetics.SupportIds[0],"蜂蜜罐","增加 120 本关蜂蜜 · 每关可用 1 次",BearShopCategory.Item,50,1,BearShopEffect.HoneyJar,CandyCosmetics.SupportIcons[0]);
            Add(items,CandyCosmetics.SupportIds[1],"糖果锤","全盘糖砖减少 30% 初始数字 · 每关 1 次",BearShopCategory.Item,65,4,BearShopEffect.CandyHammer,CandyCosmetics.SupportIcons[1]);
            Add(items,CandyCosmetics.SupportIds[2],"甜蜜风","全队加速 35%，持续 12 秒 · 每关 1 次",BearShopCategory.Item,55,9,BearShopEffect.SweetWind,CandyCosmetics.SupportIcons[2]);
            catalog.items=items.ToArray();EditorUtility.SetDirty(catalog);
            foreach(var level in Resources.LoadAll<LevelData>("Levels"))
            {
                int n=level.levelNumber,chapter=CandyProgression.Chapter(n);
                int within=n-CandyProgression.ChapterStarts[chapter];
                level.columns=n<4?7:n<16?8:9;
                level.rows=n<3?4:n<9?5:Mathf.Clamp(5+chapter/2+(within%4==3?1:0),5,8);
                level.brickHealth=n==1?1:2+(n-1)/6+(within%6==5?1:0);
                // Eight silhouettes alternate with lighter recovery boards after each big candy.
                level.pattern=n<=3?0:n<=8?(n-4)%2:new[]{0,5,3,1,4,7,2,6}[(within+chapter)%8];
                level.boss=n%10==0 || n==101;
                level.startingHoney=90+chapter*24+Mathf.Min(60,within*3);
                level.coinReward=30+chapter*8+(level.boss?20:0);
                level.threeStarTime=35+chapter*10+(level.boss?15:0);
                level.twoStarTime=level.threeStarTime*1.7f;
                EditorUtility.SetDirty(level);
            }
            AssetDatabase.SaveAssets();
            BearMenuBuilder.Build();
            Debug.Log("RELEASE CONTENT: 101 levels, six chapters, six progressive companions, "+items.Count+" shop items and 12 journey rewards baked.");
        }
        private static void Add(List<BearShopItem> items,string id,string title,string description,BearShopCategory category,int coins,int unlock,BearShopEffect effect,string art)
        {
            // Keep hand-authored changes when rebuilding the menu or adding future products.
            if(items.Any(x=>x!=null && x.id==id))return;
            items.Add(new BearShopItem { id=id,title=title,description=description,category=category,coinPrice=coins,unlockLevel=unlock,effect=effect,artKey=art,consumable=category==BearShopCategory.Item });
        }
    }
}
