using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
            Import("ShopReference",941,1672,new[] {
                new Slice("ShopFrame",0,0,941,1672),new Slice("ShopRow1",43,946,855,136),
                new Slice("ShopRow2",43,1095,855,132),new Slice("ShopRow3",43,1243,855,132),
                new Slice("ShopRow4",43,1390,855,137),new Slice("SubCoinPaper",675,32,7,67),new Slice("ShopSelectedTab",30,499,173,83)
            },sprites);
            Import("SettingsReference",941,1672,new[] {
                new Slice("SettingsFrame",0,0,941,1672),new Slice("PawSlider",586,619,75,76),new Slice("SliderPaper",838,615,8,85),
                new Slice("TogglePaper0",838,827,8,72),new Slice("TogglePaper1",838,914,8,72)
            },sprites);
            BearMenuArt art = AssetDatabase.LoadAssetAtPath<BearMenuArt>(ThemePath);
            if (art == null) { art=ScriptableObject.CreateInstance<BearMenuArt>(); AssetDatabase.CreateAsset(art,ThemePath); }
            art.sprites = sprites.ToArray();
            art.ResetLookup();
            art.chineseFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/ZCOOLKuaiLe-Regular.ttf");
            var numberImporter=AssetImporter.GetAtPath(Root+"/Fonts/Baloo2.ttf") as TrueTypeFontImporter;
            if(numberImporter!=null){numberImporter.fontNames=new[]{"BuBu Menu Round"};numberImporter.SaveAndReimport();}
            art.numberFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Baloo2.ttf");
            EditorUtility.SetDirty(art);
            Directory.CreateDirectory(Root + "/Prefabs");
            GameObject contents = PrefabUtility.LoadPrefabContents("Assets/Diamond_Loop/Prefabs/Views/HomeView.prefab");
            try
            {
                BearHomeMenuController menu = contents.GetComponent<BearHomeMenuController>();
                if (menu == null) menu = contents.AddComponent<BearHomeMenuController>();
                menu.art = art;
                BearMenuFactory.Build(menu);
                PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            AssetDatabase.SaveAssets();
            HomeViewController view = AssetDatabase.LoadAssetAtPath<HomeViewController>(PrefabPath);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(view,out string guid,out long id)) throw new Exception("无法读取新主菜单预制体。");
            const string scene = "Assets/Diamond_Loop/Scenes/Home.unity";
            string yaml = File.ReadAllText(scene);
            const string pattern = @"homeViewControllerPrefab: \{fileID: [^,]+, guid: [^,]+,\s*type: 3\}";
            if (Regex.Matches(yaml,pattern).Count != 1) throw new Exception("Home 场景中的 ViewManager 引用数量异常，未替换。");
            File.WriteAllText(scene,Regex.Replace(yaml,pattern,$"homeViewControllerPrefab: {{fileID: {id}, guid: {guid}, type: 3}}"));
            AssetDatabase.ImportAsset(scene,ImportAssetOptions.ForceUpdate);
            Debug.Log($"[BearMenu] 已切分 {sprites.Count} 个原图精灵，生成 {PrefabPath} 并接入 Home 场景。");
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
