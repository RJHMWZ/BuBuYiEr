using UnityEngine;
using System.Collections.Generic;

namespace ClawbearGames
{
    public class MapItemController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer mapSprite = null;
        [SerializeField] private LevelItemController levelItemPrefab = null;

        public float Length => mapSprite.bounds.size.z;
        private List<LevelItemController> levelItems = new List<LevelItemController>();



        /// <summary>
        /// Init this map item.
        /// </summary>
        /// <param name="levels"></param>
        public void OnInit(int[] levels)
        {
            float xAmplitude = 2f;          // horizontal wiggle range
            float waveFrequency = 1f;       // number of wave cycles per map item

            for (int i = 0; i < levels.Length; i++)
            {
                int level = levels[i];
                float t = (levels.Length > 1) ? i / (float)(levels.Length - 1) : 0f;

                //Place level between -40% and +40% of the map tile to avoid edges
                float temp = (levels[0] == 1) ? 0.2f : 0.4f;
                float z = Mathf.Lerp(-Length * temp, Length * 0.4f, t);
                float x = Mathf.Sin(t * Mathf.PI * waveFrequency) * xAmplitude;

                //Create the item
                Vector3 itemPos = transform.TransformPoint(new Vector3(x, 0f, z));
                LevelItemController levelItem = Instantiate(levelItemPrefab, itemPos, Quaternion.identity, transform);
                levelItem.OnInit(level);
                levelItems.Add(levelItem);
            }
        }



        /// <summary>
        /// Find the level item with level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public LevelItemController FindLevelItem(int level)
        {
            foreach (LevelItemController levelItem in levelItems)
            {
                if(levelItem.Level == level) { return levelItem; }
            }
            return null;
        }
    }
}
