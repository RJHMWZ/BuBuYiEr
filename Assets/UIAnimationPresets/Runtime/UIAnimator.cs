using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UIAnimationPresets
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    [AddComponentMenu("UI Animation/UI Animator")]
    public sealed class UIAnimator : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform animationTarget;
        public UIAnimationSource source;
        public UIAnimationDefinition animation = new UIAnimationDefinition();
        public UIAnimationPresetAsset presetAsset;
        public List<UIAnimationStep> steps = new List<UIAnimationStep>();
        public int sequenceLoops = 1;
        public bool sequenceUnscaledTime = true;
        public bool playOnEnable;
        public bool activateOnPlay = true;
        public bool restoreWhenDisabled = true;
        public UIAnimationStopMode stopOnDisable = UIAnimationStopMode.RestoreState;
        public bool animateClick;
        public UIAnimationPreset clickPreset = UIAnimationPreset.ClickPunch;
        public bool animateHover;
        public UIAnimationPreset hoverPreset = UIAnimationPreset.Pulse;
        public UnityEvent onStarted = new UnityEvent();
        public UnityEvent onCompleted = new UnityEvent();
        public UnityEvent onCanceled = new UnityEvent();
        public UnityEvent onLoop = new UnityEvent();
        public event Action Started, Completed, Canceled;
        public event Action<int> Looped;
        public RectTransform Target => animationTarget != null ? animationTarget : (RectTransform)transform;
        public UIAnimationHandle Handle => handle;
        public bool IsPlaying => handle.IsPlaying;
        public bool IsPreviewing { get; internal set; }
        public UIAnimationDefinition Definition => source == UIAnimationSource.Asset ? presetAsset != null ? presetAsset.animation : null : animation;
        private UIAnimationHandle handle, hoverHandle;
        private UIAnimationBinding restBinding;
        private UIAnimationPose restPose;
        private bool hasRest, suppressEnable;
        private UIAnimationCallbacks callbacks;
        private bool callbacksReady;
        private int restGeneration = -1;
#if UNITY_EDITOR
        public static Action<UIAnimator> EditorPreviewRequested;
        public static Action<UIAnimator> EditorStopRequested;
#endif

        private void OnEnable()
        {
            if (Application.isPlaying && playOnEnable && !suppressEnable && !UIAnimationManager.IsActivatingTarget(Target)) UIAnimationManager.ScheduleAutoPlay(this);
        }
        private void OnDisable()
        {
            UIAnimationManager.CancelAutoPlay(this);
#if UNITY_EDITOR
            if (!Application.isPlaying) { EditorStopRequested?.Invoke(this); return; }
#endif
            if (!UIAnimationManager.IsCompletingTarget(Target))
            {
                handle.Stop(stopOnDisable);
                hoverHandle.Stop(stopOnDisable);
            }
            if (restoreWhenDisabled && hasRest && restBinding.target != null) restPose.Apply(restBinding, UIAnimationChannels.All);
        }
        private void OnDestroy()
        {
            handle.Stop(UIAnimationStopMode.KeepCurrentState);
            hoverHandle.Stop(UIAnimationStopMode.KeepCurrentState);
        }

        private void EnsureRest()
        {
            if (hasRest && restGeneration == UIAnimationManager.Generation && restBinding != null && restBinding.target == Target) return;
            restBinding = new UIAnimationBinding(Target, Application.isPlaying);
            restPose = UIAnimationPose.Capture(restBinding);
            hasRest = true;
            restGeneration = UIAnimationManager.Generation;
        }
        private void EnsureCallbacks()
        {
            if (callbacksReady) return;
            callbacksReady = true;
            callbacks = new UIAnimationCallbacks {
                onStart = () => { if (this == null) return; onStarted.Invoke(); Started?.Invoke(); },
                onComplete = () => { if (this == null) return; onCompleted.Invoke(); Completed?.Invoke(); },
                onCancel = () => { if (this == null) return; onCanceled.Invoke(); Canceled?.Invoke(); },
                onLoop = count => { if (this == null) return; onLoop.Invoke(); Looped?.Invoke(count); }
            };
        }
        internal void PlayAutomatically() { if (playOnEnable && !suppressEnable) Play(); }

        [ContextMenu("Play / Preview")]
        public void Play()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { EditorPreviewRequested?.Invoke(this); return; }
