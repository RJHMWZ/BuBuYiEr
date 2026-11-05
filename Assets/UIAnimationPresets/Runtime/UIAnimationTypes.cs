using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UIAnimationPresets
{
    public enum UIAnimationCategory { Entrance, Exit, Click, Loop, Emphasis, Reward }
    public enum UIAnimationPreset
    {
        FadeIn, ScaleIn, PopIn, SlideInLeft, SlideInRight, SlideInUp, SlideInDown, RotateIn, FlipIn,
        FadeOut, ScaleOut, PopOut, SlideOutLeft, SlideOutRight, SlideOutUp, SlideOutDown, RotateOut,
        ClickScale, ClickPunch, ClickTilt, Pulse, Float, Spin, Breathe, Heartbeat,
        Shake, Punch, Wiggle, Bounce, Flash, RewardPop, CoinFly, StarBurst, Celebrate, Glow
    }
    public enum UIAnimationEase
    {
        Linear, InSine, OutSine, InOutSine, InQuad, OutQuad, InOutQuad, InCubic, OutCubic, InOutCubic,
        InQuart, OutQuart, InOutQuart, InExpo, OutExpo, InOutExpo, InBack, OutBack, InOutBack,
        OutBounce, InOutBounce, OutElastic, Custom
    }
    public enum UIAnimationProperty { Position, Scale, Rotation, Alpha, Color }
    public enum UIAnimationWave { Tween, Punch, Shake, Pulse, Bounce, Heartbeat }
    public enum UIAnimationValueMode { Relative, Absolute, Multiply }
    public enum UIAnimationLoopMode { Restart, PingPong }
    public enum UIAnimationCompletion { KeepFinalState, RestoreState, DisableGameObject }
    public enum UIAnimationStopMode { RestoreState, KeepCurrentState, Complete }
    public enum UIAnimationConflict { Replace, Ignore }
    public enum UIAnimationSource { Preset, Asset, Sequence }
    public enum UIAnimationStepTiming { Append, Join, Insert }

    [Flags]
    public enum UIAnimationChannels { None = 0, Position = 1, Scale = 2, Rotation = 4, Alpha = 8, Color = 16, All = 31 }

    [Serializable]
    public struct UIAnimationTrack
    {
        public bool enabled;
        public UIAnimationProperty property;
        public UIAnimationWave wave;
        public UIAnimationValueMode valueMode;
        public Vector3 from, to;
        public float fromValue, toValue;
        public Color fromColor, toColor;
        public bool fromCurrent;
        public bool additive;
        public bool useDistance;
        public bool useAngle;
        public bool colorFromRest;
        public bool overrideEase;
        public UIAnimationEase ease;
        public AnimationCurve curve;

        public static UIAnimationTrack Vector(UIAnimationProperty property, Vector3 from, Vector3 to,
            UIAnimationValueMode mode = UIAnimationValueMode.Relative, UIAnimationWave wave = UIAnimationWave.Tween)
        {
            return new UIAnimationTrack { enabled = true, property = property, from = from, to = to,
                valueMode = mode, wave = wave, fromColor = Color.white, toColor = Color.white };
        }

        public static UIAnimationTrack Alpha(float from, float to, UIAnimationWave wave = UIAnimationWave.Tween)
        {
            return new UIAnimationTrack { enabled = true, property = UIAnimationProperty.Alpha,
                valueMode = UIAnimationValueMode.Multiply, wave = wave, fromValue = from, toValue = to };
        }
    }

    [Serializable]
    public sealed class UIAnimationMarker
    {
        public string name = "Event";
        [Range(0, 1)] public float time = .5f;
        public UnityEvent onTrigger = new UnityEvent();
        [NonSerialized] private Action cached;
        internal Action Callback => cached ?? (cached = onTrigger.Invoke);
    }

    [Serializable]
    public sealed class UIAnimationDefinition
    {
        public UIAnimationCategory category = UIAnimationCategory.Entrance;
        public UIAnimationPreset preset = UIAnimationPreset.PopIn;
        [Min(0)] public float duration = .35f;
        [Min(0)] public float delay;
        public UIAnimationEase ease = UIAnimationEase.OutCubic;
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("1 = 播放一次，-1 = 无限循环。PingPong 每个方向算一次。")]
        public int loops = 1;
        [Min(0)] public float loopDelay;
        public UIAnimationLoopMode loopMode;
        public bool useUnscaledTime = true;
        [Min(0)] public float distance = 160;
        public float angle = 15;
        [Min(0)] public float strength = 1;
        [Min(1)] public int vibrato = 4;
        [Min(0)] public float damping = 5;
        public bool blockInput;
        public UIAnimationCompletion completion;
        public UIAnimationConflict conflict;
        public UIAnimationStopMode interruptedState = UIAnimationStopMode.RestoreState;
        public bool useCustomTracks;
        public List<UIAnimationTrack> tracks = new List<UIAnimationTrack>();
        public List<UIAnimationMarker> markers = new List<UIAnimationMarker>();

        public double TotalDuration
        {
            get
            {
                if (loops < 0) return double.PositiveInfinity;
                int count = Mathf.Max(1, loops);
                return Math.Max(0, delay) + Math.Max(0, duration) * count + Math.Max(0, loopDelay) * (count - 1);
            }
        }

        public UIAnimationDefinition Clone()
        {
            var copy = (UIAnimationDefinition)MemberwiseClone();
            copy.tracks = tracks != null ? new List<UIAnimationTrack>(tracks) : new List<UIAnimationTrack>();
            copy.markers = markers != null ? new List<UIAnimationMarker>(markers) : new List<UIAnimationMarker>();
            return copy;
        }
    }

    [Serializable]
    public sealed class UIAnimationStep
    {
        public string name = "Step";
        public RectTransform target;
        public UIAnimationPresetAsset asset;
        public UIAnimationDefinition animation = new UIAnimationDefinition();
        public UIAnimationStepTiming timing;
        [Min(0)] public float offset;
        public UnityEvent onComplete = new UnityEvent();
        internal UIAnimationDefinition Definition => asset != null ? asset.animation : animation;
    }

    public struct UIAnimationCallbacks
    {
        public Action onStart, onComplete, onCancel;
        public Action<int> onLoop;
    }

    public readonly struct UIAnimationHandle
    {
        internal readonly int id;
        internal UIAnimationHandle(int id) { this.id = id; }
        public bool IsValid => id != 0 && UIAnimationManager.Exists(id);
        public bool IsPlaying => id != 0 && UIAnimationManager.IsPlaying(id);
        public bool IsPaused => id != 0 && UIAnimationManager.IsPaused(id);
        public float Progress => id == 0 ? 0 : UIAnimationManager.Progress(id);
        public void Pause() => UIAnimationManager.Pause(id, true);
        public void Resume() => UIAnimationManager.Pause(id, false);
        public void Stop(UIAnimationStopMode mode = UIAnimationStopMode.RestoreState) => UIAnimationManager.Stop(id, mode);
        public void Complete() => Stop(UIAnimationStopMode.Complete);
    }
}
