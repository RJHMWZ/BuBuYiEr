using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ClawbearGames.EditorTools
{
    public static class BearMenuBuilder
    {
        public const string Root = "Assets/BuBuYiEr/UI";
        public const string ThemePath = Root + "/BearMenuArt.asset";
        public const string PrefabPath = Root + "/Prefabs/BearHomeView.prefab";
        private struct Slice
        {
            public string name; public int x,y,w,h;
            public Slice(string name, int x, int y, int w, int h) { this.name=name; this.x=x; this.y=y; this.w=w; this.h=h; }
        }

        [MenuItem("Tools/BuBuYiEr/应用参考图主菜单")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出 Play Mode 再构建菜单。");
            var sprites = new List<Sprite>();
            Import("HomeReference",1024,1536,new[] {
                new Slice("HomeTop",0,0,1024,130),new Slice("HomeLogo",0,130,1024,230),new Slice("HomeHeartCap",450,120,130,10),
                new Slice("HomeRoom",0,360,1024,595),new Slice("HomePlay",0,955,1024,190),
                new Slice("HomeNavigation",0,1145,1024,255),new Slice("HomeFooter",0,1400,1024,136),
                new Slice("CleanPaper",355,15,80,80),new Slice("StoryImage4",200,575,640,270),
                new Slice("LevelPaper",145,36,8,50),new Slice("CoinPaper",738,42,8,57)
            },sprites);
            Import("DailyReference",941,1672,new[] {
                new Slice("DailyFrame",0,0,941,1672),new Slice("CoinIcon",151,703,112,108),
                new Slice("HeartIcon",420,712,112,98),new Slice("CloverIcon",682,704,113,113),
                new Slice("FlowerIcon",146,1031,119,116),new Slice("TicketIcon",393,1034,153,100),
                new Slice("CoinsIcon",654,1034,171,104),new Slice("JarIcon",366,1308,141,174),
                new Slice("LockIcon",652,864,49,54),new Slice("HeartBurst",370,696,212,149),new Slice("WideClaimPaper",832,1394,8,67),
                new Slice("DayBadge0",132,639,151,57),new Slice("DayBadge1",398,638,156,57),new Slice("DayBadge2",662,639,151,57),
                new Slice("DayBadge3",132,973,151,57),new Slice("DayBadge4",398,972,156,57),new Slice("DayBadge5",662,973,151,57)
                ,new Slice("DailyClaimedFooter",84,855,245,74),new Slice("DailyClaimButton",369,848,211,77),
                new Slice("DailyLockedFooter",618,855,239,74),new Slice("DailyFooterPaper",837,705,8,80),
                new Slice("DailyWideLockedFooter",633,1394,229,68)
            },sprites);
            Import("CollectionReference",941,1672,new[] {
                new Slice("CollectionFrame",0,0,941,1672),new Slice("StoryImage0",95,505,344,156),
                new Slice("StoryImage1",508,478,339,185),new Slice("StoryImage2",92,768,347,139),
                new Slice("StoryImage3",507,766,341,140),new Slice("StoryImage5",508,1016,339,122),
                new Slice("StoryPreview0",92,1218,354,321),new Slice("CollectionSelectedTab",351,331,238,77),
                new Slice("CollectionCoinPaper",697,31,8,56)
            },sprites);
            Sprite[] settingsSprites=ImportSettingArt();
            Sprite[] dailySprites=ImportDailyArt();
            Sprite[] shopSprites=ImportShopArt();
            Sprite[] collectionSprites=ImportCollectionArt();
            ImportSceneArt(sprites);
            BearMenuArt art = AssetDatabase.LoadAssetAtPath<BearMenuArt>(ThemePath);
            if (art == null) { art=ScriptableObject.CreateInstance<BearMenuArt>(); AssetDatabase.CreateAsset(art,ThemePath); }
            art.sprites = sprites.ToArray();
            art.settingsSprites = settingsSprites;
            art.dailySprites = dailySprites;
            art.shopSprites = shopSprites;
            art.collectionSprites = collectionSprites;
            art.ResetLookup();
            art.chineseFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/ZCOOLKuaiLe-Regular.ttf");
            var numberImporter=AssetImporter.GetAtPath(Root+"/Fonts/Baloo2.ttf") as TrueTypeFontImporter;
            if(numberImporter!=null){numberImporter.fontNames=new[]{"BuBu Menu Round"};numberImporter.SaveAndReimport();}
            art.numberFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Baloo2.ttf");
            EditorUtility.SetDirty(art);
            Directory.CreateDirectory(Root + "/Prefabs");
            bool hasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            GameObject contents = hasPrefab ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject("BearHomeView", typeof(RectTransform), typeof(HomeViewController));
            try
            {
                BearHomeMenuController menu = contents.GetComponent<BearHomeMenuController>();
                if (menu == null) menu = contents.AddComponent<BearHomeMenuController>();
                menu.art = art;
                BearMenuFactory.Build(menu);
                // The bear menu uses its own controls; remove the old disabled template UI.
                foreach (Transform child in contents.transform.Cast<Transform>().ToArray())
                    if (child != menu.themeRoot) UnityEngine.Object.DestroyImmediate(child.gameObject);
                var homeView = new SerializedObject(contents.GetComponent<HomeViewController>());
                homeView.FindProperty("coinEffectControllerPrefab").objectReferenceValue = null;
                homeView.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            }
            finally
            {
                if (hasPrefab) PrefabUtility.UnloadPrefabContents(contents);
                else UnityEngine.Object.DestroyImmediate(contents);
            }
            AssetDatabase.SaveAssets();
            BearMenuSceneSetup.Apply();
            Debug.Log($"[BearMenu] 已切分 {sprites.Count} 个原图精灵，生成 {PrefabPath} 并保存到 Home 场景。");
        }

        [MenuItem("Tools/BuBuYiEr/使用 SceneArt 重建主菜单")]
        public static void RebuildMainFromSceneArt()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。");
            BearMenuArt art=AssetDatabase.LoadAssetAtPath<BearMenuArt>(ThemePath);
            if(art==null) throw new InvalidOperationException("缺少 BearMenuArt 配置。");
            var sprites=art.sprites.Where(s=>s!=null && AssetDatabase.GetAssetPath(s)!=Root+"/Art/SceneArt.png").ToList();
            ImportSceneArt(sprites);
            art.sprites=sprites.ToArray();
            art.ResetLookup();
            EditorUtility.SetDirty(art);
            GameObject contents=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BearHomeMenuController menu=contents.GetComponent<BearHomeMenuController>();
                menu.art=art;
                BearMenuFactory.RebuildMain(menu);
                PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();
            BearMenuSceneSetup.RebuildSceneMenu();
        }

        private static void ImportSceneArt(List<Sprite> result)
        {
            const string path=Root+"/Art/SceneArt.png";
            TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("缺少 SceneArt.png。");
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=2048;
            importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects=provider.GetSpriteRects();
            foreach(SpriteRect rect in rects)
            {
                if(rect.name=="TopUIBg") rect.border=new Vector4(48,40,48,40);
                if(rect.name=="BtnBg") rect.border=new Vector4(48,48,48,48);
                if(rect.name=="Table") rect.border=new Vector4(80,28,80,42);
            }
            provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
            result.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
        }

        public static string CollectionArtPath => File.Exists(Root + "/Art/CollectArt.png")
            ? Root + "/Art/CollectArt.png" : Root + "/Art/CollectionArt.png";

        public static void ApplySoloMenuActions()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BearMenuFactory.EnsureSoloActions(contents.GetComponent<BearHomeMenuController>());
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        [MenuItem("Tools/BuBuYiEr/使用 CollectArt 重建图鉴收藏")]
        public static void RebuildCollectionFromCollectArt()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            BearMenuArt art = AssetDatabase.LoadAssetAtPath<BearMenuArt>(ThemePath);
            if (art == null) return;
            art.collectionSprites = ImportCollectionArt();
            art.ResetLookup();
            EditorUtility.SetDirty(art);
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BearHomeMenuController menu = contents.GetComponent<BearHomeMenuController>();
                menu.art = art;
                BearMenuFactory.RebuildCollection(menu);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            AssetDatabase.SaveAssets();
        }

        private static Sprite[] ImportCollectionArt()
        {
            string path = CollectionArtPath;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return new Sprite[0];
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects = provider.GetSpriteRects();
            if (rects.Length == 0)
            {
                Slice[] slices = {
                    new Slice("CardBg",0,0,420,300),
                    new Slice("MilkTea",460,0,300,240),new Slice("GoodNight",760,0,300,242),
                    new Slice("CandyJar",1072,0,300,246),new Slice("Garden",1389,0,300,242),
                    new Slice("Travel",1709,0,300,238),new Slice("Picnic",460,260,300,240),
                    new Slice("Reading",760,260,300,240),new Slice("CatNap",1072,257,300,243),
                    new Slice("TapePink",1386,260,88,80),new Slice("TapeYellow",1485,260,90,80),
                    new Slice("IconFlower",1591,267,70,73),new Slice("IconHeart",1670,277,61,57),
                    new Slice("IconStar",1744,267,65,72),new Slice("IconMoon",1818,266,65,73),
                    new Slice("IconLock",1913,259,64,81)
                };
                rects = slices.Select(s => new SpriteRect { name=s.name, rect=new Rect(s.x,500-s.y-s.h,s.w,s.h),
                    pivot=new Vector2(.5f,.5f), alignment=SpriteAlignment.Center, spriteID=GUID.Generate() }).ToArray();
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name,r.spriteID)));
            }
            foreach (SpriteRect rect in rects)
                if (rect.name == "CardBg" && rect.border == Vector4.zero) rect.border = new Vector4(50,50,50,50);
            provider.SetSpriteRects(rects);
            provider.Apply();
            importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        [MenuItem("Tools/BuBuYiEr/使用 SettingArt 重建设置面板")]
        public static void RebuildSettingsFromSettingArt()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。");
            BearMenuArt art=AssetDatabase.LoadAssetAtPath<BearMenuArt>(ThemePath);
            if(art==null) throw new InvalidOperationException("缺少 BearMenuArt 配置。");
            var sprites=art.sprites.Where(s=>s!=null && AssetDatabase.GetAssetPath(s)!=Root+"/Art/SettingArt.png"
                && AssetDatabase.GetAssetPath(s)!=Root+"/Art/SettingsReference.png").ToList();
            art.settingsSprites=ImportSettingArt();
            art.sprites=sprites.ToArray();art.ResetLookup();EditorUtility.SetDirty(art);
            GameObject contents=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BearHomeMenuController menu=contents.GetComponent<BearHomeMenuController>();
                menu.art=art;BearMenuFactory.RebuildSettings(menu);
                PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();BearMenuSceneSetup.Apply();
        }

        private static Sprite[] ImportSettingArt()
        {
            const string path=Root+"/Art/SettingArt.png";
            TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("缺少 SettingArt.png。");
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=2048;importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects=provider.GetSpriteRects();
            foreach(SpriteRect rect in rects)
            {
                if(rect.border!=Vector4.zero) continue;
                if(rect.name=="UIBg") rect.border=new Vector4(100,100,100,100);
                if(rect.name=="TitleBg") rect.border=new Vector4(85,65,85,65);
                if(rect.name=="ChoicBg") rect.border=new Vector4(50,50,50,50);
                if(rect.name=="FillBgSlider" || rect.name=="FillSlider") rect.border=new Vector4(24,20,24,20);
            }
            provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        [MenuItem("Tools/BuBuYiEr/使用 DailyArt 重建每日奖励")]
        public static void RebuildDailyFromDailyArt()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。");
            BearMenuArt art=AssetDatabase.LoadAssetAtPath<BearMenuArt>(ThemePath);
            if(art==null) throw new InvalidOperationException("缺少 BearMenuArt 配置。");
            art.dailySprites=ImportDailyArt();art.ResetLookup();EditorUtility.SetDirty(art);
            GameObject contents=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BearHomeMenuController menu=contents.GetComponent<BearHomeMenuController>();
                menu.art=art;BearMenuFactory.RebuildDaily(menu);
                PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();BearMenuSceneSetup.Apply();
        }

        private static Sprite[] ImportDailyArt()
        {
            const string path=Root+"/Art/DailyArt.png";
            TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("缺少 DailyArt.png。");
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=2048;importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects=provider.GetSpriteRects();
            foreach(SpriteRect rect in rects)
            {
                if(rect.border!=Vector4.zero) continue;
                if(rect.name=="FinalRewardBg") rect.border=new Vector4(50,50,50,50);
                if(rect.name=="UnLockRewardBg") rect.border=new Vector4(45,100,45,45);
                if(rect.name=="TodyRewardBg") rect.border=new Vector4(40,52,40,100);
            }
            provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        [MenuItem("Tools/BuBuYiEr/使用 ShopArt 重建商店")]
        public static void RebuildShopFromShopArt()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。");
            BearMenuArt art=AssetDatabase.LoadAssetAtPath<BearMenuArt>(ThemePath);
            if(art==null) throw new InvalidOperationException("缺少 BearMenuArt 配置。");
            art.sprites=art.sprites.Where(s=>s!=null && AssetDatabase.GetAssetPath(s)!=Root+"/Art/ShopReference.png").ToArray();
            art.shopSprites=ImportShopArt();art.ResetLookup();EditorUtility.SetDirty(art);
            GameObject contents=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                BearHomeMenuController menu=contents.GetComponent<BearHomeMenuController>();
                menu.art=art;BearMenuFactory.RebuildShop(menu);
                PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();BearMenuSceneSetup.Apply();
        }

        private static Sprite[] ImportShopArt()
        {
            const string path=Root+"/Art/ShopArt.png";
            TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null) throw new InvalidOperationException("缺少 ShopArt.png。");
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=2048;importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects=provider.GetSpriteRects();
            if(rects.Length==0)
            {
                Slice[] slices={
                    new Slice("Header",0,0,668,342),new Slice("TabNormal",737,20,181,103),
                    new Slice("TabSelected",931,20,186,120),new Slice("BuyButton",719,139,205,75),
                    new Slice("GiftIcon",1154,29,63,57),new Slice("CoinIcon",1241,29,81,58),
                    new Slice("HeartIcon",1354,30,66,57),new Slice("TicketIcon",1452,28,80,61),
                    new Slice("AlbumIcon",1557,29,84,61),new Slice("Banner",1696,18,322,98),
                    new Slice("Hearts",735,232,189,92),new Slice("Bear",1012,158,193,151),
                    new Slice("NameBadge",500,373,193,55),new Slice("RowBg",775,347,498,81),
                    new Slice("FeaturedBg",1278,129,770,299)
                };
                rects=slices.Select(s=>new SpriteRect{name=s.name,rect=new Rect(s.x,428-s.y-s.h,s.w,s.h),
                    pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate()}).ToArray();
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            }
            foreach(SpriteRect rect in rects)
            {
                if(rect.border!=Vector4.zero) continue;
                if(rect.name=="TabNormal") rect.border=new Vector4(40,40,40,40);
                if(rect.name=="TabSelected") rect.border=new Vector4(50,46,50,40);
                if(rect.name=="BuyButton") rect.border=new Vector4(32,24,32,24);
                if(rect.name=="NameBadge") rect.border=new Vector4(24,20,24,20);
                if(rect.name=="RowBg") rect.border=new Vector4(36,32,36,32);
                if(rect.name=="FeaturedBg") rect.border=new Vector4(45,45,45,45);
            }
            provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        private static void Import(string file,int width,int height,Slice[] slices,List<Sprite> result)
        {
            string path=Root+"/Art/"+file+".png";
            TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer==null) throw new Exception("缺少参考图："+path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=100;
            importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;
            importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.isReadable=false;
            importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();
            factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
            var metadata=new List<SpriteRect>();
            foreach(Slice slice in slices)
            {
                if (slice.x<0 || slice.y<0 || slice.x+slice.w>width || slice.y+slice.h>height) throw new Exception("切图越界："+slice.name);
                metadata.Add(new SpriteRect { name=slice.name,rect=new Rect(slice.x,height-slice.y-slice.h,slice.w,slice.h),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,
                    spriteID=previous.TryGetValue(slice.name,out GUID existing)?existing:GUID.Generate() });
            }
            provider.SetSpriteRects(metadata.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(metadata.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            result.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
        }
    }
}
