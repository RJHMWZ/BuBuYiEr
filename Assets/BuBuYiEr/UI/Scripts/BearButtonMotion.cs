using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClawbearGames
{
    // Move artwork only; pointer hit boxes stay fixed throughout the press.
    public sealed class BearButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform artwork;
        public bool primary;
        private Button button;
        private bool pressed;
        private float age;

        private void OnEnable() { button = GetComponent<Button>(); pressed = false; age = 0; }
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && button != null && button.IsInteractable()) pressed = true;
        }
        public void OnPointerUp(PointerEventData e) => pressed = false;
        public void OnPointerExit(PointerEventData e) => pressed = false;
        private void OnDisable()
        {
            pressed = false;
            if (artwork != null) artwork.localScale = Vector3.one;
        }
        private void Update()
        {
            if (artwork == null) return;
            age += Time.unscaledDeltaTime;
            bool available = button != null && button.IsInteractable();
            float scale = pressed && available ? .965f : primary && available ? 1 + Mathf.Sin(age * 2) * .004f : 1;
            artwork.localScale = Vector3.Lerp(artwork.localScale, Vector3.one * scale, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 28));
        }
    }
}