#endif
            UIAnimationManager.CancelAutoPlay(this);
            EnsureRest(); EnsureCallbacks();
            handle.Stop(stopOnDisable);
            if (activateOnPlay && !Target.gameObject.activeSelf)
            {
                suppressEnable = true;
                UIAnimationManager.ActivateTarget(Target);
                suppressEnable = false;
            }
            if (source == UIAnimationSource.Sequence)
            {
                UIAnimationSequence sequence = UIAnimationSequence.FromSteps(this)
                    .OnStart(callbacks.onStart).OnComplete(callbacks.onComplete).OnCancel(callbacks.onCancel).OnLoop(callbacks.onLoop);
                handle = sequence.PlayInternal(Target, restPose);
            }
            else handle = UIAnimationManager.PlayInternal(Target, Definition, callbacks, restPose, 0);
        }

        public UIAnimationHandle PlayPreset(UIAnimationPreset preset)
        {
            UIAnimationManager.CancelAutoPlay(this);
            EnsureRest(); EnsureCallbacks();
            handle.Stop(UIAnimationStopMode.RestoreState);
            if (activateOnPlay) UIAnimationManager.ActivateTarget(Target);
            handle = UIAnimationManager.PlayInternal(Target, UIPresetLibrary.Create(preset), callbacks, restPose, 0);
            return handle;
        }
        public void Pause() { handle.Pause(); hoverHandle.Pause(); }
        public void Resume() { handle.Resume(); hoverHandle.Resume(); }
        public void TogglePause() { if (handle.IsPaused) Resume(); else Pause(); }
        public void ResetAnimation()
        {
            Stop(); EnsureRest();
            suppressEnable = true;
            UIAnimationManager.ActivateTarget(Target);
            suppressEnable = false;
            restPose.Apply(restBinding, UIAnimationChannels.All);
        }
        public void Complete() => handle.Complete();
        public void Stop() => Stop(UIAnimationStopMode.RestoreState);
        public void Stop(UIAnimationStopMode mode)
        {
            UIAnimationManager.CancelAutoPlay(this);
#if UNITY_EDITOR
            if (!Application.isPlaying) { EditorStopRequested?.Invoke(this); return; }
#endif
            handle.Stop(mode);
            hoverHandle.Stop(mode);
        }
        [ContextMenu("Capture Rest Pose")]
        public void CaptureRestPose()
        {
            Stop(); hasRest = false; EnsureRest();
        }
        public void RestoreRestPose()
        {
            Stop();
            if (hasRest && restBinding.target != null) restPose.Apply(restBinding, UIAnimationChannels.All);
        }
        internal UIAnimationPose? CachedRest => hasRest && restBinding != null && restBinding.target == Target ? restPose : (UIAnimationPose?)null;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (TryGetComponent<UnityEngine.UI.Selectable>(out var selectable) && !selectable.IsInteractable()) return;
            if (animateClick && Application.isPlaying) PlayPreset(clickPreset);
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TryGetComponent<UnityEngine.UI.Selectable>(out var selectable) && !selectable.IsInteractable()) return;
            if (!animateHover || !Application.isPlaying) return;
            EnsureRest();
            var definition = UIPresetLibrary.Create(hoverPreset);
            definition.blockInput = false;
            hoverHandle = UIAnimationManager.PlayInternal(Target, definition, default, restPose, 0);
        }
        public void OnPointerExit(PointerEventData eventData) => hoverHandle.Stop(UIAnimationStopMode.RestoreState);
    }
}
