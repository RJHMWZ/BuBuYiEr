using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIAnimationPresets.Samples
{
    [ExecuteAlways]
    public sealed class UIAnimationDemo : MonoBehaviour
    {
        public TMP_FontAsset demoFont;
        [SerializeField] private UIAnimator[] animators;
        [SerializeField] private GameObject[] pages;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Text status;
        [SerializeField] private EventSystem input;
        [SerializeField] private UIAnimator sequenceAnimator;
        private TMP_FontAsset generatedFont;
        private int currentCategory, completedCount;
        private static readonly Color Background = new Color32(20, 25, 39, 255);
        private static readonly Color[] Accents = {
            new Color32(96, 204, 178,255), new Color32(246,158,151,255), new Color32(255,199,102,255),
            new Color32(125,178,255,255), new Color32(190,151,244,255), new Color32(255,214,106,255)
        };

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                if (pages == null || pages.Length == 0) Build();
                EnsureInput();
            }
#if UNITY_EDITOR
            else UnityEditor.EditorApplication.delayCall += BuildIfMissing;
#endif
        }
#if UNITY_EDITOR
        private void BuildIfMissing()
        {
            if (this != null && !Application.isPlaying && transform.Find("DemoCanvas") == null && gameObject.scene.IsValid()) Build();
        }
#endif
        private void Start() { if (Application.isPlaying) EnsureInput(); }
        private void Update()
        {
            if (Application.isPlaying && input != null && !input.gameObject.activeSelf && EventSystem.current == null) input.gameObject.SetActive(true);
        }
        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= BuildIfMissing;
