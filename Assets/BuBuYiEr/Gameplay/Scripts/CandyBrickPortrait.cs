using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CandyBrickPortrait : MaskableGraphic
    {
        public string skin;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect area = GetPixelAdjustedRect();
            float width = Mathf.Min(area.width * .9f, area.height * 2.4f);
            Vector2 size = new Vector2(width, width / 2.7f);
            CandyBrickArt.Draw(vh, new Rect(area.center - size * .5f, size), CandyBrickSkin.Resolve(skin));
        }
    }
}
