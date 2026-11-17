using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CandyBallThemePreview : MaskableGraphic
    {
        public string skin;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float cellWidth = rect.width / 3, cellHeight = rect.height / 2;
            float radius = Mathf.Min(cellWidth, cellHeight) * .41f;
            for (int kind = 0; kind < 6; kind++)
            {
                Vector2 center = new Vector2(rect.x + cellWidth * (kind % 3 + .5f),
                    rect.yMax - cellHeight * (kind / 3 + .5f));
                CandyBallArt.Draw(vh, center, radius, kind, skin);
            }
        }
    }
}
