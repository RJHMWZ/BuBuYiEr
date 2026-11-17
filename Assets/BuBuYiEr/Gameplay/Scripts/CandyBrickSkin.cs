using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public enum CandyBrickMotif { Heart, Star, Cloud, Flower }

    [CreateAssetMenu(menuName = "BuBuYiEr/Brick Skin")]
    public sealed class CandyBrickSkin : ScriptableObject
    {
        public CandyBrickMotif motif;
        public Color top = new Color32(255, 238, 242, 255);
        public Color bottom = new Color32(250, 197, 213, 255);
        public Color rim = new Color32(222, 151, 174, 255);
        public Color baseColor = new Color32(238, 172, 193, 255);
        public Color accent = new Color32(230, 137, 167, 255);
        public Color number = new Color32(112, 66, 83, 255);

        private const string Folder = "CandyArt/BrickSkins/";
        private static readonly Dictionary<string, CandyBrickSkin> skins = new Dictionary<string, CandyBrickSkin>();
        private static CandyBrickSkin defaultSkin;

        public static CandyBrickSkin Equipped => Resolve(PlayerDataHandler.GetEquipment(BearShopEffect.BrickSkin));

        // A shop item's permanent ID is also its skin asset's resource name.
        // Missing or retired skins always fall back to the free default.
        public static CandyBrickSkin Resolve(string id)
        {
            if (defaultSkin == null)
            {
                defaultSkin = Resources.Load<CandyBrickSkin>(Folder + "Default");
                if (defaultSkin == null)
                {
                    defaultSkin = CreateInstance<CandyBrickSkin>();
                    defaultSkin.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            if (string.IsNullOrEmpty(id) || id == "Default") return defaultSkin;
            if (!skins.TryGetValue(id, out CandyBrickSkin skin))
            {
                skin = Resources.Load<CandyBrickSkin>(Folder + id);
                skins[id] = skin;
            }
            return skin != null ? skin : defaultSkin;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            skins.Clear();
            defaultSkin = null;
        }
    }
}
