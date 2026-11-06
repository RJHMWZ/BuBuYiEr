using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClawbearGames
{
    // Animate the face only: the touch rectangle never moves or changes size.
    public sealed class CandyBreakoutButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Button button;
        public BearMenuShape face, sheen;
        public Text caption, price;
        public bool primary, emphasized;
        private float animationTime;
        private bool pressed, available, wasMastered;
        private Color fill;
        private static readonly Color MutedInk = new Color32(137, 116, 97, 255);

        public void SetState(bool enabled, Color tint, string title, string amount, bool mastered = false)
        {
            button.interactable = enabled;
            caption.text = title;
            if (price != null) price.text = amount;
            if (available == enabled && fill == tint && face.color.a > 0 && wasMastered == mastered) return;
            available = enabled;
            wasMastered = mastered;
            fill = tint;
            Color top = enabled ? Color.Lerp(tint, Color.white, .18f)
                : mastered ? new Color32(218, 232, 216, 255) : new Color32(237, 227, 210, 255);
            face.color = top;
            face.bottomColor = enabled ? tint : top;
            face.borderColor = enabled ? BearGameTheme.Brown : new Color32(174, 153, 129, 255);
            caption.color = price != null ? enabled ? BearGameTheme.Brown : MutedInk : BearGameTheme.Brown;
            if (price != null) price.color = enabled ? BearGameTheme.Brown : MutedInk;
            face.SetVerticesDirty();
            if (!enabled) pressed = false;
        }
        public void OnPointerDown(PointerEventData e) { if (button.IsInteractable()) pressed = true; }
        public void OnPointerUp(PointerEventData e) => pressed = false;
        public void OnPointerExit(PointerEventData e) => pressed = false;
        private void OnDisable()
        {
            pressed = false;
            animationTime = 0;
            if (face != null) face.rectTransform.localScale = Vector3.one;
        }
        private void Update()
        {
            if (face == null || Time.timeScale <= 0 || (IngameManager.Instance != null && IngameManager.Instance.IsPaused)) return;
            animationTime += Time.deltaTime;
            float wave = available && (primary || emphasized) ? (Mathf.Sin(animationTime * 2.4f) + 1) * .5f : 0;
            float scale = pressed ? .965f : 1 + wave * (primary ? .008f : .004f);
            face.rectTransform.localScale = Vector3.Lerp(face.rectTransform.localScale, Vector3.one * scale,
                1 - Mathf.Exp(-Time.deltaTime * 24));
            if (sheen != null)
            {
                Color c = Color.white; c.a = available ? .13f + wave * (emphasized ? .30f : .2f) : .06f;
                sheen.color = sheen.bottomColor = c;
            }
        }
    }
}
