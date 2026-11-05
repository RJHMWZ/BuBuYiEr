using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIAnimationPresets
{
    internal sealed class UIAnimationScheduledStep
    {
        internal RectTransform target;
        internal UIAnimationDefinition animation;
        internal Action callback;
        internal double start;
        internal int order;
    }

    public sealed class UIAnimationSequence
    {
        internal readonly List<UIAnimationScheduledStep> entries = new List<UIAnimationScheduledStep>();
        internal double duration;
        private double joinStart;
        private int loops = 1;
        private bool unscaled = true;
        private UIAnimationCallbacks callbacks;
        public static UIAnimationSequence Create() => new UIAnimationSequence();
        public UIAnimationSequence Append(RectTransform target, UIAnimationDefinition animation)
        { joinStart = duration; Add(joinStart, target, animation); return this; }
        public UIAnimationSequence Append(RectTransform target, UIAnimationPreset preset) => Append(target, UIPresetLibrary.Create(preset));
        public UIAnimationSequence Join(RectTransform target, UIAnimationDefinition animation) { Add(joinStart, target, animation); return this; }
        public UIAnimationSequence Join(RectTransform target, UIAnimationPreset preset) => Join(target, UIPresetLibrary.Create(preset));
        public UIAnimationSequence Insert(float time, RectTransform target, UIAnimationDefinition animation)
        { Add(Math.Max(0, time), target, animation); return this; }
        public UIAnimationSequence AppendInterval(float seconds) { duration += Math.Max(0, seconds); return this; }
        public UIAnimationSequence AppendCallback(Action callback)
        {
            entries.Add(new UIAnimationScheduledStep { start = duration, callback = callback, order = entries.Count });
            return this;
        }
        public UIAnimationSequence SetLoops(int count) { loops = count < 0 ? -1 : Math.Max(1, count); return this; }
        public UIAnimationSequence SetUnscaledTime(bool value) { unscaled = value; return this; }
        public UIAnimationSequence OnStart(Action callback) { callbacks.onStart += callback; return this; }
        public UIAnimationSequence OnComplete(Action callback) { callbacks.onComplete += callback; return this; }
        public UIAnimationSequence OnCancel(Action callback) { callbacks.onCancel += callback; return this; }
        public UIAnimationSequence OnLoop(Action<int> callback) { callbacks.onLoop += callback; return this; }

        internal void Add(double time, RectTransform target, UIAnimationDefinition animation, Action callback = null)
        {
            if (animation == null) return;
            UIAnimationDefinition copy = animation.Clone();
            double start = time + Math.Max(0, copy.delay);
            duration = Math.Max(duration, time + copy.TotalDuration);
            copy.delay = 0;
            entries.Add(new UIAnimationScheduledStep { target = target, animation = copy, start = start, callback = callback, order = entries.Count });
        }

        public UIAnimationHandle Play() => PlayInternal(null, null);
        internal UIAnimationHandle PlayInternal(RectTransform restTarget, UIAnimationPose? rest)
        {
            if (!Application.isPlaying) return default;
            var array = entries.ToArray();
            Array.Sort(array, (a, b) => a.start != b.start ? a.start.CompareTo(b.start) : a.order.CompareTo(b.order));
            return UIAnimationManager.StartSequence(new UIAnimationSequenceRunner(array, duration, loops, unscaled, callbacks, restTarget, rest));
        }

        internal static UIAnimationSequence FromSteps(UIAnimator owner)
        {
            var sequence = Create().SetLoops(owner.sequenceLoops).SetUnscaledTime(owner.sequenceUnscaledTime);
            double lastAppend = 0;
            foreach (UIAnimationStep step in owner.steps)
            {
                if (step == null) continue;
                double at = step.timing == UIAnimationStepTiming.Insert ? Math.Max(0, step.offset)
                    : step.timing == UIAnimationStepTiming.Join ? lastAppend + Math.Max(0, step.offset)
                    : sequence.duration + Math.Max(0, step.offset);
                if (step.timing == UIAnimationStepTiming.Append) lastAppend = at;
                sequence.Add(at, step.target != null ? step.target : owner.Target, step.Definition, step.onComplete.Invoke);
            }
            return sequence;
        }
    }

    internal sealed class UIAnimationSequenceRunner : UIAnimationTask
    {
        private sealed class Rest
        {
            internal UIAnimationBinding binding;
            internal UIAnimationPose pose;
            internal UIAnimationChannels channels;
        }
        private readonly UIAnimationScheduledStep[] entries;
        private readonly UIAnimationHandle[] children;
        private readonly Dictionary<int, Rest> states = new Dictionary<int, Rest>();
        private readonly double duration;
        private readonly int loops;
        private readonly bool unscaled;
        private readonly UIAnimationCallbacks callbacks;
        private double elapsed, origin;
        private int next, iteration;
        private bool started;
        internal override float Progress => double.IsInfinity(duration) ? 0 : duration <= 0 ? 1
            : Mathf.Clamp01((float)((elapsed - origin) / duration));

        internal UIAnimationSequenceRunner(UIAnimationScheduledStep[] entries, double duration, int loops, bool unscaled,
            UIAnimationCallbacks callbacks, RectTransform restTarget, UIAnimationPose? rest)
        {
            this.entries = entries; this.duration = duration; this.loops = loops;
            this.unscaled = unscaled; this.callbacks = callbacks;
            children = new UIAnimationHandle[entries.Length];
            foreach (UIAnimationScheduledStep entry in entries)
            {
                if (entry.target == null || entry.animation == null) continue;
                int key = entry.target.GetInstanceID();
                if (!states.TryGetValue(key, out Rest state))
                {
                    var binding = new UIAnimationBinding(entry.target);
                    state = new Rest { binding = binding, pose = entry.target == restTarget && rest.HasValue ? rest.Value : UIAnimationPose.Capture(binding) };
                    states.Add(key, state);
                }
                state.channels |= UIAnimationEvaluator.Channels(UIPresetLibrary.Tracks(entry.animation));
            }
        }

        internal override bool UsesTarget(RectTransform target)
        {
            for (int i = 0; i < entries.Length; i++) if (entries[i].target == target) return true;
            return false;
        }
        internal void PauseChildren(bool value)
        {
            for (int i = 0; i < children.Length; i++) { if (value) children[i].Pause(); else children[i].Resume(); }
        }
        internal void Tick(float scaled, float real)
        {
            if (paused || stopRequested) return;
            if (!started) { started = true; UIAnimationManager.Dispatch(callbacks.onStart); }
            elapsed += unscaled ? real : scaled;
            double local = elapsed - origin;
            while (next < entries.Length && entries[next].start <= local)
            {
                UIAnimationScheduledStep entry = entries[next];
                if (entry.animation == null) UIAnimationManager.Dispatch(entry.callback);
                else if (entry.target != null)
                {
                    if (entry.animation.category == UIAnimationCategory.Entrance) UIAnimationManager.ActivateTarget(entry.target);
                    entry.animation.useUnscaledTime = unscaled;
                    children[next] = UIAnimationManager.PlayInternal(entry.target, entry.animation,
                        new UIAnimationCallbacks { onComplete = entry.callback }, states[entry.target.GetInstanceID()].pose, local - entry.start);
                }
                next++;
            }
            if (double.IsInfinity(duration) || local < duration || next < entries.Length) return;
            for (int i = 0; i < children.Length; i++) if (children[i].IsValid) return;
            iteration++;
            UIAnimationManager.Dispatch(callbacks.onLoop, iteration);
            if (loops > 0 && iteration >= loops) { finished = true; return; }
            origin += Math.Max(.001, duration);
            next = 0;
            Array.Clear(children, 0, children.Length);
            foreach (Rest state in states.Values) if (state.binding.target != null) state.pose.Apply(state.binding, state.channels);
        }

        internal void Finish(bool completed)
        {
            for (int i = 0; i < children.Length; i++)
                if (children[i].IsValid) children[i].Stop(completed ? UIAnimationStopMode.Complete : stopMode);
            if (completed && !finished)
            {
                for (int i = next; i < entries.Length; i++)
                {
                    UIAnimationScheduledStep entry = entries[i];
                    if (entry.animation != null && entry.target != null)
                    {
                        Rest state = states[entry.target.GetInstanceID()];
                        UIAnimationTrack[] tracks = UIPresetLibrary.Tracks(entry.animation);
                        double end = double.IsInfinity(entry.animation.TotalDuration) ? Math.Max(.001, entry.animation.duration) : entry.animation.TotalDuration;
                        int iteration; bool done;
                        float t = double.IsInfinity(entry.animation.TotalDuration) ? 1 : UIAnimationEvaluator.NormalizedTime(entry.animation, end, out iteration, out done);
                        UIAnimationEvaluator.Sample(entry.animation, tracks, state.pose, UIAnimationPose.Capture(state.binding), t)
                            .Apply(state.binding, UIAnimationEvaluator.Channels(tracks));
                        if (entry.animation.completion == UIAnimationCompletion.RestoreState) state.pose.Apply(state.binding, state.channels);
                        if (entry.animation.completion == UIAnimationCompletion.DisableGameObject) UIAnimationManager.Deactivate(entry.target);
                    }
                    UIAnimationManager.Dispatch(entry.callback);
                }
            }
            if (!completed && stopMode == UIAnimationStopMode.RestoreState)
                foreach (Rest state in states.Values) if (state.binding.target != null) state.pose.Apply(state.binding, state.channels);
            UIAnimationManager.Dispatch(completed ? callbacks.onComplete : callbacks.onCancel);
        }
    }
}