#endif
            if (generatedFont == null) return;
            if (generatedFont.material != null) Destroy(generatedFont.material);
            foreach (Texture2D texture in generatedFont.atlasTextures) if (texture != null) Destroy(texture);
            Destroy(generatedFont);
        }

        public void Build()
        {
            if (Application.isPlaying && demoFont == null)
            {
                demoFont = Resources.Load<TMP_FontAsset>("UIAnimationDemoFont");
                if (demoFont == null)
                {
                    generatedFont = TMP_FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                    demoFont = generatedFont;
                }
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) { child.SetActive(false); Destroy(child); }
                else DestroyImmediate(child);
            }
            RectTransform canvas = Node(transform, "DemoCanvas", 0, 0, 1440, 960);
            Canvas rootCanvas = canvas.gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 250;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 960);
            scaler.matchWidthOrHeight = 0;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            Image backdrop = canvas.gameObject.AddComponent<Image>();
            backdrop.color = Background;
            backdrop.raycastTarget = true;
            RectTransform safe = Node(canvas, "Gallery", 0, 0, 1440, 960);
            Stretch(safe);
            Label(safe, "Title", "UI ANIMATION PRESETS", 38, 22, 980, 50, 35);
            Label(safe, "Subtitle", "Select any animated UI in the Hierarchy to configure or preview it in the Inspector.", 41, 78, 1160, 34, 19);
            MakeButton(safe, "PlayCategory", "PLAY ALL", 1142, 25, 121, 49, PlayCategory);
            MakeButton(safe, "StopAll", "RESET ALL", 1276, 25, 123, 49, ResetAll);
            string[] categoryNames = { "ENTRANCE", "EXIT", "CLICK", "LOOP", "EMPHASIS", "REWARD" };
            for (int i = 0; i < 6; i++)
            {
                Button tab = MakeButton(safe, "Category" + i, categoryNames[i], 40 + i * 228, 133, 214, 51, null);
                BindInt(tab, ShowCategory, i);
                tab.GetComponent<Image>().color = Color.Lerp(Background, Accents[i], .32f);
            }
            RectTransform viewport = Node(safe, "Viewport", 40, 207, 1360, 600);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(40, 158); viewport.offsetMax = new Vector2(-40, -207);
            viewport.gameObject.AddComponent<Image>().color = new Color(0,0,0,.04f);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Node(viewport, "Content", 0, 0, 1360, 750);
            content.anchorMin = new Vector2(0,1); content.anchorMax = new Vector2(1,1); content.sizeDelta = new Vector2(0,750);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 80;
            pages = new GameObject[6];
            var created = new System.Collections.Generic.List<UIAnimator>();
            for (int category = 0; category < 6; category++)
            {
                RectTransform page = Node(content, categoryNames[category], 0, 0, 1360, 750);
                pages[category] = page.gameObject;
                int card = 0;
                foreach (UIAnimationPreset preset in Enum.GetValues(typeof(UIAnimationPreset)))
                {
                    if ((int)UIPresetLibrary.Category(preset) != category) continue;
                    created.Add(BuildCard(page, preset, category, card++));
                }
            }
            animators = created.ToArray();
            RectTransform footer = Node(safe, "SequenceDemo", 40, 0, 1360, 126);
            footer.anchorMin = footer.anchorMax = footer.pivot = Vector2.zero;
            footer.anchoredPosition = new Vector2(40, 17);
            footer.gameObject.AddComponent<Image>().color = new Color32(35,43,61,255);
            Label(footer, "SequenceTitle", "SEQUENCE + PARALLEL", 20, 10, 450, 30, 21);
            RectTransform item = Node(footer, "SequenceTarget", 24, 56, 165, 49);
            item.gameObject.AddComponent<Image>().color = Accents[5];
            Label(item, "Caption", "COMBO", 0, 0, 165, 49, 20).color = Background;
            UIAnimator combo = item.gameObject.AddComponent<UIAnimator>();
            sequenceAnimator = combo;
            combo.source = UIAnimationSource.Sequence;
            combo.steps.Add(new UIAnimationStep { name="Pop In", animation=UIPresetLibrary.Create(UIAnimationPreset.PopIn) });
            combo.steps.Add(new UIAnimationStep { name="Parallel Glow", timing=UIAnimationStepTiming.Join, animation=UIPresetLibrary.Create(UIAnimationPreset.Glow) });
            combo.steps.Add(new UIAnimationStep { name="Punch", animation=UIPresetLibrary.Create(UIAnimationPreset.Punch), offset=.12f });
            combo.steps.Add(new UIAnimationStep { name="Exit", animation=UIPresetLibrary.Create(UIAnimationPreset.SlideOutRight), offset=.15f });
            MakeButton(footer, "PlaySequence", "PLAY COMBO", 253, 57, 168, 46, combo.Play);
            MakeButton(footer, "Interrupt", "INTERRUPT", 437, 57, 158, 46, combo.Stop);
            MakeButton(footer, "Restore", "RESTORE", 611, 57, 138, 46, combo.ResetAnimation);
            status = Label(footer, "Events", "Events: ready", 782, 19, 549, 90, 20);
            BindEvent(combo.onStarted, AnimationStarted); BindEvent(combo.onCompleted, AnimationCompleted);
            BindEvent(combo.onCanceled, AnimationCanceled); BindEvent(combo.onLoop, AnimationLooped);
            EnsureInput();
            ShowCategory(0);
        }

        private UIAnimator BuildCard(Transform parent, UIAnimationPreset preset, int category, int index)
        {
            RectTransform card = Node(parent, preset.ToString(), 6 + index % 3 * 453, 8 + index / 3 * 229, 436, 213);
            card.gameObject.AddComponent<Image>().color = new Color32(34,43,60,255);
            Label(card, "PresetName", preset.ToString(), 17, 12, 402, 31, 23);
            RectTransform visual = Node(card, "AnimatedVisual", 134, 64, 168, 65);
            visual.gameObject.AddComponent<Image>().color = Accents[category];
            bool tmp = index % 2 == 1 && demoFont != null;
            RectTransform target = visual;
            if (tmp)
            {
                RectTransform text = Node(visual, "TextMeshProTarget", 0, 0, 168, 65);
                TextMeshProUGUI label = text.gameObject.AddComponent<TextMeshProUGUI>();
                label.font = demoFont; label.text = "TMP TEXT"; label.fontSize = 24;
                label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false;
                target = text;
            }
            else Label(visual, "Label", "UGUI", 0, 0, 168, 65, 24).color = Background;
            UIAnimator animator = target.gameObject.AddComponent<UIAnimator>();
            animator.animation = UIPresetLibrary.Create(preset);
            Label(card, "Backend", tmp ? "TextMeshPro Graphic" : "UGUI + CanvasGroup", 17, 135, 400, 24, 15);
            MakeButton(card, "Play", "PLAY", 17, 169, 124, 32, animator.Play);
            MakeButton(card, "Pause", "PAUSE / RESUME", 154, 169, 130, 32, animator.TogglePause);
            MakeButton(card, "Reset", "RESET", 297, 169, 120, 32, animator.ResetAnimation);
            BindEvent(animator.onStarted, AnimationStarted); BindEvent(animator.onCompleted, AnimationCompleted);
            BindEvent(animator.onCanceled, AnimationCanceled); BindEvent(animator.onLoop, AnimationLooped);
            return animator;
        }

        public void ShowCategory(int index)
        {
            currentCategory = Mathf.Clamp(index, 0,5);
            if (pages == null) return;
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == currentCategory);
            int count = 0;
            foreach (UIAnimationPreset preset in Enum.GetValues(typeof(UIAnimationPreset))) if ((int)UIPresetLibrary.Category(preset) == currentCategory) count++;
            content.sizeDelta = new Vector2(0, Mathf.CeilToInt(count / 3f) * 229 + 16);
            if (scroll != null) { scroll.StopMovement(); content.anchoredPosition = Vector2.zero; }
        }
        public void PlayCategory()
        {
            if (animators == null) return;
            foreach (UIAnimator animator in animators) if (animator != null && animator.transform.parent.gameObject.activeInHierarchy) animator.Play();
        }
        public void ResetAll()
        {
            if (animators == null) return;
            foreach (UIAnimator animator in animators) if (animator != null) animator.ResetAnimation();
            if (sequenceAnimator != null) sequenceAnimator.ResetAnimation();
        }
        public void AnimationStarted() { if (status != null) status.text = "Event: started | Active: " + UIAnimationManager.ActiveCount; }
        public void AnimationCompleted() { if (status != null) status.text = "Event: completed " + ++completedCount + " | Active: " + UIAnimationManager.ActiveCount; }
        public void AnimationCanceled() { if (status != null) status.text = "Event: interrupted / canceled"; }
        public void AnimationLooped() { if (status != null) status.text = "Event: loop completed"; }

        private void EnsureInput()
        {
            if (input == null)
            {
                var go = new GameObject("DemoEventSystem"); go.transform.SetParent(transform, false); go.SetActive(false);
                input = go.AddComponent<EventSystem>();
                Type module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (module != null)
                {
                    Component component = go.AddComponent(module);
                    module.GetMethod("AssignDefaultActions")?.Invoke(component, null);
                }
                else go.AddComponent<StandaloneInputModule>();
            }
            if (EventSystem.current == null || EventSystem.current == input) input.gameObject.SetActive(true);
        }

        private static RectTransform Node(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(w,h);
            return rect;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private static Text Label(Transform parent, string name, string text, float x, float y, float w, float h, int size)
        {
            Text label = Node(parent,name,x,y,w,h).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=size; label.text=text;
            label.color = new Color32(224,232,244,255); label.alignment=TextAnchor.MiddleCenter; label.raycastTarget=false;
            return label;
        }
        private static Button MakeButton(Transform parent, string name, string text, float x, float y, float w, float h, UnityAction action)
        {
            RectTransform rect = Node(parent,name,x,y,w,h);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color32(61,77,103,255);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode=Navigation.Mode.None };
            Label(rect,"Label",text,0,0,w,h,15);
            if (action != null) BindEvent(button.onClick, action);
            return button;
        }
        private static void BindEvent(UnityEvent evt, UnityAction action)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { UnityEditor.Events.UnityEventTools.AddPersistentListener(evt, action); return; }
#endif
            evt.AddListener(action);
        }
        private static void BindInt(Button button, UnityAction<int> action, int value)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { UnityEditor.Events.UnityEventTools.AddIntPersistentListener(button.onClick, action, value); return; }
#endif
            button.onClick.AddListener(() => action(value));
        }
    }
}
