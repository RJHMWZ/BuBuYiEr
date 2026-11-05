using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public sealed class BearJarView : MonoBehaviour
    {
        private PotController pot;
        private RectTransform billboard;
        private BearIllustration jar;
        private BearIllustration badge;
        private SpriteRenderer[] legacyStates;
        private Text countdown;
        private int lastCandyCount = -1;
        private BearDrawing lastState;
        private Camera sceneCamera;
        private CanvasGroup opacity;
        public Vector3 CollectionPosition => billboard.TransformPoint(new Vector3(0, 67, 0));

        public void SetOpacity(float value)
        {
            if (opacity != null) opacity.alpha = Mathf.Clamp01(value);
        }

        public void Initialize(PotController owner, BearMenuArt art, Text percent)
        {
            pot = owner;
            sceneCamera = IngameManager.Instance.CameraParent.MainCamera;
            foreach (MeshRenderer renderer in owner.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;
            legacyStates = owner.GetComponentsInChildren<SpriteRenderer>(true);
            billboard = BearMenuFactory.Node(owner.transform, "IllustratedCandyJar", 0, 0, 128, 155);
            billboard.pivot = new Vector2(.5f, .5f);
            billboard.localPosition = Vector3.up * .65f;
            billboard.localScale = Vector3.one * .008f;
            Canvas canvas = billboard.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            opacity = billboard.gameObject.AddComponent<CanvasGroup>();
            opacity.blocksRaycasts = opacity.interactable = false;
            jar = BearIllustration.Create(billboard, "GlassJar", BearDrawing.Jar, 0, 5, 128, 128);
            badge = BearIllustration.Create(billboard, "StateBadge", BearDrawing.Lock, 83, 0, 46, 46);
            BearGameTheme.Place(percent.rectTransform, billboard, 20, 129, 88, 29);
            BearGameTheme.StyleText(percent, art, 23, true);
            countdown = owner.DecayState != null ? owner.DecayState.GetComponentInChildren<Text>(true) : null;
            if (countdown != null)
            {
                BearGameTheme.Place(countdown.rectTransform, billboard, 84, 41, 44, 30);
                BearGameTheme.StyleText(countdown, art, 22, true);
            }
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (pot == null || jar == null) return;
            if (sceneCamera == null) sceneCamera = Camera.main;
            if (sceneCamera != null && billboard.rotation != sceneCamera.transform.rotation) billboard.rotation = sceneCamera.transform.rotation;
            foreach (SpriteRenderer sprite in legacyStates) if (sprite != null && sprite.enabled) sprite.enabled = false;
            IngameManager game = IngameManager.Instance;
            bool highlighted = game != null && game.IsRoundStarted && game.IsLearningRound && !game.HasDeployedPot && game.TutorialPot == pot;
            if (jar.highlighted != highlighted) { jar.highlighted = highlighted; jar.SetVerticesDirty(); }
            float fill = pot.DisplayedFill;
            int candyCount = Mathf.Clamp(Mathf.CeilToInt(fill * 9), 0, 9);
            if (lastCandyCount != candyCount || jar.candyColor != (Color)pot.TargetColor)
            {
                // The drawing has nine candy cells. Rebuild only when one becomes visible.
                lastCandyCount = candyCount;
                jar.fill = fill;
                jar.candyColor = pot.TargetColor;
                jar.SetVerticesDirty();
            }
            bool locked = pot.LockedState != null && pot.LockedState.gameObject.activeSelf;
            bool frozen = pot.FrozenState != null && pot.FrozenState.gameObject.activeSelf;
            bool warm = pot.DecayState != null && pot.DecayState.gameObject.activeSelf;
            if (badge.gameObject.activeSelf != (locked || frozen || warm)) badge.gameObject.SetActive(locked || frozen || warm);
            BearDrawing state = locked ? BearDrawing.Lock : frozen ? BearDrawing.Cooler : BearDrawing.Warm;
            if (lastState != state)
            {
                lastState = state;
                badge.drawing = state;
                badge.SetVerticesDirty();
            }
            if (countdown != null && countdown.gameObject.activeSelf != warm) countdown.gameObject.SetActive(warm);
        }
    }
}
