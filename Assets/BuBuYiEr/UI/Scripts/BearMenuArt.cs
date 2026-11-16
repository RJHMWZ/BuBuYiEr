using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    [CreateAssetMenu(menuName = "BuBuYiEr/Menu Art")]
    public sealed class BearMenuArt : ScriptableObject
    {
        public Sprite[] sprites;
        public Sprite[] settingsSprites;
        public Sprite[] dailySprites;
        public Sprite[] shopSprites;
        public Sprite[] collectionSprites;
        public Font chineseFont;
        public Font numberFont;
        public BearShopCatalog shopCatalog;
        public string privacyUrl;
        public string supportUrl;
        private Dictionary<string, Sprite> lookup;

        public void ResetLookup() => lookup = null;
        private void OnValidate() => ResetLookup();

        public Sprite Get(string name)
        {
            if (name == "Daily/IconCoin") name = "IconCoin";
            if (name == "Daily/IconLove") name = "IconLove";
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>();
                foreach (Sprite sprite in sprites) if (sprite != null) lookup[sprite.name] = sprite;
                if(settingsSprites!=null)
                    foreach(Sprite sprite in settingsSprites)
                        if(sprite!=null) lookup["Setting/"+sprite.name]=sprite;
                if(dailySprites!=null)
                    foreach(Sprite sprite in dailySprites)
                        if(sprite!=null) lookup["Daily/"+sprite.name]=sprite;
                if(shopSprites!=null)
                    foreach(Sprite sprite in shopSprites)
                        if(sprite!=null) lookup["Shop/"+sprite.name]=sprite;
                if(collectionSprites!=null)
                    foreach(Sprite sprite in collectionSprites)
                        if(sprite!=null) lookup["Collect/"+sprite.name]=sprite;
            }
            return lookup.TryGetValue(name, out Sprite result) ? result : null;
        }
    }
}
