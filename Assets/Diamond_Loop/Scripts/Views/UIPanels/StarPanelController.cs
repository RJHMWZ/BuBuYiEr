using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class StarPanelController : MonoBehaviour
    {
        [SerializeField] private Text timeText = null;
        [SerializeField] private RectTransform leftStar = null;
        [SerializeField] private RectTransform leftCover = null;
        [SerializeField] private RectTransform centerStar = null;
        [SerializeField] private RectTransform centerCover = null;
        [SerializeField] private RectTransform rightStar = null;
        [SerializeField] private RectTransform rightCover = null;


        /// <summary>
        /// Update the time left of the level.
        /// </summary>
        /// <param name="timeLeft"></param>
        public void UpdateTime(float timeLeft)
        {
            timeText.text = Utilities.FloatToTime(timeLeft);

            if (IngameManager.Instance.CurrentStar == 3)
            {
                leftStar.gameObject.SetActive(true);
                leftCover.gameObject.SetActive(true);
                rightStar.gameObject.SetActive(true);
                rightCover.gameObject.SetActive(true);
                centerStar.gameObject.SetActive(true);
                centerCover.gameObject.SetActive(true);
            }
            else if (IngameManager.Instance.CurrentStar == 2)
            {
                leftStar.gameObject.SetActive(true);
                leftCover.gameObject.SetActive(true);
                centerStar.gameObject.SetActive(true);
                centerCover.gameObject.SetActive(true);
                rightStar.gameObject.SetActive(false);
                rightCover.gameObject.SetActive(false);
            }
            else if (IngameManager.Instance.CurrentStar == 1)
            {
                leftStar.gameObject.SetActive(true);
                leftCover.gameObject.SetActive(true);
                centerStar.gameObject.SetActive(false);
                centerCover.gameObject.SetActive(false);
                rightStar.gameObject.SetActive(false);
                rightCover.gameObject.SetActive(false);
            }
            else
            {
                leftStar.gameObject.SetActive(false);
                leftCover.gameObject.SetActive(false);
                centerStar.gameObject.SetActive(false);
                centerCover.gameObject.SetActive(false);
                rightStar.gameObject.SetActive(false);
                rightCover.gameObject.SetActive(false);
            }
        }
    }
}
