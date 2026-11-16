using UnityEngine;

namespace ClawbearGames
{
    public sealed class BearHomeAmbience : MonoBehaviour
    {
        public RectTransform whiteBear, brownBear, heart;
        private Vector2 whiteRest, brownRest, heartRest;
        private float age;
        private bool bound;
        public void Bind(RectTransform white, RectTransform brown, RectTransform love)
        {
            whiteBear = white; brownBear = brown; heart = love;
            Centre(whiteBear); Centre(brownBear); Centre(heart);
            whiteRest = whiteBear.anchoredPosition;
            brownRest = brownBear.anchoredPosition;
            heartRest = heart.anchoredPosition;
            bound = true;
        }
        private static void Centre(RectTransform rect)
        {
            Vector2 pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition += Vector2.Scale(pivot - rect.pivot, rect.rect.size);
            rect.pivot = pivot;
        }
        private void OnEnable()
        {
            age = 0;
            // Baked prefabs retain the centred rectangles, but not private runtime state.
            if (!bound && whiteBear != null && brownBear != null && heart != null) Bind(whiteBear, brownBear, heart);
        }
        private void Update()
        {
            if (!bound || whiteBear == null || brownBear == null || heart == null) return;
            age += Time.unscaledDeltaTime;
            float breath = Mathf.Sin(age * 1.8f);
            whiteBear.anchoredPosition = whiteRest + Vector2.up * (breath * 2);
            brownBear.anchoredPosition = brownRest - Vector2.up * (breath * 2);
            heart.anchoredPosition = heartRest + Vector2.up * (Mathf.Sin(age * 1.6f) * 2);
            heart.localScale = Vector3.one * (1 + Mathf.Sin(age * 2.1f) * .025f);
        }
        private void OnDisable()
        {
            if (!bound || whiteBear == null || brownBear == null || heart == null) return;
            whiteBear.anchoredPosition = whiteRest;
            brownBear.anchoredPosition = brownRest;
            heart.anchoredPosition = heartRest;
            heart.localScale = Vector3.one;
        }
    }
}
