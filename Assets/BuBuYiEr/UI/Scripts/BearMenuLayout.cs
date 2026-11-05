using UnityEngine;

namespace ClawbearGames
{
    [ExecuteAlways]
    public sealed class BearMenuLayout : MonoBehaviour
    {
        public Vector2 designSize = new Vector2(941, 1672);
        public bool flexibleMain;
        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            RectTransform rect = transform as RectTransform;
            RectTransform parent = transform.parent as RectTransform;
            if (rect == null || parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
            Rect safe = Application.isPlaying ? Screen.safeArea : new Rect(0,0,Screen.width,Screen.height);
            float safeWidth = Screen.width > 0 ? safe.width / Screen.width : 1;
            float safeHeight = Screen.height > 0 ? safe.height / Screen.height : 1;
            float width = parent.rect.width * safeWidth;
            float height = parent.rect.height * safeHeight;
            float scale = Mathf.Min(width / designSize.x, height / designSize.y);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            Vector2 size = new Vector2(designSize.x, flexibleMain ? Mathf.Max(designSize.y, height / scale) : designSize.y);
            Vector3 localScale = Vector3.one * scale;
            Vector2 position = Screen.width > 0 && Screen.height > 0 ? new Vector2(
                (safe.center.x / Screen.width - .5f) * parent.rect.width,
                (safe.center.y / Screen.height - .5f) * parent.rect.height) : Vector2.zero;
            if (rect.sizeDelta != size) rect.sizeDelta = size;
            if (rect.localScale != localScale) rect.localScale = localScale;
            if (rect.anchoredPosition != position) rect.anchoredPosition = position;
        }
    }
}
