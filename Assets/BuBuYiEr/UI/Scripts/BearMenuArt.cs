using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    [CreateAssetMenu(menuName = "BuBuYiEr/Menu Art")]
    public sealed class BearMenuArt : ScriptableObject
    {
        public Sprite[] sprites;
        public Font chineseFont;
        public Font numberFont;
        [Tooltip("对应新人、金币、爱心、装饰券、图鉴券。留空时不会发起购买。")]
        public string[] productIds = new string[5];
        public string privacyUrl;
        public string supportUrl;
        private Dictionary<string, Sprite> lookup;

        public void ResetLookup() => lookup = null;
        private void OnValidate() => ResetLookup();

        public Sprite Get(string name)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>();
                foreach (Sprite sprite in sprites) if (sprite != null) lookup[sprite.name] = sprite;
            }
            return lookup.TryGetValue(name, out Sprite result) ? result : null;
        }
    }
}
