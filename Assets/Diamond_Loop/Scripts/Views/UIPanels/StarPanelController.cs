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
        private int displayedSeconds = -1, displayedStars = -1;

        public void ApplyBearTheme(BearMenuArt art)
        {
            BearGameTheme.Place(timeText.rectTransform, transform, 0, 3, 190, 66);
            BearGameTheme.StyleText(timeText, art, 40, true);
            RectTransform[] stars = { leftStar, centerStar, rightStar };
            RectTransform[] covers = { leftCover, centerCover, rightCover };
            for (int i = 0; i < stars.Length; i++)
            {
                BearGameTheme.Picture(art, transform, "EmptyStar" + i, "IconStar", 13 + i * 57, 78, 50, 48).color =
                    new Color(.67f, .52f, .38f, .32f);
                BearGameTheme.Place(stars[i], transform, 13 + i * 57, 78, 50, 48);
                Image image = stars[i].GetComponent<Image>();
                image.sprite = art.Get("IconStar");
                image.color = Color.white;
                image.raycastTarget = false;
                image.type = Image.Type.Simple;
                foreach (Graphic graphic in covers[i].GetComponentsInChildren<Graphic>(true))
                {
                    graphic.color = Color.clear;
                    graphic.raycastTarget = false;
                }
                stars[i].SetAsLastSibling();
            }
            // The legacy time and star containers only supplied template backgrounds.
            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != art.Get("IconStar")) image.enabled = false;
            }
            timeText.transform.SetAsLastSibling();
        }


        /// <summary>
        /// Update the time left of the level.
        /// </summary>
        /// <param name="timeLeft"></param>
        public void UpdateTime(float timeLeft)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0, timeLeft));
            if (seconds != displayedSeconds)
            {
                displayedSeconds = seconds;
                timeText.text = Utilities.FloatToTime(seconds);
            }
            int stars = IngameManager.Instance.CurrentStar;
            if (stars == displayedStars) return;
            displayedStars = stars;

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
