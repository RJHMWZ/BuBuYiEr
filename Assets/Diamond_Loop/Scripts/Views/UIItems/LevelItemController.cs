using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class LevelItemController : MonoBehaviour
    {
        [SerializeField] private Text levelText = null;
        [SerializeField] private Transform activeTrans = null;
        [SerializeField] private Transform disableTrans = null;
        [SerializeField] private Transform lineTrans = null;
        [SerializeField] private Image leftStar = null;
        [SerializeField] private Image centerStar = null;
        [SerializeField] private Image rightStar = null;
        [SerializeField] private SphereCollider sphereCollider = null;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color disableColor = Color.gray;

        public int Level { private set; get; }


        /// <summary>
        /// Init this level item.
        /// </summary>
        /// <param name="level"></param>
        public void OnInit(int level)
        {
            Level = level;
            levelText.text = level.ToString();
            if (level <= PlayerDataHandler.GetCurrentLevel())
            {
                activeTrans.gameObject.SetActive(true);
                disableTrans.gameObject.SetActive(false);
                sphereCollider.enabled = true;
            }
            else
            {
                sphereCollider.enabled = false;
                activeTrans.gameObject.SetActive(false);
                disableTrans.gameObject.SetActive(true);
            }

            int star = PlayerDataHandler.GetStar(level);
            if (star == 1)
            {
                leftStar.color = activeColor;
                centerStar.color = disableColor;
                rightStar.color = disableColor;
            }
            else if (star == 2)
            {
                leftStar.color = activeColor;
                centerStar.color = activeColor;
                rightStar.color = disableColor;
            }
            else if (star == 3)
            {
                leftStar.color = activeColor;
                centerStar.color = activeColor;
                rightStar.color = activeColor;
            }
            else
            {
                leftStar.color = disableColor;
                centerStar.color = disableColor;
                rightStar.color = disableColor;
            }
        }



        /// <summary>
        /// Cache the next level item.
        /// </summary>
        /// <param name="next"></param>
        public void CacheNextLevel(LevelItemController next)
        {
            if (next != null)
            {
                Vector3 dir = next.transform.position - transform.position;
                lineTrans.forward = dir.normalized;
                lineTrans.localScale = new Vector3(1f, 1f, Vector3.Distance(transform.position, next.transform.position));
            }
        }
    }
}
