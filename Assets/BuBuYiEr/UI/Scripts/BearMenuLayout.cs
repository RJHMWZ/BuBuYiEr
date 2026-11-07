using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    [ExecuteAlways]
    public sealed class BearMenuLayout : MonoBehaviour
    {
        public Vector2 designSize = new Vector2(941, 1672);
        public bool flexibleMain;
        private struct Placement
        {
            public Vector2 Position, Size;
        }
        private readonly Dictionary<RectTransform, Placement> original = new Dictionary<RectTransform, Placement>();
        private Rect lastParentRect, lastSafe;
        private Vector2 lastViewport, lastDesign;
        private Transform lastParent;
        private int lastChildCount = -1;
        private bool lastFlexible, layoutDirty = true;
        private readonly List<RectTransform> removedChildren = new List<RectTransform>();
        public void SetContentPosition(RectTransform child, Vector2 position)
        {
            if (child == null) return;
            if (original.TryGetValue(child, out Placement previous) && previous.Position == position) return;
            original[child] = new Placement { Position = position, Size = child.sizeDelta };
            child.anchoredPosition = position;
            layoutDirty = true;
        }
        private void OnEnable() { layoutDirty = true; Refresh(); }
        private void OnTransformChildrenChanged() => layoutDirty = true;
        private void OnRectTransformDimensionsChange() => layoutDirty = true;
        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            Refresh(Application.isPlaying ? Screen.safeArea : new Rect(0,0,Screen.width,Screen.height),
                new Vector2(Screen.width,Screen.height));
        }

        public void Refresh(Rect safe, Vector2 viewport)
        {
            RectTransform rect = transform as RectTransform;
            RectTransform parent = transform.parent as RectTransform;
            if (rect == null || parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
            bool changed = layoutDirty || lastParent != parent || lastParentRect != parent.rect || lastSafe != safe ||
                lastViewport != viewport || lastDesign != designSize || lastFlexible != flexibleMain || lastChildCount != transform.childCount;
            if (Application.isPlaying && !changed) return;
            float safeWidth = viewport.x > 0 ? safe.width / viewport.x : 1;
            float safeHeight = viewport.y > 0 ? safe.height / viewport.y : 1;
            float width = parent.rect.width * safeWidth;
            float height = parent.rect.height * safeHeight;
            float scale = Mathf.Min(width / designSize.x, height / designSize.y);
            if (scale <= 0) return;
            bool menuPage = transform.parent.name == "BearMenuUI";
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            Vector2 size = new Vector2(designSize.x, flexibleMain || menuPage ? Mathf.Max(designSize.y, height / scale) : designSize.y);
            Vector3 localScale = Vector3.one * scale;
            Vector2 position = viewport.x > 0 && viewport.y > 0 ? new Vector2(
                (safe.center.x / viewport.x - .5f) * parent.rect.width,
                (safe.center.y / viewport.y - .5f) * parent.rect.height) : Vector2.zero;
            if (rect.sizeDelta != size) rect.sizeDelta = size;
            if (rect.localScale != localScale) rect.localScale = localScale;
            if (rect.anchoredPosition != position) rect.anchoredPosition = position;
            if (changed)
            {
                removedChildren.Clear();
                foreach (var entry in original)
                    if (entry.Key == null || entry.Key.parent != transform) removedChildren.Add(entry.Key);
                foreach (var child in removedChildren) original.Remove(child);
                removedChildren.Clear();
            }
            if (menuPage) LayoutMenu(rect);
            lastParent = parent; lastParentRect = parent.rect; lastSafe = safe; lastViewport = viewport;
            lastDesign = designSize; lastFlexible = flexibleMain; lastChildCount = transform.childCount;
            layoutDirty = false;
        }

        private void LayoutMenu(RectTransform frame)
        {
            float extra = Mathf.Max(0, frame.rect.height - designSize.y);
            if (name == "Home")
            {
                RectTransform room = transform.Find("BearRoom") as RectTransform;
                if (room == null) return;
                float available = frame.rect.height - 540 - 388;
                float roomScale = Mathf.Min(1, (available - 24) / 640);
                roomScale = Mathf.Max(.6f, roomScale);
                room.anchorMin = room.anchorMax = room.pivot = new Vector2(.5f,.5f);
                Vector2 centre = new Vector2(0, frame.rect.height * .5f - (388 + available * .5f));
                if (room.anchoredPosition != centre) room.anchoredPosition = centre;
                if (room.localScale != Vector3.one * roomScale) room.localScale = Vector3.one * roomScale;
                return;
            }
            if (name != "Settings" && name != "DailyReward" && name != "Shop" && name != "Collection") return;
            Transform back = transform.Find("BackArrow") ?? transform.Find("Header/BackArrow");
            var backPaper = (transform.Find("BackPaper") ?? transform.Find("Header/BackPanel")) as RectTransform;
            if (back is RectTransform arrow && backPaper != null)
            {
                arrow.pivot = new Vector2(.5f,.5f);
                arrow.position = backPaper.TransformPoint(new Vector3(backPaper.rect.xMin + 40, backPaper.rect.center.y, 0));
            }
            foreach (RectTransform child in transform)
            {
                if (child == back) continue;
                if (!original.TryGetValue(child, out Placement basis))
                {
                    basis = new Placement { Position = child.anchoredPosition, Size = child.sizeDelta };
                    original.Add(child, basis);
                }
                Vector2 p = basis.Position, size = basis.Size;
                float y = -p.y;
                if (name == "Collection")
                {
                    if (child.name == "CollectionLibrary" || child.name.EndsWith("Shelf")) size.y += extra;
                    if (child.name == "CollectionDetails" || child.name == "BottomBow" || child.name == "FooterGarden") p.y -= extra;
                    if (child.name.EndsWith("Shelf"))
                        foreach (RectTransform item in child)
                            if (item.GetComponent<UnityEngine.UI.ScrollRect>() != null)
                                item.sizeDelta = new Vector2(item.sizeDelta.x, 653 + extra);
                }
                else
                {
                    p.y -= extra * Mathf.InverseLerp(580, 1514, y);
                    bool panel = child.name.EndsWith("Panel");
                    if (panel) size.y += extra * (Mathf.InverseLerp(580, 1514, y + basis.Size.y) - Mathf.InverseLerp(580, 1514, y));
                    if (name == "DailyReward" && child.name.StartsWith("Day") && int.TryParse(child.name.Substring(3), out int day))
                    {
                        float growth = extra / 3;
                        bool final = day == 7;
                        p.y = final ? -1283 - growth * 2 : -623 - (day - 1) / 3 * (337 + growth);
                        size.y = basis.Size.y + growth;
                        SetHeight(child.Find("CardArtwork"), size.y);
                        SetHeight(child.Find("SelectionOutline"), size.y);
                        SetHeight(child.Find("AvailableArtwork"), 371 + growth);
                        SetY(child.Find("Claim"), final ? 110 + growth * .5f : 231 + growth);
                        SetY(child.Find("Amount"), final ? 120 + growth * .5f : 184 + growth * .65f);
                        SetY(child.Find("RewardIcon"), final ? 23 + growth * .5f : 80 + growth * .35f);
                    }
                }
                if (child.anchoredPosition != p) child.anchoredPosition = p;
                if (child.sizeDelta != size) child.sizeDelta = size;
            }
        }
        private static void SetHeight(Transform target, float height)
        {
            if (target is RectTransform rect && !Mathf.Approximately(rect.sizeDelta.y, height))
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }
        private static void SetY(Transform target, float y)
        {
            if (target is RectTransform rect && !Mathf.Approximately(rect.anchoredPosition.y, -y))
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -y);
        }
    }
}
