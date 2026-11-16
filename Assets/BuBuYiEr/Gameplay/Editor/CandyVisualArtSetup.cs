using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ClawbearGames.EditorTools
{
    public static class CandyVisualArtSetup
    {
        [MenuItem("Tools/BuBuYiEr/应用糖果工坊视觉")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("请先退出 Play Mode。");
            const string path="Assets/BuBuYiEr/Gameplay/Resources/CandyArt/SweetBakeryBackdrop.png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=false;importer.isReadable=false;
            importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out int w,out int h);
            var factory=new SpriteDataProviderFactories();factory.Init();
            var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var previous=provider.GetSpriteRects();
            string[] names={"BakeryRoom","BakeryTray"};
            Rect[] rects={new Rect(0,h*.72f,w,h*.28f),new Rect(0,0,w,h*.72f)};
            var slices=new SpriteRect[2];
            for(int i=0;i<2;i++)
            {
                var old=previous.FirstOrDefault(x=>x.name==names[i]);
                slices[i]=new SpriteRect {name=names[i],rect=rects[i],pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,
                    spriteID=old!=null?old.spriteID:GUID.Generate()};
            }
            provider.SetSpriteRects(slices);provider.Apply();importer.SaveAndReimport();
            var catalog=AssetDatabase.LoadAssetAtPath<BearShopCatalog>("Assets/BuBuYiEr/UI/BearShopCatalog.asset");
            foreach(var item in catalog.items)
            {
                switch(item.id)
                {
                    case "skin_milk":item.title="奶霜曲奇";item.description="曲奇糖霜与可可碎屑拖尾";break;
                    case "skin_jelly":item.title="果冻泡泡";item.description="清透果冻与薄荷气泡拖尾";break;
                    case "skin_sunset":item.title="焦糖流星";item.description="焦糖条纹与双层彗星尾迹";break;
                    case "skin_starry":item.title="星光棱晶";item.description="棱面星核与闪星拖尾";break;
                }
            }
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            BearMenuBuilder.Build();
            Debug.Log("CANDY VISUAL ART: bakery sprites imported at 1024, four distinct materials and live skin swatches baked.");
        }
    }
}
