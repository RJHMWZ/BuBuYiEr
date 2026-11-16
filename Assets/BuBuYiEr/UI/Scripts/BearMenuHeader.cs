using UnityEngine;

namespace ClawbearGames
{
    // One physical header shared by every menu page. Page aspect ratios cannot move or resize it.
    [ExecuteAlways]
    public sealed class BearMenuHeader : MonoBehaviour
    {
        public GameObject levelStatus, backStatus;
        private Rect lastParent, lastSafe;
        private Vector2 lastViewport;

        public void ShowPage(int page)
        {
            levelStatus.SetActive(page == 0);
            backStatus.SetActive(page != 0);
            Refresh(Application.isPlaying ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height),
                new Vector2(Screen.width, Screen.height));
        }

        private void LateUpdate() => Refresh(Application.isPlaying ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height),
            new Vector2(Screen.width, Screen.height));

        public void Refresh(Rect safe, Vector2 viewport)
        {
            var rect = (RectTransform)transform;
            var parent = transform.parent as RectTransform;
            if (parent == null || viewport.x <= 0 || viewport.y <= 0 || safe.width <= 0 || safe.height <= 0) return;
            if (lastParent == parent.rect && lastSafe == safe && lastViewport == viewport) return;
            float width = parent.rect.width * safe.width / viewport.x;
            float height = parent.rect.height * safe.height / viewport.y;
            float scale = Mathf.Min(width / 1024, height / 1536);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(1024, 140);
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = new Vector2((safe.center.x / viewport.x - .5f) * parent.rect.width,
                (safe.yMax / viewport.y - .5f) * parent.rect.height);
            lastParent = parent.rect; lastSafe = safe; lastViewport = viewport;
        }
    }
}
