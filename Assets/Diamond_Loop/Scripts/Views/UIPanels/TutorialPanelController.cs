using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class TutorialPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform handTrans = null;
        [SerializeField] private Image circleImage = null;
        [SerializeField] private RectTransform circleTrans = null;
        [SerializeField] private Text instructionText = null;
        [SerializeField] private string firstInstruction = string.Empty;
        [SerializeField] private string secondInstruction = string.Empty;
        [SerializeField] private Vector2Int firstPos = new Vector2Int(0, 0);
        [SerializeField] private Vector2Int secondPos = new Vector2Int(0, 0);

        private bool isShownFirst = false;

        /// <summary>
        /// Coroutine fade circle image.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRFadeCircle()
        {
            float t = 0;
            float scaleTime = 0.5f;

            Color startColor = circleImage.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.one * 5f;

            while (gameObject.activeSelf)
            {
                t = 0;
                while (t < scaleTime)
                {
                    t += Time.deltaTime;
                    float factor = t / scaleTime;
                    circleImage.color = Color.Lerp(startColor, endColor, factor);
                    circleTrans.localScale = Vector3.Lerp(startScale, endScale, factor);
                    yield return null;
                }

                circleImage.color = startColor;
                circleTrans.localScale = startScale;
            }
        }


        /// <summary>
        /// Show the first tutorial instruction.
        /// </summary>
        public void OnShow()
        {
            handTrans.anchoredPosition = firstPos;
            instructionText.text = firstInstruction;
            StartCoroutine(CRFadeCircle());
        }


        public void OnClick()
        {
            if (!isShownFirst)
            {
                isShownFirst = true;
                handTrans.anchoredPosition = secondPos;
                instructionText.text = secondInstruction;
            }
            else
            {
                IngameManager.Instance.OnTutorialClosed();
                PlayerDataHandler.UpdateWatchedTutorial();
                gameObject.SetActive(false);
            }
        }
    }
}
