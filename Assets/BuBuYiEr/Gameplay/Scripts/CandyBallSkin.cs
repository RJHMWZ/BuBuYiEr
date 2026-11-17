using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public enum CandyBallFinish { Plain, Cookie, Jelly, Comet, Crystal }

    [CreateAssetMenu(menuName = "BuBuYiEr/Ball Theme")]
    public sealed class CandyBallSkin : ScriptableObject
    {
        public CandyBallFinish finish;
        [Tooltip("按奶糖、草莓、星星、薄荷、可可、彩虹的顺序，为六种球分别设置主题颜色。")]
        public Color[] colors = {
            new Color32(247,211,124,255), new Color32(239,166,180,255),
            new Color32(166,192,234,255), new Color32(164,212,183,255),
            new Color32(201,163,140,255), new Color32(203,180,226,255) };

        private static readonly Dictionary<string, CandyBallSkin> skins = new Dictionary<string, CandyBallSkin>();

        public Color ColorFor(int kind)
        {
            kind = Mathf.Clamp(kind, 0, 5);
            Color color = colors != null && kind < colors.Length ? colors[kind] : CandyBreakoutGraphic.CandyColors[kind];
            color.a = 1;
            return color;
        }

        public static CandyBallSkin Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!skins.TryGetValue(id, out CandyBallSkin skin))
            {
                skin = Resources.Load<CandyBallSkin>("CandyArt/BallSkins/" + id);
                skins[id] = skin;
            }
            return skin;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => skins.Clear();
    }
}
