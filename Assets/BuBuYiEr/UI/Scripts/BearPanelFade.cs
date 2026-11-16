using UnityEngine;

namespace ClawbearGames
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BearPanelFade : MonoBehaviour
    {
        private CanvasGroup group;
        private float age;
        private bool playing;
        private void OnEnable()
        {
            group = GetComponent<CanvasGroup>();
            age = 0;
            playing = Application.isPlaying;
            group.alpha = playing ? 0 : 1;
        }
        private void Update()
        {
            if (!playing) return;
            age += Time.unscaledDeltaTime;
            group.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .16f));
            if (age >= .16f) Complete();
        }
        public void Complete()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            group.alpha = 1;
            playing = false;
        }
        private void OnDisable() { if (group != null) group.alpha = 1; playing = false; }
    }
}
